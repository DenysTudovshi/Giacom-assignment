using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Order.Data;
using Order.Service;
using OrderService.WebAPI.Mapping;
using OrderService.WebAPI.Middleware;
using OrderService.WebAPI.Models;
using OrderService.WebAPI.Validators;
using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace OrderService.WebAPI
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
            services.AddDbContext<OrderContext>(options =>
            {
                var serviceOptions = Configuration["OrderConnectionString"];
                options
                .UseLazyLoadingProxies()
                .UseMySQL(serviceOptions);
            });

            services.AddScoped<IOrderService, Order.Service.OrderService>();
            services.AddScoped<IOrderRepository, OrderRepository>();

            // Configure AutoMapper
            services.AddSingleton<IMapper>(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var configExpression = new MapperConfigurationExpression();
                configExpression.AddProfile<MappingProfile>();
                var mapperConfig = new MapperConfiguration(configExpression, loggerFactory);
                return mapperConfig.CreateMapper();
            });

            services.AddControllers();
            
            // Configure FluentValidation with automatic validation
            services.AddFluentValidationAutoValidation()
                    .AddFluentValidationClientsideAdapters();
            
            // Register validators automatically from assembly
            services.AddValidatorsFromAssemblyContaining<GetOrdersByStatusRequestValidator>();

            // Configure Swagger/OpenAPI
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Order Service API",
                    Version = "v1",
                    Description = "A comprehensive API for managing orders, order status updates, order creation, and profit calculations.",
                    Contact = new OpenApiContact
                    {
                        Name = "Order Service Team",
                        Email = "orders@company.com"
                    }
                });

                // Include XML comments for better documentation
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }

                // Add security definition if needed in the future
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                
                // Enable Swagger in development
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service API v1");
                    c.RoutePrefix = "swagger"; // Access Swagger UI at /swagger
                    c.DisplayRequestDuration();
                    c.EnableDeepLinking();
                    c.EnableFilter();
                    c.ShowExtensions();
                });
            }
            else
            {
                // Use global exception handling middleware in non-development environments
                app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
