using MicroserviceBooking.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Newtonsoft.Json;

namespace MicroserviceBooking.Controllers
{
    [Route("bookings")]
    [ApiController]
    public class BookingController : ControllerBase
    {
     
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        [HttpPost]
        [Route("book")]
        public async Task<IActionResult> CreateBooking([FromBody] Booking request)
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
                    // Create the Booking entity
                    var booking = new Booking
                    {
                        ListingId = request.ListingId,
                        UserId = request.UserId,
                        StartDate = request.StartDate,  
                        EndDate = request.EndDate,
                        PaymentMethod = request.PaymentMethod
                    };

                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync();

                    transaction.Commit();

                    return Ok($"Booking created successfully with id {booking.Id}");

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
        [Route("{Id}")]
        public async Task<IActionResult> GetBooking(Guid Id)
        {
            // Query the database to find the booking with the specified Id
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == Id);

            if (booking == null)
            {
                return NotFound(); // No booking found with the specified Id
            }

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Make an HTTP GET request to the Listings API to retrieve listing details
            var httpClient = new HttpClient();
            var listingId = booking.ListingId; // Get the ListingId from the booking

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);
                var listingResponse = await httpClient.GetStringAsync($"http://localhost:5114/listings/{listingId}");

                // Deserialize the listing response from the external API
                var listingDetails = JsonConvert.DeserializeObject<List<Helper.Listing>>(listingResponse);


                // Create a response object that combines data from the booking and listing
                var response = new
                {
                    BookingId = booking.Id,
                    StartDate = booking.StartDate,
                    EndDate = booking.EndDate,
                    ListingId = booking.ListingId,
                    PaymentMethod = booking.PaymentMethod,
                    ListingDetails = listingDetails
                };

                return Ok(response);
            }
            catch (OperationCanceledException)
            {
                // Handle the timeout
                return StatusCode(504, "Request timed out");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error while fetching listing details: " + ex.Message);
            }
        }


    }

}
