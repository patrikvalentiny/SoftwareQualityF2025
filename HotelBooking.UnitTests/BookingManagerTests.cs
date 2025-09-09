using System;
using HotelBooking.UnitTests.Fakes;
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using HotelBooking.Core.Interfaces;
using HotelBooking.Core.Entities;
using HotelBooking.Core.Services;


namespace HotelBooking.UnitTests;

public class BookingManagerTests
{
    private IBookingManager bookingManager;
    IRepository<Booking> bookingRepository;

    public BookingManagerTests()
    {
        DateTime start = DateTime.Today.AddDays(10);
        DateTime end = DateTime.Today.AddDays(20);
        bookingRepository = new FakeBookingRepository(start, end);
        IRepository<Room> roomRepository = new FakeRoomRepository();
        bookingManager = new BookingManager(bookingRepository, roomRepository);
    }

    [Fact]
    public async Task FindAvailableRoom_StartDateNotInTheFuture_ThrowsArgumentException()
    {
        // Arrange
        DateTime date = DateTime.Today;

        // Act
        Task result() => bookingManager.FindAvailableRoom(date, date);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(result);
    }

    [Fact]
    public async Task FindAvailableRoom_RoomAvailable_RoomIdNotMinusOne()
    {
        // Arrange
        DateTime date = DateTime.Today.AddDays(1);
        // Act
        int roomId = await bookingManager.FindAvailableRoom(date, date);
        // Assert
        Assert.NotEqual(-1, roomId);
    }

    [Fact]
    public async Task FindAvailableRoom_RoomAvailable_ReturnsAvailableRoom()
    {
        // This test was added to satisfy the following test design
        // principle: "Tests should have strong assertions".

        // Arrange
        DateTime date = DateTime.Today.AddDays(1);

        // Act
        int roomId = await bookingManager.FindAvailableRoom(date, date);

        var bookingForReturnedRoomId = (await bookingRepository.GetAllAsync()).
            Where(b => b.RoomId == roomId
                       && b.StartDate <= date
                       && b.EndDate >= date
                       && b.IsActive);

        // Assert
        Assert.Empty(bookingForReturnedRoomId);
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
        // Based on FakeBookingRepository, rooms 1 and 2 are both booked from Day 10 to Day 20
        DateTime startDate = DateTime.Today.AddDays(10);
        DateTime endDate = DateTime.Today.AddDays(20);

        // Act
        var fullyOccupiedDates = await bookingManager.GetFullyOccupiedDates(startDate, endDate);

        // Assert
        Assert.Equal(11, fullyOccupiedDates.Count); // 10 days inclusive (10th to 20th = 11 days)
        Assert.Contains(DateTime.Today.AddDays(10), fullyOccupiedDates);
        Assert.Contains(DateTime.Today.AddDays(15), fullyOccupiedDates);
        Assert.Contains(DateTime.Today.AddDays(20), fullyOccupiedDates);
    }

    [Fact]
    public async Task GetFullyOccupiedDates_DateRangeWithPartiallyOccupiedDates_ReturnsEmptyList()
    {
        // Arrange
        // Day 1 has only room 1 booked, room 2 is available
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
        // Range from Day 1 (partially occupied) through Day 15 (fully occupied)
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
        DateTime singleDay = DateTime.Today.AddDays(15); // Day within fully occupied period

        // Act
        var fullyOccupiedDates = await bookingManager.GetFullyOccupiedDates(singleDay, singleDay);

        // Assert
        Assert.Single(fullyOccupiedDates);
        Assert.Contains(singleDay, fullyOccupiedDates);
    }

}
