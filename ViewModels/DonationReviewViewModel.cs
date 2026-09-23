namespace NGODonationSystem.ViewModels
{
    public class DonationReviewViewModel
    {
        public int CampaignId { get; set; }
        public string CampaignTitle { get; set; } = string.Empty;
        public string NGOName { get; set; } = string.Empty;
        public string DonorName { get; set; } = string.Empty;
        public string DonorEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsAnonymous { get; set; }
    }
}
