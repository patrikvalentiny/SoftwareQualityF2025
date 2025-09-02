using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBooking.Core;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests
{
    /// <summary>
    /// Data-driven tests that comprehensively test various scenarios using Theory and MemberData attributes.
    /// These tests demonstrate extensive use of data-driven testing as required.
    /// </summary>
    public class BookingManagerDataDrivenTests
    {
        private readonly Mock<IRepository<Booking>> _mockBookingRepository;
        private readonly Mock<IRepository<Room>> _mockRoomRepository;
        private readonly BookingManager _bookingManager;

        public BookingManagerDataDrivenTests()
        {
            _mockBookingRepository = new Mock<IRepository<Booking>>();
            _mockRoomRepository = new Mock<IRepository<Room>>();
            _bookingManager = new BookingManager(_mockBookingRepository.Object, _mockRoomRepository.Object);
        }

        #region Date Validation Tests

        [Theory]
        [MemberData(nameof(GetInvalidDateRanges))]
        public async Task FindAvailableRoom_InvalidDateRanges_ThrowsArgumentException(
            DateTime startDate, DateTime endDate, string testCaseName)
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
                _bookingManager.FindAvailableRoom(startDate, endDate));
            
            // Additional assertion to ensure we get a meaningful error message
            Assert.NotNull(exception.Message);
        }

        public static IEnumerable<object[]> GetInvalidDateRanges()
        {
            var today = DateTime.Today;
            
            return new List<object[]>
            {
                new object[] { today.AddDays(-5), today.AddDays(-1), "Both dates in the past" },
                new object[] { today, today, "Start date is today" },
                new object[] { today.AddDays(-1), today.AddDays(-1), "Both dates yesterday" },
                new object[] { today.AddDays(5), today.AddDays(3), "Start date after end date" },
                new object[] { today.AddDays(10), today.AddDays(10).AddHours(-1), "End time before start time same day" }
            };
        }

        [Theory]
        [MemberData(nameof(GetValidDateRanges))]
        public async Task FindAvailableRoom_ValidDateRanges_DoesNotThrowException(
            DateTime startDate, DateTime endDate, string testCaseName)
        {
            // Arrange
            var rooms = new List<Room> { new Room { Id = 1, Description = "Test Room" } };
            var bookings = new List<Booking>();

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act & Assert - Should not throw
            var result = await _bookingManager.FindAvailableRoom(startDate, endDate);
            Assert.Equal(1, result); // Should find available room
        }

        public static IEnumerable<object[]> GetValidDateRanges()
        {
            var today = DateTime.Today;
            
            return new List<object[]>
            {
                new object[] { today.AddDays(1), today.AddDays(1), "Single day booking tomorrow" },
                new object[] { today.AddDays(1), today.AddDays(7), "Week-long booking" },
                new object[] { today.AddDays(30), today.AddDays(60), "Month-long booking in future" },
                new object[] { today.AddDays(365), today.AddDays(400), "Far future booking" }
            };
        }

        #endregion

        #region Room Availability Scenarios

        [Theory]
        [MemberData(nameof(GetRoomAvailabilityScenarios))]
        public async Task FindAvailableRoom_VariousAvailabilityScenarios_ReturnsExpectedResult(
            List<Room> rooms, List<Booking> bookings, DateTime startDate, DateTime endDate, 
            int expectedRoomId, string scenarioDescription)
        {
            // Arrange
            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var result = await _bookingManager.FindAvailableRoom(startDate, endDate);

            // Assert
            Assert.Equal(expectedRoomId, result);
        }

        public static IEnumerable<object[]> GetRoomAvailabilityScenarios()
        {
            var baseDate = DateTime.Today.AddDays(10);
            var requestStart = baseDate;
            var requestEnd = baseDate.AddDays(2);

            var rooms = new List<Room>
            {
                new Room { Id = 1, Description = "Standard Room" },
                new Room { Id = 2, Description = "Deluxe Room" },
                new Room { Id = 3, Description = "Suite" },
                new Room { Id = 4, Description = "Presidential Suite" }
            };

            return new List<object[]>
            {
                // Scenario 1: All rooms free
                new object[]
                {
                    rooms,
                    new List<Booking>(),
                    requestStart,
                    requestEnd,
                    1,
                    "All rooms available - should return first room"
                },

                // Scenario 2: First room partially overlapping
                new object[]
                {
                    rooms,
                    new List<Booking>
                    {
                        new Booking { Id = 1, StartDate = requestStart.AddDays(1), EndDate = requestEnd.AddDays(1), RoomId = 1, IsActive = true }
                    },
                    requestStart,
                    requestEnd,
                    2,
                    "First room has overlapping booking - should return second room"
                },

                // Scenario 3: Multiple rooms with complex booking patterns
                new object[]
                {
                    rooms,
                    new List<Booking>
                    {
                        new Booking { Id = 1, StartDate = requestStart, EndDate = requestEnd, RoomId = 1, IsActive = true },
                        new Booking { Id = 2, StartDate = requestStart.AddDays(-2), EndDate = requestStart.AddDays(-1), RoomId = 2, IsActive = true }, // Past booking
                        new Booking { Id = 3, StartDate = requestEnd.AddDays(1), EndDate = requestEnd.AddDays(3), RoomId = 3, IsActive = true }, // Future booking
                        new Booking { Id = 4, StartDate = requestStart, EndDate = requestEnd, RoomId = 4, IsActive = false } // Inactive booking
                    },
                    requestStart,
                    requestEnd,
                    2,
                    "Complex booking pattern - room 2 should be available"
                },

                // Scenario 4: All rooms booked
                new object[]
                {
                    rooms.GetRange(0, 2), // Only first 2 rooms
                    new List<Booking>
                    {
                        new Booking { Id = 1, StartDate = requestStart, EndDate = requestEnd, RoomId = 1, IsActive = true },
                        new Booking { Id = 2, StartDate = requestStart, EndDate = requestEnd, RoomId = 2, IsActive = true }
                    },
                    requestStart,
                    requestEnd,
                    -1,
                    "All rooms booked - should return -1"
                },

                // Scenario 5: Room available due to inactive booking
                new object[]
                {
                    new List<Room> { new Room { Id = 1, Description = "Single Room" } },
                    new List<Booking>
                    {
                        new Booking { Id = 1, StartDate = requestStart, EndDate = requestEnd, RoomId = 1, IsActive = false }
                    },
                    requestStart,
                    requestEnd,
                    1,
                    "Room available because existing booking is inactive"
                }
            };
        }

        #endregion

        #region Fully Occupied Dates Scenarios

        [Theory]
        [MemberData(nameof(GetFullyOccupiedDateScenarios))]
        public async Task GetFullyOccupiedDates_VariousScenarios_ReturnsExpectedDates(
            List<Room> rooms, List<Booking> bookings, DateTime startDate, DateTime endDate,
            List<int> expectedOccupiedDayOffsets, string scenarioDescription)
        {
            // Arrange
            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var result = await _bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            var expectedDates = new List<DateTime>();
            foreach (var offset in expectedOccupiedDayOffsets)
            {
                expectedDates.Add(startDate.AddDays(offset));
            }

            Assert.Equal(expectedDates.Count, result.Count);
            foreach (var expectedDate in expectedDates)
            {
                Assert.Contains(expectedDate, result);
            }
        }

        public static IEnumerable<object[]> GetFullyOccupiedDateScenarios()
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
                // Scenario 1: No bookings
                new object[]
                {
                    rooms,
                    new List<Booking>(),
                    baseDate,
                    baseDate.AddDays(6),
                    new List<int>(), // No fully occupied days
                    "No bookings - no fully occupied dates"
                },

                // Scenario 2: Alternating full occupancy
                new object[]
                {
                    rooms,
                    new List<Booking>
                    {
                        // Day 0 - all rooms booked
                        new Booking { Id = 1, StartDate = baseDate, EndDate = baseDate, RoomId = 1, IsActive = true },
                        new Booking { Id = 2, StartDate = baseDate, EndDate = baseDate, RoomId = 2, IsActive = true },
                        new Booking { Id = 3, StartDate = baseDate, EndDate = baseDate, RoomId = 3, IsActive = true },

                        // Day 1 - only 2 rooms booked
                        new Booking { Id = 4, StartDate = baseDate.AddDays(1), EndDate = baseDate.AddDays(1), RoomId = 1, IsActive = true },
                        new Booking { Id = 5, StartDate = baseDate.AddDays(1), EndDate = baseDate.AddDays(1), RoomId = 2, IsActive = true },

                        // Day 2 - all rooms booked again
                        new Booking { Id = 6, StartDate = baseDate.AddDays(2), EndDate = baseDate.AddDays(2), RoomId = 1, IsActive = true },
                        new Booking { Id = 7, StartDate = baseDate.AddDays(2), EndDate = baseDate.AddDays(2), RoomId = 2, IsActive = true },
                        new Booking { Id = 8, StartDate = baseDate.AddDays(2), EndDate = baseDate.AddDays(2), RoomId = 3, IsActive = true }
                    },
                    baseDate,
                    baseDate.AddDays(2),
                    new List<int> { 0, 2 }, // Days 0 and 2 are fully occupied
                    "Alternating full occupancy pattern"
                },

                // Scenario 3: Multi-day booking spanning period
                new object[]
                {
                    rooms,
                    new List<Booking>
                    {
                        new Booking { Id = 1, StartDate = baseDate, EndDate = baseDate.AddDays(4), RoomId = 1, IsActive = true },
                        new Booking { Id = 2, StartDate = baseDate, EndDate = baseDate.AddDays(4), RoomId = 2, IsActive = true },
                        new Booking { Id = 3, StartDate = baseDate, EndDate = baseDate.AddDays(4), RoomId = 3, IsActive = true }
                    },
                    baseDate,
                    baseDate.AddDays(4),
                    new List<int> { 0, 1, 2, 3, 4 }, // All 5 days fully occupied
                    "Multi-day booking covering entire period"
                },

                // Scenario 4: Mixed active and inactive bookings
                new object[]
                {
                    rooms,
                    new List<Booking>
                    {
                        // Day 0 - mix of active and inactive
                        new Booking { Id = 1, StartDate = baseDate, EndDate = baseDate, RoomId = 1, IsActive = true },
                        new Booking { Id = 2, StartDate = baseDate, EndDate = baseDate, RoomId = 2, IsActive = false }, // Inactive
                        new Booking { Id = 3, StartDate = baseDate, EndDate = baseDate, RoomId = 3, IsActive = true },

                        // Day 1 - all active bookings
                        new Booking { Id = 4, StartDate = baseDate.AddDays(1), EndDate = baseDate.AddDays(1), RoomId = 1, IsActive = true },
                        new Booking { Id = 5, StartDate = baseDate.AddDays(1), EndDate = baseDate.AddDays(1), RoomId = 2, IsActive = true },
                        new Booking { Id = 6, StartDate = baseDate.AddDays(1), EndDate = baseDate.AddDays(1), RoomId = 3, IsActive = true }
                    },
                    baseDate,
                    baseDate.AddDays(1),
                    new List<int> { 1 }, // Only day 1 is fully occupied (day 0 has inactive booking)
                    "Mixed active and inactive bookings"
                },

                // Scenario 5: Edge case - more bookings than rooms for same day
                new object[]
                {
                    new List<Room> { new Room { Id = 1, Description = "Single Room" } },
                    new List<Booking>
                    {
                        new Booking { Id = 1, StartDate = baseDate, EndDate = baseDate, RoomId = 1, IsActive = true },
                        new Booking { Id = 2, StartDate = baseDate, EndDate = baseDate, RoomId = 1, IsActive = true }, // Double booking same room
                        new Booking { Id = 3, StartDate = baseDate, EndDate = baseDate, RoomId = 1, IsActive = true }  // Triple booking same room
                    },
                    baseDate,
                    baseDate,
                    new List<int> { 0 }, // Day should still be considered fully occupied
                    "Multiple bookings for same room same day"
                }
            };
        }

        #endregion

        #region Boundary Value Tests

        [Theory]
        [MemberData(nameof(GetBoundaryValueTestData))]
        public async Task BoundaryValueTests_VariousEdgeCases_HandleCorrectly(
            DateTime startDate, DateTime endDate, string testDescription, 
            bool shouldThrowException, Type expectedExceptionType)
        {
            // Arrange
            var rooms = new List<Room> { new Room { Id = 1, Description = "Test Room" } };
            var bookings = new List<Booking>();

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act & Assert
            if (shouldThrowException)
            {
                var exception = await Assert.ThrowsAsync(expectedExceptionType, () => 
                    _bookingManager.FindAvailableRoom(startDate, endDate));
                Assert.NotNull(exception);
            }
            else
            {
                var result = await _bookingManager.FindAvailableRoom(startDate, endDate);
                Assert.Equal(1, result); // Should find the available room
            }
        }

        public static IEnumerable<object[]> GetBoundaryValueTestData()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var dayAfter = today.AddDays(2);

            return new List<object[]>
            {
                // Valid boundary cases
                new object[] { tomorrow, tomorrow, "Minimum valid booking (1 day tomorrow)", false, null },
                new object[] { tomorrow, dayAfter, "2-day booking starting tomorrow", false, null },
                new object[] { today.AddDays(365), today.AddDays(365), "Far future single day", false, null },

                // Invalid boundary cases
                new object[] { today, today, "Booking for today", true, typeof(ArgumentException) },
                new object[] { today.AddDays(-1), today.AddDays(-1), "Booking for yesterday", true, typeof(ArgumentException) },
                new object[] { tomorrow, today, "End before start", true, typeof(ArgumentException) }
            };
        }

        #endregion

        #region Performance and Scale Tests

        [Theory]
        [MemberData(nameof(GetScaleTestData))]
        public async Task ScaleTests_LargeNumberOfRoomsAndBookings_PerformsCorrectly(
            int numberOfRooms, int numberOfBookings, DateTime startDate, DateTime endDate,
            bool expectAvailableRoom, string testDescription)
        {
            // Arrange
            var rooms = new List<Room>();
            for (int i = 1; i <= numberOfRooms; i++)
            {
                rooms.Add(new Room { Id = i, Description = $"Room {i}" });
            }

            var bookings = new List<Booking>();
            for (int i = 1; i <= numberOfBookings; i++)
            {
                bookings.Add(new Booking 
                { 
                    Id = i, 
                    StartDate = startDate, 
                    EndDate = endDate, 
                    RoomId = (i % numberOfRooms) + 1, // Distribute bookings across rooms
                    IsActive = true 
                });
            }

            _mockRoomRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            _mockBookingRepository.Setup(b => b.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            var availableRoom = await _bookingManager.FindAvailableRoom(startDate, endDate);
            var fullyOccupiedDates = await _bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            if (expectAvailableRoom)
            {
                Assert.NotEqual(-1, availableRoom);
            }
            else
            {
                Assert.Equal(-1, availableRoom);
            }

            // Verify the method completes without error (performance test)
            Assert.NotNull(fullyOccupiedDates);
        }

        public static IEnumerable<object[]> GetScaleTestData()
        {
            var testDate = DateTime.Today.AddDays(10);

            return new List<object[]>
            {
                new object[] { 10, 5, testDate, testDate, true, "10 rooms, 5 bookings - should have availability" },
                new object[] { 10, 10, testDate, testDate, false, "10 rooms, 10 bookings - no availability" },
                new object[] { 100, 50, testDate, testDate, true, "100 rooms, 50 bookings - should have availability" },
                new object[] { 1000, 999, testDate, testDate, true, "1000 rooms, 999 bookings - should have 1 available" },
                new object[] { 1000, 1000, testDate, testDate, false, "1000 rooms, 1000 bookings - no availability" }
            };
        }

        #endregion
    }
}
