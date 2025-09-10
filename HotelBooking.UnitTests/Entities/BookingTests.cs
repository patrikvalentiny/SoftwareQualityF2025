using FluentAssertions;
using HotelBooking.Core.Entities;
using System;
using Xunit;

namespace HotelBooking.UnitTests.Entities;

public class BookingTests
{
    [Fact]
    public void Booking_CanBeCreated_WithDefaultValues()
    {
        // Act
        var booking = new Booking();

        // Assert
        booking.Should().NotBeNull();
        booking.Id.Should().Be(0);
        booking.StartDate.Should().Be(default);
        booking.EndDate.Should().Be(default);
        booking.IsActive.Should().BeFalse();
        booking.CustomerId.Should().Be(0);
        booking.RoomId.Should().Be(0);
        booking.Customer.Should().BeNull();
        booking.Room.Should().BeNull();
    }

    [Theory]
    [InlineData(1, 101, 201, true)]
    [InlineData(2, 102, 202, false)]
    [InlineData(999, 999, 999, true)]
    [InlineData(-1, 0, int.MaxValue, false)]
    public void Booking_CanSetBasicProperties_WithVariousValues(int id, int customerId, int roomId, bool isActive)
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 5);

        // Act
        var booking = new Booking
        {
            Id = id,
            CustomerId = customerId,
            RoomId = roomId,
            IsActive = isActive,
            StartDate = startDate,
            EndDate = endDate
        };

        // Assert
        booking.Id.Should().Be(id);
        booking.CustomerId.Should().Be(customerId);
        booking.RoomId.Should().Be(roomId);
        booking.IsActive.Should().Be(isActive);
        booking.StartDate.Should().Be(startDate);
        booking.EndDate.Should().Be(endDate);
    }

    [Fact]
    public void Booking_CanSetNavigationProperties_WithValidObjects()
    {
        // Arrange
        var customer = new Customer { Id = 10, Name = "Test Customer", Email = "test@example.com" };
        var room = new Room { Id = 15, Description = "Test Room" };
        var booking = new Booking();

        // Act
        booking.Customer = customer;
        booking.Room = room;

        // Assert
        booking.Customer.Should().Be(customer);
        booking.Room.Should().Be(room);
        booking.Customer.Id.Should().Be(10);
        booking.Room.Description.Should().Be("Test Room");
    }

    [Fact]
    public void Booking_CanSetDateRange_WithValidDates()
    {
        // Arrange
        var booking = new Booking();
        var startDate = new DateTime(2025, 7, 1);
        var endDate = new DateTime(2025, 7, 5);

        // Act
        booking.StartDate = startDate;
        booking.EndDate = endDate;

        // Assert
        booking.StartDate.Should().Be(startDate);
        booking.EndDate.Should().Be(endDate);
        booking.EndDate.Should().BeAfter(booking.StartDate);
    }

    [Fact]
    public void Booking_CanSetCompleteBooking_WithAllProperties()
    {
        // Arrange
        var startDate = new DateTime(2025, 6, 1, 14, 0, 0);
        var endDate = new DateTime(2025, 6, 5, 11, 0, 0);
        var customer = new Customer { Id = 50, Name = "John Smith", Email = "john@example.com" };
        var room = new Room { Id = 101, Description = "Deluxe Room" };

        // Act
        var booking = new Booking
        {
            Id = 123,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true,
            CustomerId = 50,
            RoomId = 101,
            Customer = customer,
            Room = room
        };

        // Assert
        booking.Id.Should().Be(123);
        booking.StartDate.Should().Be(startDate);
        booking.EndDate.Should().Be(endDate);
        booking.IsActive.Should().BeTrue();
        booking.CustomerId.Should().Be(50);
        booking.RoomId.Should().Be(101);
        booking.Customer.Should().Be(customer);
        booking.Room.Should().Be(room);
    }
}
