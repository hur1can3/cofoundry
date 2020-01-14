﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cofoundry.Domain.Data;
using Cofoundry.Domain.CQS;
using Microsoft.EntityFrameworkCore;
using Cofoundry.Core;

namespace Cofoundry.Domain
{
    public class GetCustomEntityRenderSummariesByUrlSlugQueryHandler
        : IAsyncQueryHandler<GetCustomEntityRenderSummariesByUrlSlugQuery, ICollection<CustomEntityRenderSummary>>
        , IPermissionRestrictedQueryHandler<GetCustomEntityRenderSummariesByUrlSlugQuery, ICollection<CustomEntityRenderSummary>>
    {
        #region constructor

        private readonly CofoundryDbContext _dbContext;
        private readonly ICustomEntityRenderSummaryMapper _customEntityRenderSummaryMapper;
        private readonly ICustomEntityDefinitionRepository _customEntityDefinitionRepository;

        public GetCustomEntityRenderSummariesByUrlSlugQueryHandler(
            CofoundryDbContext dbContext,
            IQueryExecutor queryExecutor,
            ICustomEntityRenderSummaryMapper customEntityRenderSummaryMapper,
            ICustomEntityDefinitionRepository customEntityDefinitionRepository
            )
        {
            _dbContext = dbContext;
            _customEntityRenderSummaryMapper = customEntityRenderSummaryMapper;
            _customEntityDefinitionRepository = customEntityDefinitionRepository;
        }

        #endregion constructor

        public async Task<ICollection<CustomEntityRenderSummary>> ExecuteAsync(GetCustomEntityRenderSummariesByUrlSlugQuery query, IExecutionContext executionContext)
        {
            var dbResult = await _dbContext
                .CustomEntityPublishStatusQueries
                .AsNoTracking()
                .FilterActive()
                .FilterByCustomEntityDefinitionCode(query.CustomEntityDefinitionCode)
                .FilterByCustomEntityUrlSlug(query.UrlSlug)
                .FilterByStatus(query.PublishStatus, executionContext.ExecutionDate)
                .Include(e => e.CustomEntityVersion)
                .ThenInclude(e => e.CustomEntity)
                .Select(e => e.CustomEntityVersion)
                .ToListAsync();

            if (!dbResult.Any()) return Array.Empty<CustomEntityRenderSummary>();

            var result = await _customEntityRenderSummaryMapper.MapAsync(dbResult, executionContext);

            return result;
        }

        #region Permission

        public IEnumerable<IPermissionApplication> GetPermissions(GetCustomEntityRenderSummariesByUrlSlugQuery query)
        {
            var definition = _customEntityDefinitionRepository.GetByCode(query.CustomEntityDefinitionCode);
            EntityNotFoundException.ThrowIfNull(definition, query.CustomEntityDefinitionCode);

            yield return new CustomEntityReadPermission(definition);
        }

        #endregion Permission
    }
}