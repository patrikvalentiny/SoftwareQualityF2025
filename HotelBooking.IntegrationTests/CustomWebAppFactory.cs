using System;
using System.Data.Common;
using System.Linq;
using HotelBooking.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
namespace HotelBooking.IntegrationTests;

public class CustomWebAppFactory<T> : WebApplicationFactory<T> where T : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                    typeof(DbContextOptions<HotelBookingContext>));
            
                services.Remove(dbContextDescriptor);
            

            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                    typeof(DbConnection));
           
                services.Remove(dbConnectionDescriptor);
            
            services.AddDbContext<HotelBookingContext>(options =>
            {
                options.UseInMemoryDatabase("HotelBookingTestDb");
            }); 
        });

        builder.UseEnvironment("Development");
    }
}
