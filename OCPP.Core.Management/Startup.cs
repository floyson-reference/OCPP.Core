/*
 * OCPP.Core - https://github.com/dallmann-consulting/OCPP.Core
 * Copyright (C) 2020-2021 dallmann consulting GmbH.
 * All Rights Reserved.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Dashboard.BasicAuthorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OCPP.Core.Database.Repository;
using OCPP.Core.Database.Repository.Impl;
using OCPP.Core.Management.BackgroundServices;
using OCPP.Core.Management.BackgroundServices.Impl;
using OCPP.Core.Management.BackgroundServices.Models;

namespace OCPP.Core.Management
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddAuthentication(
                CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
                    options =>
                    {
                        options.LoginPath = "/Account/Login";
                        options.LogoutPath = "/Account/Logout";
                    });

            services.AddLocalization(opts => { opts.ResourcesPath = "Resources"; });
            services.AddMvc()
                .AddViewLocalization(
                    LanguageViewLocationExpanderFormat.Suffix,
                    opts => { opts.ResourcesPath = "Resources"; })
                .AddDataAnnotationsLocalization();

            // authentication 
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });

            services.AddHttpClient();
            services.AddScoped<ITransactionsRepository, TransactionsRepository>();
            services.AddScoped<IEmailExcelExportService, EmailExcelExportService>();
            services.AddScoped<ICregPriceService, CregPriceService>();
            services.Configure<SmtpSettings>(Configuration.GetSection("SmtpSettings"));

            services.AddTransient(
                m => new UserManager(Configuration)
                );
            services.AddDistributedMemoryCache();

            services.AddHangfireServer(backgroundJosServerOptions =>
            {
                backgroundJosServerOptions.WorkerCount = 1;
            });

            services.AddHangfire(configuration =>
                configuration.UseSqlServerStorage(Configuration.GetConnectionString("HangfireDb"), new Hangfire.SqlServer.SqlServerStorageOptions
                {
                    UseRecommendedIsolationLevel = true,
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                    PrepareSchemaIfNecessary = true,
                    DashboardJobListLimit = 50000,
                    TransactionTimeout = TimeSpan.FromMinutes(1),
                    SchemaName = "Hangfire"
                }));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();

            var supportedCultures = new[] { "en", "de" };
            var localizationOptions = new RequestLocalizationOptions().SetDefaultCulture(supportedCultures[0])
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);
            app.UseRequestLocalization(localizationOptions);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}/{connectorId?}/");
            });

            app.UseHangfireDashboard(options: new DashboardOptions
            {
                Authorization =
                [
                    new Hangfire.Dashboard.BasicAuthorization.BasicAuthAuthorizationFilter(new Hangfire.Dashboard.BasicAuthorization.BasicAuthAuthorizationFilterOptions
                    {
                        RequireSsl = false,
                        SslRedirect = false,
                        LoginCaseSensitive = false,
                        Users = [
                            new BasicAuthAuthorizationUser
                            {
                                Login = "frederic",
                                PasswordClear = "H@ngf!re123"
                            },
                        ]
                    })
                ],
                IsReadOnlyFunc = (DashboardContext context) => false,
            });

            var exportLastQuarterTransactionsCron = Configuration.GetSection("CronSettings")["ExportLastQuarterTransactionsCron"];
            RecurringJob.AddOrUpdate<IEmailExcelExportService>("exportLastQuarterTransactions", x => x.ExportLastQuarterTransactionsAsync(), () => exportLastQuarterTransactionsCron, new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });
        }
    }
}
