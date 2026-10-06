namespace NGODonationSystem.ViewModels
{
    public class DonorDashboardViewModel
    {
        // Summary Statistics
        public decimal TotalDonated { get; set; }
        public int TotalDonations { get; set; }
        public int CampaignsSupported { get; set; }

        // Recent Donations
        public List<RecentDonationItem> RecentDonations { get; set; } = new List<RecentDonationItem>();

        // Active Campaigns
        public List<ActiveCampaignItem> ActiveCampaigns { get; set; } = new List<ActiveCampaignItem>();

        // Donor Info
        public string DonorName { get; set; } = string.Empty;
    }

    public class RecentDonationItem
    {
        public int DonationId { get; set; }
        public string CampaignTitle { get; set; } = string.Empty;
        public string NGOName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime DonationDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ActiveCampaignItem
    {
        public int CampaignId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string NGOName { get; set; } = string.Empty;
        public byte[]? Image { get; set; }
        public string? ImageContentType { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal RaisedAmount { get; set; }
        public DateTime Deadline { get; set; }
        public int ProgressPercentage { get; set; }
    }
}
