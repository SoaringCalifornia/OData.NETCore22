using System;
using System.Linq;
using SimpleOData.Models;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SimpleOData
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOData();

            services.AddMvc(options =>
                {
                    options.EnableEndpointRouting = false;
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddDbContext<PeopleContext>(opt => opt.UseInMemoryDatabase(typeof(PeopleContext).Name));

            services.Configure<Settings>(Configuration.GetSection(typeof(Settings).Name));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider, IOptions<Settings> settings)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // AWS rounting will take care of HTTPS
            // app.UseHttpsRedirection();

            // Shows UseCors with CorsPolicyBuilder.
            app.UseCors(builder =>
            {
                builder
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin()
                    .AllowCredentials()
                    .Build();
            });

            var ctx = serviceProvider.GetService<PeopleContext>();

            var edmModel = DbContextHelper.BuildEdmModel(ctx);

            app.UseMvc(routeBuilder =>
                {
                    // Eg https://localhost/odata/person?$count=true&$top=10&$filter=contains(frist,'11')
                    routeBuilder.EnableDependencyInjection();
                    // Enable supported OData oprations and functions
                    routeBuilder.Select().Expand().Filter().OrderBy().MaxTop(settings.Value.odataMaxTop).Count();
                    // Define OData endpoint entry
                    routeBuilder.MapODataServiceRoute("odata", settings.Value.routePrefixOData, edmModel);
                });

            // https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-apache?view=aspnetcore-2.1&tabs=aspnetcore2x
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            if (!string.IsNullOrEmpty(settings.Value.urlMockPeople))
                // Save mock people data into default context
                DbContextHelper.PopulateDbAsync<Person>(ctx, settings.Value.urlMockPeople).Wait();
            else
            if (settings.Value.testSeedCount > 0)
                // Testing scenario
                DbContextHelper.PopulateTestDbSetAsync<Person>(ctx, settings.Value.testSeedCount).Wait();
            else
                throw new Exception("Unable to find correct configuration");
        }
    }
}
