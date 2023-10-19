namespace Microservice1
{
    using Microservice1.Models;
    using System;
    using System.ComponentModel.DataAnnotations;
    public class Listing
    {
        [Key]
        public Guid Id { get; set; } // Adjust the data type to match the database
        public string Name { get; set; }
        public string Description { get; set; }
        public string User_Id { get; set; }
        public string Location { get; set; }
        public string Country { get; set; }
        public decimal Price { get; set; }
        public string Amenities { get; set; }  // Added Amenities property
        public ICollection<Availability> Availability { get; set; }

    }
}