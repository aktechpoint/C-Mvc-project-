//using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
//using System.ComponentModel.DataAnnotations.Schema;
//using System.ComponentModel.DataAnnotations;

//namespace Id_card.Models
//{
//    public class Users
//    {
//        [Key]
//        public int UserId { get; set; }

//        [Required(ErrorMessage = "Full Name is required")]
//        [MaxLength(100)]
//        public string FullName { get; set; }

//        [Required, EmailAddress]
//        [MaxLength(150)]
//        public string Email { get; set; }

//        [NotMapped]
//        public string Username => Email;  // auto-generated username same as email

//        [MaxLength(15)]
//        public string MobileNo { get; set; }

//        [MaxLength(255)]
//        public string Image { get; set; }  // image path or URL

//        [Required]
//        public string PasswordHash { get; set; }

//        [MaxLength(45)]
//        public string IPAddress { get; set; }

//        public bool IsActive { get; set; } = true;

//        [MaxLength(50)]
//        public string Role { get; set; } = "User";  // Admin, HR, Employee

//        // Address relationship
//        public int? AddressId { get; set; }
//        public Address Address { get; set; }

//        public DateTime? LastLogin { get; set; }

//        public DateTime CreatedAt { get; set; } = DateTime.Now;

//        public DateTime UpdatedAt { get; set; } = DateTime.Now;
//    }
//}

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Id_card.Models
{
    public class Users
    {
        [Key]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        [MaxLength(100)]
        public string? FullName { get; set; }

        [Required, EmailAddress]
        [MaxLength(150)]
        public string? Email { get; set; }

        [NotMapped]
        public string? Username => Email;

        [MaxLength(15)]
        public string? MobileNo { get; set; }

        [MaxLength(255)]
        public string? Image { get; set; }

        [Required]
        public string? PasswordHash { get; set; }

        [MaxLength(45)]
        public string? IPAddress { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(50)]
        public string? Role { get; set; } = "User";

        public int? AddressId { get; set; }
        public Address? Address { get; set; }

        public DateTime? LastLogin { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

