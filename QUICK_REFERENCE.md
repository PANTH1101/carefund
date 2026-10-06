# CareFund - Quick Reference Guide

Quick navigation to understand the codebase flows and key files.

---

## 🔑 Key Flows Overview

### 1️⃣ User Registration & Login
```
┌─────────────────────────────────────────────────┐
│  DONOR REGISTRATION                             │
│  /Account/RegisterDonor                         │
│  → AccountController.cs (RegisterDonor)         │
│  → Creates User with "Donor" role               │
│  → Auto-login → Redirect to Campaigns           │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│  NGO REGISTRATION                               │
│  /Account/RegisterNGO                           │
│  → AccountController.cs (RegisterNGO)           │
│  → Creates User + NGO (Status: Pending)         │
│  → Uploads documents (cert, 80G, logo)          │
│  → Auto-login → Redirect to VerificationStatus  │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│  LOGIN                                          │
│  /Account/Login                                 │
│  → AccountController.cs (Login)                 │
│  → Role-based redirect:                         │
│     • Admin → /Admin/Dashboard                  │
│     • NGO → /Profile/VerificationStatus         │
│     • Donor → /Campaign/Index                   │
└─────────────────────────────────────────────────┘
```

---

### 2️⃣ NGO Verification Workflow
```
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│   NGO Pending│    │ Admin Reviews│    │   Approved   │
│              │───▶│   /Admin/    │───▶│  Can Create  │
│  Cannot Do   │    │   NGODetails │    │  Campaigns   │
│  Anything    │    │              │    │              │
└──────────────┘    └──────────────┘    └──────────────┘
                            │
                            │ (Reject)
                            ▼
                    ┌──────────────┐
                    │   Rejected   │
                    │  Cannot      │
                    │  Resubmit    │
                    └──────────────┘

Key Files:
- ProfileController.cs → VerificationStatus()
- AdminController.cs → ApproveNGO(), RejectNGO()
- Models/NGO.cs → VerificationStatus enum
```

---

### 3️⃣ Campaign Creation Flow
```
┌─────────────────────────────────────────────────┐
│  NGO (Approved) Creates Campaign                │
│  /Campaign/Create                               │
│                                                 │
│  1. Check NGO is Approved ✓                     │
│  2. Fill campaign form (title, goal, dates)     │
│  3. Upload optional logo                        │
│  4. Submit                                      │
│     → CampaignController.cs (Create POST)       │
│     → Creates Campaign record                   │
│     → Initial: RaisedAmount=0, IsCancelled=false│
│  5. Redirect to /Campaign/MyCampaigns           │
└─────────────────────────────────────────────────┘

Campaign Status (CampaignExtensions.cs):
┌──────────┬──────────────────────────────────────┐
│ Upcoming │ StartDate > Today                    │
│ Active   │ StartDate ≤ Today ≤ EndDate          │
│ Goal Met │ RaisedAmount ≥ GoalAmount            │
│ Completed│ EndDate < Today                      │
│ Cancelled│ IsCancelled = true                   │
└──────────┴──────────────────────────────────────┘

Only "Active" and "Goal Met" campaigns accept donations!
```

---

