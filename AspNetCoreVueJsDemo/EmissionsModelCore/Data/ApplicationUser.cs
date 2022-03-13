using Microsoft.AspNetCore.Identity;

namespace Emissions.Data {
    public class ApplicationUser: IdentityUser {
        public double DailyEmissionsWarningThreshold { get; set; }
        public decimal MontlyExpensesWarningThreshold { get; set; }
    }
}
