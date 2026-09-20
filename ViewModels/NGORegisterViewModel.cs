using System.ComponentModel.DataAnnotations;

namespace NGODonationSystem.ViewModels
{
    public class NGORegisterViewModel
    {
        // Account Information
        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Password and Confirm Password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // NGO Information
        [Required(ErrorMessage = "NGO Name is required")]
        [StringLength(200, ErrorMessage = "NGO Name cannot exceed 200 characters")]
        [Display(Name = "NGO Name")]
        public string NGOName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        [Display(Name = "Description / About")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact Information is required")]
        [StringLength(500, ErrorMessage = "Contact Information cannot exceed 500 characters")]
        [Display(Name = "Contact Information")]
        public string ContactInformation { get; set; } = string.Empty;

        [Display(Name = "Logo")]
        public IFormFile? Logo { get; set; }

        [Required(ErrorMessage = "At least one verification document is required")]
        [Display(Name = "Verification Documents")]
        public List<IFormFile> VerificationDocuments { get; set; } = new List<IFormFile>();
    }
}
