using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TokenCastWebApp.Managers.Interfaces;
using TokenCastWebApp.Managers;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using TokenCastWebApp.Middlewares;
using System.Text.Json;

namespace TokenCast
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
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins("https://tokencast.net",
                                            "http://localhost",
                                            "http://localhost:4200",
                                            "https://canvia.art",
                                            "https://my.canvia.art",
                                            "https://canvia.netlify.app");
                    });
            });

            services.AddTransient(options =>
            {
                var serializeOptions = new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };
                serializeOptions.Converters.Add(SerializationOptionsFactory.GetNumberConverters().First());
                return serializeOptions;
            });

            services.AddSingleton<IDatabase, Database>();
            services.AddSingleton<IJsonSerializer, DefaultJsonSerializer>();
            services.AddTransient<ISystemTextJsonSerializer, SystemTextJsonSerializer>();

            services.AddSingleton<IWebSocketConnectionManager, WebSocketConnectionManager>();

            services.AddControllersWithViews();
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
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseCors();

            app.UseAuthorization();

            //app.UseMiddleware<WebSocketMiddleware>();
            app.UseWebSockets();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Account}/{action=Index}/{id?}");
            });
        }
    }
}
