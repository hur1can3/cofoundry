using Cofoundry.Core;
using Cofoundry.Domain.CQS;
using Cofoundry.Domain.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cofoundry.Domain
{
    public class SearchCustomEntitiesQueryHandler
        : IAsyncQueryHandler<SearchCustomEntitiesQuery, PagedQueryResult<CustomEntityRenderSummary>>
        , IPermissionRestrictedQueryHandler<SearchCustomEntitiesQuery, PagedQueryResult<CustomEntityRenderSummary>>

    {
        private readonly CofoundryDbContext _dbContext;
        private readonly ICustomEntityDefinitionRepository _customEntityDefinitionRepository;
        private readonly ICustomEntityRenderSummaryMapper _customEntityRenderSummaryMapper;
        private readonly ISearchSpecificationMapper<Expression> _searchSpecificationMapper;

        public SearchCustomEntitiesQueryHandler(
            CofoundryDbContext dbContext,
            ICustomEntityDefinitionRepository customEntityDefinitionRepository,
            ICustomEntityRenderSummaryMapper customEntityRenderSummaryMapper,
            ISearchSpecificationMapper<Expression> searchSpecificationMapper
            )
        {
            _dbContext = dbContext;
            _customEntityDefinitionRepository = customEntityDefinitionRepository;
            _customEntityRenderSummaryMapper = customEntityRenderSummaryMapper;
            _searchSpecificationMapper = searchSpecificationMapper;
        }

        public async Task<PagedQueryResult<CustomEntityRenderSummary>> ExecuteAsync(SearchCustomEntitiesQuery query, IExecutionContext executionContext)
        {
            PagedQueryResult<CustomEntityVersion> dbPagedResult = await GetQueryAsync(query, executionContext);

            ICollection<CustomEntityRenderSummary> results = await _customEntityRenderSummaryMapper.MapAsync(dbPagedResult.Items, executionContext);

            return dbPagedResult.ChangeType(results);
        }

        private async Task<PagedQueryResult<CustomEntityVersion>> GetQueryAsync(SearchCustomEntitiesQuery query, IExecutionContext executionContext)
        {
            ICustomEntityDefinition definition = _customEntityDefinitionRepository.GetByCode(query.CustomEntityDefinitionCode);
            EntityNotFoundException.ThrowIfNull(definition, query.CustomEntityDefinitionCode);

            IQueryable<CustomEntityPublishStatusQuery> dbQuery = _dbContext
                .CustomEntityPublishStatusQueries
                .AsNoTracking()
                .FilterByCustomEntityDefinitionCode(query.CustomEntityDefinitionCode)
                .FilterActive()
                .FilterByStatus(query.PublishStatus, executionContext.ExecutionDate)
                .Include(x => x.CustomEntity);

            // Filter by locale
            if (query.LocaleId > 0 && definition.HasLocale)
            {
                dbQuery = dbQuery.Where(p => p.CustomEntity.LocaleId == query.LocaleId);
            }
            else
            {
                dbQuery = dbQuery.Where(p => !p.CustomEntity.LocaleId.HasValue);
            }

            Expression baseExpression = _searchSpecificationMapper.Map(query.Specifications);
            if (baseExpression != null)
            {
                JsonValueModifier<CustomEntityPublishStatusQuery> jsonValueModifier = new JsonValueModifier<CustomEntityPublishStatusQuery>(c => c.CustomEntityVersion.SerializedData);
                Expression queryResult = jsonValueModifier.Visit(baseExpression);
                Expression<Func<CustomEntityPublishStatusQuery, bool>> translatedQuery =
                    (Expression<Func<CustomEntityPublishStatusQuery, bool>>)queryResult
                  ;

                dbQuery = dbQuery.Where(translatedQuery);
            }

            PagedQueryResult<CustomEntityVersion> dbPagedResult = await dbQuery
                .SortBy(definition, query.SortBy, query.SortDirection)
                .Include(e => e.CustomEntityVersion)
                .ThenInclude(e => e.CustomEntity)
                .Select(p => p.CustomEntityVersion)
                .ToPagedResultAsync(query);

            return dbPagedResult;
        }

        public IEnumerable<IPermissionApplication> GetPermissions(SearchCustomEntitiesQuery query)
        {
            ICustomEntityDefinition definition = _customEntityDefinitionRepository.GetByCode(query.CustomEntityDefinitionCode);
            EntityNotFoundException.ThrowIfNull(definition, query.CustomEntityDefinitionCode);

            yield return new CustomEntityReadPermission(definition);
        }

        private class JsonValueModifier<TEntity> : ExpressionVisitor
        {
            private readonly Dictionary<Type, string> typesToConvert = new Dictionary<Type, string>
                {
                    { typeof(int), nameof(Convert.ToInt32) },
                    { typeof(long), nameof(Convert.ToInt64) },
                    { typeof(decimal), nameof(Convert.ToDecimal) },
                    { typeof(double), nameof(Convert.ToDouble) },
                    { typeof(float), nameof(Convert.ToSingle) },
                    { typeof(bool), nameof(Convert.ToBoolean) }
                };

            private readonly ParameterExpression parameterExpression;
            private readonly MemberExpression memberExpression;

            public JsonValueModifier(Expression<Func<TEntity, string>> expr)
            {
                MemberExpression casted = (MemberExpression)expr.Body;
                if (!expr.Parameters.Any()) throw new ArgumentException();
                parameterExpression = expr.Parameters.First();
                memberExpression = casted;
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                return Expression.Lambda(Visit(node.Body), parameterExpression);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression != null && node.Expression.NodeType == ExpressionType.Constant)
                {
                    return node;
                }
                ConstantExpression constantExpression = Expression.Constant($"$.{Char.ToLower(node.Member.Name[0])}{node.Member.Name.Substring(1)}");
                if (node.Member is PropertyInfo)
                {
                    if (typesToConvert.TryGetValue(((PropertyInfo)node.Member).PropertyType, out string convertFunc))
                    {
                        MethodCallExpression call = Expression.Call(typeof(SqlServerJsonExtension)
                            .GetMethod(nameof(SqlServerJsonExtension.JsonValue)), memberExpression, constantExpression);
                        return Expression.Call(typeof(Convert).GetMethod(convertFunc, new Type[] { typeof(string) }), call);
                    }
                }

                return Expression.Call(typeof(SqlServerJsonExtension)
                    .GetMethod(nameof(SqlServerJsonExtension.JsonValue)), memberExpression, constantExpression);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return Expression.Parameter(typeof(TEntity), node.Name);
            }
        }
    }
}