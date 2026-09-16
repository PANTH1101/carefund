namespace NGODonationSystem.Models
{
    public class NGO
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string ContactInformation { get; set; } = string.Empty;

        public byte[]? Logo { get; set; }

        public string? LogoContentType { get; set; }

        public string VerificationStatus { get; set; } = "Pending";

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;

        public ICollection<NGODocument> Documents { get; set; } = new List<NGODocument>();

        public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
    }
}