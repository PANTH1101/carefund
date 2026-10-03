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
                    .Include(n => n.Documents)
                    .FirstOrDefaultAsync(n => n.UserId == user.Id);

                if (ngo != null)
                {
                    model.NGOName = ngo.Name;
                    model.Description = ngo.Description;
                    model.ContactInformation = ngo.ContactInformation;
                    model.VerificationStatus = ngo.VerificationStatus;

                    // Load existing documents
                    model.ExistingDocuments = ngo.Documents.Select(d => new NGODocumentInfo
                    {
                        Id = d.Id,
                        FileName = d.FileName,
                        ContentType = d.ContentType
                    }).ToList();
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
                            .Include(n => n.Documents)
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

                            // Handle verification document resubmission (only for rejected NGOs)
                            bool documentsUploaded = false;
                            if (ngo.VerificationStatus == "Rejected" && model.VerificationDocuments != null && model.VerificationDocuments.Any())
                            {
                                // Validate all documents before processing
                                foreach (var file in model.VerificationDocuments)
                                {
                                    if (!IsValidVerificationDocument(file))
                                    {
                                        ModelState.AddModelError("VerificationDocuments", 
                                            $"Invalid file: {file.FileName}. Only PDF, DOC, DOCX, JPG, JPEG, PNG files are allowed.");
                                        return View(model);
                                    }

                                    if (file.Length > 10 * 1024 * 1024) // 10MB
                                    {
                                        ModelState.AddModelError("VerificationDocuments", 
                                            $"File {file.FileName} exceeds 10MB limit.");
                                        return View(model);
                                    }
                                }

                                // All documents valid, add them
                                foreach (var file in model.VerificationDocuments)
                                {
                                    using (var memoryStream = new MemoryStream())
                                    {
                                        await file.CopyToAsync(memoryStream);
                                        
                                        var document = new NGODocument
                                        {
                                            FileName = file.FileName,
                                            ContentType = file.ContentType,
                                            FileData = memoryStream.ToArray(),
                                            NGOId = ngo.Id
                                        };

                                        _context.NGODocuments.Add(document);
                                    }
                                }

                                documentsUploaded = true;
                            }

                            // Change status from Rejected to Pending if new documents uploaded
                            if (ngo.VerificationStatus == "Rejected" && documentsUploaded)
                            {
                                ngo.VerificationStatus = "Pending";
                                TempData["SuccessMessage"] = "Profile updated and verification documents resubmitted successfully! Your verification status has been changed to Pending.";
                            }
                            else
                            {
                                TempData["SuccessMessage"] = "Profile updated successfully!";
                            }

                            await _context.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Profile updated successfully!";
                    }

                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // Helper method to validate verification documents
        private bool IsValidVerificationDocument(IFormFile file)
        {
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
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
