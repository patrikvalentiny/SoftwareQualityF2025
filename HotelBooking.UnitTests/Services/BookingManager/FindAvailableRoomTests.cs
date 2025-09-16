using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using HotelBooking.Core.Entities;
using HotelBooking.Core.Interfaces;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests.Services.BookingManager;

public class FindAvailableRoomTests
{
    private readonly Mock<IRepository<Booking>> bookingRepository;
    private readonly Mock<IRepository<Room>> roomRepository;
    private readonly IBookingManager bookingManager;

    public FindAvailableRoomTests()
    {
        bookingRepository = new Mock<IRepository<Booking>>();
        roomRepository = new Mock<IRepository<Room>>();
        bookingManager = new Core.Services.BookingManager(bookingRepository.Object, roomRepository.Object);
    }

    [Fact]
    public async Task FindAvailableRoom_StartDateNotInTheFuture_ThrowsArgumentException()
    {
        // Arrange
        DateTime date = DateTime.Today;

        // Act
        var act = () => bookingManager.FindAvailableRoom(date, date);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }


    [Fact]
    public async Task FindAvailableRoom_StartDateAfterEndDate_ThrowsArgumentException()
    {
        // Arrange
        DateTime startDate = DateTime.Today.AddDays(10);
        DateTime endDate = DateTime.Today.AddDays(5);

        // Act
        var act = () => bookingManager.FindAvailableRoom(startDate, endDate);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task FindAvailableRoom_RoomAvailable_ReturnsRoomId()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = 1, Description = "Room A" },
            new() { Id = 2, Description = "Room B" }
        };

        var bookings = new List<Booking>(); // No existing bookings

        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime startDate = DateTime.Today.AddDays(1);
        DateTime endDate = DateTime.Today.AddDays(3);

        // Act
        int roomId = await bookingManager.FindAvailableRoom(startDate, endDate);

        // Assert
        roomId.Should().NotBe(-1);
        (roomId == 1 || roomId == 2).Should().BeTrue();
    }

    [Fact]
    public async Task FindAvailableRoom_NoRoomsAvailable_ReturnsMinusOne()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = 1, Description = "Room A" },
            new() { Id = 2, Description = "Room B" }
        };

        var bookings = new List<Booking>
        {
            // Both rooms are booked for the requested period
            new() { Id = 1, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(5), IsActive = true, RoomId = 1 },
            new() { Id = 2, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(5), IsActive = true, RoomId = 2 }
        };

        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime startDate = DateTime.Today.AddDays(2);
        DateTime endDate = DateTime.Today.AddDays(4);

        // Act
        int roomId = await bookingManager.FindAvailableRoom(startDate, endDate);

        // Assert
        roomId.Should().Be(-1);
    }

    [Fact]
    public async Task FindAvailableRoom_PartialOverlap_ReturnsAvailableRoom()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = 1, Description = "Room A" },
            new() { Id = 2, Description = "Room B" }
        };

        var bookings = new List<Booking>
        {
            // Room 1 is booked from day 1 to day 5, but room 2 is free
            new() { Id = 1, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(5), IsActive = true, RoomId = 1 }
        };

        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime startDate = DateTime.Today.AddDays(2);
        DateTime endDate = DateTime.Today.AddDays(4);

        // Act
        int roomId = await bookingManager.FindAvailableRoom(startDate, endDate);

        // Assert
        roomId.Should().Be(2);
    }

    [Fact]
    public async Task FindAvailableRoom_InactiveBookingsIgnored_ReturnsRoom()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = 1, Description = "Room A" }
        };

        var bookings = new List<Booking>
        {
            // Inactive booking should be ignored
            new() { Id = 1, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(5), IsActive = false, RoomId = 1 }
        };

        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime startDate = DateTime.Today.AddDays(2);
        DateTime endDate = DateTime.Today.AddDays(4);

        // Act
        int roomId = await bookingManager.FindAvailableRoom(startDate, endDate);

        // Assert
        roomId.Should().Be(1);
    }

    [Fact]
    public async Task FindAvailableRoom_NoRoomsExist_ReturnsMinusOne()
    {
        // Arrange
        var rooms = new List<Room>(); // No rooms available
        var bookings = new List<Booking>();

        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime startDate = DateTime.Today.AddDays(1);
        DateTime endDate = DateTime.Today.AddDays(3);

        // Act
        int roomId = await bookingManager.FindAvailableRoom(startDate, endDate);

        // Assert
        roomId.Should().Be(-1);
    }
}