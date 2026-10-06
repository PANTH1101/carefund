using NGODonationSystem.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NGODonationSystem.Helpers
{
    public static class ReceiptPdfGenerator
    {
        public static byte[] GenerateReceipt(Donation donation)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                    page.Header().Column(column =>
                    {
                        // Top Row: CareFund branding, NGO Logo, and Status
                        column.Item().Row(row =>
                        {
                            // Left: CareFund branding
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("CAREFUND")
                                    .FontSize(22)
                                    .Bold()
                                    .FontColor(Colors.Blue.Medium);
                                
                                col.Item().Text("DONATION RECEIPT")
                                    .FontSize(11)
                                    .SemiBold()
                                    .FontColor(Colors.Grey.Darken2);
                            });

                            // Center: NGO Logo (if available)
                            if (donation.NGO.Logo != null && donation.NGO.Logo.Length > 0)
                            {
                                row.ConstantItem(80).AlignCenter().AlignMiddle().Height(50).Image(donation.NGO.Logo).FitArea();
                            }

                            // Right: Status badge
                            row.ConstantItem(100).AlignRight().AlignMiddle().Column(col =>
                            {
                                col.Item().Background(Colors.Green.Lighten3).Border(1).BorderColor(Colors.Green.Lighten1)
                                    .PaddingHorizontal(12).PaddingVertical(6)
                                    .AlignCenter()
                                    .Text("PAID")
                                    .FontSize(11)
                                    .Bold()
                                    .FontColor(Colors.Green.Darken2);
                            });
                        });

                        column.Item().PaddingTop(15).LineHorizontal(1.5f).LineColor(Colors.Blue.Medium);

                        // Receipt Number Section
                        column.Item().PaddingTop(12).Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten2)
                            .PaddingHorizontal(15).PaddingVertical(10).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Receipt Number")
                                        .FontSize(8)
                                        .SemiBold()
                                        .FontColor(Colors.Grey.Darken1);
                                    col.Item().Text(donation.ReceiptNumber)
                                        .FontSize(13)
                                        .Bold()
                                        .FontColor(Colors.Blue.Medium);
                                });

                                row.RelativeItem().AlignRight().Column(col =>
                                {
                                    col.Item().AlignRight().Text("Donation Date")
                                        .FontSize(8)
                                        .SemiBold()
                                        .FontColor(Colors.Grey.Darken1);
                                    col.Item().AlignRight().Text(donation.DonationDate.ToString("dd MMMM yyyy"))
                                        .FontSize(11)
                                        .SemiBold()
                                        .FontColor(Colors.Grey.Darken3);
                                });
                            });
                    });

                    page.Content().PaddingTop(20).Column(column =>
                    {
                        column.Spacing(12);

                        // Donation Amount - Prominent Display
                        column.Item().AlignCenter().Background(Colors.Green.Lighten4).Border(1).BorderColor(Colors.Green.Lighten2)
                            .PaddingVertical(18).PaddingHorizontal(20).Column(col =>
                            {
                                col.Item().AlignCenter().Text("DONATION AMOUNT")
                                    .FontSize(9)
                                    .SemiBold()
                                    .LetterSpacing(1)
                                    .FontColor(Colors.Grey.Darken1);
                                
                                col.Item().AlignCenter().PaddingTop(5).Text($"₹{donation.Amount:N2}")
                                    .FontSize(32)
                                    .Bold()
                                    .FontColor(Colors.Green.Darken2);
                            });

                        column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Two-Column Layout: Donor Info and Campaign Info
                        column.Item().PaddingTop(10).Row(row =>
                        {
                            // Left Column: Donor Information
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("DONOR INFORMATION")
                                    .FontSize(8)
                                    .SemiBold()
                                    .LetterSpacing(0.5f)
                                    .FontColor(Colors.Grey.Darken1);
                                
                                col.Item().PaddingTop(8).Column(innerCol =>
                                {
                                    innerCol.Item().Row(r =>
                                    {
                                        r.ConstantItem(80).Text("Donor Name")
                                            .FontSize(9)
                                            .SemiBold()
                                            .FontColor(Colors.Grey.Darken2);
                                        r.RelativeItem().Text(donation.IsAnonymous ? "Anonymous" : donation.Donor.FullName)
                                            .FontSize(10)
                                            .FontColor(Colors.Grey.Darken3);
                                    });

                                    innerCol.Item().PaddingTop(5).Row(r =>
                                    {
                                        r.ConstantItem(80).Text("Email")
                                            .FontSize(9)
                                            .SemiBold()
                                            .FontColor(Colors.Grey.Darken2);
                                        r.RelativeItem().Text(donation.IsAnonymous ? "Hidden" : (donation.Donor.Email ?? "N/A"))
                                            .FontSize(9)
                                            .FontColor(Colors.Grey.Darken1);
                                    });
                                });
                            });

                            row.ConstantItem(30);

                            // Right Column: Campaign and NGO
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("CAMPAIGN")
                                    .FontSize(8)
                                    .SemiBold()
                                    .LetterSpacing(0.5f)
                                    .FontColor(Colors.Grey.Darken1);
                                
                                col.Item().PaddingTop(8).Text(donation.Campaign.Title)
                                    .FontSize(11)
                                    .SemiBold()
                                    .FontColor(Colors.Grey.Darken3);

                                col.Item().PaddingTop(12).Text("NGO")
                                    .FontSize(8)
                                    .SemiBold()
                                    .LetterSpacing(0.5f)
                                    .FontColor(Colors.Grey.Darken1);
                                
                                col.Item().PaddingTop(3).Text(donation.NGO.Name)
                                    .FontSize(10)
                                    .SemiBold()
                                    .FontColor(Colors.Grey.Darken3);
                            });
                        });

                        column.Item().PaddingTop(12).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Payment Information Section
                        column.Item().PaddingTop(10).Column(col =>
                        {
                            col.Item().Text("PAYMENT INFORMATION")
                                .FontSize(8)
                                .SemiBold()
                                .LetterSpacing(0.5f)
                                .FontColor(Colors.Grey.Darken1);

                            col.Item().PaddingTop(8).Background(Colors.Grey.Lighten3)
                                .PaddingHorizontal(12).PaddingVertical(10).Column(innerCol =>
                                {
                                    innerCol.Spacing(5);

                                    innerCol.Item().Row(r =>
                                    {
                                        r.ConstantItem(150).Text("Donation ID")
                                            .FontSize(9)
                                            .SemiBold()
                                            .FontColor(Colors.Grey.Darken2);
                                        r.RelativeItem().Text(donation.Id.ToString())
                                            .FontSize(9)
                                            .FontColor(Colors.Grey.Darken3);
                                    });

                                    innerCol.Item().Row(r =>
                                    {
                                        r.ConstantItem(150).Text("Receipt Number")
                                            .FontSize(9)
                                            .SemiBold()
                                            .FontColor(Colors.Grey.Darken2);
                                        r.RelativeItem().Text(donation.ReceiptNumber)
                                            .FontSize(9)
                                            .FontColor(Colors.Blue.Medium);
                                    });

                                    if (!string.IsNullOrEmpty(donation.TransactionId))
                                    {
                                        innerCol.Item().Row(r =>
                                        {
                                            r.ConstantItem(150).Text("Razorpay Transaction ID")
                                                .FontSize(9)
                                                .SemiBold()
                                                .FontColor(Colors.Grey.Darken2);
                                            r.RelativeItem().Text(donation.TransactionId)
                                                .FontSize(8)
                                                .FontColor(Colors.Grey.Darken1);
                                        });
                                    }
                                });
                        });

                        // Thank You Section
                        column.Item().PaddingTop(20).AlignCenter().Column(col =>
                        {
                            col.Item().AlignCenter().Text("Thank you for your generous contribution.")
                                .FontSize(11)
                                .SemiBold()
                                .FontColor(Colors.Grey.Darken2);
                            
                            col.Item().AlignCenter().PaddingTop(3).Text("You are helping make a difference.")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);
                        });
                    });

                    page.Footer().Column(column =>
                    {
                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        
                        column.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("CareFund")
                                    .FontSize(9)
                                    .SemiBold()
                                    .FontColor(Colors.Grey.Darken2);
                                col.Item().Text("NGO Donation Management System")
                                    .FontSize(8)
                                    .FontColor(Colors.Grey.Medium);
                            });

                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().AlignRight().Text("Generated on")
                                    .FontSize(7)
                                    .FontColor(Colors.Grey.Medium);
                                col.Item().AlignRight().Text(DateTime.Now.ToString("dd MMMM yyyy HH:mm"))
                                    .FontSize(8)
                                    .FontColor(Colors.Grey.Darken1);
                            });
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
