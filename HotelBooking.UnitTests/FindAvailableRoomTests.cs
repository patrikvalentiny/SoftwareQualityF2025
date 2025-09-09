using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBooking.Core.Entities;
using HotelBooking.Core.Interfaces;
using HotelBooking.Core.Services;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests;

public class FindAvailableRoomTests
{
    private readonly Mock<IRepository<Booking>> mockBookingRepository;
    private readonly Mock<IRepository<Room>> mockRoomRepository;
    private readonly IBookingManager bookingManager;

    public FindAvailableRoomTests()
    {
        mockBookingRepository = new Mock<IRepository<Booking>>();
        mockRoomRepository = new Mock<IRepository<Room>>();
        bookingManager = new BookingManager(mockBookingRepository.Object, mockRoomRepository.Object);
    }

    [Fact]
    public async Task FindAvailableRoom_StartDateNotInTheFuture_ThrowsArgumentException()
    {
        // Arrange
        DateTime date = DateTime.Today;

        // Act
        Task Result() => bookingManager.FindAvailableRoom(date, date);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(Result);
    }

    [Fact]
    public async Task FindAvailableRoom_StartDateInPast_ThrowsArgumentException()
    {
        // Arrange
        DateTime pastDate = DateTime.Today.AddDays(-5);
        DateTime endDate = DateTime.Today.AddDays(5);

        // Act
        Task Result() => bookingManager.FindAvailableRoom(pastDate, endDate);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(Result);
    }

    [Fact]
    public async Task FindAvailableRoom_StartDateAfterEndDate_ThrowsArgumentException()
    {
        // Arrange
        DateTime startDate = DateTime.Today.AddDays(10);
        DateTime endDate = DateTime.Today.AddDays(5);

        // Act
        Task Result() => bookingManager.FindAvailableRoom(startDate, endDate);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(Result);
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

        mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime startDate = DateTime.Today.AddDays(1);
        DateTime endDate = DateTime.Today.AddDays(3);

        // Act
        int roomId = await bookingManager.FindAvailableRoom(startDate, endDate);

        // Assert
        Assert.NotEqual(-1, roomId);
        Assert.True(roomId == 1 || roomId == 2); // Should return one of the available rooms
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

        mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime startDate = DateTime.Today.AddDays(2);
        DateTime endDate = DateTime.Today.AddDays(4);

        // Act
        int roomId = await bookingManager.FindAvailableRoom(startDate, endDate);

        // Assert
        Assert.Equal(-1, roomId);
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

        mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime startDate = DateTime.Today.AddDays(2);
        DateTime endDate = DateTime.Today.AddDays(4);

        // Act
        int roomId = await bookingManager.FindAvailableRoom(startDate, endDate);

        // Assert
        Assert.Equal(2, roomId); // Should return room 2 as it's available
    }

    [Fact]
    public async Task FindAvailableRoom_BookingEndDateBeforeRequestedStart_ReturnsRoom()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = 1, Description = "Room A" }
        };

        var bookings = new List<Booking>
        {
            // Booking ends before the requested period starts
            new() { Id = 1, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(3), IsActive = true, RoomId = 1 }
        };

        mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime startDate = DateTime.Today.AddDays(5);
        DateTime endDate = DateTime.Today.AddDays(7);

        // Act
        int roomId = await bookingManager.FindAvailableRoom(startDate, endDate);

        // Assert
        Assert.Equal(1, roomId);
    }

    [Fact]
    public async Task FindAvailableRoom_BookingStartDateAfterRequestedEnd_ReturnsRoom()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = 1, Description = "Room A" }
        };

        var bookings = new List<Booking>
        {
            // Booking starts after the requested period ends
            new() { Id = 1, StartDate = DateTime.Today.AddDays(10), EndDate = DateTime.Today.AddDays(12), IsActive = true, RoomId = 1 }
        };

        mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime startDate = DateTime.Today.AddDays(5);
        DateTime endDate = DateTime.Today.AddDays(7);

        // Act
        int roomId = await bookingManager.FindAvailableRoom(startDate, endDate);

        // Assert
        Assert.Equal(1, roomId);
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

        mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime startDate = DateTime.Today.AddDays(2);
        DateTime endDate = DateTime.Today.AddDays(4);

        // Act
        int roomId = await bookingManager.FindAvailableRoom(startDate, endDate);

        // Assert
        Assert.Equal(1, roomId);
    }

    [Fact]
    public async Task FindAvailableRoom_NoRoomsExist_ReturnsMinusOne()
    {
        // Arrange
        var rooms = new List<Room>(); // No rooms available
        var bookings = new List<Booking>();

        mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime startDate = DateTime.Today.AddDays(1);
        DateTime endDate = DateTime.Today.AddDays(3);

        // Act
        int roomId = await bookingManager.FindAvailableRoom(startDate, endDate);

        // Assert
        Assert.Equal(-1, roomId);
    }

    [Fact]
    public async Task FindAvailableRoom_SingleDayBooking_ReturnsAvailableRoom()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = 1, Description = "Room A" },
            new() { Id = 2, Description = "Room B" }
        };

        var bookings = new List<Booking>
        {
            // Room 1 is booked for a single day
            new() { Id = 1, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(1), IsActive = true, RoomId = 1 }
        };

        mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime singleDay = DateTime.Today.AddDays(1);

        // Act
        int roomId = await bookingManager.FindAvailableRoom(singleDay, singleDay);

        // Assert
        Assert.Equal(2, roomId); // Should return room 2 as room 1 is booked
    }

    [Fact]
    public async Task FindAvailableRoom_ReturnsFirstAvailableRoom_WhenMultipleRoomsAvailable()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = 1, Description = "Room A" },
            new() { Id = 2, Description = "Room B" },
            new() { Id = 3, Description = "Room C" }
        };

        var bookings = new List<Booking>(); // No bookings, all rooms available

        mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        DateTime startDate = DateTime.Today.AddDays(1);
        DateTime endDate = DateTime.Today.AddDays(3);

        // Act
        int roomId = await bookingManager.FindAvailableRoom(startDate, endDate);

        // Assert
        Assert.Equal(1, roomId); // Should return the first room since all are available
    }
}