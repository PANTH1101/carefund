namespace NGODonationSystem.Models
{
    public class NGO
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        // Structured address fields
        public string Address { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string Pincode { get; set; } = string.Empty;

        public string? Website { get; set; }

        // Legacy field - kept for backward compatibility, can be deprecated later
        public string? ContactInformation { get; set; }

        public byte[]? Logo { get; set; }

        public string? LogoContentType { get; set; }

        public string VerificationStatus { get; set; } = "Pending";

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;

        public ICollection<NGODocument> Documents { get; set; } = new List<NGODocument>();

        public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
    }
}