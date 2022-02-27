using Emissions.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Emissions.Controllers {
    public class ControllerAppCommon : ControllerBase {
        public ControllerAppCommon(ApplicationDbContext context, ILogger logger) {
            context_ = context;
            log_ = logger;
        }
        protected string currentUserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        protected readonly ApplicationDbContext context_;
        protected readonly ILogger log_;
    }
}
