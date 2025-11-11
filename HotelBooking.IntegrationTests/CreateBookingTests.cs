using System;
using HotelBooking.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Net.Http.Json;
using HotelBooking.Core.Entities;
using System.Threading.Tasks;
using FluentAssertions;

namespace HotelBooking.IntegrationTests;

public class CreateBookingTests : IClassFixture<CustomWebAppFactory<Program>>, IDisposable
{
    private readonly CustomWebAppFactory<Program> _factory;

    public CreateBookingTests(CustomWebAppFactory<Program> factory)
    {
        _factory = factory;

        using var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        var db = scopedServices.GetRequiredService<HotelBookingContext>();

        var dbInitializer = new TestDbInitializer();
        dbInitializer.Initialize(db);
    }

    public void Dispose()
    {
        using var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        var db = scopedServices.GetRequiredService<HotelBookingContext>();
        db.Database.EnsureDeleted();
        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData("2026-10-01 12:00:00", "2026-10-02 12:00:00", 1, 1)]
    [InlineData("2026-10-05 12:00:00", "2026-10-06 12:00:00", 1, 1)]
    public async Task SuccessfullyCreateBooking(string startDate, string endDate, int roomId, int customerId)
    {
        var client = _factory.CreateClient();
        var booking = new Booking
        {
            StartDate = DateTime.Parse(startDate),
            EndDate = DateTime.Parse(endDate),
            RoomId = roomId,
            CustomerId = customerId
        };

        var response = await client.PostAsJsonAsync("/bookings", booking);

        response.EnsureSuccessStatusCode();
    }

    [Theory]
    [InlineData("2026-10-02 12:00:00", "2026-10-05 12:00:00", 1, 1)]
    public async Task FailToCreateBookingDueToOverlap(string startDate, string endDate, int roomId, int customerId)
    {
        var client = _factory.CreateClient();
        var booking = new Booking
        {
            StartDate = DateTime.Parse(startDate),
            EndDate = DateTime.Parse(endDate),
            RoomId = roomId,
            CustomerId = customerId
        };

        var response = await client.PostAsJsonAsync("/bookings", booking);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("2026-10-02 12:00:00", "2026-10-03 12:00:00", 1, 1)]
    [InlineData("2026-10-01 12:00:00", "2026-10-04 12:00:00", 1, 1)]
    public async Task FailToCreateBookingDueToEndDateOverlap(string startDate, string endDate, int roomId, int customerId)
    {
        var client = _factory.CreateClient();
        var booking = new Booking
        {
            StartDate = DateTime.Parse(startDate),
            EndDate = DateTime.Parse(endDate),
            RoomId = roomId,
            CustomerId = customerId
        };

        var response = await client.PostAsJsonAsync("/bookings", booking);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("2026-10-03 12:00:00", "2026-10-05 12:00:00", 1, 1)]
    [InlineData("2026-10-04 12:00:00", "2026-10-05 12:00:00", 1, 1)]
    public async Task FailToCreateBookingDueToStartDateOverlap(string startDate, string endDate, int roomId, int customerId)
    {
        var client = _factory.CreateClient();
        var booking = new Booking
        {
            StartDate = DateTime.Parse(startDate),
            EndDate = DateTime.Parse(endDate),
            RoomId = roomId,
            CustomerId = customerId
        };

        var response = await client.PostAsJsonAsync("/bookings", booking);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("2026-10-03 12:00:00", "2026-10-03 12:00:00", 1, 1)]
    [InlineData("2026-10-04 12:00:00", "2026-10-04 12:00:00", 1, 1)]
    [InlineData("2026-10-03 12:00:00", "2026-10-04 12:00:00", 1, 1)]
    public async Task FailToCreateBookingDueToStartAndEndDateOverlap(string startDate, string endDate, int roomId, int customerId)
    {
        var client = _factory.CreateClient();
        var booking = new Booking
        {
            StartDate = DateTime.Parse(startDate),
            EndDate = DateTime.Parse(endDate),
            RoomId = roomId,
            CustomerId = customerId
        };

        var response = await client.PostAsJsonAsync("/bookings", booking);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }
}
