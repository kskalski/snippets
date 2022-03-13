using Emissions.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Emissions.Controllers {
    [Route("api/[controller]")]
    [Authorize(Roles = Parameters.ADMIN_ROLE)]
    [ApiController]
    public class AdminReportController : ControllerAppCommon {
        public AdminReportController(ApplicationDbContext context, ILogger<AdminReportController> logger): base(context, logger) {
        }

        /**
         *  GET: api/AdminReport
         *
         * <summary>
         * Calculates added entries stats for the time window spanning configured number of days ending at 'until'
         * datetime (exclusive range) or current time (when not specified).
         * </summary>
         */
        [HttpGet]
        public async Task<ActionResult<AdminReport>> GetAdminReport(DateTime? until) {
            until ??= DateTime.UtcNow;
            log_.LogInformation("Calculating admin start for time interval until {0}", until);
            var emissions_stats = await Core.Queries.EmissionsPerUserStatsQuery(context_.CarbonEntries, until.Value).SingleOrDefaultAsync();
            var added_counts = await Core.Queries.AddedEntriesDayCountsQuery(context_.CarbonEntries, until.Value).ToArrayAsync();
            return new AdminReport() {
                AddedEntries = Core.Queries.CalculateAddedEntriesStats(added_counts),
                UsersEmissions = emissions_stats ?? new AdminReport.EmissionsByUsers()
            };
        }
    }
}
