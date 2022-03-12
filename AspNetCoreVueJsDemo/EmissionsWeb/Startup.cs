using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using VueCliMiddleware;

namespace Emissions {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddSingleton<Controllers.NotificationQueue>();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                  .AddJwtBearer(options => {
                      options.TokenValidationParameters = new TokenValidationParameters {
                          ValidateIssuerSigningKey = true,
                          ValidIssuer = DevBootstrap.FAKE_ISSUER,
                          ValidAudience = DevBootstrap.FAKE_ISSUER,
                          IssuerSigningKey = DevBootstrap.FAKE_SECRET_KEY
                      };
                  });
            services.AddGrpc();
            services.AddControllers().AddJsonOptions(options => {
                options.JsonSerializerOptions.DictionaryKeyPolicy = null;
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.Converters.Add(new Utils.ExplicitUtcFormatDateTimeConverter());
            });
            services.AddSpaStaticFiles(configuration => {
                configuration.RootPath = "ClientApp/dist";
            });
            services.AddDbContext<Data.ApplicationDbContext>(options =>
                           options.UseSqlite(Configuration.GetConnectionString("DefaultConnection")));
            services.AddIdentityCore<Data.ApplicationUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<Data.ApplicationDbContext>();
        }
                  

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider service_provider, Data.ApplicationDbContext db_ctx) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            db_ctx.Database.Migrate();
            DevBootstrap.EnsureExampleUsersExist(service_provider).Wait();

            app.UseRouting();
            app.UseSpaStaticFiles();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseGrpcWeb();

            app.UseEndpoints(endpoints => {
                endpoints.MapGrpcService<Controllers.WebNotifierGrpcService>().EnableGrpcWeb();
                endpoints.MapControllers();
            });

            app.UseSpa(spa => {
                if (env.IsDevelopment()) {
                    spa.Options.SourcePath = "ClientApp/";
                    spa.UseVueCli(npmScript: "serve");
                }
            });
        }
    }
}
