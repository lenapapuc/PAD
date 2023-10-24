namespace MicroserviceBooking.Models
{
    public class Helper
    {
        public class Listing
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Location { get; set; }
            public string Country { get; set; }
            public decimal Price { get; set; }
            public string Amenities { get; set; }
        }
    }
}
