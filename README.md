# Mini Project Part 1: What We Have Implemented

## Overview

This project implements a clean, async hotel booking system with a strong focus on unit testing and testability. All core business logic in the `HotelBooking.Core` project is covered by unit tests, and the code is designed for easy testing and maintainability.

## What Has Been Implemented

- **Comprehensive Unit Tests:**
  - All business logic in `HotelBooking.Core` is covered by unit tests found in the `HotelBooking.UnitTests` project.
  - Test classes include:
    - `CreateBookingTests`: Tests for creating bookings, including edge cases and validation.
    - `FindAvailableRoomTests`: Tests for finding available rooms given a date range and existing bookings.
    - `GetFullyOccupiedDatesTests`: Tests for identifying fully occupied dates in a given range.

- **Use of Mocking Framework:**
  - All dependencies on repositories are mocked using Moq, ensuring that business logic is tested in isolation.
  - Example: `Mock<IRepository<Booking>>` and `Mock<IRepository<Room>>` are used in all service tests.

- **Data-driven Unit Testing:**
  - Where appropriate, `[Theory]` and `[InlineData]` are used to test multiple scenarios with a single test method.
  - Example: Parameterized tests for invalid date ranges and booking overlaps.

- **Test Data Generation:**
  - The Bogus library is used to generate realistic test data for bookings and rooms, improving test reliability and coverage.

- **Design for Testability:**
  - The core logic uses interfaces and dependency injection, making it easy to substitute real implementations with mocks or fakes in tests.
  - Business logic is separated from data access and presentation layers.

## Example Highlights

- **Mocking Example:**

  ```csharp
  var mockRepo = new Mock<IRepository<Booking>>();
  mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());
  // ...use mockRepo.Object in business logic...
  ```

- **Data-driven Test Example:**

  ```csharp
  [Theory]
  [InlineData(1, 2)]
  [InlineData(3, 4)]
  public void ExampleTheoryTest(int a, int b)
  {
      // ...test logic...
  }
  ```

## Summary

This solution demonstrates best practices in unit testing and testable design. All core business logic is robustly tested, and the codebase is structured for maintainability and future extension.