### 4️⃣ Complete Donation Flow
```
┌─────────────────────────────────────────────────┐
│  STEP 1: Enter Amount                           │
│  /Donation/Create/{campaignId}                  │
│  → DonationController.cs (Create)               │
│  → Donor enters amount (min ₹10)                │
│  → Optional: Anonymous checkbox, Comment        │
│  → Store in TempData                            │
│  → Redirect to Review                           │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│  STEP 2: Review & Pay                           │
│  /Donation/Review                               │
│  → DonationController.cs (Review GET)           │
│  → Display summary (campaign, amount, NGO)      │
│  → Donor clicks "Confirm & Pay"                 │
│  → DonationController.cs (Review POST)          │
│     1. Create Donation (Status: Pending)        │
│     2. Create Razorpay Order                    │
│     3. Return order details to frontend         │
│     4. Open Razorpay Checkout Modal             │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│  STEP 3: Payment Processing                     │
│  Razorpay Checkout Modal                        │
│  → User enters card details                     │
│  → Razorpay processes payment                   │
│  → Razorpay returns:                            │
│     • razorpay_payment_id                       │
│     • razorpay_order_id                         │
│     • razorpay_signature                        │
│  → Frontend sends to /Donation/VerifyPayment    │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│  STEP 4: Verify & Complete                      │
│  /Donation/VerifyPayment (POST)                 │
│  → DonationController.cs (VerifyPayment)        │
│  → Verify HMAC SHA256 signature ✓               │
│  → Update Donation: Status = Completed          │
│  → Create Payment record                        │
│  → Update Campaign.RaisedAmount                 │
│  → Generate PDF Receipt                         │
│  → Send Email with PDF attachment               │
│  → Return success JSON                          │
│  → Frontend redirects to Success page           │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│  STEP 5: Success & Receipt                      │
│  /Donation/Success/{donationId}                 │
│  → Shows success message                        │
│  → Link to download receipt                     │
│  → Link to view donation details                │
└─────────────────────────────────────────────────┘
```

---

### 5️⃣ Razorpay Payment Integration
```
┌─────────────────────────────────────────────────┐
│  CONFIGURATION (appsettings.json)               │
│  {                                              │
│    "Razorpay": {                                │
│      "KeyId": "rzp_test_...",                   │
│      "KeySecret": "your_secret"                 │
│    }                                            │
│  }                                              │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│  CREATE ORDER (Server-Side)                     │
│  DonationController.cs → CreateRazorpayOrder()  │
│                                                 │
│  POST https://api.razorpay.com/v1/orders        │
│  Authorization: Basic <base64(key:secret)>      │
│  {                                              │
│    "amount": 50000,  // in paise (₹500.00)     │
│    "currency": "INR",                           │
│    "receipt": "donation_123"                    │
│  }                                              │
│                                                 │
│  Returns: { "id": "order_xyz...", ... }         │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│  CHECKOUT (Frontend - Review.cshtml)            │
│  var options = {                                │
│    "key": "rzp_test_...",                       │
│    "amount": 50000,                             │
│    "order_id": "order_xyz...",                  │
│    "handler": function(response) {              │
│      // Payment success                         │
│      verifyPayment(                             │
│        response.razorpay_payment_id,            │
│        response.razorpay_order_id,              │
│        response.razorpay_signature              │
│      );                                         │
│    }                                            │
│  };                                             │
│  var rzp = new Razorpay(options);               │
│  rzp.open();                                    │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│  VERIFY SIGNATURE (Server-Side)                 │
│  DonationController.cs → VerifyPayment()        │
│                                                 │
│  text = order_id + "|" + payment_id             │
│  secret = razorpay_key_secret                   │
│                                                 │
│  hash = HMAC_SHA256(secret, text)               │
│                                                 │
│  if (hash == razorpay_signature)                │
│    ✓ Payment is authentic                       │
│  else                                           │
│    ✗ Payment verification failed                │
└─────────────────────────────────────────────────┘
```

---

