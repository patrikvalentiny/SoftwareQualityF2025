using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelBooking.Core;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests
{
    public class BookingManagerTests
    {
        private readonly Mock<IRepository<Booking>> _mockBookingRepository;
        private readonly Mock<IRepository<Room>> _mockRoomRepository;
        private readonly BookingManager _bookingManager;

        public BookingManagerTests()
        {
            _mockBookingRepository = new Mock<IRepository<Booking>>();
            _mockRoomRepository = new Mock<IRepository<Room>>();
            _bookingManager = new BookingManager(_mockBookingRepository.Object, _mockRoomRepository.Object);
        }

        #region CreateBooking Tests

        [Fact]
        public async Task CreateBooking_ValidBookingWithAvailableRoom_ReturnsTrue()
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
                new Room { Id = 1, Description = "Single Room" },
                new Room { Id = 2, Description = "Double Room" }
            };

            var bookings = new List<Booking>();

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);
            _mockBookingRepository.Setup(b => b.AddAsync(It.IsAny<Booking>()));

            // Act
            var result = await _bookingManager.CreateBooking(booking);

            // Assert
            Assert.True(result);
            Assert.Equal(1, booking.RoomId);
            Assert.True(booking.IsActive);
            _mockBookingRepository.Verify(b => b.AddAsync(booking), Times.Once);
        }

        [Fact]
        public async Task CreateBooking_NoAvailableRoom_ReturnsFalse()
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
                new Room { Id = 1, Description = "Single Room" }
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
            _mockBookingRepository.Verify(b => b.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        #endregion

        #region FindAvailableRoom Tests

        [Theory]
        [InlineData(-1)] // Yesterday
        [InlineData(0)]  // Today
        public async Task FindAvailableRoom_StartDateNotInFuture_ThrowsArgumentException(int daysFromToday)
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(daysFromToday);
            var endDate = startDate.AddDays(1);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _bookingManager.FindAvailableRoom(startDate, endDate));
        }

        [Fact]
        public async Task FindAvailableRoom_StartDateAfterEndDate_ThrowsArgumentException()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(5);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _bookingManager.FindAvailableRoom(startDate, endDate));
        }

        [Fact]
        public async Task FindAvailableRoom_RoomAvailable_ReturnsRoomId()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(15);

            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Single Room" },
                new Room { Id = 2, Description = "Double Room" }
            };

            var bookings = new List<Booking>();

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var result = await _bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task FindAvailableRoom_AllRoomsOccupied_ReturnsMinusOne()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(15);

            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Single Room" },
                new Room { Id = 2, Description = "Double Room" }
            };

            var bookings = new List<Booking>
            {
                new Booking 
                { 
                    Id = 1, 
                    StartDate = startDate, 
                    EndDate = endDate, 
                    RoomId = 1, 
                    IsActive = true 
                },
                new Booking 
                { 
                    Id = 2, 
                    StartDate = startDate, 
                    EndDate = endDate, 
                    RoomId = 2, 
                    IsActive = true 
                }
            };

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var result = await _bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(-1, result);
        }

        [Theory]
        [MemberData(nameof(GetOverlappingBookingTestData))]
        public async Task FindAvailableRoom_OverlappingBookings_ReturnsCorrectResult(
            DateTime requestStart, DateTime requestEnd, 
            DateTime bookingStart, DateTime bookingEnd, 
            bool shouldHaveAvailableRoom)
        {
            // Arrange
            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Test Room" }
            };

            var bookings = new List<Booking>
            {
                new Booking 
                { 
                    Id = 1, 
                    StartDate = bookingStart, 
                    EndDate = bookingEnd, 
                    RoomId = 1, 
                    IsActive = true 
                }
            };

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var result = await _bookingManager.FindAvailableRoom(requestStart, requestEnd);

            // Assert
            if (shouldHaveAvailableRoom)
            {
                Assert.Equal(1, result);
            }
            else
            {
                Assert.Equal(-1, result);
            }
        }

        public static IEnumerable<object[]> GetOverlappingBookingTestData()
        {
            var baseDate = DateTime.Today.AddDays(10);
            
            return new List<object[]>
            {
                // Request before existing booking (no overlap)
                new object[] { baseDate.AddDays(1), baseDate.AddDays(2), baseDate.AddDays(5), baseDate.AddDays(8), true },
                
                // Request after existing booking (no overlap)
                new object[] { baseDate.AddDays(10), baseDate.AddDays(12), baseDate.AddDays(5), baseDate.AddDays(8), true },
                
                // Request overlaps start of existing booking
                new object[] { baseDate.AddDays(3), baseDate.AddDays(6), baseDate.AddDays(5), baseDate.AddDays(8), false },
                
                // Request overlaps end of existing booking
                new object[] { baseDate.AddDays(7), baseDate.AddDays(10), baseDate.AddDays(5), baseDate.AddDays(8), false },
                
                // Request completely contains existing booking
                new object[] { baseDate.AddDays(4), baseDate.AddDays(9), baseDate.AddDays(5), baseDate.AddDays(8), false },
                
                // Request completely contained within existing booking
                new object[] { baseDate.AddDays(6), baseDate.AddDays(7), baseDate.AddDays(5), baseDate.AddDays(8), false },
                
                // Request exactly matches existing booking
                new object[] { baseDate.AddDays(5), baseDate.AddDays(8), baseDate.AddDays(5), baseDate.AddDays(8), false }
            };
        }

        [Fact]
        public async Task FindAvailableRoom_InactiveBookingsIgnored_ReturnsRoomId()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(15);

            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Single Room" }
            };

            var bookings = new List<Booking>
            {
                new Booking 
                { 
                    Id = 1, 
                    StartDate = startDate, 
                    EndDate = endDate, 
                    RoomId = 1, 
                    IsActive = false // Inactive booking should be ignored
                }
            };

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var result = await _bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(1, result);
        }

        #endregion

        #region GetFullyOccupiedDates Tests

        [Fact]
        public async Task GetFullyOccupiedDates_StartDateAfterEndDate_ThrowsArgumentException()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(5);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _bookingManager.GetFullyOccupiedDates(startDate, endDate));
        }

        [Fact]
        public async Task GetFullyOccupiedDates_NoBookings_ReturnsEmptyList()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(1);
            var endDate = DateTime.Today.AddDays(5);

            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Room 1" },
                new Room { Id = 2, Description = "Room 2" }
            };

            var bookings = new List<Booking>();

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var result = await _bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFullyOccupiedDates_AllRoomsBookedForSomeDates_ReturnsCorrectDates()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(1);
            var endDate = DateTime.Today.AddDays(5);
            var fullyOccupiedDate1 = DateTime.Today.AddDays(2);
            var fullyOccupiedDate2 = DateTime.Today.AddDays(3);

            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Room 1" },
                new Room { Id = 2, Description = "Room 2" }
            };

            var bookings = new List<Booking>
            {
                // Day 2 - all rooms occupied
                new Booking { Id = 1, StartDate = fullyOccupiedDate1, EndDate = fullyOccupiedDate1, RoomId = 1, IsActive = true },
                new Booking { Id = 2, StartDate = fullyOccupiedDate1, EndDate = fullyOccupiedDate1, RoomId = 2, IsActive = true },
                
                // Day 3 - all rooms occupied
                new Booking { Id = 3, StartDate = fullyOccupiedDate2, EndDate = fullyOccupiedDate2, RoomId = 1, IsActive = true },
                new Booking { Id = 4, StartDate = fullyOccupiedDate2, EndDate = fullyOccupiedDate2, RoomId = 2, IsActive = true },
                
                // Day 4 - only one room occupied (not fully occupied)
                new Booking { Id = 5, StartDate = DateTime.Today.AddDays(4), EndDate = DateTime.Today.AddDays(4), RoomId = 1, IsActive = true }
            };

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var result = await _bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(fullyOccupiedDate1, result);
            Assert.Contains(fullyOccupiedDate2, result);
        }

        [Theory]
        [MemberData(nameof(GetFullyOccupiedDatesTestData))]
        public async Task GetFullyOccupiedDates_VariousScenarios_ReturnsCorrectResults(
            List<Room> rooms, List<Booking> bookings, DateTime startDate, DateTime endDate, 
            int expectedFullyOccupiedDaysCount)
        {
            // Arrange
            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var result = await _bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.Equal(expectedFullyOccupiedDaysCount, result.Count);
        }

        public static IEnumerable<object[]> GetFullyOccupiedDatesTestData()
        {
            var baseDate = DateTime.Today.AddDays(1);
            
            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Room 1" },
                new Room { Id = 2, Description = "Room 2" },
                new Room { Id = 3, Description = "Room 3" }
            };

            return new List<object[]>
            {
                // Scenario 1: No rooms, no bookings
                new object[] 
                { 
                    new List<Room>(), 
                    new List<Booking>(), 
                    baseDate, 
                    baseDate.AddDays(2), 
                    0 // No days are "fully occupied" when there are no bookings (actual implementation behavior)
                },
                
                // Scenario 2: Multi-day bookings spanning the entire period
                new object[] 
                { 
                    rooms, 
                    new List<Booking>
                    {
                        new Booking { Id = 1, StartDate = baseDate, EndDate = baseDate.AddDays(3), RoomId = 1, IsActive = true },
                        new Booking { Id = 2, StartDate = baseDate, EndDate = baseDate.AddDays(3), RoomId = 2, IsActive = true },
                        new Booking { Id = 3, StartDate = baseDate, EndDate = baseDate.AddDays(3), RoomId = 3, IsActive = true }
                    },
                    baseDate, 
                    baseDate.AddDays(3), 
                    4 // All 4 days should be fully occupied
                },
                
                // Scenario 3: Inactive bookings should be ignored
                new object[] 
                { 
                    rooms, 
                    new List<Booking>
                    {
                        new Booking { Id = 1, StartDate = baseDate, EndDate = baseDate.AddDays(2), RoomId = 1, IsActive = false },
                        new Booking { Id = 2, StartDate = baseDate, EndDate = baseDate.AddDays(2), RoomId = 2, IsActive = false },
                        new Booking { Id = 3, StartDate = baseDate, EndDate = baseDate.AddDays(2), RoomId = 3, IsActive = false }
                    },
                    baseDate, 
                    baseDate.AddDays(2), 
                    0 // No days should be fully occupied (all bookings inactive)
                }
            };
        }

        [Fact]
        public async Task GetFullyOccupiedDates_InactiveBookingsIgnored_ReturnsCorrectDates()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(1);
            var endDate = DateTime.Today.AddDays(3);

            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Room 1" },
                new Room { Id = 2, Description = "Room 2" }
            };

            var bookings = new List<Booking>
            {
                // Active bookings for day 1
                new Booking { Id = 1, StartDate = startDate, EndDate = startDate, RoomId = 1, IsActive = true },
                new Booking { Id = 2, StartDate = startDate, EndDate = startDate, RoomId = 2, IsActive = true },
                
                // Mix of active and inactive bookings for day 2
                new Booking { Id = 3, StartDate = startDate.AddDays(1), EndDate = startDate.AddDays(1), RoomId = 1, IsActive = true },
                new Booking { Id = 4, StartDate = startDate.AddDays(1), EndDate = startDate.AddDays(1), RoomId = 2, IsActive = false }, // Inactive
                
                // All inactive bookings for day 3
                new Booking { Id = 5, StartDate = startDate.AddDays(2), EndDate = startDate.AddDays(2), RoomId = 1, IsActive = false },
                new Booking { Id = 6, StartDate = startDate.AddDays(2), EndDate = startDate.AddDays(2), RoomId = 2, IsActive = false }
            };

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var result = await _bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.Single(result);
            Assert.Contains(startDate, result); // Only day 1 should be fully occupied
        }

        #endregion

        #region Integration Tests with Multiple Methods

        [Fact]
        public async Task EndToEndScenario_CreateBookingAndCheckAvailability_WorksCorrectly()
        {
            // Arrange
            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Single Room" },
                new Room { Id = 2, Description = "Double Room" }
            };

            var existingBookings = new List<Booking>();
            var bookingsToAdd = new List<Booking>();

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(() => existingBookings.Concat(bookingsToAdd));
            _mockBookingRepository.Setup(b => b.AddAsync(It.IsAny<Booking>()))
                .Callback<Booking>(booking => 
                {
                    booking.Id = bookingsToAdd.Count + 1;
                    bookingsToAdd.Add(booking);
                });

            var startDate = DateTime.Today.AddDays(10);
            var endDate = DateTime.Today.AddDays(12);

            // Act & Assert - First booking should succeed
            var booking1 = new Booking { StartDate = startDate, EndDate = endDate, CustomerId = 1 };
            var result1 = await _bookingManager.CreateBooking(booking1);
            Assert.True(result1);
            Assert.Equal(1, booking1.RoomId);

            // Act & Assert - Second booking should succeed (different room)
            var booking2 = new Booking { StartDate = startDate, EndDate = endDate, CustomerId = 2 };
            var result2 = await _bookingManager.CreateBooking(booking2);
            Assert.True(result2);
            Assert.Equal(2, booking2.RoomId);

            // Act & Assert - Third booking should fail (no available rooms)
            var booking3 = new Booking { StartDate = startDate, EndDate = endDate, CustomerId = 3 };
            var result3 = await _bookingManager.CreateBooking(booking3);
            Assert.False(result3);

            // Act & Assert - Check fully occupied dates
            var fullyOccupiedDates = await _bookingManager.GetFullyOccupiedDates(startDate, endDate);
            Assert.Equal(3, fullyOccupiedDates.Count); // All three days should be fully occupied
        }

        #endregion
    }
}
