using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGODonationSystem.Data;
using NGODonationSystem.Models;
using NGODonationSystem.ViewModels;

namespace NGODonationSystem.Controllers
{
    [Authorize(Roles = "NGO")]
    public class CampaignController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Valid campaign categories
        private readonly string[] _validCategories = new[]
        {
            "Medical", "Education", "Food", "Disaster Relief",
            "Children", "Elderly", "Animals", "Other"
        };

        public CampaignController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Campaign/MyCampaigns
        public async Task<IActionResult> MyCampaigns()
        {
            var ngo = await GetLoggedInNGOAsync();
            if (ngo == null)
            {
                return NotFound("NGO profile not found.");
            }

            var campaigns = await _context.Campaigns
                .Where(c => c.NGOId == ngo.Id)
                .Include(c => c.Donations)
                .OrderByDescending(c => c.StartDate)
                .ToListAsync();

            return View(campaigns);
        }

        // GET: Campaign/Create
        public async Task<IActionResult> Create()
        {
            var ngo = await GetLoggedInNGOAsync();
            if (ngo == null)
            {
                return NotFound("NGO profile not found.");
            }

            // Check if NGO is approved
            if (ngo.VerificationStatus != "Approved")
            {
                TempData["ErrorMessage"] = "Your NGO must be approved before you can create campaigns. Current status: " + ngo.VerificationStatus;
                return RedirectToAction("VerificationStatus", "Profile");
            }

            ViewBag.Categories = _validCategories;
            return View();
        }

        // POST: Campaign/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCampaignViewModel model)
        {
            var ngo = await GetLoggedInNGOAsync();
            if (ngo == null)
            {
                return NotFound("NGO profile not found.");
            }

            // Check if NGO is approved
            if (ngo.VerificationStatus != "Approved")
            {
                TempData["ErrorMessage"] = "Your NGO must be approved before you can create campaigns.";
                return RedirectToAction("VerificationStatus", "Profile");
            }

            // Validate category
            if (!_validCategories.Contains(model.Category))
            {
                ModelState.AddModelError("Category", "Invalid category selected.");
            }

            // Validate deadline
            if (model.Deadline <= model.StartDate)
            {
                ModelState.AddModelError("Deadline", "Deadline must be after Start Date.");
            }

            // Validate image if provided
            if (model.Image != null)
            {
                if (!IsValidImageFile(model.Image))
                {
                    ModelState.AddModelError("Image", "Please upload a valid image file (jpg, jpeg, png, gif).");
                }
                else if (model.Image.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Image", "Image file size cannot exceed 5MB.");
                }
            }

            if (ModelState.IsValid)
            {
                var campaign = new Campaign
                {
                    Title = model.Title,
                    Description = model.Description,
                    Category = model.Category,
                    TargetAmount = model.TargetAmount,
                    StartDate = model.StartDate,
                    Deadline = model.Deadline,
                    NGOId = ngo.Id
                };

                // Handle image upload
                if (model.Image != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await model.Image.CopyToAsync(memoryStream);
                        campaign.Image = memoryStream.ToArray();
                        campaign.ImageContentType = model.Image.ContentType;
                    }
                }

                _context.Campaigns.Add(campaign);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Campaign created successfully!";
                return RedirectToAction(nameof(MyCampaigns));
            }

