using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGODonationSystem.Data;
using NGODonationSystem.Models;
using NGODonationSystem.ViewModels;
using NGODonationSystem.Extensions;

namespace NGODonationSystem.Controllers
{
    [Authorize(Roles = "Donor")]
    public class DonorDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DonorDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: DonorDashboard/Index
        public async Task<IActionResult> Index()
        {
            // Get authenticated donor user ID (security: never trust browser)
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var viewModel = new DonorDashboardViewModel
            {
                DonorName = user.FullName
            };

            // ============================================
            // SUMMARY STATISTICS
            // ============================================

            // Get all successful/completed donations for this donor
            var successfulDonations = await _context.Donations
                .Include(d => d.Campaign)
                .Where(d => d.DonorId == userId)
                .Where(d => d.Status == "Success")
                .ToListAsync();

            // Total Donated
            viewModel.TotalDonated = successfulDonations.Sum(d => d.Amount);

            // Total Donations (count)
            viewModel.TotalDonations = successfulDonations.Count;

            // Campaigns Supported (distinct campaign count)
            viewModel.CampaignsSupported = successfulDonations
                .Select(d => d.CampaignId)
                .Distinct()
                .Count();

            // ============================================
            // RECENT DONATIONS (Latest 5)
            // ============================================

            viewModel.RecentDonations = await _context.Donations
                .Include(d => d.Campaign)
                .Include(d => d.NGO)
                .Where(d => d.DonorId == userId)
                .Where(d => d.Status == "Success")
                .OrderByDescending(d => d.DonationDate)
                .Take(5)
                .Select(d => new RecentDonationItem
                {
                    DonationId = d.Id,
                    CampaignTitle = d.Campaign.Title,
                    NGOName = d.NGO.Name,
                    Amount = d.Amount,
                    DonationDate = d.DonationDate,
                    Status = d.Status
                })
                .ToListAsync();

            // ============================================
            // ACTIVE CAMPAIGNS (Up to 3)
            // ============================================

            var today = DateTime.Today;

            // Get active campaigns using the same logic as public discovery
            var activeCampaignsQuery = _context.Campaigns
                .Include(c => c.NGO)
                .Include(c => c.Donations)
                .Where(c => c.NGO.VerificationStatus == "Approved")
                .Where(c => c.StartDate <= today)
                .Where(c => c.Deadline >= today);

            var activeCampaignsWithData = await activeCampaignsQuery
                .OrderByDescending(c => c.StartDate)
                .Take(10) // Get a few extra to filter completed ones
                .Select(c => new
                {
                    Campaign = c,
                    RaisedAmount = c.Donations
                        .Where(d => d.Status == "Success")
                        .Sum(d => d.Amount)
                })
                .ToListAsync();

            // Filter out completed campaigns and take 3
            var activeCampaigns = activeCampaignsWithData
                .Where(x => x.RaisedAmount < x.Campaign.TargetAmount)
                .Take(3)
                .Select(x => new ActiveCampaignItem
                {
                    CampaignId = x.Campaign.Id,
                    Title = x.Campaign.Title,
                    NGOName = x.Campaign.NGO.Name,
                    Image = x.Campaign.Image,
                    ImageContentType = x.Campaign.ImageContentType,
                    TargetAmount = x.Campaign.TargetAmount,
                    RaisedAmount = x.RaisedAmount,
                    Deadline = x.Campaign.Deadline,
                    ProgressPercentage = x.Campaign.TargetAmount > 0
                        ? (int)Math.Min((x.RaisedAmount / x.Campaign.TargetAmount) * 100, 100)
                        : 0
                })
                .ToList();

            viewModel.ActiveCampaigns = activeCampaigns;

            return View(viewModel);
        }
    }
}
