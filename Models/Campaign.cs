namespace NGODonationSystem.Models
{
    public class Campaign
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public byte[]? Image { get; set; }

        public string? ImageContentType { get; set; }

        public decimal TargetAmount { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime Deadline { get; set; }

        public int NGOId { get; set; }

        public NGO NGO { get; set; } = null!;

        public ICollection<Donation> Donations { get; set; } = new List<Donation>();
    }
}