### 6️⃣ PDF Receipt Generation
```
┌─────────────────────────────────────────────────┐
│  LIBRARY: QuestPDF                              │
│  NuGet: QuestPDF                                │
│                                                 │
│  File: Helpers/ReceiptPdfGenerator.cs           │
│                                                 │
│  Key Method:                                    │
│  public static byte[] GenerateReceipt(          │
│      Donation donation)                         │
│  {                                              │
│    return Document.Create(container => {        │
│      container.Page(page => {                   │
│        page.Header().Element(ComposeHeader);    │
│        page.Content().Element(content =>        │
│          ComposeContent(content, donation));    │
│        page.Footer().Element(ComposeFooter);    │
│      });                                        │
│    }).GeneratePdf();                            │
│  }                                              │
└─────────────────────────────────────────────────┘

PDF Structure:
┌─────────────────────────────────────────────────┐
│  HEADER                                         │
│  • CareFund logo (blue)                         │
│  • "Donation Receipt"                           │
│  • Receipt #123, Date                           │
├─────────────────────────────────────────────────┤
│  CONTENT                                        │
│  • "PAID" badge (green background)              │
│  • Large amount: ₹500.00 (green, 36px)          │
│  • Donor info (or "Anonymous")                  │
│  • Campaign details                             │
│  • NGO information                              │
│  • Payment details (Transaction ID, method)     │
├─────────────────────────────────────────────────┤
│  FOOTER                                         │
│  • "Thank you for your generous contribution!"  │
│  • "Computer-generated receipt" note            │
└─────────────────────────────────────────────────┘

⚠️ IMPORTANT FIXES APPLIED:
❌ Hex colors "#3b82f6" → ✅ Colors.Blue.Medium
❌ Checkmark "✓ PAID" → ✅ "PAID"
❌ Console.WriteLine → ✅ _logger.LogError()
```

---

### 7️⃣ Email Notifications
```
┌─────────────────────────────────────────────────┐
│  CONFIGURATION (appsettings.json)               │
│  {                                              │
│    "EmailSettings": {                           │
│      "SmtpServer": "smtp.gmail.com",            │
│      "SmtpPort": 587,                           │
│      "SenderEmail": "carefund@gmail.com",       │
│      "SenderPassword": "app-password",          │
│      "SenderName": "CareFund"                   │
│    }                                            │
│  }                                              │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│  HELPER: EmailHelper.cs                         │
│                                                 │
│  public static async Task SendEmailAsync(       │
│      string toEmail,                            │
│      string subject,                            │
│      string body,                               │
│      byte[] pdfAttachment,                      │
│      string attachmentFileName,                 │
│      IConfiguration configuration)              │
│  {                                              │
│    // Uses System.Net.Mail.SmtpClient           │
│    // Sends HTML email with PDF attachment      │
│  }                                              │
└─────────────────────────────────────────────────┘

Triggered When:
✓ Payment verified successfully
✓ Donation status = Completed
✓ Includes PDF receipt as attachment
✓ HTML formatted email body
```

---

### 8️⃣ Authorization Summary
```
┌──────────────────┬───────────────────────────────┐
│ Role             │ Permissions                   │
├──────────────────┼───────────────────────────────┤
│ Admin            │ • View all NGOs               │
│                  │ • Approve/Reject NGOs         │
│                  │ • View admin dashboard        │
│                  │ • Cannot donate/create camps  │
├──────────────────┼───────────────────────────────┤
│ NGO (Approved)   │ • Create campaigns            │
│                  │ • Edit campaigns (no donations│
│                  │ • View donations received     │
│                  │ • View verification status    │
│                  │ • Cannot donate               │
├──────────────────┼───────────────────────────────┤
│ NGO (Pending)    │ • View verification status    │
│                  │ • BLOCKED from campaigns      │
├──────────────────┼───────────────────────────────┤
│ NGO (Rejected)   │ • View rejection reason       │
│                  │ • BLOCKED from everything     │
│                  │ • Cannot resubmit             │
├──────────────────┼───────────────────────────────┤
│ Donor            │ • Browse all campaigns        │
│                  │ • Make donations              │
│                  │ • View donation history       │
│                  │ • Download receipts           │
│                  │ • View donor dashboard        │
│                  │ • Cannot create campaigns     │
├──────────────────┼───────────────────────────────┤
│ Anonymous        │ • Browse campaigns (read-only)│
│                  │ • View campaign details       │
│                  │ • Cannot donate (must login)  │
└──────────────────┴───────────────────────────────┘

Implementation:
- [Authorize(Roles = "Donor")] on controllers/actions
- Custom checks in code (NGO verification status)
- @if (User.IsInRole("NGO")) in views
```

