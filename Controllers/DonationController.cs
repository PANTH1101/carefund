using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGODonationSystem.Data;
using NGODonationSystem.Models;
using NGODonationSystem.ViewModels;
using NGODonationSystem.Helpers;
using NGODonationSystem.Extensions;
using Razorpay.Api;
using System.Security.Cryptography;
using System.Text;

namespace NGODonationSystem.Controllers
{
    [Authorize]
    public class DonationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DonationController> _logger;

        public DonationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger<DonationController> logger)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        // GET: Donation/Create?campaignId=123
        [Authorize(Roles = "Donor")]
        public async Task<IActionResult> Create(int campaignId)
        {
            // Validate campaign
            var campaign = await _context.Campaigns
                .Include(c => c.NGO)
                .Include(c => c.Donations)
                .FirstOrDefaultAsync(c => c.Id == campaignId);

            if (campaign == null)
            {
                return NotFound("Campaign not found.");
            }

            // Check NGO is approved
            if (campaign.NGO.VerificationStatus != "Approved")
            {
                return NotFound("This campaign is not available for donations.");
            }

            // Calculate raised amount
            var raisedAmount = campaign.Donations
                .Where(d => d.Status == "Success" || d.Status == "Completed")
                .Sum(d => d.Amount);

            // Check campaign status using centralized extension
            if (!campaign.CanAcceptDonations(raisedAmount))
            {
                var status = campaign.GetStatus(raisedAmount);
                TempData["ErrorMessage"] = $"This campaign is {status} and cannot accept donations at this time.";
                return RedirectToAction("Details", "Campaign", new { id = campaignId });
            }

            // ENFORCE START DATE: Campaign must have started
            var today = DateTime.Today;
            if (campaign.StartDate > today)
            {
                TempData["ErrorMessage"] = $"This campaign has not started yet. It will begin on {campaign.StartDate:MMM dd, yyyy}.";
                return RedirectToAction("Details", "Campaign", new { id = campaignId });
            }

            ViewBag.Campaign = campaign;
            return View(new DonationViewModel { CampaignId = campaignId });
        }

        // POST: Donation/Create
        [Authorize(Roles = "Donor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DonationViewModel model)
        {
            // Re-validate campaign
            var campaign = await _context.Campaigns
                .Include(c => c.NGO)
                .Include(c => c.Donations)
                .FirstOrDefaultAsync(c => c.Id == model.CampaignId);

            if (campaign == null)
            {
                return NotFound("Campaign not found.");
            }

            // Check NGO is approved
            if (campaign.NGO.VerificationStatus != "Approved")
            {
                return NotFound("This campaign is not available for donations.");
            }

            // Calculate raised amount
            var raisedAmount = campaign.Donations
                .Where(d => d.Status == "Success" || d.Status == "Completed")
                .Sum(d => d.Amount);

            // Check campaign can accept donations
            if (!campaign.CanAcceptDonations(raisedAmount))
            {
                var status = campaign.GetStatus(raisedAmount);
                TempData["ErrorMessage"] = $"This campaign is {status} and cannot accept donations.";
                return RedirectToAction("Details", "Campaign", new { id = model.CampaignId });
            }

            // ENFORCE START DATE: Campaign must have started
            var today = DateTime.Today;
            if (campaign.StartDate > today)
            {
                TempData["ErrorMessage"] = "This campaign has not started yet.";
                return RedirectToAction("Details", "Campaign", new { id = model.CampaignId });
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Campaign = campaign;
                return View(model);
            }

            // Get donor information
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Prepare review
            var reviewModel = new DonationReviewViewModel
            {
                CampaignId = model.CampaignId,
                CampaignTitle = campaign.Title,
                NGOName = campaign.NGO.Name,
                DonorName = user.FullName,
                DonorEmail = user.Email ?? "",
                Amount = model.Amount,
                IsAnonymous = model.IsAnonymous
            };

            return View("Review", reviewModel);
        }

        // POST: Donation/ProcessPayment
        [Authorize(Roles = "Donor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(DonationReviewViewModel model)
        {
            // Re-validate campaign
            var campaign = await _context.Campaigns
                .Include(c => c.NGO)
                .Include(c => c.Donations)
                .FirstOrDefaultAsync(c => c.Id == model.CampaignId);

            if (campaign == null)
            {
                return NotFound("Campaign not found.");
            }

            // Check NGO is approved
            if (campaign.NGO.VerificationStatus != "Approved")
            {
                return NotFound("This campaign is not available.");
            }

            // Calculate raised amount
            var raisedAmount = campaign.Donations
                .Where(d => d.Status == "Success" || d.Status == "Completed")
                .Sum(d => d.Amount);

            // Check campaign can accept donations
            if (!campaign.CanAcceptDonations(raisedAmount))
            {
                var status = campaign.GetStatus(raisedAmount);
                TempData["ErrorMessage"] = $"This campaign is {status} and cannot accept donations.";
                return RedirectToAction("Details", "Campaign", new { id = model.CampaignId });
            }

            // ENFORCE START DATE: Campaign must have started
            var today = DateTime.Today;
            if (campaign.StartDate > today)
            {
                TempData["ErrorMessage"] = "This campaign has not started yet.";
                return RedirectToAction("Details", "Campaign", new { id = model.CampaignId });
            }

            // Validate amount
            if (model.Amount <= 0)
            {
                TempData["ErrorMessage"] = "Invalid donation amount.";
                return RedirectToAction("Create", new { campaignId = model.CampaignId });
            }

            // Get Razorpay credentials
            var keyId = _configuration["Razorpay:KeyId"];
            var keySecret = _configuration["Razorpay:KeySecret"];

            if (string.IsNullOrEmpty(keyId) || string.IsNullOrEmpty(keySecret))
            {
                TempData["ErrorMessage"] = "Payment gateway configuration error. Please contact administrator.";
                return RedirectToAction("Details", "Campaign", new { id = model.CampaignId });
            }

            try
            {
                // Create Razorpay client
                RazorpayClient client = new RazorpayClient(keyId, keySecret);

                // Convert amount to paise (smallest currency unit)
                int amountInPaise = (int)(model.Amount * 100);

                // Create order options
                Dictionary<string, object> options = new Dictionary<string, object>
                {
                    { "amount", amountInPaise },
                    { "currency", "INR" },
                    { "receipt", $"CF-{DateTime.Now.Ticks}" },
                    { "payment_capture", 1 }
                };

                // Create Razorpay order
                Order order = client.Order.Create(options);
                string orderId = order["id"].ToString();

                // Get donor information
                var user = await _userManager.GetUserAsync(User);

                // Pass to checkout view
                ViewBag.RazorpayKeyId = keyId;
                ViewBag.OrderId = orderId;
                ViewBag.Amount = amountInPaise;
                ViewBag.Currency = "INR";
                ViewBag.DonorName = user?.FullName ?? "";
                ViewBag.DonorEmail = user?.Email ?? "";
                ViewBag.CampaignId = model.CampaignId;
                ViewBag.CampaignTitle = campaign.Title;
                ViewBag.DonationAmount = model.Amount;
                ViewBag.IsAnonymous = model.IsAnonymous;

                return View("Checkout");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to create payment order. Please try again.";
                return RedirectToAction("Create", new { campaignId = model.CampaignId });
            }
        }

        // POST: Donation/VerifyPayment
        [Authorize(Roles = "Donor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPayment(PaymentVerificationViewModel model)
        {
            try
            {
                // Get Razorpay secret
                var keySecret = _configuration["Razorpay:KeySecret"];

                if (string.IsNullOrEmpty(keySecret))
                {
                    TempData["ErrorMessage"] = "Payment verification failed - configuration error.";
                    return RedirectToAction("Details", "Campaign", new { id = model.CampaignId });
                }

                // Verify signature
                string payload = $"{model.RazorpayOrderId}|{model.RazorpayPaymentId}";
                string expectedSignature = CalculateSignature(payload, keySecret);

                if (expectedSignature != model.RazorpaySignature)
                {
                    TempData["ErrorMessage"] = "Payment verification failed. Please contact support.";
                    return RedirectToAction("Details", "Campaign", new { id = model.CampaignId });
                }

                // Check for duplicate payment
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.RazorpayPaymentId == model.RazorpayPaymentId);

                if (existingPayment != null)
                {
                    TempData["ErrorMessage"] = "This payment has already been processed.";
                    return RedirectToAction("Details", "Campaign", new { id = model.CampaignId });
                }

                // Re-validate campaign
                var campaign = await _context.Campaigns
                    .Include(c => c.NGO)
                    .FirstOrDefaultAsync(c => c.Id == model.CampaignId);

                if (campaign == null || campaign.NGO.VerificationStatus != "Approved")
                {
                    TempData["ErrorMessage"] = "Campaign is no longer available.";
                    return RedirectToAction("Index", "Campaign");
                }

                // Get donor
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                // Generate receipt number
                var lastReceipt = await _context.Donations
                    .OrderByDescending(d => d.Id)
                    .Select(d => d.ReceiptNumber)
                    .FirstOrDefaultAsync();

                string receiptNumber = GenerateReceiptNumber(lastReceipt);

                // Create donation
                var donation = new Donation
                {
                    DonorId = user.Id,
                    NGOId = campaign.NGOId,
                    CampaignId = campaign.Id,
                    Amount = model.Amount,
                    DonationDate = DateTime.Now,
                    Status = "Success",
                    TransactionId = model.RazorpayPaymentId,
                    ReceiptNumber = receiptNumber,
                    IsAnonymous = model.IsAnonymous
                };

                _context.Donations.Add(donation);
                await _context.SaveChangesAsync();

                // Create payment record
                var payment = new Models.Payment
                {
                    DonationId = donation.Id,
                    RazorpayOrderId = model.RazorpayOrderId,
                    RazorpayPaymentId = model.RazorpayPaymentId,
                    RazorpaySignature = model.RazorpaySignature,
                    Status = "Success",
                    CreatedAt = DateTime.Now
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Generate PDF receipt and send email (failures should not affect donation success)
                try
                {
                    // Reload donation with all necessary relationships for PDF
                    var donationForReceipt = await _context.Donations
                        .Include(d => d.Donor)
                        .Include(d => d.NGO)
                        .Include(d => d.Campaign)
                        .Include(d => d.Payment)
                        .FirstOrDefaultAsync(d => d.Id == donation.Id);

                    if (donationForReceipt != null)
                    {
                        // Generate PDF
                        byte[] pdfBytes = ReceiptPdfGenerator.GenerateReceipt(donationForReceipt);

                        // Send email (failure should not affect donation)
                        var emailHelper = new EmailHelper(_configuration);
                        bool emailSent = await emailHelper.SendDonationReceiptAsync(
                            donationForReceipt.Donor.Email ?? "",
                            donationForReceipt.Donor.FullName,
                            donationForReceipt.ReceiptNumber,
                            donationForReceipt.Campaign.Title,
                            donationForReceipt.Amount,
                            donationForReceipt.DonationDate,
                            donationForReceipt.Id,
                            donationForReceipt.TransactionId ?? "",
                            pdfBytes
                        );

                        if (emailSent)
                        {
                            TempData["EmailSent"] = "Receipt sent to your email.";
                        }
                    }
                }
                catch
                {
                    // PDF/Email failure should not affect successful donation
                    // User can still download receipt manually
                }

                TempData["SuccessMessage"] = $"Thank you for your donation of ₹{model.Amount:N2}! Your receipt number is {receiptNumber}.";
                return RedirectToAction("Success", new { id = donation.Id });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Payment processing failed. Please contact support if amount was deducted.";
                return RedirectToAction("Details", "Campaign", new { id = model.CampaignId });
            }
        }

        // GET: Donation/Success/5
        [Authorize(Roles = "Donor")]
        public async Task<IActionResult> Success(int id)
        {
            var donation = await _context.Donations
                .Include(d => d.Campaign)
                .Include(d => d.NGO)
                .FirstOrDefaultAsync(d => d.Id == id && d.DonorId == _userManager.GetUserId(User));

            if (donation == null)
            {
                return NotFound();
            }

            return View(donation);
        }

        // GET: Donation/DownloadReceipt/5
        [Authorize(Roles = "Donor")]
        public async Task<IActionResult> DownloadReceipt(int id)
        {
            // Get current user
            var userId = _userManager.GetUserId(User);

            // Get donation with all necessary relationships
            var donation = await _context.Donations
                .Include(d => d.Donor)
                .Include(d => d.NGO)
                .Include(d => d.Campaign)
                .Include(d => d.Payment)
                .FirstOrDefaultAsync(d => d.Id == id);

            // Verify donation exists
            if (donation == null)
            {
                return NotFound();
            }

            // Verify donation belongs to logged-in donor (security check)
            if (donation.DonorId != userId)
            {
                return NotFound();
            }

            // Verify donation is successful
            if (donation.Status != "Success")
            {
                TempData["ErrorMessage"] = "Receipt is only available for successful donations.";
                return RedirectToAction("Success", new { id = donation.Id });
            }

            try
            {
                // Generate PDF
                byte[] pdfBytes = ReceiptPdfGenerator.GenerateReceipt(donation);

                // Return PDF file for download
                string fileName = $"CareFund-Receipt-{donation.ReceiptNumber}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                // Log the actual error for debugging
                _logger.LogError(ex, "Error generating PDF receipt for donation {DonationId}", id);
                TempData["ErrorMessage"] = "Failed to generate receipt. Please try again later.";
                return RedirectToAction("MyDonations");
            }
        }

        // ========================================
        // DONOR MY DONATIONS
        // ========================================

        // GET: Donation/MyDonations
        [Authorize(Roles = "Donor")]
        public async Task<IActionResult> MyDonations()
        {
            // Get current user ID
            var userId = _userManager.GetUserId(User);

            // Get all donations by this donor
            var donations = await _context.Donations
                .Include(d => d.Campaign)
                .Include(d => d.NGO)
                .Where(d => d.DonorId == userId)
                .OrderByDescending(d => d.DonationDate)
                .ToListAsync();

            return View(donations);
        }

        // GET: Donation/DonorDetails/5
        [Authorize(Roles = "Donor")]
        public async Task<IActionResult> DonorDetails(int id)
        {
            // Get current user ID
            var userId = _userManager.GetUserId(User);

            // Get donation with all necessary relationships
            var donation = await _context.Donations
                .Include(d => d.Donor)
                .Include(d => d.Campaign)
                .Include(d => d.NGO)
                .Include(d => d.Payment)
                .FirstOrDefaultAsync(d => d.Id == id);

            // Verify donation exists
            if (donation == null)
            {
                return NotFound();
            }

            // Verify donation belongs to logged-in donor (security check)
            if (donation.DonorId != userId)
            {
                return NotFound();
            }

            return View(donation);
        }

        // ========================================
        // NGO DONATIONS RECEIVED
        // ========================================

        // GET: Donation/Received
        [Authorize(Roles = "NGO")]
        public async Task<IActionResult> Received()
        {
            // Get current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Get NGO owned by this user
            var ngo = await _context.NGOs
                .FirstOrDefaultAsync(n => n.UserId == user.Id);

            if (ngo == null)
            {
                return NotFound("NGO profile not found.");
            }

            // Get all successful donations for this NGO
            var donations = await _context.Donations
                .Include(d => d.Donor)
                .Include(d => d.Campaign)
                .Where(d => d.NGOId == ngo.Id)
                .Where(d => d.Status == "Success")
                .OrderByDescending(d => d.DonationDate)
                .Select(d => new DonationItemViewModel
                {
                    DonationId = d.Id,
                    DonorName = d.IsAnonymous ? "Anonymous" : d.Donor.FullName,
                    IsAnonymous = d.IsAnonymous,
                    CampaignName = d.Campaign.Title,
                    Amount = d.Amount,
                    DonationDate = d.DonationDate,
                    Status = d.Status,
                    TransactionId = d.TransactionId,
                    ReceiptNumber = d.ReceiptNumber
                })
                .ToListAsync();

            // Calculate totals
            var totalDonations = await _context.Donations
                .Where(d => d.NGOId == ngo.Id)
                .Where(d => d.Status == "Success")
                .CountAsync();

            var totalAmount = await _context.Donations
                .Where(d => d.NGOId == ngo.Id)
                .Where(d => d.Status == "Success")
                .SumAsync(d => (decimal?)d.Amount) ?? 0;

            var viewModel = new ReceivedDonationsViewModel
            {
                Donations = donations,
                TotalDonations = totalDonations,
                TotalAmount = totalAmount,
                NGOName = ngo.Name
            };

            return View(viewModel);
        }

        // GET: Donation/Details/5
        [Authorize(Roles = "NGO")]
        public async Task<IActionResult> Details(int id)
        {
            // Get current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Get NGO owned by this user
            var ngo = await _context.NGOs
                .FirstOrDefaultAsync(n => n.UserId == user.Id);

            if (ngo == null)
            {
                return NotFound("NGO profile not found.");
            }

            // Get donation - verify it belongs to this NGO
            var donation = await _context.Donations
                .Include(d => d.Donor)
                .Include(d => d.Campaign)
                .Include(d => d.NGO)
                .Include(d => d.Payment)
                .Where(d => d.Id == id)
                .Where(d => d.NGOId == ngo.Id) // Security: Only this NGO's donations
                .FirstOrDefaultAsync();

            if (donation == null)
            {
                return NotFound("Donation not found or access denied.");
            }

            return View(donation);
        }

        // ========================================
        // HELPER METHODS
        // ========================================

        // Helper: Calculate HMAC SHA256 signature
        private string CalculateSignature(string payload, string secret)
        {
            var encoding = new UTF8Encoding();
            byte[] keyBytes = encoding.GetBytes(secret);
            byte[] messageBytes = encoding.GetBytes(payload);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(messageBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        // Helper: Generate receipt number
        private string GenerateReceiptNumber(string? lastReceipt)
        {
            int nextNumber = 1;

            if (!string.IsNullOrEmpty(lastReceipt))
            {
                // Extract number from format: CF-2026-000001
                var parts = lastReceipt.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"CF-{DateTime.Now.Year}-{nextNumber:D6}";
        }
    }
}
