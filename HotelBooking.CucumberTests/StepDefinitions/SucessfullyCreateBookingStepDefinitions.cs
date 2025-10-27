using Bogus;
using FluentAssertions;
using HotelBooking.Core.Entities;
using HotelBooking.Core.Interfaces;
using HotelBooking.Core.Services;
using Moq;
using Reqnroll;

namespace HotelBooking.CucumberTests.StepDefinitions;

[Binding]
public class CreateBookingStepDefinitions
{
    private readonly Mock<IRepository<Booking>> bookingRepository;
    private readonly Mock<IRepository<Room>> roomRepository;
    private readonly IBookingManager bookingManager;
    private bool createBookingResult;
    private DateTime testStartDate;
    private DateTime testEndDate;
    private int testRoomId;
    private int testCustomerId;
    private readonly Faker<Room> roomFaker;
    private DateTime occupiedStart;
    private DateTime occupiedEnd;


    public CreateBookingStepDefinitions()
    {
        bookingRepository = new Mock<IRepository<Booking>>();
        roomRepository = new Mock<IRepository<Room>>();
        bookingManager = new BookingManager(bookingRepository.Object, roomRepository.Object);

        roomFaker = new Faker<Room>()
            .RuleFor(r => r.Id, f => f.Random.Int(1, 1000))
            .RuleFor(r => r.Description, f => f.Commerce.ProductName());
        occupiedStart = DateTime.MinValue;
        occupiedEnd = DateTime.MinValue;
    }

    [Given(@"the occupied period is from {string} to {string}")]
    public void GivenTheOccupiedPeriodIsFromTo(string start, string end)
    {
        occupiedStart = Convert.ToDateTime(start.Trim('"'));
        occupiedEnd = Convert.ToDateTime(end.Trim('"'));
    }

    [Given(@"I have a booking with start date {string}, end date {string}, room id {int} and customer id {int}")]
    public void GivenIHaveABookingWithStartDateEndDateRoomIdAndCustomerId(string startDate, string endDate, int roomId, int customerId)
    {
        var start = Convert.ToDateTime(startDate.Trim('"'));
        var end = Convert.ToDateTime(endDate.Trim('"'));
        testStartDate = start;
        testEndDate = end;
        testRoomId = roomId;
        testCustomerId = customerId;
    }

    [When(@"I create the booking")]
    public async Task WhenICreateTheBooking()
    {
        var rooms = roomFaker.Generate(2);
        var bookings = new List<Booking>();
        if (occupiedStart != DateTime.MinValue && occupiedEnd != DateTime.MinValue)
        {
            foreach (var room in rooms)
            {
                bookings.Add(new Booking
                {
                    StartDate = occupiedStart,
                    EndDate = occupiedEnd,
                    RoomId = room.Id,
                    CustomerId = 999,
                    IsActive = true
                });
            }
        }
        roomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
        bookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

        var booking = new Booking
        {
            StartDate = testStartDate,
            EndDate = testEndDate,
            RoomId = testRoomId,
            CustomerId = testCustomerId
        };

        createBookingResult = await bookingManager.CreateBooking(booking);
    }

    [Then(@"the booking should be created successfully")]
    public void ThenTheBookingShouldBeCreatedSuccessfully()
    {
        createBookingResult.Should().Be(true);
    }

    [Then(@"the booking creation should fail due to overlap")]
    public void ThenTheBookingCreationShouldFailDueToOverlap()
    {
        createBookingResult.Should().Be(false);

    }
}
