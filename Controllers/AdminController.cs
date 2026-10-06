using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGODonationSystem.Data;

namespace NGODonationSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/NGOs
        public async Task<IActionResult> NGOs(string status = "All")
        {
            ViewBag.CurrentFilter = status;

            IQueryable<NGODonationSystem.Models.NGO> query = _context.NGOs
                .Include(n => n.User)
                .Include(n => n.Documents);

            if (status != "All")
            {
                query = query.Where(n => n.VerificationStatus == status);
            }

            var ngos = await query.OrderByDescending(n => n.Id).ToListAsync();

            return View(ngos);
        }

        // GET: Admin/NGODetails/5
        public async Task<IActionResult> NGODetails(int id)
        {
            var ngo = await _context.NGOs
                .Include(n => n.User)
                .Include(n => n.Documents)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (ngo == null)
            {
                return NotFound();
            }

            return View(ngo);
        }

        // GET: Admin/DownloadDocument/5
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var document = await _context.NGODocuments.FindAsync(id);

            if (document == null)
            {
                return NotFound();
            }

            return File(document.FileData, document.ContentType, document.FileName);
        }

        // POST: Admin/ApproveNGO/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveNGO(int id)
        {
            var ngo = await _context.NGOs.FindAsync(id);

            if (ngo == null)
            {
                return NotFound();
            }

            // ENFORCE: Only Pending NGOs can be approved
            if (ngo.VerificationStatus != "Pending")
            {
                TempData["ErrorMessage"] = $"Cannot approve NGO '{ngo.Name}'. Only NGOs with Pending status can be approved. Current status: {ngo.VerificationStatus}";
                return RedirectToAction(nameof(NGODetails), new { id = id });
            }

            ngo.VerificationStatus = "Approved";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"NGO '{ngo.Name}' has been approved successfully.";

            return RedirectToAction(nameof(NGODetails), new { id = id });
        }

        // POST: Admin/RejectNGO/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectNGO(int id)
        {
            var ngo = await _context.NGOs.FindAsync(id);

            if (ngo == null)
            {
                return NotFound();
            }

            // ENFORCE: Only Pending NGOs can be rejected
            if (ngo.VerificationStatus != "Pending")
            {
                TempData["ErrorMessage"] = $"Cannot reject NGO '{ngo.Name}'. Only NGOs with Pending status can be rejected. Current status: {ngo.VerificationStatus}";
                return RedirectToAction(nameof(NGODetails), new { id = id });
            }

            ngo.VerificationStatus = "Rejected";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"NGO '{ngo.Name}' has been rejected.";

            return RedirectToAction(nameof(NGODetails), new { id = id });
        }
    }
}
