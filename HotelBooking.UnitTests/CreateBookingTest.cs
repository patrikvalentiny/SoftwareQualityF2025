using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBooking.Core.Entities;
using HotelBooking.Core.Interfaces;
using HotelBooking.Core.Services;
using Moq;
using Xunit;
using FluentAssertions;

namespace HotelBooking.UnitTests;

public class CreateBookingTest
{
    private readonly Mock<IRepository<Booking>> bookingRepository;
    private readonly Mock<IRepository<Room>> roomRepository;
    private readonly IBookingManager bookingManager;

    public CreateBookingTest()
    {
        bookingRepository = new Mock<IRepository<Booking>>();
        roomRepository = new Mock<IRepository<Room>>();
        bookingManager = new BookingManager(bookingRepository.Object, roomRepository.Object);
    }

    [Fact]
    public async Task CreateBooking_RoomAvailable_ReturnsTrueAndAddsBooking()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = 1, Description = "Room A" },
            new() { Id = 2, Description = "Room B" }
        };

        var bookings = new List<Booking>(); // No existing bookings, rooms are available

        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        var booking = new Booking
        {
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(1),
            CustomerId = 1
        };

        // Act
        var result = await bookingManager.CreateBooking(booking);

        // Assert
        result.Should().BeTrue();
        booking.IsActive.Should().BeTrue();
        booking.RoomId.Should().NotBe(0);
        bookingRepository.Verify(b => b.AddAsync(booking), Times.Once);
    }

    [Fact]
    public async Task CreateBooking_NoRoomAvailable_ReturnsFalseAndDoesNotAddBooking()
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
            new() { Id = 1, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(3), IsActive = true, RoomId = 1 },
            new() { Id = 2, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(3), IsActive = true, RoomId = 2 }
        };

        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        var booking = new Booking
        {
            StartDate = DateTime.Today.AddDays(2),
            EndDate = DateTime.Today.AddDays(2),
            CustomerId = 1
        };

        // Act
        var result = await bookingManager.CreateBooking(booking);

        // Assert
        result.Should().BeFalse();
        bookingRepository.Verify(b => b.AddAsync(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task CreateBooking_InvalidDates_ThrowsArgumentException()
    {
        // Arrange
        var booking = new Booking
        {
            StartDate = DateTime.Today, // Today is not allowed
            EndDate = DateTime.Today,
            CustomerId = 1
        };

        // Act
        var act = () => bookingManager.CreateBooking(booking);

        // Act & Assert
        await act.Should().ThrowAsync<ArgumentException>();

        // Verify that no repository methods were called due to early validation
        bookingRepository.Verify(b => b.GetAllAsync(), Times.Never);
        roomRepository.Verify(r => r.GetAllAsync(), Times.Never);
        bookingRepository.Verify(b => b.AddAsync(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task CreateBooking_StartDateInPast_ThrowsArgumentException()
    {
        // Arrange
        var booking = new Booking
        {
            StartDate = DateTime.Today.AddDays(-1), // Past date
            EndDate = DateTime.Today.AddDays(1),
            CustomerId = 1
        };

        // Act
        var act = () => bookingManager.CreateBooking(booking);
        // Act & Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateBooking_StartDateAfterEndDate_ThrowsArgumentException()
    {
        // Arrange
        var booking = new Booking
        {
            StartDate = DateTime.Today.AddDays(5),
            EndDate = DateTime.Today.AddDays(3),
            CustomerId = 1
        };

        // Act 
        var act = () => bookingManager.CreateBooking(booking);
        // Act & Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateBooking_RoomBecomesAvailableAfterExistingBooking_ReturnsTrue()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = 1, Description = "Room A" }
        };

        var bookings = new List<Booking>
        {
            // Room 1 is booked until day 2
            new() { Id = 1, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(2), IsActive = true, RoomId = 1 }
        };

        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        var booking = new Booking
        {
            StartDate = DateTime.Today.AddDays(4), // After existing booking ends
            EndDate = DateTime.Today.AddDays(5),
            CustomerId = 1
        };

        // Act
        var result = await bookingManager.CreateBooking(booking);

        // Assert
        result.Should().BeTrue();
        booking.RoomId.Should().Be(1);
        booking.IsActive.Should().BeTrue();
        bookingRepository.Verify(b => b.AddAsync(booking), Times.Once);
    }

    [Fact]
    public async Task CreateBooking_InactiveBookingIgnored_ReturnsTrue()
    {
        // Arrange
        var rooms = new List<Room>
        {
            new() { Id = 1, Description = "Room A" }
        };

        var bookings = new List<Booking>
        {
            // Inactive booking should be ignored
            new() { Id = 1, StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(3), IsActive = false, RoomId = 1 }
        };

        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        var booking = new Booking
        {
            StartDate = DateTime.Today.AddDays(2),
            EndDate = DateTime.Today.AddDays(2),
            CustomerId = 1
        };

        // Act
        var result = await bookingManager.CreateBooking(booking);

        // Assert
        result.Should().BeTrue();
        booking.RoomId.Should().Be(1);
        booking.IsActive.Should().BeTrue();
        bookingRepository.Verify(b => b.AddAsync(booking), Times.Once);
    }
}
