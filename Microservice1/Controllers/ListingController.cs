using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microservice1.Models;
using Microservice1;

[Route("listings")]
[ApiController]
public class ListingController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ListingController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Route("create")]
    [HttpPost]
    public async Task<IActionResult> CreateListing([FromBody] Listing request)
    {
        if (request == null)
        {
            return BadRequest("Invalid data.");
        }

        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                // Create the Listing entity
                var listing = new Listing
                {
                    Name = request.Name,
                    Description = request.Description,
                    User_Id = request.User_Id,
                    Location = request.Location,
                    Price = request.Price,
                    Country = request.Country,
                    Amenities = request.Amenities,
                };

                _context.Listings.Add(listing);
                await _context.SaveChangesAsync();

                // Create Availability entries
                if (request.Availability != null)
                {
                    foreach (var availabilityItem in request.Availability)
                    {
                        var availability = new Availability
                        {
                            ListingId = listing.Id,
                            Date = availabilityItem.Date,
                            Available = availabilityItem.Available,
                            Price = availabilityItem.Price
                        };
                        _context.Availabilities.Add(availability);
                    }
                    await _context.SaveChangesAsync();
                }

                transaction.Commit();

                return Ok($"Listing created successfully with id {listing.Id}");

            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
    }

    [HttpGet]
    [Route("city/{cityName}")]

    public IActionResult GetListingsInCity(string cityName)
    {
        // Query the database to find listings in the specified city
        var listingsInCity = _context.Listings
            .Where(listing => listing.Location == cityName)
            .ToList();

        if (listingsInCity.Count == 0)
        {
            return NotFound(); // No listings found in the specified city
        }

        var responseList = new List<object>();
        foreach (var listing in listingsInCity)
        {
            // Retrieve availability data for the current listing
            var availabilityData = _context.Availabilities
                .Where(avail => avail.ListingId == listing.Id && avail.Available == true)
                .ToDictionary(avail => avail.Date, avail => new Helper.AvailabilityData
                {
                    Date = avail.Date.Date,
                    Price = avail.Price
                }) ;
            //Console.WriteLine(availabilityData);

            // Assemble the response
            var response = new
            {
                name = listing.Name,
                location = listing.Location,
                rating = 5, // You can calculate the rating as needed
                user_name = listing.User_Id,
                price = listing.Price,
                description = listing.Description,
                amenities = listing.Amenities,
                availability = availabilityData
            };

            responseList.Add(response);
        }

        return Ok(responseList);
    }

    [HttpGet]
    [Route("{Id}")]

    public IActionResult GetListing(Guid Id)
    {
        // Query the database to find listing with specified Id
        var listingSearched = _context.Listings
            .Where(listing => listing.Id == Id)
            .ToList();

        if (listingSearched.Count == 0)
        {
            return NotFound(); // No listings found with specified Id
        }

        var responseList = new List<object>();
        foreach (var listing in listingSearched)
        {
            // Retrieve availability data for the current listing
            var availabilityData = _context.Availabilities
                .Where(avail => avail.ListingId == listing.Id && avail.Available == true)
                .ToDictionary(avail => avail.Date, avail => new Helper.AvailabilityData
                {
                    Date = avail.Date.Date,
                    Price = avail.Price
                });
            //Console.WriteLine(availabilityData);

            // Assemble the response
            var response = new
            {
                name = listing.Name,
                location = listing.Location,
                country = listing.Country,
                rating = 5, // You can calculate the rating as needed
                user_name = listing.User_Id,
                price = listing.Price,
                description = listing.Description,
                amenities = listing.Amenities,
                availability = availabilityData
            };

            responseList.Add(response);
        }

        return Ok(responseList);
    }

}
