using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Emissions {

    class DevBootstrap {
        public static async Task EnsureExampleUsersExist(IServiceProvider service_provider) {
            //initializing custom roles
            var role_manager = service_provider.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var role_name in Data.Parameters.ROLE_NAMES) {
                if (!await role_manager.RoleExistsAsync(role_name))
                    await role_manager.CreateAsync(new IdentityRole(role_name));
            }

            var user_manager = service_provider.GetRequiredService<UserManager<Data.ApplicationUser>>();
            var users = new List<Tuple<Data.ApplicationUser, string>>() { 
                Tuple.Create(new Data.ApplicationUser { Id = "123-admin-1", UserName = "admin" }, Data.Parameters.ADMIN_ROLE) };
            for (int i = 1; i <= 3; ++i) {
                users.Add(Tuple.Create(new Data.ApplicationUser {
                    Id = $"123-user-{i}",
                    UserName = $"user{i}",
                    DailyEmissionsWarningThreshold = Data.Parameters.DEFAULT_DAILY_EMISSIONS_WARNING_THRESHOLD,
                    MontlyExpensesWarningThreshold = Data.Parameters.DEFAULT_MONTHLY_EXPENSES_WARNING_THRESHOLD
                }, Data.Parameters.USER_ROLE));
            }

            var log = service_provider.GetService<ILoggerFactory>().CreateLogger("dev_secrets");
            foreach (var (user, role) in users) {
                log.LogWarning("Token for {0} is {1}", user.UserName, get_hardcoded_token(user, role));
                if (await user_manager.FindByNameAsync(user.UserName) != null)
                    continue;
                var user_creation = await user_manager.CreateAsync(user);
                if (user_creation.Succeeded) {
                    await user_manager.AddToRoleAsync(user, role);
                }
            }
        }

        static string get_hardcoded_token(Data.ApplicationUser user, string role) {
            var credentials = new SigningCredentials(FAKE_SECRET_KEY, SecurityAlgorithms.HmacSha256);

            //Create a List of Claims, Keep claims name short    
            var permClaims = new List<Claim>();
            permClaims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            permClaims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            permClaims.Add(new Claim(ClaimTypes.Name, user.UserName));
            permClaims.Add(new Claim(ClaimTypes.Role, role));

            //Create Security Token object by giving required parameters    
            var token = new JwtSecurityToken(
                FAKE_ISSUER, //Issure    
                FAKE_ISSUER,  //Audience    
                permClaims,
                expires: DateTime.Now.AddMonths(12),
                signingCredentials: credentials);
            var jwt_token = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt_token;
        }

        internal static readonly SymmetricSecurityKey FAKE_SECRET_KEY = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("my_secret_key_12345"));    
        internal const string FAKE_ISSUER = "http://localhost";
    }
}