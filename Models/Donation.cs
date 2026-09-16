namespace NGODonationSystem.Models
{
    public class Donation
    {
        public int Id { get; set; }

        public string DonorId { get; set; } = string.Empty;

        public ApplicationUser Donor { get; set; } = null!;

        public int NGOId { get; set; }

        public NGO NGO { get; set; } = null!;

        public int CampaignId { get; set; }

        public Campaign Campaign { get; set; } = null!;

        public decimal Amount { get; set; }

        public DateTime DonationDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? TransactionId { get; set; }

        public string ReceiptNumber { get; set; } = string.Empty;

        public Payment? Payment { get; set; }
    }
}