﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cofoundry.Domain.Data;
using Cofoundry.Domain.CQS;
using Microsoft.EntityFrameworkCore;

namespace Cofoundry.Domain
{
    /// <summary>
    /// Gets a page PageRenderSummary projection by id, which is
    /// a lighter weight projection designed for rendering to a site when the
    /// templates, region and block data is not required. The result is
    /// version-sensitive and defaults to returning published versions only, but
    /// this behavior can be controlled by the publishStatus query property.
    /// </summary>
    public class GetPageRenderSummaryByIdQueryHandler
        : IAsyncQueryHandler<GetPageRenderSummaryByIdQuery, PageRenderSummary>
        , IPermissionRestrictedQueryHandler<GetPageRenderSummaryByIdQuery, PageRenderSummary>
    {
        #region constructor

        private readonly CofoundryDbContext _dbContext;
        private readonly IQueryExecutor _queryExecutor;
        private readonly IPageRenderSummaryMapper _pageRenderSummaryMapper;

        public GetPageRenderSummaryByIdQueryHandler(
            CofoundryDbContext dbContext,
            IQueryExecutor queryExecutor,
            IPageRenderSummaryMapper pageRenderSummaryMapper
            )
        {
            _dbContext = dbContext;
            _queryExecutor = queryExecutor;
            _pageRenderSummaryMapper = pageRenderSummaryMapper;
        }

        #endregion constructor

        #region execution

        public async Task<PageRenderSummary> ExecuteAsync(GetPageRenderSummaryByIdQuery query, IExecutionContext executionContext)
        {
            var pageRouteQuery = new GetPageRouteByIdQuery(query.PageId);
            var pageRoute = await _queryExecutor.ExecuteAsync(pageRouteQuery, executionContext);
            if (pageRoute == null) return null;

            var dbPage = await QueryPageAsync(query, executionContext);
            if (dbPage == null) return null;

            var page = _pageRenderSummaryMapper.Map<PageRenderSummary>(dbPage, pageRoute);

            return page;
        }

        private async Task<PageVersion> QueryPageAsync(GetPageRenderSummaryByIdQuery query, IExecutionContext executionContext)
        {
            PageVersion result;

            if (query.PublishStatus == PublishStatusQuery.SpecificVersion)
            {
                if (!query.PageVersionId.HasValue)
                {
                    throw new Exception("A PageVersionId must be included in the query to use PublishStatusQuery.SpecificVersion");
                }

                result = await _dbContext
                    .PageVersions
                    .AsNoTracking()
                    .Include(v => v.OpenGraphImageAsset)
                    .FilterActive()
                    .FilterByPageId(query.PageId)
                    .FilterByPageVersionId(query.PageVersionId.Value)
                    .FirstOrDefaultAsync();
            }
            else
            {
                result = await _dbContext
                    .PagePublishStatusQueries
                    .AsNoTracking()
                    .FilterActive()
                    .FilterByStatus(query.PublishStatus, executionContext.ExecutionDate)
                    .FilterByPageId(query.PageId)
                    .Include(v => v.PageVersion.OpenGraphImageAsset)
                    .Select(p => p.PageVersion)
                    .FirstOrDefaultAsync();
            }

            return result;
        }

        #endregion execution

        #region Permission

        public IEnumerable<IPermissionApplication> GetPermissions(GetPageRenderSummaryByIdQuery query)
        {
            yield return new PageReadPermission();
        }

        #endregion Permission
    }
}