---

### 9️⃣ Search & Filtering
```
┌─────────────────────────────────────────────────┐
│  CAMPAIGN SEARCH (/Campaign/Index)              │
│                                                 │
│  Filter 1: Text Search                          │
│  ?search=education                              │
│  → Searches Title and Description               │
│  → Case-insensitive, partial match              │
│                                                 │
│  Filter 2: Category                             │
│  ?category=Healthcare                           │
│  → Exact match on Category field                │
│  → Options: Education, Healthcare, Environment, │
│             Animal Welfare, Disaster Relief     │
│                                                 │
│  Filter 3: Status                               │
│  ?status=Active                                 │
│  → Filters by campaign status                   │
│  → Uses CampaignExtensions.GetStatus()          │
│  → Options: Active, Upcoming, Completed,        │
│             Goal Reached                        │
│                                                 │
│  Combined Example:                              │
│  /Campaign/Index?search=school&category=Educat  │
│  ion&status=Active                              │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│  NGO ADMIN SEARCH (/Admin/NGOs)                 │
│                                                 │
│  Filter: Verification Status                    │
│  ?status=Pending                                │
│  → Shows only NGOs with that status             │
│  → Options: Pending, Approved, Rejected         │
└─────────────────────────────────────────────────┘
```

---

### 🔟 Dashboard Features
```
┌─────────────────────────────────────────────────┐
│  ADMIN DASHBOARD (/Admin/Dashboard)             │
│  • Total NGOs count                             │
│  • Pending verification count                   │
│  • Total campaigns count                        │
│  • Total donations amount                       │
│  • Quick links to manage NGOs                   │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│  DONOR DASHBOARD (/DonorDashboard/Index)        │
│  • Total donations count                        │
│  • Campaigns supported count                    │
│  • Total amount donated                         │
│  • 5 most recent donations                      │
│  • Links to My Donations, Browse Campaigns      │
└─────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────┐
│  NGO VERIFICATION STATUS                        │
│  (/Profile/VerificationStatus)                  │
│  • Current status (Pending/Approved/Rejected)   │
│  • Rejection reason (if rejected)               │
│  • Documents submitted                          │
│  • Next steps guidance                          │
└─────────────────────────────────────────────────┘
```

---

## 📁 File Structure Quick Reference

```
Controllers/
├── AccountController.cs      → Registration, Login, Logout
├── AdminController.cs         → NGO approval, Admin dashboard
├── CampaignController.cs      → CRUD campaigns, Browse/Search
├── DonationController.cs      → Donation flow, Payment, PDF
├── DonorDashboardController.cs→ Donor statistics
└── ProfileController.cs       → NGO profile, Verification status

Models/
├── ApplicationUser.cs         → Identity user (base)
├── NGO.cs                     → NGO entity
├── Campaign.cs                → Campaign entity
├── Donation.cs                → Donation entity
└── Payment.cs                 → Payment entity

ViewModels/
├── DonorRegisterViewModel.cs
├── NGORegisterViewModel.cs
├── LoginViewModel.cs
├── CreateCampaignViewModel.cs
├── CreateDonationViewModel.cs
├── ReviewDonationViewModel.cs
└── DonorDashboardViewModel.cs

Helpers/
├── ReceiptPdfGenerator.cs     → QuestPDF receipt generation
└── EmailHelper.cs             → SMTP email sending

Extensions/
└── CampaignExtensions.cs      → GetStatus(), CanAcceptDonations()

ValidationAttributes/
└── IndianPincodeAttribute.cs  → 6-digit pincode validation

Data/
└── ApplicationDbContext.cs    → EF Core DbContext

Migrations/
├── 20261005153828_AddIsCancelledToCampaign.*
└── 20261005164028_AddStructuredNGOFields.*

Views/
├── Account/
│   ├── Login.cshtml
│   ├── RegisterDonor.cshtml
│   └── RegisterNGO.cshtml
├── Admin/
│   ├── Dashboard.cshtml
│   ├── NGOs.cshtml
│   └── NGODetails.cshtml
├── Campaign/
│   ├── Index.cshtml          → Browse/Search
│   ├── Create.cshtml
│   ├── Edit.cshtml
│   ├── Details.cshtml
│   └── MyCampaigns.cshtml
├── Donation/
│   ├── Create.cshtml         → Step 1: Enter amount
│   ├── Review.cshtml         → Step 2: Review & Pay
│   ├── Success.cshtml        → Step 3: Success page
│   ├── MyDonations.cshtml
│   ├── DonorDetails.cshtml
│   ├── Details.cshtml        → NGO view
│   └── Received.cshtml       → NGO donations list
├── DonorDashboard/
│   └── Index.cshtml
├── Profile/
│   ├── Index.cshtml          → NGO profile view
│   ├── Edit.cshtml           → NGO profile edit
│   └── VerificationStatus.cshtml
├── Home/
│   └── Index.cshtml          → Homepage
└── Shared/
    ├── _Layout.cshtml        → Main layout with navbar
    └── Error.cshtml
```