            ViewBag.Categories = _validCategories;
            return View(model);
        }

        // GET: Campaign/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var ngo = await GetLoggedInNGOAsync();
            if (ngo == null)
            {
                return NotFound("NGO profile not found.");
            }

            var campaign = await _context.Campaigns
                .Include(c => c.NGO)
                .Include(c => c.Donations)
                .FirstOrDefaultAsync(c => c.Id == id && c.NGOId == ngo.Id);

            if (campaign == null)
            {
                return NotFound();
            }

            // Calculate raised amount from successful donations
            decimal raisedAmount = campaign.Donations
                .Where(d => d.Status == "Success" || d.Status == "Completed")
                .Sum(d => d.Amount);

            ViewBag.RaisedAmount = raisedAmount;
            ViewBag.Progress = campaign.TargetAmount > 0
                ? Math.Min((raisedAmount / campaign.TargetAmount) * 100, 100)
                : 0;
            ViewBag.DonorCount = campaign.Donations
                .Where(d => d.Status == "Success" || d.Status == "Completed")
                .Select(d => d.DonorId)
                .Distinct()
                .Count();

            // Calculate status
            ViewBag.Status = GetCampaignStatus(campaign, raisedAmount);

            return View(campaign);
        }

        // GET: Campaign/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var ngo = await GetLoggedInNGOAsync();
            if (ngo == null)
            {
                return NotFound("NGO profile not found.");
            }

            var campaign = await _context.Campaigns
                .FirstOrDefaultAsync(c => c.Id == id && c.NGOId == ngo.Id);

            if (campaign == null)
            {
                return NotFound();
            }

            var model = new EditCampaignViewModel
            {
                Id = campaign.Id,
                Title = campaign.Title,
                Description = campaign.Description,
                Category = campaign.Category,
                TargetAmount = campaign.TargetAmount,
                StartDate = campaign.StartDate,
                Deadline = campaign.Deadline,
                ExistingImage = campaign.Image,
                ExistingImageContentType = campaign.ImageContentType
            };

            ViewBag.Categories = _validCategories;
            return View(model);
        }

        // POST: Campaign/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditCampaignViewModel model)
        {
            var ngo = await GetLoggedInNGOAsync();
            if (ngo == null)
            {
                return NotFound("NGO profile not found.");
            }

            var campaign = await _context.Campaigns
                .FirstOrDefaultAsync(c => c.Id == model.Id && c.NGOId == ngo.Id);

            if (campaign == null)
            {
                return NotFound();
            }

            // Validate category
            if (!_validCategories.Contains(model.Category))
            {
                ModelState.AddModelError("Category", "Invalid category selected.");
            }

            // Validate deadline
            if (model.Deadline <= model.StartDate)
            {
                ModelState.AddModelError("Deadline", "Deadline must be after Start Date.");
            }

            // Validate new image if provided
            if (model.NewImage != null)
            {
                if (!IsValidImageFile(model.NewImage))
                {
                    ModelState.AddModelError("NewImage", "Please upload a valid image file (jpg, jpeg, png, gif).");
                }
                else if (model.NewImage.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("NewImage", "Image file size cannot exceed 5MB.");
                }
            }

            if (ModelState.IsValid)
            {
                campaign.Title = model.Title;
                campaign.Description = model.Description;
                campaign.Category = model.Category;
                campaign.TargetAmount = model.TargetAmount;
                campaign.StartDate = model.StartDate;
                campaign.Deadline = model.Deadline;

                // Handle new image upload
                if (model.NewImage != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await model.NewImage.CopyToAsync(memoryStream);
                        campaign.Image = memoryStream.ToArray();
                        campaign.ImageContentType = model.NewImage.ContentType;
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Campaign updated successfully!";
                return RedirectToAction(nameof(Details), new { id = campaign.Id });
            }

            model.ExistingImage = campaign.Image;
            model.ExistingImageContentType = campaign.ImageContentType;
            ViewBag.Categories = _validCategories;
            return View(model);
        }

        // POST: Campaign/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ngo = await GetLoggedInNGOAsync();
            if (ngo == null)
            {
                return NotFound("NGO profile not found.");
            }

            var campaign = await _context.Campaigns
                .Include(c => c.Donations)
                .FirstOrDefaultAsync(c => c.Id == id && c.NGOId == ngo.Id);

            if (campaign == null)
            {
                return NotFound();
            }

            // Check if campaign has donations
            if (campaign.Donations.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete campaign that has donations. Consider canceling it instead.";
                return RedirectToAction(nameof(Details), new { id = campaign.Id });
            }

            _context.Campaigns.Remove(campaign);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Campaign deleted successfully.";
            return RedirectToAction(nameof(MyCampaigns));
        }

        // POST: Campaign/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var ngo = await GetLoggedInNGOAsync();
            if (ngo == null)
            {
                return NotFound("NGO profile not found.");
            }

            var campaign = await _context.Campaigns
                .FirstOrDefaultAsync(c => c.Id == id && c.NGOId == ngo.Id);

            if (campaign == null)
            {
                return NotFound();
            }

            // Cancel by setting deadline to yesterday
            campaign.Deadline = DateTime.Today.AddDays(-1);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Campaign has been canceled.";
            return RedirectToAction(nameof(Details), new { id = campaign.Id });
        }

        // Helper method to get logged-in NGO
        private async Task<NGO?> GetLoggedInNGOAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return null;
            }

            return await _context.NGOs.FirstOrDefaultAsync(n => n.UserId == user.Id);
        }

        // Helper method to validate image files
        private bool IsValidImageFile(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }

        // Helper method to determine campaign status
        private string GetCampaignStatus(Campaign campaign, decimal raisedAmount)
        {
            var today = DateTime.Today;

            // Check if completed (target reached)
            if (raisedAmount >= campaign.TargetAmount)
            {
                return "Completed";
            }

            // Check if expired (deadline passed)
            if (campaign.Deadline < today)
            {
                return "Expired";
            }

            // Check if active (between start and deadline)
            if (campaign.StartDate <= today && campaign.Deadline >= today)
            {
                return "Active";
            }

            // Not yet started
            return "Scheduled";
        }
    }
}
