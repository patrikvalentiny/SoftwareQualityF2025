using System;
using System.Collections.Generic;
using System.Linq;
using HotelBooking.Core.Entities;
using HotelBooking.Infrastructure;

namespace HotelBooking.IntegrationTests;

public class TestDbInitializer : IDbInitializer
{
    // This method will create and seed the database.
    public void Initialize(HotelBookingContext context)
    {
        // Delete the database, if it already exists. I do this because an
        // existing database may not be compatible with the entity model,
        // if the entity model was changed since the database was created.
        context.Database.EnsureDeleted();

        // Create the database, if it does not already exists. This operation
        // is necessary, if you don't use the in-memory database.
        context.Database.EnsureCreated();

        // Look for any bookings.
        if (context.Booking.Any())
        {
            return;   // DB has been seeded
        }

        List<Customer> customers =
            [
                new Customer { Name="John Smith", Email="js@gmail.com" },
                new Customer { Name="Jane Doe", Email="jd@gmail.com" }
            ];

        List<Room> rooms =
            [
                new Room { Description="A" }
            ];

        DateTime date = DateTime.Today.AddDays(4);
        List<Booking> bookings =
            [
            new() { StartDate=DateTime.Parse("2026-10-03 12:00:00"), EndDate=DateTime.Parse("2026-10-04 12:00:00"), IsActive=true, CustomerId=2, RoomId=1 }
            ];

        context.Customer.AddRange(customers);
        context.Room.AddRange(rooms);
        context.SaveChanges();
        context.Booking.AddRange(bookings);
        context.SaveChanges();
    }
}
