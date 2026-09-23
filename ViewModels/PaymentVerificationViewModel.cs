namespace NGODonationSystem.ViewModels
{
    public class PaymentVerificationViewModel
    {
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public string RazorpaySignature { get; set; } = string.Empty;
        public int CampaignId { get; set; }
        public decimal Amount { get; set; }
        public bool IsAnonymous { get; set; }
    }
}
