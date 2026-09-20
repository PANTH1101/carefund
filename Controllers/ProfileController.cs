using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGODonationSystem.Data;
using NGODonationSystem.Models;
using NGODonationSystem.ViewModels;

namespace NGODonationSystem.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: Profile/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Role = roles.FirstOrDefault() ?? "Unknown";

            // If user is NGO, load NGO data
            if (roles.Contains("NGO"))
            {
                var ngo = await _context.NGOs
                    .FirstOrDefaultAsync(n => n.UserId == user.Id);

                ViewBag.NGO = ngo;
            }

            return View(user);
        }

        // GET: Profile/Edit
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var isNGO = roles.Contains("NGO");

            var model = new EditProfileViewModel
            {
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                IsNGO = isNGO
            };

            // If user is NGO, load NGO data
            if (isNGO)
            {
                var ngo = await _context.NGOs
                    .FirstOrDefaultAsync(n => n.UserId == user.Id);

                if (ngo != null)
                {
                    model.NGOName = ngo.Name;
                    model.Description = ngo.Description;
                    model.ContactInformation = ngo.ContactInformation;
                }
            }

            return View(model);
        }

        // POST: Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                // Update user information
                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    // If user is NGO, update NGO data
                    if (model.IsNGO)
                    {
                        var ngo = await _context.NGOs
                            .FirstOrDefaultAsync(n => n.UserId == user.Id);

                        if (ngo != null)
                        {
                            ngo.Name = model.NGOName ?? ngo.Name;
                            ngo.Description = model.Description ?? ngo.Description;
                            ngo.ContactInformation = model.ContactInformation ?? ngo.ContactInformation;

                            // Handle logo upload
                            if (model.Logo != null)
                            {
                                if (IsValidImageFile(model.Logo))
                                {
                                    if (model.Logo.Length <= 5 * 1024 * 1024) // 5MB
                                    {
                                        using (var memoryStream = new MemoryStream())
                                        {
                                            await model.Logo.CopyToAsync(memoryStream);
                                            ngo.Logo = memoryStream.ToArray();
                                            ngo.LogoContentType = model.Logo.ContentType;
                                        }
                                    }
                                    else
                                    {
                                        ModelState.AddModelError("Logo", "Logo file size cannot exceed 5MB");
                                        return View(model);
                                    }
                                }
                                else
                                {
                                    ModelState.AddModelError("Logo", "Please upload a valid image file (jpg, jpeg, png, gif)");
                                    return View(model);
                                }
                            }

                            await _context.SaveChangesAsync();
                        }
                    }

                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // Helper method to validate image files
        private bool IsValidImageFile(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }

        // GET: Profile/VerificationStatus (NGO only)
        [HttpGet]
        [Authorize(Roles = "NGO")]
        public async Task<IActionResult> VerificationStatus()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var ngo = await _context.NGOs
                .FirstOrDefaultAsync(n => n.UserId == user.Id);

            if (ngo == null)
            {
                return NotFound("NGO profile not found.");
            }

            return View(ngo);
        }
    }
}
