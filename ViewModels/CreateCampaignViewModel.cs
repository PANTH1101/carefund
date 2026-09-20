using System.ComponentModel.DataAnnotations;

namespace NGODonationSystem.ViewModels
{
    public class CreateCampaignViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        [Display(Name = "Campaign Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
        [Display(Name = "Campaign Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;

        [Display(Name = "Campaign Image")]
        public IFormFile? Image { get; set; }

        [Required(ErrorMessage = "Target Amount is required")]
        [Range(1, 100000000, ErrorMessage = "Target Amount must be greater than 0")]
        [Display(Name = "Target Amount (₹)")]
        public decimal TargetAmount { get; set; }

        [Required(ErrorMessage = "Start Date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Deadline is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Deadline")]
        public DateTime Deadline { get; set; } = DateTime.Today.AddDays(30);
    }
}
