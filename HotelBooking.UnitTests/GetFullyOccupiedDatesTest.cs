using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBooking.Core.Entities;
using HotelBooking.Core.Interfaces;
using HotelBooking.Core.Services;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests;

public class GetFullyOccupiedDatesTest
{
    private readonly Mock<IRepository<Booking>> bookingRepository;
    private readonly Mock<IRepository<Room>> roomRepository;
    private readonly IBookingManager bookingManager;

    public GetFullyOccupiedDatesTest()
    {
        bookingRepository = new Mock<IRepository<Booking>>();
        roomRepository = new Mock<IRepository<Room>>();
        bookingManager = new BookingManager(bookingRepository.Object, roomRepository.Object);
    }

    [Fact]
    public async Task GetFullyOccupiedDates_StartDateAfterEndDate_ThrowsArgumentException()
    {
        // Arrange
        DateTime startDate = DateTime.Today.AddDays(15);
        DateTime endDate = DateTime.Today.AddDays(10);

        // Act
        Task result() => bookingManager.GetFullyOccupiedDates(startDate, endDate);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(result);
    }

    [Fact]
    public async Task GetFullyOccupiedDates_DateRangeWithFullyOccupiedDates_ReturnsCorrectDates()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = 1, Description = "Room A" },
            new() { Id = 2, Description = "Room B" }
        };

        var bookings = new List<Booking>
        {
            new() { Id = 1, StartDate = DateTime.Today.AddDays(10), EndDate = DateTime.Today.AddDays(20), IsActive = true, RoomId = 1 },
            new() { Id = 2, StartDate = DateTime.Today.AddDays(10), EndDate = DateTime.Today.AddDays(20), IsActive = true, RoomId = 2 }
        };

        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime startDate = DateTime.Today.AddDays(10);
        DateTime endDate = DateTime.Today.AddDays(20);

        // Act
        var fullyOccupiedDates = await bookingManager.GetFullyOccupiedDates(startDate, endDate);

        // Assert
        Assert.Equal(11, fullyOccupiedDates.Count); // 11 days inclusive (10th to 20th)
        Assert.Contains(DateTime.Today.AddDays(10), fullyOccupiedDates);
        Assert.Contains(DateTime.Today.AddDays(15), fullyOccupiedDates);
        Assert.Contains(DateTime.Today.AddDays(20), fullyOccupiedDates);
    }

    [Fact]
    public async Task GetFullyOccupiedDates_DateRangeWithPartiallyOccupiedDates_ReturnsEmptyList()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = 1, Description = "Room A" },
            new() { Id = 2, Description = "Room B" }
        };

        var bookings = new List<Booking>
        {
            // Only room 1 is booked, room 2 is available
            new() { Id = 1, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(1), IsActive = true, RoomId = 1 }
        };

        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime startDate = DateTime.Today.AddDays(1);
        DateTime endDate = DateTime.Today.AddDays(1);

        // Act
        var fullyOccupiedDates = await bookingManager.GetFullyOccupiedDates(startDate, endDate);

        // Assert
        Assert.Empty(fullyOccupiedDates);
    }

    [Fact]
    public async Task GetFullyOccupiedDates_DateRangeWithNoBookings_ReturnsEmptyList()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = 1, Description = "Room A" },
            new() { Id = 2, Description = "Room B" }
        };

        var bookings = new List<Booking>(); // No bookings

        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime startDate = DateTime.Today.AddDays(25);
        DateTime endDate = DateTime.Today.AddDays(30);

        // Act
        var fullyOccupiedDates = await bookingManager.GetFullyOccupiedDates(startDate, endDate);

        // Assert
        Assert.Empty(fullyOccupiedDates);
    }

    [Fact]
    public async Task GetFullyOccupiedDates_DateRangeMixedWithFullyAndPartiallyOccupied_ReturnsOnlyFullyOccupiedDates()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = 1, Description = "Room A" },
            new() { Id = 2, Description = "Room B" }
        };

        var bookings = new List<Booking>
        {
            // Day 1: Only room 1 booked (partially occupied)
            new() { Id = 1, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(1), IsActive = true, RoomId = 1 },
            // Days 10-15: Both rooms booked (fully occupied)
            new() { Id = 2, StartDate = DateTime.Today.AddDays(10), EndDate = DateTime.Today.AddDays(15), IsActive = true, RoomId = 1 },
            new() { Id = 3, StartDate = DateTime.Today.AddDays(10), EndDate = DateTime.Today.AddDays(15), IsActive = true, RoomId = 2 }
        };

        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime startDate = DateTime.Today.AddDays(1);
        DateTime endDate = DateTime.Today.AddDays(15);

        // Act
        var fullyOccupiedDates = await bookingManager.GetFullyOccupiedDates(startDate, endDate);

        // Assert
        // Should return 6 dates (Day 10 through Day 15)
        Assert.Equal(6, fullyOccupiedDates.Count);
        Assert.DoesNotContain(DateTime.Today.AddDays(1), fullyOccupiedDates); // Only room 1 booked
        Assert.Contains(DateTime.Today.AddDays(10), fullyOccupiedDates); // Both rooms booked
        Assert.Contains(DateTime.Today.AddDays(15), fullyOccupiedDates); // Both rooms booked
    }

    [Fact]
    public async Task GetFullyOccupiedDates_SingleDayQuery_ReturnsCorrectResult()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = 1, Description = "Room A" },
            new() { Id = 2, Description = "Room B" }
        };

        var bookings = new List<Booking>
        {
            new() { Id = 1, StartDate = DateTime.Today.AddDays(15), EndDate = DateTime.Today.AddDays(15), IsActive = true, RoomId = 1 },
            new() { Id = 2, StartDate = DateTime.Today.AddDays(15), EndDate = DateTime.Today.AddDays(15), IsActive = true, RoomId = 2 }
        };

        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime singleDay = DateTime.Today.AddDays(15);

        // Act
        var fullyOccupiedDates = await bookingManager.GetFullyOccupiedDates(singleDay, singleDay);

        // Assert
        Assert.Single(fullyOccupiedDates);
        Assert.Contains(singleDay, fullyOccupiedDates);
    }

    [Fact]
    public async Task GetFullyOccupiedDates_WithInactiveBookings_IgnoresInactiveBookings()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = 1, Description = "Room A" },
            new() { Id = 2, Description = "Room B" }
        };

        var bookings = new List<Booking>
        {
            // Active booking for room 1
            new() { Id = 1, StartDate = DateTime.Today.AddDays(10), EndDate = DateTime.Today.AddDays(10), IsActive = true, RoomId = 1 },
            // Inactive booking for room 2 - should be ignored
            new() { Id = 2, StartDate = DateTime.Today.AddDays(10), EndDate = DateTime.Today.AddDays(10), IsActive = false, RoomId = 2 }
        };

        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime startDate = DateTime.Today.AddDays(10);
        DateTime endDate = DateTime.Today.AddDays(10);

        // Act
        var fullyOccupiedDates = await bookingManager.GetFullyOccupiedDates(startDate, endDate);

        // Assert
        Assert.Empty(fullyOccupiedDates);
    }
}