using FluentAssertions;
using HotelBooking.Core.Entities;
using Xunit;

namespace HotelBooking.UnitTests.Entities;

public class CustomerTests
{
    [Fact]
    public void Customer_CanBeCreated_WithDefaultValues()
    {
        // Act
        var customer = new Customer();

        // Assert
        customer.Should().NotBeNull();
        customer.Id.Should().Be(0);
        customer.Name.Should().BeNull();
        customer.Email.Should().BeNull();
    }

    [Theory]
    [InlineData(1, "Alice Smith", "alice.smith@gmail.com")]
    [InlineData(100, "Bob Johnson", "bob@company.org")]
    [InlineData(999, "Charlie Brown", "charlie.brown@hotmail.com")]
    [InlineData(0, "", "")]
    [InlineData(-1, null, null)]
    [InlineData(789, "Name With Spaces", "firstname.lastname@subdomain.domain.co.uk")]
    public void Customer_CanSetProperties_WithVariousValues(int id, string name, string email)
    {
        // Act
        var customer = new Customer { Id = id, Name = name, Email = email };

        // Assert
        customer.Id.Should().Be(id);
        customer.Name.Should().Be(name);
        customer.Email.Should().Be(email);
    }

    [Fact]
    public void Customer_PropertiesCanBeModified_AfterCreation()
    {
        // Arrange
        var customer = new Customer { Id = 1, Name = "Old Name", Email = "old@email.com" };

        // Act
        customer.Id = 999;
        customer.Name = "New Name";
        customer.Email = "new@email.com";

        // Assert
        customer.Id.Should().Be(999);
        customer.Name.Should().Be("New Name");
        customer.Email.Should().Be("new@email.com");
    }
}
