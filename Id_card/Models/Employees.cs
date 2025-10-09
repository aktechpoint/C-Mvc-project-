using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Id_card.Models
{
    public class Employees
    {

        [Key]
        public int EmployeeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(100)]
        public string? FatherName { get; set; }

        [MaxLength(100)]
        public string? MotherName { get; set; }

        public DateTime? DOB { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(100)]
        public string? Designation { get; set; }

        public DateTime? DateOfJoining { get; set; }

        public DateTime? IDCardIssueDate { get; set; }

        public DateTime? ValidTill { get; set; }

        [MaxLength(10)]
        public string? BloodGroup { get; set; }

        [MaxLength(255)]
        public string? Image { get; set; } // image path or URL

        [MaxLength(15)]
        public string? MobileNo { get; set; }

        [MaxLength(150)]
        [EmailAddress]
        public string? Email { get; set; }

        // Address relationship
        public int? AddressId { get; set; }
        public Address? Address { get; set; }

        [MaxLength(100)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(15)]
        public string? EmergencyContactNo { get; set; }

        public DateTime CardCreateDate { get; set; } = DateTime.Now;

        public bool PrintedStatus { get; set; } = false;       // 0 = Not printed
        public bool SentOnMailStatus { get; set; } = false;    // 0 = Not sent

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Link to user who created this employee
        public int? CreatedBy { get; set; }

        [ForeignKey("CreatedBy")]
        public Users? CreatedUser { get; set; }
    }
}
