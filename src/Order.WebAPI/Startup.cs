using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Order.Data;
using Order.Service;
using OrderService.WebAPI.Middleware;
using OrderService.WebAPI.Models;
using OrderService.WebAPI.Validators;

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

            services.AddControllers();
            
            // Configure FluentValidation
            services.AddScoped<IValidator<GetOrdersByStatusRequest>, GetOrdersByStatusRequestValidator>();
            services.AddScoped<IValidator<UpdateOrderStatusRequest>, UpdateOrderStatusRequestValidator>();
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