---

## 🔍 Key Code Locations

### Registration Flow
```csharp
// AccountController.cs - Line ~50
[HttpPost]
public async Task<IActionResult> RegisterDonor(DonorRegisterViewModel model)

// AccountController.cs - Line ~120
[HttpPost]
public async Task<IActionResult> RegisterNGO(NGORegisterViewModel model)
```

### NGO Approval
```csharp
// AdminController.cs - Line ~80
[HttpPost]
public async Task<IActionResult> ApproveNGO(int id)

// AdminController.cs - Line ~100
[HttpPost]
public async Task<IActionResult> RejectNGO(int ngoId, string rejectionReason)
```

### Campaign Creation
```csharp
// CampaignController.cs - Line ~150
[HttpPost]
[Authorize(Roles = "NGO")]
public async Task<IActionResult> Create(CreateCampaignViewModel model)
```

### Campaign Status Logic
```csharp
// Extensions/CampaignExtensions.cs - Line ~10
public static string GetStatus(this Campaign campaign)
{
    if (campaign.IsCancelled) return "Cancelled";
    if (now < campaign.StartDate) return "Upcoming";
    if (now > campaign.EndDate) return "Completed";
    if (campaign.RaisedAmount >= campaign.GoalAmount) return "Goal Reached";
    return "Active";
}
```

### Donation Flow
```csharp
// DonationController.cs - Line ~80
[HttpPost]
public async Task<IActionResult> Review()  // Creates order

// DonationController.cs - Line ~200
[HttpPost]
public async Task<IActionResult> VerifyPayment(...)  // Verifies & completes
```

### Razorpay Integration
```csharp
// DonationController.cs - Line ~350
private async Task<dynamic> CreateRazorpayOrder(Donation donation)

// DonationController.cs - Line ~200
// HMAC SHA256 signature verification
var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
if (hash != razorpay_signature) return error;
```

### PDF Generation
```csharp
// Helpers/ReceiptPdfGenerator.cs - Line ~15
public static byte[] GenerateReceipt(Donation donation)
{
    return Document.Create(...).GeneratePdf();
}
```

### Email Sending
```csharp
// Helpers/EmailHelper.cs - Line ~20
public static async Task SendEmailAsync(
    string toEmail, string subject, string body,
    byte[] pdfAttachment, ...)
```

---

## ⚙️ Configuration Files

