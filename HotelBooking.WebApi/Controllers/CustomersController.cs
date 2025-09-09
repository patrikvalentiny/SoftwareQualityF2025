using HotelBooking.Core.Entities;
using HotelBooking.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;


namespace HotelBooking.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class CustomersController(IRepository<Customer> repository) : Controller
{
    // GET: customers
    [HttpGet]
    public async Task<IEnumerable<Customer>> Get()
    {
        return await repository.GetAllAsync();
    }

}
