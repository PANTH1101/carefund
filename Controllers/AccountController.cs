using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NGODonationSystem.Data;
using NGODonationSystem.Models;
using NGODonationSystem.ViewModels;

namespace NGODonationSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // GET: Account/RegisterDonor
        [HttpGet]
        public IActionResult RegisterDonor()
        {
            return View();
        }

        // POST: Account/RegisterDonor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterDonor(DonorRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "This email is already registered");
                    return View(model);
                }

                // Create ApplicationUser
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber
                };

                // Create user with password
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Assign Donor role
                    await _userManager.AddToRoleAsync(user, "Donor");

                    // Sign in the user
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return RedirectToAction("Index", "Home");
                }

                // Add errors to ModelState
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // GET: Account/RegisterNGO
        [HttpGet]
        public IActionResult RegisterNGO()
        {
            return View();
        }

        // POST: Account/RegisterNGO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterNGO(NGORegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "This email is already registered");
                    return View(model);
                }

                // Validate logo file
                if (model.Logo != null)
                {
                    if (!IsValidImageFile(model.Logo))
                    {
                        ModelState.AddModelError("Logo", "Please upload a valid image file (jpg, jpeg, png, gif)");
                        return View(model);
                    }

                    if (model.Logo.Length > 5 * 1024 * 1024) // 5MB
                    {
                        ModelState.AddModelError("Logo", "Logo file size cannot exceed 5MB");
                        return View(model);
                    }
                }

                // Validate verification documents
                if (model.VerificationDocuments == null || model.VerificationDocuments.Count == 0)
                {
                    ModelState.AddModelError("VerificationDocuments", "Please upload at least one verification document");
                    return View(model);
                }

                foreach (var doc in model.VerificationDocuments)
                {
                    if (!IsValidDocumentFile(doc))
                    {
                        ModelState.AddModelError("VerificationDocuments", "Please upload valid document files (pdf, jpg, jpeg, png, doc, docx)");
                        return View(model);
                    }

                    if (doc.Length > 10 * 1024 * 1024) // 10MB
                    {
                        ModelState.AddModelError("VerificationDocuments", "Each document file size cannot exceed 10MB");
                        return View(model);
                    }
                }

                // Create ApplicationUser
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber
                };

                // Create user with password
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Assign NGO role
                    await _userManager.AddToRoleAsync(user, "NGO");

                    // Create NGO entity
                    var ngo = new NGO
                    {
                        Name = model.NGOName,
                        Description = model.Description,
                        ContactInformation = model.ContactInformation,
                        VerificationStatus = "Pending",
                        UserId = user.Id
                    };

                    // Handle logo upload
                    if (model.Logo != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await model.Logo.CopyToAsync(memoryStream);
                            ngo.Logo = memoryStream.ToArray();
                            ngo.LogoContentType = model.Logo.ContentType;
                        }
                    }

                    _context.NGOs.Add(ngo);
                    await _context.SaveChangesAsync();

                    // Handle verification documents
                    foreach (var doc in model.VerificationDocuments)
                    {
                        var ngoDocument = new NGODocument
                        {
                            FileName = doc.FileName,
                            ContentType = doc.ContentType,
                            NGOId = ngo.Id
                        };

                        using (var memoryStream = new MemoryStream())
                        {
                            await doc.CopyToAsync(memoryStream);
                            ngoDocument.FileData = memoryStream.ToArray();
                        }

                        _context.NGODocuments.Add(ngoDocument);
                    }

                    await _context.SaveChangesAsync();

                    // Sign in the user
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return RedirectToAction("Index", "Home");
                }

                // Add errors to ModelState
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    var roles = await _userManager.GetRolesAsync(user!);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    // Redirect based on role
                    if (roles.Contains("Admin"))
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    else if (roles.Contains("NGO"))
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    else if (roles.Contains("Donor"))
                    {
                        return RedirectToAction("Index", "Home");
                    }

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt");
            }

            return View(model);
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Helper method to validate image files
        private bool IsValidImageFile(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }

        // Helper method to validate document files
        private bool IsValidDocumentFile(IFormFile file)
        {
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }
    }
}
