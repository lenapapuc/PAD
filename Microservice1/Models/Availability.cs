using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Microservice1.Models
{
    public class Availability
    {
        [Key]
     
        [Column(Order = 0)]
        public Guid ListingId { get; set; }

        [Key]
        [Column(Order = 1)]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        public bool Available { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Price { get; set; }
    }

}
