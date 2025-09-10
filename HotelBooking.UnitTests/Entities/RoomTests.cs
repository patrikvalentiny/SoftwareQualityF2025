using FluentAssertions;
using HotelBooking.Core.Entities;
using Xunit;

namespace HotelBooking.UnitTests.Entities;

public class RoomTests
{
    [Fact]
    public void Room_CanBeCreated_WithDefaultValues()
    {
        // Act
        var room = new Room();

        // Assert
        room.Should().NotBeNull();
        room.Id.Should().Be(0);
        room.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(1, "Standard Room")]
    [InlineData(999, "Presidential Suite")]
    [InlineData(-1, "Test Room")]
    [InlineData(0, "")]
    [InlineData(42, null)]
    public void Room_CanSetProperties_WithVariousValues(int id, string description)
    {
        // Act
        var room = new Room { Id = id, Description = description };

        // Assert
        room.Id.Should().Be(id);
        room.Description.Should().Be(description);
    }

    [Fact]
    public void Room_PropertiesCanBeModified_AfterCreation()
    {
        // Arrange
        var room = new Room { Id = 1, Description = "Old Description" };

        // Act
        room.Id = 100;
        room.Description = "New Description";

        // Assert
        room.Id.Should().Be(100);
        room.Description.Should().Be("New Description");
    }
}
