using System;
using System.Threading.Tasks;
using HotelBooking.Core.Entities;
using HotelBooking.Core.Services;
using HotelBooking.UnitTests.Fakes;
using Xunit;

namespace HotelBooking.UnitTests;

public class CreateBookingTest
{
    [Fact]
    public async Task CreateBooking_RoomAvailable_ReturnsTrueAndAddsBooking()
    {
        // Arrange
        var bookingRepository = new FakeBookingRepository(DateTime.Today.AddDays(10), DateTime.Today.AddDays(20));
        var roomRepository = new FakeRoomRepository();
        var bookingManager = new BookingManager(bookingRepository, roomRepository);
        var booking = new Booking
        {
            StartDate = DateTime.Today.AddDays(1),
            EndDate = DateTime.Today.AddDays(1),
            CustomerId = 1
        };

        // Act
        var result = await bookingManager.CreateBooking(booking);

        // Assert
        Assert.True(result);
        Assert.True(bookingRepository.addWasCalled);
        Assert.True(booking.IsActive);
        Assert.NotEqual(0, booking.RoomId);
    }

    [Fact]
    public async Task CreateBooking_NoRoomAvailable_ReturnsFalseAndDoesNotAddBooking()
    {
        // Arrange: set up dates that are fully occupied in FakeBookingRepository
        var start = DateTime.Today.AddDays(10);
        var end = DateTime.Today.AddDays(20);
        var bookingRepository = new FakeBookingRepository(start, end);
        var roomRepository = new FakeRoomRepository();
        var bookingManager = new BookingManager(bookingRepository, roomRepository);
        var booking = new Booking
        {
            StartDate = start,
            EndDate = end,
            CustomerId = 1
        };

        // Act
        var result = await bookingManager.CreateBooking(booking);

        // Assert
        Assert.False(result);
        Assert.False(bookingRepository.addWasCalled);
    }

    [Fact]
    public async Task CreateBooking_InvalidDates_ThrowsArgumentException()
    {
        // Arrange
        var bookingRepository = new FakeBookingRepository(DateTime.Today.AddDays(10), DateTime.Today.AddDays(20));
        var roomRepository = new FakeRoomRepository();
        var bookingManager = new BookingManager(bookingRepository, roomRepository);
        var booking = new Booking
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today,
            CustomerId = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => bookingManager.CreateBooking(booking));
    }
}

