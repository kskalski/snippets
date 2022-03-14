using Emissions.Data;
using Emissions.Proto.Reports;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Emissions.Controllers {
    [Authorize(Roles = Parameters.ADMIN_ROLE)]
    public class AdminReportGrpcService : Proto.Services.AdminReports.AdminReportsBase {
        public AdminReportGrpcService(ApplicationDbContext context, ILogger<AdminReportGrpcService> logger) {
            context_ = context;
            log_ = logger;
        }

        /**
         *  GET: api/AdminReport
         *
         * <summary>
         * Calculates added entries stats for the time window spanning configured number of days ending at 'until'
         * datetime (exclusive range) or current time (when not specified).
         * </summary>
         */
        public async override Task<AdminReport> Report(AdminReportRequest spec, ServerCallContext ctx) {
            var until = spec.Until?.ToDateTime() ?? DateTime.UtcNow;
            log_.LogInformation("Calculating admin start for time interval until {0}", until);
            var emissions_stats = await Core.Queries.EmissionsPerUserStatsQuery(context_.CarbonEntries, until)
                .SingleOrDefaultAsync() ?? new AdminReport.Types.EmissionsByUsers();
            var added_counts = await Core.Queries.AddedEntriesDayCountsQuery(context_.CarbonEntries, until)
                .ToArrayAsync();
            return new AdminReport() {
                AddedEntries = Core.Queries.CalculateAddedEntriesStats(added_counts),
                UsersEmissions = emissions_stats
            };
        }

        readonly ApplicationDbContext context_;
        readonly ILogger log_;
    }
}
