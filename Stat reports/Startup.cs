using Core.Entities;
using Core.Interfaces;
using Core.Services;
using Infrastructure.Data;
using Infrastructure.Repository;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stat_reports.Filters;
using Stat_reportsnt.Filters;
using System;
using System.Linq;

namespace Stat_reports
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration) => Configuration = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IBranchService, BranchService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IReportTemplateService, ReportTemplateService>();
            services.AddScoped<IExcelSplitterService, ExcelSplitterService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ISummaryReportService, SummaryReportService>();
            services.AddScoped<IDeadlineService, DeadlineService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<IPostService, PostService>();

            services.AddHostedService<DeadlineNotificationHostedService>();

            services.AddSingleton<AdminAuthFilter>();
            services.AddSingleton<AuthorizeBranchAndUserAttribute>();

            services.AddHttpContextAccessor();
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Auth/BranchLogin";
                    options.AccessDeniedPath = "/Auth/AccessDenied";
                });

            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IPasswordHasher<Branch>, PasswordHasher<Branch>>();
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

            services.AddControllersWithViews();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ApplicationDbContext dbContext)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            if (dbContext.Database.GetPendingMigrations().Any())
            {
                dbContext.Database.Migrate();
            }

            if (!dbContext.SystemRoles.Any() || !dbContext.Users.Any() || !dbContext.Branches.Any())
            {
                DbSeeder.SeedAsync(app.ApplicationServices).GetAwaiter().GetResult();
            }

            // app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapAreaControllerRoute(
                    name: "admin",
                    areaName: "Admin",
                    pattern: "Admin/{controller=Admin}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=ReportMvc}/{action=WorkingReports}/{id?}");
            });
        }
    }
}
