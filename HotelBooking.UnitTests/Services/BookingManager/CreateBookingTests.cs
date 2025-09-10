using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bogus;
using FluentAssertions;
using HotelBooking.Core.Entities;
using HotelBooking.Core.Interfaces;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests.Services.BookingManager;

public class CreateBookingTests
{
    private readonly Mock<IRepository<Booking>> bookingRepository;
    private readonly Mock<IRepository<Room>> roomRepository;
    private readonly IBookingManager bookingManager;
    private readonly Faker<Booking> bookingFaker;
    private readonly Faker<Room> roomFaker;

    public CreateBookingTests()
    {
        bookingRepository = new Mock<IRepository<Booking>>();
        roomRepository = new Mock<IRepository<Room>>();
        bookingManager = new Core.Services.BookingManager(bookingRepository.Object, roomRepository.Object);

        bookingFaker = new Faker<Booking>()
            .RuleFor(b => b.StartDate, f => f.Date.Future())
            .RuleFor(b => b.EndDate, (f, b) => b.StartDate.AddDays(f.Random.Int(0, 5)))
            .RuleFor(b => b.CustomerId, f => f.Random.Int(1, 100))
            .RuleFor(b => b.IsActive, f => true);
        roomFaker = new Faker<Room>()
            .RuleFor(r => r.Id, f => f.Random.Int(1, 1000))
            .RuleFor(r => r.Description, f => f.Commerce.ProductName());
    }

    private Booking CreateBookingWithDates(DateTime start, DateTime end, int? customerId = null)
    {
        return bookingFaker.Clone()
            .RuleFor(b => b.StartDate, start)
            .RuleFor(b => b.EndDate, end)
            .RuleFor(b => b.CustomerId, customerId ?? bookingFaker.Generate().CustomerId)
            .Generate();
    }

    private Booking CreateBookingWithRoom(DateTime start, DateTime end, int roomId, bool isActive = true, int? id = null)
    {
        return bookingFaker.Clone()
            .RuleFor(b => b.Id, id ?? bookingFaker.Generate().Id)
            .RuleFor(b => b.StartDate, start)
            .RuleFor(b => b.EndDate, end)
            .RuleFor(b => b.RoomId, roomId)
            .RuleFor(b => b.IsActive, isActive)
            .Generate();
    }

    [Fact]
    public async Task CreateBooking_RoomAvailable_ReturnsTrueAndAddsBooking()
    {
        // Arrange
        var rooms = roomFaker.Generate(2);
        var bookings = new List<Booking>();
        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);
        var booking = CreateBookingWithDates(DateTime.Today.AddDays(1), DateTime.Today.AddDays(1), 1);

        // Act
        var result = await bookingManager.CreateBooking(booking);

        // Assert
        result.Should().BeTrue();
        booking.IsActive.Should().BeTrue();
        booking.RoomId.Should().BeGreaterThan(0);
        bookingRepository.Verify(b => b.AddAsync(booking), Times.Once);
    }

    [Fact]
    public async Task CreateBooking_NoRoomAvailable_ReturnsFalseAndDoesNotAddBooking()
    {
        // Arrange
        var rooms = roomFaker.Generate(2);
        var bookings = new List<Booking>
        {
            CreateBookingWithRoom(DateTime.Today.AddDays(1), DateTime.Today.AddDays(3), rooms[0].Id, true, 1),
            CreateBookingWithRoom(DateTime.Today.AddDays(1), DateTime.Today.AddDays(3), rooms[1].Id, true, 2)
        };
        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);
        var booking = CreateBookingWithDates(DateTime.Today.AddDays(2), DateTime.Today.AddDays(2), 1);

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
        var booking = CreateBookingWithDates(DateTime.Today, DateTime.Today, 1);

        // Act
        Func<Task> act = async () => await bookingManager.CreateBooking(booking);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*start*end*");
        bookingRepository.Verify(b => b.GetAllAsync(), Times.Never);
        roomRepository.Verify(r => r.GetAllAsync(), Times.Never);
        bookingRepository.Verify(b => b.AddAsync(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task CreateBooking_StartDateInPast_ThrowsArgumentException()
    {
        // Arrange
        var booking = CreateBookingWithDates(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1), 1);

        // Act
        Func<Task> act = async () => await bookingManager.CreateBooking(booking);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*start*past*");
    }

    [Fact]
    public async Task CreateBooking_StartDateAfterEndDate_ThrowsArgumentException()
    {
        // Arrange
        var booking = CreateBookingWithDates(DateTime.Today.AddDays(5), DateTime.Today.AddDays(3), 1);

        // Act
        Func<Task> act = async () => await bookingManager.CreateBooking(booking);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateBooking_RoomBecomesAvailableAfterExistingBooking_ReturnsTrue()
    {
        // Arrange
        var rooms = roomFaker.Generate(1);
        var bookings = new List<Booking>
        {
            CreateBookingWithRoom(DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), rooms[0].Id, true, 1)
        };
        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);
        var booking = CreateBookingWithDates(DateTime.Today.AddDays(4), DateTime.Today.AddDays(5), 1);

        // Act
        var result = await bookingManager.CreateBooking(booking);

        // Assert
        result.Should().BeTrue();
        booking.RoomId.Should().Be(rooms[0].Id);
        booking.IsActive.Should().BeTrue();
        bookingRepository.Verify(b => b.AddAsync(booking), Times.Once);
    }

    [Fact]
    public async Task CreateBooking_InactiveBookingIgnored_ReturnsTrue()
    {
        // Arrange
        var rooms = roomFaker.Generate(1);
        var bookings = new List<Booking>
        {
            CreateBookingWithRoom(DateTime.Today.AddDays(1), DateTime.Today.AddDays(3), rooms[0].Id, false, 1)
        };
        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);
        var booking = CreateBookingWithDates(DateTime.Today.AddDays(2), DateTime.Today.AddDays(2), 1);

        // Act
        var result = await bookingManager.CreateBooking(booking);

        // Assert
        result.Should().BeTrue();
        booking.RoomId.Should().Be(rooms[0].Id);
        booking.IsActive.Should().BeTrue();
        bookingRepository.Verify(b => b.AddAsync(booking), Times.Once);
    }
}
