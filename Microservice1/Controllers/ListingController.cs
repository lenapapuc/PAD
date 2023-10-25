using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microservice1.Models;
using Microservice1;
using static Microservice1.Models.Helper;


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

        
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

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

                // Simulated long-running operation
               // await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);

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
                            Price = availabilityItem.Price ?? listing.Price
                        };
                        if (availability.Price == 0) availability.Price = listing.Price;
                        _context.Availabilities.Add(availability);
                    }
                    await _context.SaveChangesAsync();
                }

                transaction.Commit();

                return Ok($"Listing created successfully with id {listing.Id}");

            }
            catch (OperationCanceledException)
            {
                // Handle the timeout
                return StatusCode(504, "Request timed out");
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
    public async Task<IActionResult> GetListingsInCity(string cityName)
    {
        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var cancellationToken = cts.Token;

            // Query the database to find listings in the specified city
            var listingsInCity = await _context.Listings
                .Where(listing => listing.Location == cityName)
                .ToListAsync(cancellationToken); // Use ToListAsync with the CancellationToken

            if (listingsInCity.Count == 0)
            {
                return NotFound(); // No listings found in the specified city
            }

            var responseList = CreateResponseList(listingsInCity);
            return Ok(responseList);
        }
        catch (OperationCanceledException)
        {
            // Handle the timeout
            return StatusCode(504, "Request timed out");
        }
    }


    [HttpGet]
    [Route("{Id}")]

    public async Task<IActionResult> GetListing(Guid Id)
    {
        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var cancellationToken = cts.Token;
            // Query the database to find listing with specified Id
            var listingSearched = await _context.Listings
                .Where(listing => listing.Id == Id)
                .ToListAsync(cancellationToken);

            if (listingSearched.Count == 0)
            {
                return NotFound(); // No listings found with specified Id
            }

            var responseList = CreateResponseList(listingSearched);

            return Ok(responseList);

        }
        catch(OperationCanceledException)
        {
            return StatusCode(504, "Request timed out");
        }
        
    }

    [Route("update/{id}")]
    [HttpPatch]
    public async Task<IActionResult> UpdateListing(Guid id, [FromBody] ListingPatchRequest request)
    {

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        if (request == null)
        {
            return BadRequest("Invalid data.");
        }

        var listing = await _context.Listings
            .FirstOrDefaultAsync(l => l.Id == id);

        if (listing == null)
        {
            return NotFound("Listing not found.");
        }

        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                // Apply partial updates to Listing
                if (request.Name != null)
                {
                    listing.Name = request.Name;
                }

                if (request.Description != null)
                {
                    listing.Description = request.Description;
                }

                if (request.Location != null)
                {
                    listing.Location = request.Location;
                }

                if (request.Country != null)
                {
                    listing.Country = request.Country;
                }

                if (request.Price != null)
                {
                    listing.Price = request.Price ?? listing.Price;
                }

                if (request.Amenities != null)
                {
                    listing.Amenities = request.Amenities;
                }
                // Update other Listing fields as needed

                // Save changes to the Listing
                await _context.SaveChangesAsync();
                transaction.Commit();

                // If the Availability data is provided, update Availability
                if (request.Availability != null)
                {
                    foreach (var availabilityItem in request.Availability)
                    {
                        // Find the corresponding Availability entry by Date and ListingId
                        var availability = await _context.Availabilities
                            .FirstOrDefaultAsync(a => a.ListingId == listing.Id && a.Date == availabilityItem.Date);

                        if (availability != null)
                        {
                            // Update Availability fields
                            if (availabilityItem.Price.HasValue)
                            {
                                availability.Price = availabilityItem.Price.Value;
                            }

                            availability.Available = availabilityItem.Available;
                        }
                    }

                    // Save changes to Availability
                    await _context.SaveChangesAsync();
                }

                return Ok($"Listing with ID {listing.Id} updated successfully");
            }
            catch (OperationCanceledException)
            {
                // Handle the timeout
                return StatusCode(504, "Request timed out");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
    }

    [HttpDelete]
    [Route("delete/{id}")]
  
    public async Task<IActionResult> DeleteListing(Guid id)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listing = await _context.Listings
            .FirstOrDefaultAsync(l => l.Id == id);
       

        if (listing == null)
        {
            return NotFound("Listing not found.");
        }

        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                // Remove associated Availability records
                var availabilityRecords = _context.Availabilities.Where(a => a.ListingId == listing.Id);
                _context.Availabilities.RemoveRange(availabilityRecords);

                // Remove the Listing
                _context.Listings.Remove(listing);

                await _context.SaveChangesAsync();
                transaction.Commit();

                return Ok($"Listing with ID {listing.Id} and its associated Availability records have been deleted.");
            }
            catch (OperationCanceledException)
            {
                // Handle the timeout
                return StatusCode(504, "Request timed out");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
           
        }
    }

    private List<object> CreateResponseList(List<Listing> listings) {

        var responseList = new List<object>();

        foreach (var listing in listings)
        {
            // Retrieve availability data for the current listing
            var availabilityData = _context.Availabilities
                .Where(avail => avail.ListingId == listing.Id && avail.Available == true)
                .ToDictionary(avail => avail.Date, avail => new Helper.AvailabilityData
                {
                    Date = avail.Date.Date,
                    Price = avail.Price
                });

            // Assemble the response
            var response = new
            {
                name = listing.Name,
                location = listing.Location,
                country = listing.Country,
                rating = 5, // To later calculate the rating as needed
                user_name = listing.User_Id,
                price = listing.Price,
                description = listing.Description,
                amenities = listing.Amenities,
                availability = availabilityData
            };

            responseList.Add(response);
        }
        return responseList;
    }
}
