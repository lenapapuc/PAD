using System.ComponentModel.DataAnnotations;

namespace MicroserviceBooking.Models
{
    public class Booking
    {
        [Key]
        public Guid Id { get; set; }
        public Guid ListingId { get; set; }
        public string UserId { get; set; }
        public string PaymentMethod { get; set;}
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
