using System.ComponentModel.DataAnnotations;
using NGODonationSystem.ValidationAttributes;

namespace NGODonationSystem.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        // NGO-specific fields (only used if user is NGO)
        [StringLength(200, ErrorMessage = "NGO Name cannot exceed 200 characters")]
        [Display(Name = "NGO Name")]
        public string? NGOName { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        [Display(Name = "City")]
        public string? City { get; set; }

        [StringLength(100, ErrorMessage = "State cannot exceed 100 characters")]
        [Display(Name = "State")]
        public string? State { get; set; }

        [IndianPincode(ErrorMessage = "Pincode must be a valid 6-digit Indian PIN code")]
        [Display(Name = "Pincode")]
        public string? Pincode { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [StringLength(200, ErrorMessage = "Website URL cannot exceed 200 characters")]
        [Display(Name = "Website")]
        public string? Website { get; set; }

        [Display(Name = "New Logo")]
        public IFormFile? Logo { get; set; }

        public bool IsNGO { get; set; }
    }
}
