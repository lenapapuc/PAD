using System.ComponentModel.DataAnnotations;

namespace Microservice1.Models
{
    public class Helper
    {
        public class AvailabilityData
        {
            public DateTime Date { get; set; }
            public decimal? Price { get; set; }
        }

        public class ListingPatchRequest 
        {
           
            public string? Name { get; set; }
            public string? Description { get; set; }
            public string? Location { get; set; }
            public string? Country { get; set; }
            public decimal? Price { get; set; }
            public string? Amenities { get; set; }  // Added Amenities property
            public ICollection<Availability> Availability { get; set; }
        }
    }
}
