namespace NGODonationSystem.ViewModels
{
    public class ReceivedDonationsViewModel
    {
        public List<DonationItemViewModel> Donations { get; set; } = new List<DonationItemViewModel>();
        public int TotalDonations { get; set; }
        public decimal TotalAmount { get; set; }
        public string NGOName { get; set; } = string.Empty;
    }

    public class DonationItemViewModel
    {
        public int DonationId { get; set; }
        public string DonorName { get; set; } = string.Empty;
        public bool IsAnonymous { get; set; }
        public string CampaignName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime DonationDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
    }
}
