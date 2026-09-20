using System.ComponentModel.DataAnnotations;

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
        [Display(Name = "Description / About")]
        public string? Description { get; set; }

        [StringLength(500, ErrorMessage = "Contact Information cannot exceed 500 characters")]
        [Display(Name = "Contact Information")]
        public string? ContactInformation { get; set; }

        [Display(Name = "New Logo")]
        public IFormFile? Logo { get; set; }

        public bool IsNGO { get; set; }
    }
}