### Database Connection
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=CareFund;..."
  }
}
```

### Razorpay Keys
```json
// appsettings.json
{
  "Razorpay": {
    "KeyId": "rzp_test_YOUR_KEY",
    "KeySecret": "YOUR_SECRET"
  }
}
```

### Email Settings
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

## 🧪 Testing Quick Guide

### 1. Test User Registration
```
1. Visit /Account/RegisterDonor
2. Fill form, submit
3. Should auto-login and redirect to /Campaign/Index
```

### 2. Test NGO Registration & Approval
```
1. Visit /Account/RegisterNGO
2. Upload documents (cert, 80G, logo)
3. Submit → Redirect to /Profile/VerificationStatus
4. Login as admin@carefund.com (Admin@123)
5. Go to /Admin/NGOs
6. Click "View Details" on pending NGO
7. Click "Approve" → Status changes to Approved
```

### 3. Test Campaign Creation
```
1. Login as approved NGO
2. Go to /Campaign/Create
3. Fill form (min goal: ₹1000)
4. Upload logo (optional)
5. Submit → Redirect to /Campaign/MyCampaigns
```

### 4. Test Donation Flow
```
1. Login as donor
2. Go to /Campaign/Index
3. Click "Donate" on active campaign
4. Enter amount (min ₹10)
5. Click "Continue to Review"
6. Review details, click "Confirm & Pay"
7. Razorpay modal opens
8. Use test card: 4111 1111 1111 1111
9. Any future expiry, any CVV
10. Payment succeeds → Email sent → PDF attached
11. Verify email received with PDF
12. Download receipt from My Donations
```

### 5. Test Search & Filtering
```
1. Go to /Campaign/Index
2. Try text search: "education"
3. Try category filter: "Healthcare"
4. Try status filter: "Active"
5. Combine multiple filters
```

---

## 🚨 Common Issues & Solutions

### Issue: Port 5212 already in use
```bash
# Find process
lsof -i :5212

# Kill process
kill -9 <PID>

# Or change port in Properties/launchSettings.json
```

### Issue: Razorpay signature verification fails
```
✓ Check KeyId and KeySecret in appsettings.json
✓ Verify signature calculation: order_id + "|" + payment_id
✓ Use HMAC SHA256 with KeySecret
✓ Compare lowercase hex strings
```

### Issue: PDF generation fails
```
✓ Use QuestPDF color constants (Colors.Blue.Medium)
✓ DO NOT use hex colors ("#3b82f6")
✓ DO NOT use special glyphs (✓, ✗, etc.)
✓ Check ILogger is injected in controller
```

### Issue: Email not sending
```
✓ Use Gmail App Password (not regular password)
✓ Enable 2FA on Gmail account
✓ Generate App Password from Google Account settings
✓ Check SMTP port is 587 with EnableSsl = true
```

### Issue: NGO cannot create campaigns
```
✓ Check VerificationStatus is Approved
✓ Check user is in "NGO" role
✓ Check [Authorize(Roles = "NGO")] on action
```

---

## 📊 Database Relationships

```
ApplicationUser (1) ─── (1) NGO
                 │
                 └─── (N) Donations

NGO (1) ─── (N) Campaigns
      │
      └─── (N) NGODocuments

Campaign (1) ─── (N) Donations

Donation (1) ─── (1) Payment
```

---

## 🎯 Business Rules Checklist

✅ NGOs must be Approved before creating campaigns  
✅ Rejected NGOs cannot resubmit  
✅ Only Active campaigns accept donations  
✅ Campaigns with donations cannot be edited  
✅ StartDate must be today or future  
✅ EndDate must be after StartDate  
✅ Minimum donation amount: ₹10  
✅ Minimum campaign goal: ₹1,000  
✅ Only completed payments generate receipts  
✅ Razorpay signature must be verified  
✅ Campaign RaisedAmount updates only after payment verification  
✅ Anonymous donations hide donor information  
✅ Donors can only view their own donations  
✅ NGOs can only edit their own campaigns  

---

**Quick Reference Last Updated:** October 5, 2026  
**For detailed explanations, see:** `CODEBASE_FLOWS_GUIDE.md`
