namespace NGODonationSystem.Models
{
    public class Payment
    {
        public int Id { get; set; }

        public int DonationId { get; set; }

        public Donation Donation { get; set; } = null!;

        public string RazorpayOrderId { get; set; } = string.Empty;

        public string? RazorpayPaymentId { get; set; }

        public string? RazorpaySignature { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}