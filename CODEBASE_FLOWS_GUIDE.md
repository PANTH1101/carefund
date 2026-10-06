# CareFund - Codebase Flows Guide

Complete documentation of all major flows in the CareFund NGO Donation Management System.

---

## Table of Contents

1. [User Registration & Authentication](#1-user-registration--authentication)
2. [NGO Verification & Approval](#2-ngo-verification--approval)
3. [Campaign Management](#3-campaign-management)
4. [Donation Flow](#4-donation-flow)
5. [Payment Processing](#5-payment-processing)
6. [PDF Receipt Generation](#6-pdf-receipt-generation)
7. [Email Notifications](#7-email-notifications)
8. [Authorization & Role-Based Access](#8-authorization--role-based-access)
9. [Search & Filtering](#9-search--filtering)
10. [Dashboard Flows](#10-dashboard-flows)

---

## 1. User Registration & Authentication

### 1.1 Donor Registration Flow

**Entry Point:** `/Account/RegisterDonor`

**Files Involved:**
- `Controllers/AccountController.cs` → `RegisterDonor()` GET & POST
- `ViewModels/DonorRegisterViewModel.cs`
- `Views/Account/RegisterDonor.cshtml`
- `Models/ApplicationUser.cs`

**Flow Steps:**

1. **User visits registration page**
   ```
   GET /Account/RegisterDonor
   └── Returns RegisterDonor.cshtml form
   ```

2. **User submits registration form**
   ```
   POST /Account/RegisterDonor
   ├── Validates DonorRegisterViewModel
   ├── Creates ApplicationUser with role "Donor"
   ├── UserManager.CreateAsync(user, password)
   ├── UserManager.AddToRoleAsync(user, "Donor")
   ├── SignInManager.SignInAsync(user)
   └── Redirects to Campaign/Index
   ```

**Key Code:**
```csharp
// AccountController.cs - RegisterDonor POST
var user = new ApplicationUser
{
    UserName = model.Email,
    Email = model.Email,
    FullName = model.FullName,
    PhoneNumber = model.PhoneNumber,
    EmailConfirmed = true
};

var result = await _userManager.CreateAsync(user, model.Password);
await _userManager.AddToRoleAsync(user, "Donor");
await _signInManager.SignInAsync(user, isPersistent: false);
```

---

### 1.2 NGO Registration Flow

**Entry Point:** `/Account/RegisterNGO`

**Files Involved:**
- `Controllers/AccountController.cs` → `RegisterNGO()` GET & POST
- `ViewModels/NGORegisterViewModel.cs`
- `Views/Account/RegisterNGO.cshtml`
- `Models/ApplicationUser.cs`
- `Models/NGO.cs`
- `ValidationAttributes/IndianPincodeAttribute.cs`

**Flow Steps:**

1. **User visits NGO registration page**
   ```
   GET /Account/RegisterNGO
   └── Returns RegisterNGO.cshtml form
   ```

2. **User submits NGO registration with documents**
   ```
   POST /Account/RegisterNGO
   ├── Validates NGORegisterViewModel (including pincode validation)
   ├── Creates ApplicationUser with role "NGO"
   ├── Creates NGO record (status: Pending)
   ├── Handles document uploads (RegistrationCertificate, 80GCertificate, Logo)
   ├── Saves files to wwwroot/uploads/{ngo-id}/
   ├── UserManager.CreateAsync(user, password)
   ├── UserManager.AddToRoleAsync(user, "NGO")
   ├── SignInManager.SignInAsync(user)
   └── Redirects to Profile/VerificationStatus
   ```

**Key Code:**
```csharp
// AccountController.cs - RegisterNGO POST
var user = new ApplicationUser { /* ... */ };
var result = await _userManager.CreateAsync(user, model.Password);

var ngo = new NGO
{
    UserId = user.Id,
    Name = model.NGOName,
    RegistrationNumber = model.RegistrationNumber,
    Address = model.Address,
    City = model.City,
    State = model.State,
    Pincode = model.Pincode,
    Website = model.Website,
    Description = model.Description,
    VerificationStatus = VerificationStatus.Pending
};

// Handle document uploads
if (model.RegistrationCertificate != null)
{
    var fileName = SaveFile(model.RegistrationCertificate, ngo.Id, "registration");
    var doc = new NGODocument { DocumentType = "RegistrationCertificate", FilePath = fileName };
    ngo.Documents.Add(doc);
}
```

**Validation:**
- `IndianPincodeAttribute` validates 6-digit pincode format
- Email uniqueness enforced by ASP.NET Identity
- Required fields enforced by Data Annotations

---

### 1.3 Login Flow

**Entry Point:** `/Account/Login`

**Files Involved:**
- `Controllers/AccountController.cs` → `Login()` GET & POST
- `ViewModels/LoginViewModel.cs`
- `Views/Account/Login.cshtml`

**Flow Steps:**

1. **User visits login page**
   ```
   GET /Account/Login?returnUrl=/path
   └── Returns Login.cshtml form
   ```

2. **User submits credentials**
   ```
   POST /Account/Login
   ├── Validates LoginViewModel
   ├── SignInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false)
   ├── If success:
   │   ├── UserManager.GetRolesAsync(user)
   │   ├── Role-based redirect:
   │   │   ├── Admin → /Admin/Dashboard
   │   │   ├── NGO → /Profile/VerificationStatus
   │   │   └── Donor → /Campaign/Index
   │   └── Or returnUrl if specified
   └── If failure: Show error message
   ```

**Key Code:**
```csharp
// AccountController.cs - Login POST
var result = await _signInManager.PasswordSignInAsync(
    model.Email, 
    model.Password, 
    model.RememberMe, 
    lockoutOnFailure: false
);

if (result.Succeeded)
{
    var user = await _userManager.FindByEmailAsync(model.Email);
    var roles = await _userManager.GetRolesAsync(user);
    
    if (roles.Contains("Admin"))
        return RedirectToAction("Dashboard", "Admin");
    else if (roles.Contains("NGO"))
        return RedirectToAction("VerificationStatus", "Profile");
    else
        return RedirectToAction("Index", "Campaign");
}
```

---

## 2. NGO Verification & Approval

### 2.1 NGO Submits for Verification

**Entry Point:** After NGO registration

**Files Involved:**
- `Controllers/ProfileController.cs` → `VerificationStatus()`
- `Views/Profile/VerificationStatus.cshtml`
- `Models/NGO.cs` → `VerificationStatus` enum

**Flow:**
```
NGO Registration
└── Status: Pending
    ├── NGO can view status: /Profile/VerificationStatus
    ├── NGO cannot create campaigns (blocked)
    └── Waiting for admin review
```

**Key Code:**
```csharp
// ProfileController.cs - VerificationStatus
var userId = _userManager.GetUserId(User);
var ngo = await _context.NGOs
    .Include(n => n.Documents)
    .FirstOrDefaultAsync(n => n.UserId == userId);

// VerificationStatus enum values: Pending, Approved, Rejected
```

---

### 2.2 Admin Reviews NGO

**Entry Point:** `/Admin/NGOs`

**Files Involved:**
- `Controllers/AdminController.cs` → `NGOs()`, `NGODetails()`, `ApproveNGO()`, `RejectNGO()`
- `Views/Admin/NGOs.cshtml`
- `Views/Admin/NGODetails.cshtml`

**Flow Steps:**

1. **Admin views all NGOs**
   ```
   GET /Admin/NGOs
   ├── Shows list of all NGOs
   ├── Filter by status: All, Pending, Approved, Rejected
   └── Admin clicks "View Details" on an NGO
   ```

2. **Admin reviews NGO details**
   ```
   GET /Admin/NGODetails/{id}
   ├── Shows full NGO information
   ├── Shows uploaded documents (with download links)
   ├── Shows verification status
   └── Admin decides: Approve or Reject
   ```

3. **Admin approves NGO**
   ```
   POST /Admin/ApproveNGO/{id}
   ├── Sets ngo.VerificationStatus = VerificationStatus.Approved
   ├── Clears ngo.RejectionReason
   ├── Saves to database
   ├── NGO can now create campaigns
   └── Redirects back to NGO list
   ```

4. **Admin rejects NGO**
   ```
   POST /Admin/RejectNGO
   ├── Receives { ngoId, rejectionReason }
   ├── Sets ngo.VerificationStatus = VerificationStatus.Rejected
   ├── Sets ngo.RejectionReason = reason
   ├── Saves to database
   ├── NGO cannot create campaigns
   └── Returns success/error JSON
   ```

**Key Code:**
```csharp
// AdminController.cs - ApproveNGO
var ngo = await _context.NGOs.FindAsync(id);
ngo.VerificationStatus = VerificationStatus.Approved;
ngo.RejectionReason = null;
await _context.SaveChangesAsync();

// AdminController.cs - RejectNGO
var ngo = await _context.NGOs.FindAsync(ngoId);
ngo.VerificationStatus = VerificationStatus.Rejected;
ngo.RejectionReason = rejectionReason;
await _context.SaveChangesAsync();
```

**Important:** Rejected NGOs **cannot resubmit** (removed in earlier phase per requirements).

---

## 3. Campaign Management

### 3.1 Create Campaign Flow

**Entry Point:** `/Campaign/Create`

**Files Involved:**
- `Controllers/CampaignController.cs` → `Create()` GET & POST
- `ViewModels/CreateCampaignViewModel.cs`
- `Views/Campaign/Create.cshtml`
- `Models/Campaign.cs`
- `Extensions/CampaignExtensions.cs`

**Authorization Required:** NGO role + Approved verification status

**Flow Steps:**

1. **NGO clicks "Create Campaign"**
   ```
   GET /Campaign/Create
   ├── Checks: User is NGO AND status is Approved
   ├── If not approved: Redirects to VerificationStatus
   └── Returns Create.cshtml form
   ```

2. **NGO fills campaign details**
   - Title, Description, Category
   - Goal Amount (min: ₹1,000)
   - Start Date (must be today or future)
   - End Date (must be after Start Date)
   - Optional: Campaign logo image

3. **NGO submits campaign**
   ```
   POST /Campaign/Create
   ├── Validates CreateCampaignViewModel
   ├── Validates StartDate >= Today
   ├── Validates EndDate > StartDate
   ├── Creates Campaign record
   ├── Uploads logo to wwwroot/uploads/campaigns/{campaign-id}/
   ├── Sets initial values:
   │   ├── RaisedAmount = 0
   │   ├── IsCancelled = false
   │   └── NGOId = current user's NGO ID
   ├── Saves to database
   └── Redirects to Campaign/MyCampaigns
   ```

**Key Code:**
```csharp
// CampaignController.cs - Create POST
var campaign = new Campaign
{
    Title = model.Title,
    Description = model.Description,
    Category = model.Category,
    GoalAmount = model.GoalAmount,
    RaisedAmount = 0,
    StartDate = model.StartDate,
    EndDate = model.EndDate,
    NGOId = ngo.Id,
    IsCancelled = false
};

// Handle logo upload
if (model.CampaignLogo != null)
{
    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "campaigns", campaign.Id.ToString());
    Directory.CreateDirectory(uploadsFolder);
    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.CampaignLogo.FileName);
    var filePath = Path.Combine(uploadsFolder, fileName);
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await model.CampaignLogo.CopyToAsync(stream);
    }
    campaign.LogoPath = $"/uploads/campaigns/{campaign.Id}/{fileName}";
}

_context.Campaigns.Add(campaign);
await _context.SaveChangesAsync();
```

---

### 3.2 Campaign Status Management

**Files Involved:**
- `Extensions/CampaignExtensions.cs` → `GetStatus()`, `CanAcceptDonations()`
- `Models/Campaign.cs`

**Campaign Status Logic:**

```csharp
// CampaignExtensions.cs - GetStatus()
public static string GetStatus(this Campaign campaign)
{
    if (campaign.IsCancelled)
        return "Cancelled";
    
    var now = DateTime.UtcNow.Date;
    
    if (now < campaign.StartDate)
        return "Upcoming";
    
    if (now > campaign.EndDate)
        return "Completed";
    
    if (campaign.RaisedAmount >= campaign.GoalAmount)
        return "Goal Reached";
    
    return "Active";
}

// CampaignExtensions.cs - CanAcceptDonations()
public static bool CanAcceptDonations(this Campaign campaign)
{
    var status = campaign.GetStatus();
    return status == "Active" || status == "Goal Reached";
}
```

**Status Types:**
- **Upcoming:** StartDate is in the future
- **Active:** Between StartDate and EndDate, not cancelled, goal not reached
- **Goal Reached:** RaisedAmount >= GoalAmount (can still accept donations)
- **Completed:** EndDate has passed
- **Cancelled:** IsCancelled = true

---

### 3.3 Edit Campaign Flow

**Entry Point:** `/Campaign/Edit/{id}`

**Authorization:** NGO must own the campaign + No donations received yet

**Flow Steps:**

1. **NGO clicks "Edit" on their campaign**
   ```
   GET /Campaign/Edit/{id}
   ├── Verifies: Campaign belongs to current NGO
   ├── Checks: Campaign has NO donations
   ├── If donations exist: Redirects with error
   └── Returns Edit.cshtml form
   ```

2. **NGO modifies campaign details**
   - Can change: Title, Description, Category, Goal, Dates, Logo
   - Cannot edit campaigns with donations (business rule)

3. **NGO submits changes**
   ```
   POST /Campaign/Edit/{id}
   ├── Validates EditCampaignViewModel
   ├── Re-checks: No donations received
   ├── Updates campaign fields
   ├── Handles new logo upload if provided
   ├── Saves changes
   └── Redirects to MyCampaigns
   ```

**Key Code:**
```csharp
// CampaignController.cs - Edit GET
var campaign = await _context.Campaigns
    .Include(c => c.Donations)
    .FirstOrDefaultAsync(c => c.Id == id && c.NGOId == ngo.Id);

if (campaign.Donations.Any())
{
    TempData["ErrorMessage"] = "Cannot edit campaign that has received donations.";
    return RedirectToAction(nameof(MyCampaigns));
}
```

---

### 3.4 View Campaign List (Browse/Search)

**Entry Point:** `/Campaign/Index`

**Files Involved:**
- `Controllers/CampaignController.cs` → `Index()`
- `Views/Campaign/Index.cshtml`

**Flow:**

```
GET /Campaign/Index?search=education&category=Education&status=Active
├── Query all campaigns with status = Active (filter)
├── Include related NGO data
├── Apply search filter (if provided):
│   └── Matches Title or Description (case-insensitive)
├── Apply category filter (if provided)
├── Apply status filter (if provided)
├── Order by: StartDate descending
└── Return paginated results
```

**Key Code:**
```csharp
// CampaignController.cs - Index
var campaigns = _context.Campaigns
    .Include(c => c.NGO)
    .ThenInclude(n => n.User)
    .Where(c => c.NGO.VerificationStatus == VerificationStatus.Approved)
    .AsQueryable();

// Only show Active campaigns by default
campaigns = campaigns.Where(c => !c.IsCancelled 
    && c.StartDate <= DateTime.UtcNow 
    && c.EndDate >= DateTime.UtcNow);

if (!string.IsNullOrWhiteSpace(search))
{
    campaigns = campaigns.Where(c => 
        c.Title.Contains(search) || 
        c.Description.Contains(search)
    );
}

if (!string.IsNullOrWhiteSpace(category))
{
    campaigns = campaigns.Where(c => c.Category == category);
}

var campaignList = await campaigns
    .OrderByDescending(c => c.StartDate)
    .ToListAsync();
```

---

## 4. Donation Flow

### 4.1 Create Donation (Step 1: Amount)

**Entry Point:** `/Donation/Create/{campaignId}`

**Files Involved:**
- `Controllers/DonationController.cs` → `Create()` GET & POST
- `ViewModels/CreateDonationViewModel.cs`
- `Views/Donation/Create.cshtml`

**Authorization Required:** Donor role

**Flow Steps:**

1. **Donor clicks "Donate" on a campaign**
   ```
   GET /Donation/Create/{campaignId}
   ├── Fetches campaign details
   ├── Checks: Campaign can accept donations (Active/Goal Reached status)
   ├── If cannot accept: Redirects with error
   └── Returns Create.cshtml form
   ```

2. **Donor enters donation amount**
   - Minimum: ₹10
   - Optional: Leave anonymous checkbox
   - Optional: Comment/message

3. **Donor clicks "Continue to Review"**
   ```
   POST /Donation/Create
   ├── Validates CreateDonationViewModel
   ├── Stores in TempData for review page
   └── Redirects to /Donation/Review
   ```

**Key Code:**
```csharp
// DonationController.cs - Create POST
TempData["DonationAmount"] = model.Amount;
TempData["CampaignId"] = model.CampaignId;
TempData["IsAnonymous"] = model.IsAnonymous;
TempData["DonorComment"] = model.DonorComment;

return RedirectToAction("Review");
```

---

### 4.2 Review Donation (Step 2: Confirmation)

**Entry Point:** `/Donation/Review`

**Files Involved:**
- `Controllers/DonationController.cs` → `Review()` GET & POST
- `ViewModels/ReviewDonationViewModel.cs`
- `Views/Donation/Review.cshtml`

**Flow Steps:**

1. **Donor reviews donation details**
   ```
   GET /Donation/Review
   ├── Reads TempData from Create step
   ├── Fetches campaign and NGO details
   ├── Displays summary:
   │   ├── Campaign name and NGO
   │   ├── Donation amount
   │   ├── Anonymous status
   │   └── Donor comment
   └── Shows "Confirm & Pay" button
   ```

2. **Donor confirms and proceeds to payment**
   ```
   POST /Donation/Review
   ├── Creates Donation record (Status: Pending)
   ├── Creates Payment record (Status: Pending)
   ├── Initiates Razorpay order
   ├── Returns Razorpay order details to frontend
   └── Frontend opens Razorpay checkout modal
   ```

**Key Code:**
```csharp
// DonationController.cs - Review POST
var donation = new Donation
{
    CampaignId = campaignId,
    DonorId = donorUser.Id,
    Amount = amount,
    DonationDate = DateTime.UtcNow,
    IsAnonymous = isAnonymous,
    DonorComment = donorComment,
    PaymentStatus = PaymentStatus.Pending
};

_context.Donations.Add(donation);
await _context.SaveChangesAsync();

// Create Razorpay order (see Payment Processing section)
var razorpayOrder = await CreateRazorpayOrder(donation);

return Json(new { 
    success = true, 
    orderId = razorpayOrder.Id,
    donationId = donation.Id 
});
```

---

## 5. Payment Processing

### 5.1 Razorpay Integration

**Entry Point:** Review page → Razorpay Checkout Modal

**Files Involved:**
- `Controllers/DonationController.cs` → `Review()` POST, `VerifyPayment()` POST
- `Models/Payment.cs`
- `Views/Donation/Review.cshtml` (Razorpay JS integration)
- `appsettings.json` (Razorpay credentials)

**Configuration:**

```json
// appsettings.json
{
  "Razorpay": {
    "KeyId": "your_key_id",
    "KeySecret": "your_key_secret"
  }
}
```

**Flow Steps:**

### Step 1: Create Razorpay Order

```csharp
// DonationController.cs - CreateRazorpayOrder()
private async Task<dynamic> CreateRazorpayOrder(Donation donation)
{
    var client = new RestClient("https://api.razorpay.com/v1/orders");
    var request = new RestRequest("", Method.Post);
    
    // Authentication
    var credentials = Convert.ToBase64String(
        Encoding.ASCII.GetBytes($"{_razorpayKeyId}:{_razorpayKeySecret}")
    );
    request.AddHeader("Authorization", $"Basic {credentials}");
    request.AddHeader("Content-Type", "application/json");
    
    // Order details
    var orderData = new
    {
        amount = (int)(donation.Amount * 100), // Convert to paise
        currency = "INR",
        receipt = $"donation_{donation.Id}",
        notes = new
        {
            donation_id = donation.Id.ToString(),
            campaign_id = donation.CampaignId.ToString()
        }
    };
    
    request.AddJsonBody(orderData);
    var response = await client.ExecuteAsync(request);
    
    return JsonConvert.DeserializeObject<dynamic>(response.Content);
}
```

### Step 2: Frontend Razorpay Checkout

```javascript
// Views/Donation/Review.cshtml - JavaScript
var options = {
    "key": "@Model.RazorpayKeyId",
    "amount": data.amount,
    "currency": "INR",
    "name": "CareFund",
    "description": "Donation to " + "@Model.Campaign.Title",
    "order_id": data.orderId,
    "handler": function (response) {
        // Payment successful
        verifyPayment(response.razorpay_payment_id, 
                     response.razorpay_order_id, 
                     response.razorpay_signature);
    },
    "prefill": {
        "name": "@User.Identity.Name",
        "email": "@User.Identity.Name"
    },
    "theme": {
        "color": "#3b82f6"
    }
};

var rzp = new Razorpay(options);
rzp.open();
```

### Step 3: Verify Payment Signature

```csharp
// DonationController.cs - VerifyPayment POST
[HttpPost]
public async Task<IActionResult> VerifyPayment(
    string razorpay_payment_id, 
    string razorpay_order_id, 
    string razorpay_signature,
    int donationId)
{
    // Verify signature
    var text = razorpay_order_id + "|" + razorpay_payment_id;
    var secret = _razorpayKeySecret;
    
    var encoding = new UTF8Encoding();
    var keyBytes = encoding.GetBytes(secret);
    var textBytes = encoding.GetBytes(text);
    
    using (var hmac = new HMACSHA256(keyBytes))
    {
        var hashBytes = hmac.ComputeHash(textBytes);
        var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        
        if (hash != razorpay_signature)
        {
            return Json(new { success = false, message = "Payment verification failed" });
        }
    }
    
    // Update donation and payment records
    var donation = await _context.Donations
        .Include(d => d.Campaign)
        .FirstOrDefaultAsync(d => d.Id == donationId);
    
    donation.PaymentStatus = PaymentStatus.Completed;
    
    var payment = new Payment
    {
        DonationId = donation.Id,
        Amount = donation.Amount,
        PaymentDate = DateTime.UtcNow,
        PaymentMethod = "Razorpay",
        TransactionId = razorpay_payment_id,
        Status = PaymentStatus.Completed
    };
    
    _context.Payments.Add(payment);
    
    // Update campaign raised amount
    donation.Campaign.RaisedAmount += donation.Amount;
    
    await _context.SaveChangesAsync();
    
    // Send email and generate PDF (see next sections)
    await SendDonationConfirmationEmail(donation);
    
    return Json(new { success = true, donationId = donation.Id });
}
```

---

### 5.2 Payment Failure Handling

**Flow:**

```
Razorpay Checkout Modal
├── User closes modal OR payment fails
└── JavaScript handler:
    ├── Donation record remains with Status: Pending
    ├── No email sent
    ├── No PDF generated
    ├── Campaign RaisedAmount NOT updated
    └── User can retry from /Donation/Create again
```

---

## 6. PDF Receipt Generation

### 6.1 Receipt PDF Creation

**Entry Point:** After successful payment OR manual download

**Files Involved:**
- `Helpers/ReceiptPdfGenerator.cs`
- `Controllers/DonationController.cs` → `DownloadReceipt()`
- QuestPDF library

**Flow:**

### Automatic Generation (After Payment)

```csharp
// DonationController.cs - VerifyPayment
var pdfBytes = ReceiptPdfGenerator.GenerateReceipt(donation);
var pdfPath = SavePdfToFile(pdfBytes, donation.Id);

// Attach to email
await SendEmailWithPdfAttachment(donor.Email, pdfBytes);
```

### Manual Download

```csharp
// DonationController.cs - DownloadReceipt GET
[Authorize(Roles = "Donor")]
public async Task<IActionResult> DownloadReceipt(int id)
{
    var donation = await _context.Donations
        .Include(d => d.Campaign)
        .ThenInclude(c => c.NGO)
        .Include(d => d.Donor)
        .Include(d => d.Payment)
        .FirstOrDefaultAsync(d => d.Id == id);
    
    // Verify: Donor owns this donation
    if (donation.DonorId != _userManager.GetUserId(User))
        return Forbid();
    
    // Verify: Payment is completed
    if (donation.PaymentStatus != PaymentStatus.Completed)
        return BadRequest("Receipt not available for pending payments");
    
    try
    {
        var pdfBytes = ReceiptPdfGenerator.GenerateReceipt(donation);
        var fileName = $"CareFund_Receipt_{donation.Id}_{DateTime.Now:yyyyMMdd}.pdf";
        
        return File(pdfBytes, "application/pdf", fileName);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error generating receipt for donation {DonationId}", id);
        TempData["ErrorMessage"] = "Failed to generate receipt. Please try again.";
        return RedirectToAction("MyDonations");
    }
}
```

---

### 6.2 PDF Structure

**File:** `Helpers/ReceiptPdfGenerator.cs`

```csharp
public static class ReceiptPdfGenerator
{
    public static byte[] GenerateReceipt(Donation donation)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                
                page.Header().Element(ComposeHeader);
                page.Content().Element(content => ComposeContent(content, donation));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }
    
    private static void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            // CareFund logo and branding
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("CareFund")
                    .FontSize(24)
                    .Bold()
                    .FontColor(Colors.Blue.Medium); // Fixed: was "#3b82f6"
                
                column.Item().Text("Donation Receipt")
                    .FontSize(14)
                    .FontColor(Colors.Grey.Darken2);
            });
            
            // Receipt metadata
            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Item().Text($"Receipt #{donation.Id}")
                    .FontSize(10)
                    .Bold();
                
                column.Item().Text($"Date: {donation.DonationDate:dd MMM yyyy}")
                    .FontSize(9);
            });
        });
    }
    
    private static void ComposeContent(IContainer container, Donation donation)
    {
        container.PaddingVertical(20).Column(column =>
        {
            // Payment status badge
            column.Item().Background(Colors.Green.Lighten3)
                .Padding(8)
                .AlignCenter()
                .Text("PAID") // Fixed: was "✓ PAID"
                .FontSize(14)
                .Bold()
                .FontColor(Colors.Green.Darken2);
            
            // Amount
            column.Item().PaddingVertical(20).AlignCenter().Text($"₹{donation.Amount:N2}")
                .FontSize(36)
                .Bold()
                .FontColor(Colors.Green.Darken2);
            
            // Donor info
            column.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Donor Name").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().Text(donation.IsAnonymous ? "Anonymous" : donation.Donor.FullName)
                        .FontSize(11).Bold();
                });
                
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Email").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().Text(donation.IsAnonymous ? "Hidden" : donation.Donor.Email)
                        .FontSize(11).Bold();
                });
            });
            
            // Campaign info
            column.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().PaddingTop(15).Text("Campaign Details").FontSize(12).Bold();
            
            column.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Campaign").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().Text(donation.Campaign.Title).FontSize(11).Bold();
                });
                
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("NGO").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().Text(donation.Campaign.NGO.Name).FontSize(11).Bold();
                });
            });
            
            // Payment info
            column.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().PaddingTop(15).Text("Payment Details").FontSize(12).Bold();
            
            column.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Transaction ID").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().Text(donation.Payment?.TransactionId ?? "N/A").FontSize(9);
                });
                
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Payment Method").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().Text(donation.Payment?.PaymentMethod ?? "Razorpay").FontSize(9);
                });
                
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Payment Date").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().Text(donation.Payment?.PaymentDate.ToString("dd MMM yyyy, hh:mm tt") ?? "N/A")
                        .FontSize(9);
                });
            });
        });
    }
    
    private static void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Thank you for your generous contribution!").FontSize(10).Italic();
            text.Line("This is a computer-generated receipt and does not require a signature.");
            text.Span("CareFund - Making a Difference Together").FontSize(8).FontColor(Colors.Grey.Medium);
        });
    }
}
```

**Key Fixes Applied:**
- ❌ Hex colors (`"#3b82f6"`) → ✅ QuestPDF constants (`Colors.Blue.Medium`)
- ❌ Checkmark glyph `"✓ PAID"` → ✅ Plain text `"PAID"`
- ❌ `Console.WriteLine` → ✅ `_logger.LogError()`

---

## 7. Email Notifications

### 7.1 Email Configuration

**Files Involved:**
- `Helpers/EmailHelper.cs`
- `appsettings.json` (SMTP configuration)
- `Controllers/DonationController.cs`

**Configuration:**

```json
// appsettings.json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderPassword": "your-app-password",
    "SenderName": "CareFund"
  }
}
```

---

### 7.2 Send Donation Confirmation Email

**Flow:**

```
Payment Verified
└── Send email with PDF attachment
    ├── To: Donor email
    ├── Subject: "Donation Confirmation - CareFund"
    ├── Body: HTML email template
    └── Attachment: PDF receipt
```

**Implementation:**

```csharp
// Helpers/EmailHelper.cs
public static class EmailHelper
{
    public static async Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        byte[] pdfAttachment = null,
        string attachmentFileName = null,
        IConfiguration configuration = null)
    {
        var smtpServer = configuration["EmailSettings:SmtpServer"];
        var smtpPort = int.Parse(configuration["EmailSettings:SmtpPort"]);
        var senderEmail = configuration["EmailSettings:SenderEmail"];
        var senderPassword = configuration["EmailSettings:SenderPassword"];
        var senderName = configuration["EmailSettings:SenderName"];
        
        using var message = new MailMessage();
        message.From = new MailAddress(senderEmail, senderName);
        message.To.Add(toEmail);
        message.Subject = subject;
        message.Body = body;
        message.IsBodyHtml = true;
        
        // Attach PDF if provided
        if (pdfAttachment != null && !string.IsNullOrEmpty(attachmentFileName))
        {
            var stream = new MemoryStream(pdfAttachment);
            var attachment = new Attachment(stream, attachmentFileName, "application/pdf");
            message.Attachments.Add(attachment);
        }
        
        using var smtp = new SmtpClient(smtpServer, smtpPort);
        smtp.EnableSsl = true;
        smtp.Credentials = new NetworkCredential(senderEmail, senderPassword);
        
        await smtp.SendMailAsync(message);
    }
}

// DonationController.cs - SendDonationConfirmationEmail
private async Task SendDonationConfirmationEmail(Donation donation)
{
    var donor = await _userManager.FindByIdAsync(donation.DonorId);
    var pdfBytes = ReceiptPdfGenerator.GenerateReceipt(donation);
    
    var subject = "Thank You for Your Donation - CareFund";
    var body = $@"
        <html>
        <body style='font-family: Arial, sans-serif;'>
            <h2 style='color: #3b82f6;'>Thank You for Your Generous Donation!</h2>
            <p>Dear {(donation.IsAnonymous ? "Generous Donor" : donor.FullName)},</p>
            <p>Your donation of <strong>₹{donation.Amount:N2}</strong> to <strong>{donation.Campaign.Title}</strong> has been successfully processed.</p>
            <p>Your contribution will make a real difference. Thank you for supporting {donation.Campaign.NGO.Name}.</p>
            <p>Please find your donation receipt attached to this email.</p>
            <p>You can also download your receipt anytime from your <a href='https://carefund.com/Donation/MyDonations'>My Donations</a> page.</p>
            <br>
            <p>Warm regards,<br>The CareFund Team</p>
        </body>
        </html>
    ";
    
    var fileName = $"CareFund_Receipt_{donation.Id}.pdf";
    
    await EmailHelper.SendEmailAsync(
        donor.Email, 
        subject, 
        body, 
        pdfBytes, 
        fileName, 
        _configuration
    );
}
```

---

## 8. Authorization & Role-Based Access

### 8.1 Role Setup

**Files Involved:**
- `Program.cs` → Role seeding
- `Data/ApplicationDbContext.cs`
- `Models/ApplicationUser.cs` (Identity user)

**Roles:**
1. **Admin** - Full system access
2. **NGO** - Create campaigns, view donations
3. **Donor** - Browse campaigns, make donations

**Role Seeding:**

```csharp
// Program.cs - SeedRoles()
private static async Task SeedRoles(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    
    string[] roles = { "Admin", "NGO", "Donor" };
    
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

// Seed admin user
private static async Task SeedAdminUser(IServiceProvider serviceProvider)
{
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    
    var adminEmail = "admin@carefund.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Administrator",
            EmailConfirmed = true
        };
        
        await userManager.CreateAsync(adminUser, "Admin@123");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}
```

---

### 8.2 Authorization Patterns

**Controller-Level Authorization:**

```csharp
// Controllers/DonationController.cs
[Authorize(Roles = "Donor")]
public class DonationController : Controller
{
    // All actions require Donor role
}

// Controllers/AdminController.cs
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    // All actions require Admin role
}

// Controllers/CampaignController.cs - Mixed authorization
public class CampaignController : Controller
{
    // Some actions allow anonymous (Index, Details)
    // Some require NGO role (Create, Edit, Delete)
    
    [Authorize(Roles = "NGO")]
    public async Task<IActionResult> Create()
    {
        // Only NGOs can create campaigns
    }
    
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        // Anyone can browse campaigns
    }
}
```

**Custom Authorization Checks:**

```csharp
// Check NGO verification status
var ngo = await _context.NGOs
    .FirstOrDefaultAsync(n => n.UserId == userId);

if (ngo.VerificationStatus != VerificationStatus.Approved)
{
    TempData["ErrorMessage"] = "Your NGO must be verified before creating campaigns.";
    return RedirectToAction("VerificationStatus", "Profile");
}

// Check campaign ownership
var campaign = await _context.Campaigns
    .FirstOrDefaultAsync(c => c.Id == id && c.NGOId == ngo.Id);

if (campaign == null)
    return Forbid(); // Not authorized

// Check donation ownership
var donation = await _context.Donations
    .FirstOrDefaultAsync(d => d.Id == id && d.DonorId == userId);

if (donation == null)
    return Forbid(); // Not authorized
```

---

### 8.3 View-Level Authorization

**Shared Layout (_Layout.cshtml):**

```html
<!-- Views/Shared/_Layout.cshtml -->
@if (User.Identity?.IsAuthenticated == true)
{
    @if (User.IsInRole("Admin"))
    {
        <li class="nav-item">
            <a class="nav-link" asp-controller="Admin" asp-action="Dashboard">Admin Dashboard</a>
        </li>
    }
    
    @if (User.IsInRole("NGO"))
    {
        <li class="nav-item">
            <a class="nav-link" asp-controller="Campaign" asp-action="MyCampaigns">My Campaigns</a>
        </li>
        <li class="nav-item">
            <a class="nav-link" asp-controller="Campaign" asp-action="Create">Create Campaign</a>
        </li>
    }
    
    @if (User.IsInRole("Donor"))
    {
        <li class="nav-item">
            <a class="nav-link" asp-controller="DonorDashboard" asp-action="Index">My Dashboard</a>
        </li>
        <li class="nav-item">
            <a class="nav-link" asp-controller="Donation" asp-action="MyDonations">My Donations</a>
        </li>
    }
}
```

---

## 9. Search & Filtering

### 9.1 Campaign Search

**Entry Point:** `/Campaign/Index`

**Search Types:**

1. **Text Search** (search parameter)
   - Searches in: Campaign Title, Description
   - Case-insensitive
   - Partial match

2. **Category Filter** (category parameter)
   - Exact match on Category field
   - Values: Education, Healthcare, Environment, Animal Welfare, Disaster Relief, etc.

3. **Status Filter** (status parameter)
   - Options: All, Active, Upcoming, Completed, Goal Reached
   - Uses `CampaignExtensions.GetStatus()`

**Implementation:**

```csharp
// CampaignController.cs - Index
public async Task<IActionResult> Index(string search, string category, string status)
{
    var campaigns = _context.Campaigns
        .Include(c => c.NGO)
        .ThenInclude(n => n.User)
        .Where(c => c.NGO.VerificationStatus == VerificationStatus.Approved)
        .AsQueryable();
    
    // Default: Show only Active campaigns
    if (string.IsNullOrWhiteSpace(status) || status == "Active")
    {
        campaigns = campaigns.Where(c => 
            !c.IsCancelled &&
            c.StartDate <= DateTime.UtcNow &&
            c.EndDate >= DateTime.UtcNow &&
            c.RaisedAmount < c.GoalAmount
        );
    }
    
    // Text search
    if (!string.IsNullOrWhiteSpace(search))
    {
        campaigns = campaigns.Where(c => 
            c.Title.Contains(search) || 
            c.Description.Contains(search)
        );
    }
    
    // Category filter
    if (!string.IsNullOrWhiteSpace(category))
    {
        campaigns = campaigns.Where(c => c.Category == category);
    }
    
    // Status filter (if not default Active)
    if (!string.IsNullOrWhiteSpace(status) && status != "Active")
    {
        var campaignList = await campaigns.ToListAsync();
        campaignList = campaignList.Where(c => c.GetStatus() == status).ToList();
        return View(campaignList);
    }
    
    return View(await campaigns.OrderByDescending(c => c.StartDate).ToListAsync());
}
```

**View Implementation:**

```html
<!-- Views/Campaign/Index.cshtml -->
<form method="get" class="search-form">
    <div class="row">
        <div class="col-md-4">
            <input type="text" name="search" class="form-control" 
                   placeholder="Search campaigns..." value="@ViewBag.Search" />
        </div>
        <div class="col-md-3">
            <select name="category" class="form-control">
                <option value="">All Categories</option>
                <option value="Education">Education</option>
                <option value="Healthcare">Healthcare</option>
                <option value="Environment">Environment</option>
                <option value="Animal Welfare">Animal Welfare</option>
                <option value="Disaster Relief">Disaster Relief</option>
            </select>
        </div>
        <div class="col-md-3">
            <select name="status" class="form-control">
                <option value="Active">Active</option>
                <option value="Upcoming">Upcoming</option>
                <option value="Completed">Completed</option>
                <option value="Goal Reached">Goal Reached</option>
            </select>
        </div>
        <div class="col-md-2">
            <button type="submit" class="btn btn-primary w-100">Search</button>
        </div>
    </div>
</form>
```

---

### 9.2 NGO Admin Search

**Entry Point:** `/Admin/NGOs?status=Pending`

**Implementation:**

```csharp
// AdminController.cs - NGOs
public async Task<IActionResult> NGOs(string status)
{
    var ngos = _context.NGOs
        .Include(n => n.User)
        .Include(n => n.Documents)
        .AsQueryable();
    
    if (!string.IsNullOrWhiteSpace(status))
    {
        var verificationStatus = Enum.Parse<VerificationStatus>(status);
        ngos = ngos.Where(n => n.VerificationStatus == verificationStatus);
    }
    
    ViewBag.SelectedStatus = status;
    
    return View(await ngos.OrderByDescending(n => n.Id).ToListAsync());
}
```

---

## 10. Dashboard Flows

### 10.1 Admin Dashboard

**Entry Point:** `/Admin/Dashboard`

**Files:**
- `Controllers/AdminController.cs` → `Dashboard()`
- `Views/Admin/Dashboard.cshtml`

**Features:**
- Total NGOs count
- Pending verification count
- Total campaigns count
- Total donations amount
- Quick links to manage NGOs

```csharp
// AdminController.cs - Dashboard
public async Task<IActionResult> Dashboard()
{
    var totalNGOs = await _context.NGOs.CountAsync();
    var pendingNGOs = await _context.NGOs
        .CountAsync(n => n.VerificationStatus == VerificationStatus.Pending);
    var totalCampaigns = await _context.Campaigns.CountAsync();
    var totalDonations = await _context.Donations
        .Where(d => d.PaymentStatus == PaymentStatus.Completed)
        .SumAsync(d => d.Amount);
    
    ViewBag.TotalNGOs = totalNGOs;
    ViewBag.PendingNGOs = pendingNGOs;
    ViewBag.TotalCampaigns = totalCampaigns;
    ViewBag.TotalDonations = totalDonations;
    
    return View();
}
```

---

### 10.2 Donor Dashboard

**Entry Point:** `/DonorDashboard/Index`

**Files:**
- `Controllers/DonorDashboardController.cs` → `Index()`
- `ViewModels/DonorDashboardViewModel.cs`
- `Views/DonorDashboard/Index.cshtml`

**Features:**
- Total donations count
- Campaigns supported count
- Total amount donated
- 5 most recent donations

```csharp
// DonorDashboardController.cs - Index
[Authorize(Roles = "Donor")]
public async Task<IActionResult> Index()
{
    var userId = _userManager.GetUserId(User);
    
    var donations = await _context.Donations
        .Include(d => d.Campaign)
        .ThenInclude(c => c.NGO)
        .Where(d => d.DonorId == userId && d.PaymentStatus == PaymentStatus.Completed)
        .OrderByDescending(d => d.DonationDate)
        .ToListAsync();
    
    var viewModel = new DonorDashboardViewModel
    {
        TotalDonations = donations.Count,
        TotalAmount = donations.Sum(d => d.Amount),
        CampaignsSupported = donations.Select(d => d.CampaignId).Distinct().Count(),
        RecentDonations = donations.Take(5).ToList()
    };
    
    return View(viewModel);
}
```

---

### 10.3 NGO Dashboard (Verification Status)

**Entry Point:** `/Profile/VerificationStatus`

**Features:**
- Current verification status
- Rejection reason (if rejected)
- Documents submitted
- Next steps guidance

```csharp
// ProfileController.cs - VerificationStatus
[Authorize(Roles = "NGO")]
public async Task<IActionResult> VerificationStatus()
{
    var userId = _userManager.GetUserId(User);
    var ngo = await _context.NGOs
        .Include(n => n.Documents)
        .FirstOrDefaultAsync(n => n.UserId == userId);
    
    return View(ngo);
}
```

---

## Key Database Relationships

```
ApplicationUser (Identity)
├── 1:1 → NGO (UserId)
└── 1:N → Donations (DonorId)

NGO
├── N:1 → ApplicationUser (UserId)
├── 1:N → Campaigns (NGOId)
└── 1:N → NGODocuments (NGOId)

Campaign
├── N:1 → NGO (NGOId)
└── 1:N → Donations (CampaignId)

Donation
├── N:1 → Campaign (CampaignId)
├── N:1 → ApplicationUser/Donor (DonorId)
└── 1:1 → Payment (DonationId)

Payment
└── 1:1 → Donation (DonationId)
```

---

## Important Business Rules

1. **NGO Verification:**
   - NGOs must be Approved before creating campaigns
   - Rejected NGOs cannot resubmit (removed feature)

2. **Campaign Management:**
   - Only Active campaigns accept donations
   - Campaigns with donations cannot be edited
   - StartDate must be today or future
   - EndDate must be after StartDate

3. **Donations:**
   - Minimum donation: ₹10
   - Only Donors can donate
   - Anonymous donations hide donor info

4. **Payments:**
   - Only Razorpay supported
   - Razorpay signature verification required
   - Failed payments don't update campaign amount

5. **Receipts:**
   - Generated only for completed payments
   - Can be downloaded anytime from My Donations
   - Emailed immediately after payment

---

## Environment Setup

### Required Configuration

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;Trusted_Connection=True;"
  },
  "Razorpay": {
    "KeyId": "rzp_test_...",
    "KeySecret": "your_secret"
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderPassword": "app-password",
    "SenderName": "CareFund"
  }
}
```

### Database Migrations Applied

1. `20261005153828_AddIsCancelledToCampaign`
2. `20261005164028_AddStructuredNGOFields`

---

## Testing Checklist

- [ ] Register as Donor
- [ ] Register as NGO (upload documents)
- [ ] Login as Admin
- [ ] Approve NGO
- [ ] Reject NGO (verify cannot create campaigns)
- [ ] Login as approved NGO
- [ ] Create campaign
- [ ] Edit campaign (before donations)
- [ ] Login as Donor
- [ ] Search campaigns
- [ ] Make donation (test Razorpay)
- [ ] Verify payment
- [ ] Check email received
- [ ] Download PDF receipt
- [ ] View Donor Dashboard
- [ ] View My Donations

---

**Document Last Updated:** October 5, 2026  
**CareFund Version:** Phase 7 Complete  
**Build Status:** ✅ Success
