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
    /// Tests specifically focused on the business rules mentioned in the requirements:
    /// 1. A hotel room can be booked for a period (start date – end date) in the future 
    ///    provided that it is not already booked for one or more days during the desired period.
    /// 2. It should be easy for the user to find out whether any rooms are available during a given period.
    /// </summary>
    public class BookingManagerBusinessRulesTests
    {
        private readonly Mock<IRepository<Booking>> _mockBookingRepository;
        private readonly Mock<IRepository<Room>> _mockRoomRepository;
        private readonly BookingManager _bookingManager;

        public BookingManagerBusinessRulesTests()
        {
            _mockBookingRepository = new Mock<IRepository<Booking>>();
            _mockRoomRepository = new Mock<IRepository<Room>>();
            _bookingManager = new BookingManager(_mockBookingRepository.Object, _mockRoomRepository.Object);
        }

        #region Business Rule 1: Room booking validation

        [Theory]
        [InlineData(1, 5, 3, 7, false)] // Overlap at the end
        [InlineData(1, 5, 4, 6, false)] // Overlap in the middle
        [InlineData(3, 7, 1, 5, false)] // Overlap at the start
        [InlineData(2, 6, 3, 5, false)] // Completely contains existing booking
        [InlineData(3, 5, 2, 6, false)] // Completely contained within existing booking
        [InlineData(1, 3, 5, 7, true)]  // No overlap - before
        [InlineData(5, 7, 1, 3, true)]  // No overlap - after
        [InlineData(1, 2, 3, 4, true)]  // Adjacent periods (no overlap)
        public async Task BusinessRule1_BookingOverlapValidation_ReturnsCorrectResult(
            int requestStartOffset, int requestEndOffset, 
            int existingStartOffset, int existingEndOffset, 
            bool shouldBeAvailable)
        {
            // Arrange
            var baseDate = DateTime.Today.AddDays(10);
            var requestStart = baseDate.AddDays(requestStartOffset);
            var requestEnd = baseDate.AddDays(requestEndOffset);
            var existingStart = baseDate.AddDays(existingStartOffset);
            var existingEnd = baseDate.AddDays(existingEndOffset);

            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Test Room" }
            };

            var existingBookings = new List<Booking>
            {
                new Booking 
                { 
                    Id = 1, 
                    StartDate = existingStart, 
                    EndDate = existingEnd, 
                    RoomId = 1, 
                    IsActive = true 
                }
            };

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(existingBookings);

            // Act
            var availableRoomId = await _bookingManager.FindAvailableRoom(requestStart, requestEnd);

            // Assert
            if (shouldBeAvailable)
            {
                Assert.Equal(1, availableRoomId);
            }
            else
            {
                Assert.Equal(-1, availableRoomId);
            }
        }

        [Fact]
        public async Task BusinessRule1_BookingInThePast_ThrowsArgumentException()
        {
            // Arrange
            var pastDate = DateTime.Today.AddDays(-1);
            var futureDate = DateTime.Today.AddDays(1);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _bookingManager.FindAvailableRoom(pastDate, futureDate));
        }

        [Fact]
        public async Task BusinessRule1_BookingForToday_ThrowsArgumentException()
        {
            // Arrange
            var today = DateTime.Today;
            var tomorrow = DateTime.Today.AddDays(1);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _bookingManager.FindAvailableRoom(today, tomorrow));
        }

        [Theory]
        [MemberData(nameof(GetMultiRoomScenarios))]
        public async Task BusinessRule1_MultipleRoomsWithDifferentAvailability_ReturnsCorrectRoom(
            List<Room> rooms, List<Booking> bookings, DateTime startDate, DateTime endDate, int expectedRoomId)
        {
            // Arrange
            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var result = await _bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(expectedRoomId, result);
        }

        public static IEnumerable<object[]> GetMultiRoomScenarios()
        {
            var baseDate = DateTime.Today.AddDays(10);
            var startDate = baseDate;
            var endDate = baseDate.AddDays(2);

            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Room 1" },
                new Room { Id = 2, Description = "Room 2" },
                new Room { Id = 3, Description = "Room 3" }
            };

            return new List<object[]>
            {
                // Scenario 1: All rooms available - should return first room
                new object[] 
                { 
                    rooms, 
                    new List<Booking>(), 
                    startDate, 
                    endDate, 
                    1 
                },
                
                // Scenario 2: First room occupied, others available - should return second room
                new object[] 
                { 
                    rooms, 
                    new List<Booking>
                    {
                        new Booking { Id = 1, StartDate = startDate, EndDate = endDate, RoomId = 1, IsActive = true }
                    }, 
                    startDate, 
                    endDate, 
                    2 
                },
                
                // Scenario 3: First two rooms occupied - should return third room
                new object[] 
                { 
                    rooms, 
                    new List<Booking>
                    {
                        new Booking { Id = 1, StartDate = startDate, EndDate = endDate, RoomId = 1, IsActive = true },
                        new Booking { Id = 2, StartDate = startDate, EndDate = endDate, RoomId = 2, IsActive = true }
                    }, 
                    startDate, 
                    endDate, 
                    3 
                },
                
                // Scenario 4: All rooms occupied - should return -1
                new object[] 
                { 
                    rooms, 
                    new List<Booking>
                    {
                        new Booking { Id = 1, StartDate = startDate, EndDate = endDate, RoomId = 1, IsActive = true },
                        new Booking { Id = 2, StartDate = startDate, EndDate = endDate, RoomId = 2, IsActive = true },
                        new Booking { Id = 3, StartDate = startDate, EndDate = endDate, RoomId = 3, IsActive = true }
                    }, 
                    startDate, 
                    endDate, 
                    -1 
                }
            };
        }

        #endregion

        #region Business Rule 2: Easy availability checking

        [Theory]
        [MemberData(nameof(GetCalendarViewTestData))]
        public async Task BusinessRule2_CalendarViewFullyOccupiedDates_ReturnsCorrectDates(
            List<Room> rooms, List<Booking> bookings, DateTime startDate, DateTime endDate, 
            List<DateTime> expectedFullyOccupiedDates)
        {
            // Arrange
            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var result = await _bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.Equal(expectedFullyOccupiedDates.Count, result.Count);
            foreach (var expectedDate in expectedFullyOccupiedDates)
            {
                Assert.Contains(expectedDate, result);
            }
        }

        public static IEnumerable<object[]> GetCalendarViewTestData()
        {
            var baseDate = DateTime.Today.AddDays(1);
            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Room 1" },
                new Room { Id = 2, Description = "Room 2" }
            };

            return new List<object[]>
            {
                // Week with no bookings
                new object[]
                {
                    rooms,
                    new List<Booking>(),
                    baseDate,
                    baseDate.AddDays(6),
                    new List<DateTime>() // No fully occupied dates
                },
                
                // Week with some days fully booked
                new object[]
                {
                    rooms,
                    new List<Booking>
                    {
                        // Monday - both rooms booked
                        new Booking { Id = 1, StartDate = baseDate, EndDate = baseDate, RoomId = 1, IsActive = true },
                        new Booking { Id = 2, StartDate = baseDate, EndDate = baseDate, RoomId = 2, IsActive = true },
                        
                        // Tuesday - only one room booked
                        new Booking { Id = 3, StartDate = baseDate.AddDays(1), EndDate = baseDate.AddDays(1), RoomId = 1, IsActive = true },
                        
                        // Wednesday - both rooms booked
                        new Booking { Id = 4, StartDate = baseDate.AddDays(2), EndDate = baseDate.AddDays(2), RoomId = 1, IsActive = true },
                        new Booking { Id = 5, StartDate = baseDate.AddDays(2), EndDate = baseDate.AddDays(2), RoomId = 2, IsActive = true },
                    },
                    baseDate,
                    baseDate.AddDays(6),
                    new List<DateTime> { baseDate, baseDate.AddDays(2) } // Monday and Wednesday
                },
                
                // Month view simulation - alternating availability
                new object[]
                {
                    rooms,
                    new List<Booking>
                    {
                        // Weekend bookings (assuming baseDate is Monday)
                        new Booking { Id = 1, StartDate = baseDate.AddDays(5), EndDate = baseDate.AddDays(6), RoomId = 1, IsActive = true },
                        new Booking { Id = 2, StartDate = baseDate.AddDays(5), EndDate = baseDate.AddDays(6), RoomId = 2, IsActive = true },
                        
                        // Next weekend
                        new Booking { Id = 3, StartDate = baseDate.AddDays(12), EndDate = baseDate.AddDays(13), RoomId = 1, IsActive = true },
                        new Booking { Id = 4, StartDate = baseDate.AddDays(12), EndDate = baseDate.AddDays(13), RoomId = 2, IsActive = true },
                    },
                    baseDate,
                    baseDate.AddDays(20),
                    new List<DateTime> 
                    { 
                        baseDate.AddDays(5), baseDate.AddDays(6),  // First weekend
                        baseDate.AddDays(12), baseDate.AddDays(13) // Second weekend
                    }
                }
            };
        }

        [Fact]
        public async Task BusinessRule2_SingleDayAvailabilityCheck_WorksCorrectly()
        {
            // Arrange
            var checkDate = DateTime.Today.AddDays(10);
            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Room 1" },
                new Room { Id = 2, Description = "Room 2" }
            };

            var bookings = new List<Booking>
            {
                new Booking { Id = 1, StartDate = checkDate, EndDate = checkDate, RoomId = 1, IsActive = true }
                // Only one room booked, so day should not be fully occupied
            };

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var fullyOccupiedDates = await _bookingManager.GetFullyOccupiedDates(checkDate, checkDate);
            var availableRoom = await _bookingManager.FindAvailableRoom(checkDate, checkDate);

            // Assert
            Assert.Empty(fullyOccupiedDates); // Day should not be fully occupied
            Assert.Equal(2, availableRoom); // Room 2 should be available
        }

        [Fact]
        public async Task BusinessRule2_LongTermBookingImpact_CalculatedCorrectly()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(1);
            var endDate = DateTime.Today.AddDays(30); // Month-long period
            var longTermBookingStart = DateTime.Today.AddDays(10);
            var longTermBookingEnd = DateTime.Today.AddDays(20);

            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Room 1" },
                new Room { Id = 2, Description = "Room 2" }
            };

            var bookings = new List<Booking>
            {
                // Long-term booking occupying both rooms for 10 days
                new Booking { Id = 1, StartDate = longTermBookingStart, EndDate = longTermBookingEnd, RoomId = 1, IsActive = true },
                new Booking { Id = 2, StartDate = longTermBookingStart, EndDate = longTermBookingEnd, RoomId = 2, IsActive = true }
            };

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var fullyOccupiedDates = await _bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.Equal(11, fullyOccupiedDates.Count); // 10 days + 1 (inclusive of both start and end dates)
            
            // Verify that all dates in the booking period are included
            for (var date = longTermBookingStart; date <= longTermBookingEnd; date = date.AddDays(1))
            {
                Assert.Contains(date, fullyOccupiedDates);
            }
            
            // Verify that dates outside the booking period are not included
            Assert.DoesNotContain(longTermBookingStart.AddDays(-1), fullyOccupiedDates);
            Assert.DoesNotContain(longTermBookingEnd.AddDays(1), fullyOccupiedDates);
        }

        #endregion

        #region Edge Cases and Boundary Conditions

        [Fact]
        public async Task EdgeCase_NoRoomsAvailable_GetFullyOccupiedDatesReturnsEmptyList()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(1);
            var endDate = DateTime.Today.AddDays(3);
            var rooms = new List<Room>(); // No rooms available
            var bookings = new List<Booking>();

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var result = await _bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            // The actual implementation returns empty list when no bookings exist,
            // regardless of number of rooms
            Assert.Empty(result);
        }

        [Fact]
        public async Task EdgeCase_SameDateStartAndEnd_WorksCorrectly()
        {
            // Arrange
            var sameDate = DateTime.Today.AddDays(5);
            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Test Room" }
            };
            var bookings = new List<Booking>();

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var availableRoom = await _bookingManager.FindAvailableRoom(sameDate, sameDate);
            var fullyOccupiedDates = await _bookingManager.GetFullyOccupiedDates(sameDate, sameDate);

            // Assert
            Assert.Equal(1, availableRoom);
            Assert.Empty(fullyOccupiedDates);
        }

        [Theory]
        [InlineData(1)]   // 1 day
        [InlineData(7)]   // 1 week
        [InlineData(30)]  // 1 month
        [InlineData(365)] // 1 year
        public async Task EdgeCase_VariousBookingDurations_HandledCorrectly(int durationDays)
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(10);
            var endDate = startDate.AddDays(durationDays - 1);
            
            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Test Room" }
            };
            var bookings = new List<Booking>();

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var availableRoom = await _bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(1, availableRoom); // Should find the room regardless of duration
        }

        #endregion
    }
}
