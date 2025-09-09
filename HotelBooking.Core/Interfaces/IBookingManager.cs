using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBooking.Core.Entities;

namespace HotelBooking.Core.Interfaces;

public interface IBookingManager
{
    Task<bool> CreateBooking(Booking booking);
    Task<int> FindAvailableRoom(DateTime startDate, DateTime endDate);
    Task<List<DateTime>> GetFullyOccupiedDates(DateTime startDate, DateTime endDate);
}

