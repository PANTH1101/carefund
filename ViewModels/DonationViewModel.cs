using System.ComponentModel.DataAnnotations;

namespace NGODonationSystem.ViewModels
{
    public class DonationViewModel
    {
        public int CampaignId { get; set; }

        [Required(ErrorMessage = "Donation amount is required")]
        [Range(1, 10000000, ErrorMessage = "Amount must be greater than 0")]
        [Display(Name = "Donation Amount (₹)")]
        public decimal Amount { get; set; }

        [Display(Name = "Make this donation anonymous")]
        public bool IsAnonymous { get; set; }
    }
}
