using System.ComponentModel.DataAnnotations;

namespace Id_card.Models
{
    public class Address
    {

        [Key]
        public int AddressId { get; set; }

        [MaxLength(50)]
        public string? HouseNo { get; set; }

        [MaxLength(100)]
        public string? Street { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(10)]
        public string? Pincode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}
