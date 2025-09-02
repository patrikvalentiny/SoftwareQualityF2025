using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelBooking.Core;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests
{
    /// <summary>
    /// Tests that focus on interaction verification and mock behavior validation.
    /// These tests demonstrate proper use of mocking framework (Moq) for verifying
    /// that the BookingManager interacts correctly with its dependencies.
    /// </summary>
    public class BookingManagerMockVerificationTests
    {
        private readonly Mock<IRepository<Booking>> _mockBookingRepository;
        private readonly Mock<IRepository<Room>> _mockRoomRepository;
        private readonly BookingManager _bookingManager;

        public BookingManagerMockVerificationTests()
        {
            _mockBookingRepository = new Mock<IRepository<Booking>>();
            _mockRoomRepository = new Mock<IRepository<Room>>();
            _bookingManager = new BookingManager(_mockBookingRepository.Object, _mockRoomRepository.Object);
        }

        #region CreateBooking Mock Verification Tests

        [Fact]
        public async Task CreateBooking_SuccessfulBooking_CallsRepositoriesCorrectly()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(15);
            var booking = new Booking
            {
                StartDate = startDate,
                EndDate = endDate,
                CustomerId = 1
            };

            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Test Room" }
            };
            var bookings = new List<Booking>();

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var result = await _bookingManager.CreateBooking(booking);

            // Assert
            Assert.True(result);
            
            // Verify that GetAllAsync was called on both repositories
            _mockRoomRepository.Verify(r => r.GetAllAsync(), Times.Once);
            _mockBookingRepository.Verify(b => b.GetAllAsync(), Times.Once);
            
            // Verify that AddAsync was called with the correct booking
            _mockBookingRepository.Verify(b => b.AddAsync(It.Is<Booking>(
                x => x.RoomId == 1 && 
                     x.IsActive == true && 
                     x.StartDate == startDate && 
                     x.EndDate == endDate &&
                     x.CustomerId == 1)), Times.Once);

            // Verify no unexpected calls were made
            _mockBookingRepository.Verify(b => b.EditAsync(It.IsAny<Booking>()), Times.Never);
            _mockBookingRepository.Verify(b => b.RemoveAsync(It.IsAny<int>()), Times.Never);
            _mockRoomRepository.Verify(r => r.AddAsync(It.IsAny<Room>()), Times.Never);
        }

        [Fact]
        public async Task CreateBooking_NoAvailableRoom_DoesNotCallAddAsync()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(15);
            var booking = new Booking
            {
                StartDate = startDate,
                EndDate = endDate,
                CustomerId = 1
            };

            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Test Room" }
            };
            var existingBookings = new List<Booking>
            {
                new Booking 
                { 
                    Id = 1, 
                    StartDate = startDate, 
                    EndDate = endDate, 
                    RoomId = 1, 
                    IsActive = true 
                }
            };

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(existingBookings);

            // Act
            var result = await _bookingManager.CreateBooking(booking);

            // Assert
            Assert.False(result);
            
            // Verify that repositories were queried
            _mockRoomRepository.Verify(r => r.GetAllAsync(), Times.Once);
            _mockBookingRepository.Verify(b => b.GetAllAsync(), Times.Once);
            
            // Verify that AddAsync was NOT called when no room is available
            _mockBookingRepository.Verify(b => b.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CreateBooking_RepositoryException_PropagatesException()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(15);
            var booking = new Booking
            {
                StartDate = startDate,
                EndDate = endDate,
                CustomerId = 1
            };

            // Setup booking repository to work normally first
            _mockBookingRepository.Setup(b => b.GetAllAsync())
                .ReturnsAsync(new List<Booking>());
            
            // But room repository throws exception
            _mockRoomRepository.Setup(r => r.GetAllAsync())
                .ThrowsAsync(new InvalidOperationException("Database connection failed"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _bookingManager.CreateBooking(booking));

            // Verify that the booking repository was called (it's called first in FindAvailableRoom)
            _mockBookingRepository.Verify(b => b.GetAllAsync(), Times.Once);
            
            // Verify that the room repository was called and threw exception
            _mockRoomRepository.Verify(r => r.GetAllAsync(), Times.Once);
            
            // Verify that AddAsync was not called due to exception
            _mockBookingRepository.Verify(b => b.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        #endregion

        #region FindAvailableRoom Mock Verification Tests

        [Fact]
        public async Task FindAvailableRoom_ValidRequest_CallsRepositoriesInCorrectOrder()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(15);
            var rooms = new List<Room> { new Room { Id = 1, Description = "Test Room" } };
            var bookings = new List<Booking>();

            var roomRepoCallOrder = 0;
            var bookingRepoCallOrder = 0;
            var callSequence = 0;

            _mockRoomRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(rooms)
                .Callback(() => roomRepoCallOrder = ++callSequence);

            _mockBookingRepository.Setup(b => b.GetAllAsync())
                .ReturnsAsync(bookings)
                .Callback(() => bookingRepoCallOrder = ++callSequence);

            // Act
            var result = await _bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(1, result);
            
            // Verify call order - booking repository should be called first, then room repository
            Assert.True(bookingRepoCallOrder < roomRepoCallOrder, 
                "Booking repository should be called before room repository");
            
            _mockBookingRepository.Verify(b => b.GetAllAsync(), Times.Once);
            _mockRoomRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task FindAvailableRoom_InvalidDates_DoesNotCallRepositories()
        {
            // Arrange
            var startDate = DateTime.Today; // Invalid - not in future
            var endDate = DateTime.Today.AddDays(1);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _bookingManager.FindAvailableRoom(startDate, endDate));

            // Verify that no repository calls were made due to early validation
            _mockBookingRepository.Verify(b => b.GetAllAsync(), Times.Never);
            _mockRoomRepository.Verify(r => r.GetAllAsync(), Times.Never);
        }

        [Theory]
        [InlineData(1, 1)] // 1 call each when room found immediately
        public async Task FindAvailableRoom_RepositoryCallCount_IsOptimal(
            int expectedBookingCalls, int expectedRoomCalls)
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(15);
            var rooms = new List<Room> 
            { 
                new Room { Id = 1, Description = "Room 1" },
                new Room { Id = 2, Description = "Room 2" }
            };
            var bookings = new List<Booking>();

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var result = await _bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(1, result);
            
            // Verify optimal number of repository calls
            _mockBookingRepository.Verify(b => b.GetAllAsync(), Times.Exactly(expectedBookingCalls));
            _mockRoomRepository.Verify(r => r.GetAllAsync(), Times.Exactly(expectedRoomCalls));
        }

        #endregion

        #region GetFullyOccupiedDates Mock Verification Tests

        [Fact]
        public async Task GetFullyOccupiedDates_ValidRequest_CallsBothRepositories()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(1);
            var endDate = DateTime.Today.AddDays(5);
            var rooms = new List<Room> { new Room { Id = 1, Description = "Test Room" } };
            var bookings = new List<Booking>();

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var result = await _bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.NotNull(result);
            
            // Verify both repositories were called exactly once
            _mockRoomRepository.Verify(r => r.GetAllAsync(), Times.Once);
            _mockBookingRepository.Verify(b => b.GetAllAsync(), Times.Once);
            
            // Verify no other repository methods were called
            _mockRoomRepository.Verify(r => r.GetAsync(It.IsAny<int>()), Times.Never);
            _mockBookingRepository.Verify(b => b.GetAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetFullyOccupiedDates_InvalidDateRange_DoesNotCallRepositories()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(5); // End before start

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _bookingManager.GetFullyOccupiedDates(startDate, endDate));

            // Verify no repository calls were made due to early validation
            _mockRoomRepository.Verify(r => r.GetAllAsync(), Times.Never);
            _mockBookingRepository.Verify(b => b.GetAllAsync(), Times.Never);
        }

        [Fact]
        public async Task GetFullyOccupiedDates_EmptyBookingsResult_StillCallsRoomRepository()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(1);
            var endDate = DateTime.Today.AddDays(5);
            var rooms = new List<Room> { new Room { Id = 1, Description = "Test Room" } };
            var emptyBookings = new List<Booking>();

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(emptyBookings);

            // Act
            var result = await _bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.Empty(result);
            
            // Verify both repositories were called even with empty bookings
            _mockRoomRepository.Verify(r => r.GetAllAsync(), Times.Once);
            _mockBookingRepository.Verify(b => b.GetAllAsync(), Times.Once);
        }

        #endregion

        #region Mock Setup and Callback Tests

        [Fact]
        public async Task MockSetup_ComplexCallbackBehavior_WorksCorrectly()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(15);
            var callLog = new List<string>();

            _mockRoomRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Room> { new Room { Id = 1, Description = "Test Room" } })
                .Callback(() => callLog.Add("GetRooms"));

            _mockBookingRepository.Setup(b => b.GetAllAsync())
                .ReturnsAsync(new List<Booking>())
                .Callback(() => callLog.Add("GetBookings"));

            _mockBookingRepository.Setup(b => b.AddAsync(It.IsAny<Booking>()))
                .Callback<Booking>(booking => 
                {
                    callLog.Add($"AddBooking-Room{booking.RoomId}");
                    // Simulate setting an ID as would happen in real repository
                    booking.Id = 123;
                });

            var newBooking = new Booking
            {
                StartDate = startDate,
                EndDate = endDate,
                CustomerId = 1
            };

            // Act
            var result = await _bookingManager.CreateBooking(newBooking);

            // Assert
            Assert.True(result);
            Assert.Equal(123, newBooking.Id); // Verify callback set the ID
            
            // Verify call sequence
            Assert.Equal(3, callLog.Count);
            Assert.Contains("GetBookings", callLog);
            Assert.Contains("GetRooms", callLog);
            Assert.Contains("AddBooking-Room1", callLog);
        }

        [Fact]
        public async Task MockSetup_ConditionalReturns_WorkCorrectly()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(15);
            
            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Standard Room" },
                new Room { Id = 2, Description = "Deluxe Room" }
            };

            // Setup conditional behavior based on parameters
            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            
            // First call returns bookings, subsequent calls return empty
            var callCount = 0;
            _mockBookingRepository.Setup(b => b.GetAllAsync())
                .ReturnsAsync(() => 
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        return new List<Booking>
                        {
                            new Booking { Id = 1, StartDate = startDate, EndDate = endDate, RoomId = 1, IsActive = true }
                        };
                    }
                    return new List<Booking>();
                });

            // Act
            var firstCall = await _bookingManager.FindAvailableRoom(startDate, endDate);
            var secondCall = await _bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(2, firstCall); // Second room should be available in first call
            Assert.Equal(1, secondCall); // First room should be available in second call
            
            _mockBookingRepository.Verify(b => b.GetAllAsync(), Times.Exactly(2));
        }

        #endregion

        #region Argument Matching and Verification

        [Fact]
        public async Task CreateBooking_ArgumentMatching_VerifiesCorrectBookingDetails()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(15);
            var customerId = 42;
            
            var booking = new Booking
            {
                StartDate = startDate,
                EndDate = endDate,
                CustomerId = customerId
            };

            var rooms = new List<Room> { new Room { Id = 5, Description = "Test Room" } };
            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(new List<Booking>());

            // Act
            await _bookingManager.CreateBooking(booking);

            // Assert with complex argument matching
            _mockBookingRepository.Verify(b => b.AddAsync(It.Is<Booking>(x =>
                x.StartDate == startDate &&
                x.EndDate == endDate &&
                x.CustomerId == customerId &&
                x.RoomId == 5 &&
                x.IsActive == true
            )), Times.Once);

            // Verify with predicate
            _mockBookingRepository.Verify(b => b.AddAsync(It.Is<Booking>(x => 
                x.StartDate > DateTime.Today && 
                x.EndDate >= x.StartDate
            )), Times.Once);
        }

        [Fact]
        public async Task RepositoryInteraction_ComplexArgumentValidation_WorksCorrectly()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(15);
            
            var rooms = new List<Room> { new Room { Id = 1, Description = "Test Room" } };
            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(new List<Booking>());

            var capturedBooking = new Booking();
            _mockBookingRepository.Setup(b => b.AddAsync(It.IsAny<Booking>()))
                .Callback<Booking>(b => 
                {
                    capturedBooking.StartDate = b.StartDate;
                    capturedBooking.EndDate = b.EndDate;
                    capturedBooking.RoomId = b.RoomId;
                    capturedBooking.IsActive = b.IsActive;
                    capturedBooking.CustomerId = b.CustomerId;
                });

            var newBooking = new Booking
            {
                StartDate = startDate,
                EndDate = endDate,
                CustomerId = 99
            };

            // Act
            await _bookingManager.CreateBooking(newBooking);

            // Assert
            Assert.Equal(startDate, capturedBooking.StartDate);
            Assert.Equal(endDate, capturedBooking.EndDate);
            Assert.Equal(1, capturedBooking.RoomId);
            Assert.True(capturedBooking.IsActive);
            Assert.Equal(99, capturedBooking.CustomerId);
        }

        #endregion
    }
}
