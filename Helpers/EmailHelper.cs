using System.Net;
using System.Net.Mail;

namespace NGODonationSystem.Helpers
{
    public class EmailHelper
    {
        private readonly IConfiguration _configuration;

        public EmailHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendDonationReceiptAsync(string toEmail, string toName, string receiptNumber, 
            string campaignTitle, decimal amount, DateTime donationDate, int donationId, 
            string transactionId, byte[] pdfAttachment)
        {
            try
            {
                var host = _configuration["Email:Host"];
                var port = int.Parse(_configuration["Email:Port"] ?? "587");
                var username = _configuration["Email:Username"];
                var password = _configuration["Email:Password"];
                var fromEmail = _configuration["Email:FromEmail"];
                var fromName = _configuration["Email:FromName"] ?? "CareFund";
                var enableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true");

                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || 
                    string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fromEmail))
                {
                    return false;
                }

                using var smtpClient = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    Credentials = new NetworkCredential(username, password)
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = $"CareFund Donation Receipt - {receiptNumber}",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(new MailAddress(toEmail, toName));

                // Email Body
                mailMessage.Body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #0d6efd; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f8f9fa; }}
        .detail-row {{ padding: 10px 0; border-bottom: 1px solid #dee2e6; }}
        .detail-label {{ font-weight: bold; }}
        .amount {{ font-size: 24px; color: #198754; font-weight: bold; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #6c757d; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>CareFund</h1>
            <h2>Donation Receipt</h2>
        </div>
        <div class=""content"">
            <p>Dear {toName},</p>
            <p>Thank you for your generous donation to:</p>
            <h3 style=""color: #0d6efd;"">{campaignTitle}</h3>
            
            <div class=""detail-row"">
                <span class=""detail-label"">Donation Amount:</span>
                <div class=""amount"">₹{amount:N2}</div>
            </div>
            
            <div class=""detail-row"">
                <span class=""detail-label"">Receipt Number:</span>
                <span>{receiptNumber}</span>
            </div>
            
            <div class=""detail-row"">
                <span class=""detail-label"">Donation ID:</span>
                <span>{donationId}</span>
            </div>
            
            <div class=""detail-row"">
                <span class=""detail-label"">Donation Date:</span>
                <span>{donationDate:dd MMMM yyyy}</span>
            </div>
            
            <div class=""detail-row"">
                <span class=""detail-label"">Transaction ID:</span>
                <span>{transactionId}</span>
            </div>
            
            <p style=""margin-top: 20px;"">Your donation receipt is attached to this email as a PDF.</p>
            
            <p>Thank you for supporting CareFund and making a difference!</p>
        </div>
        <div class=""footer"">
            <p>This is an automated email. Please do not reply.</p>
            <p>&copy; 2026 CareFund. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

                // Attach PDF
                if (pdfAttachment != null && pdfAttachment.Length > 0)
                {
                    var attachment = new Attachment(new MemoryStream(pdfAttachment), 
                        $"CareFund-Receipt-{receiptNumber}.pdf", "application/pdf");
                    mailMessage.Attachments.Add(attachment);
                }

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch
            {
                // Email failure should not crash the application
                // Donation is already successful in database
                return false;
            }
        }
    }
}
