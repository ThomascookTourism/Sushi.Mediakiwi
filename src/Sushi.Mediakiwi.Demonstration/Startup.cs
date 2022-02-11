using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sushi.Mediakiwi.Data;
using Sushi.Mediakiwi.Data.Elastic;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Sushi.Mediakiwi.API.Extensions;
using System.Reflection;
using System.IO;
using Sushi.Mediakiwi.Framework.Interfaces;
using Sushi.Mediakiwi.ListModules.GoogleSheets;

namespace Sushi.Mediakiwi.Demonstration
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            services.AddControllersWithViews(options =>
            {
            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.IgnoreNullValues = true;
                options.JsonSerializerOptions.IgnoreReadOnlyProperties = true;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

            services.AddMediakiwi();
            services.AddMediakiwiApi();
            //services.AddMediakiwiGlobalListSetting<string>("googleSheetsUrl", "Google sheets URL", "The URL of the Google sheets doc representing this list");
            
            var elasticSettings = new Nest.ConnectionSettings(new Uri(Configuration["ElasticUrl"]))
                .BasicAuthentication(Configuration["ElasticUsername"], Configuration["ElasticPassword"])
                .ThrowExceptions(true);

            var elasticClient = new Nest.ElasticClient(elasticSettings);

            services.AddElasticNotifications(elasticClient);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
             
            }
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // swagger
            app.UseSwagger();
            app.UseSwaggerUI();

            string[] excludePaths = new string[] { "/api/custom", "/myfiles", "/mkapi" };
            
            app.UseMediakiwi(excludePaths);
            app.UseMediakiwiApi();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
