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
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .Column(column =>
                        {
                            // CareFund Header
                            column.Item().AlignCenter().Text("CAREFUND")
                                .FontSize(24)
                                .Bold()
                                .FontColor(Colors.Blue.Medium);

                            column.Item().AlignCenter().Text("DONATION RECEIPT")
                                .FontSize(16)
                                .Bold()
                                .FontColor(Colors.Grey.Darken2);

                            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                        });

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(15);

                            // NGO Logo (if available)
                            if (donation.NGO.Logo != null && donation.NGO.Logo.Length > 0)
                            {
                                column.Item().AlignCenter().Image(donation.NGO.Logo).FitWidth();
                            }

                            // NGO Information
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("NGO:").Bold().FontSize(12);
                                    col.Item().PaddingLeft(10).Text(donation.NGO.Name).FontSize(14);
                                });
                            });

                            column.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                            // Donor Information
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Donor:").Bold().FontSize(12);
                                    col.Item().PaddingLeft(10).Text(donation.Donor.FullName).FontSize(14);
                                });
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Email:").Bold().FontSize(12);
                                    col.Item().PaddingLeft(10).Text(donation.Donor.Email ?? "N/A").FontSize(12).FontColor(Colors.Grey.Darken1);
                                });
                            });

                            column.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                            // Campaign Information
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Campaign:").Bold().FontSize(12);
                                    col.Item().PaddingLeft(10).Text(donation.Campaign.Title).FontSize(14);
                                });
                            });

                            column.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                            // Donation Details
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Amount:").Bold().FontSize(12);
                                    col.Item().PaddingLeft(10).Text($"₹{donation.Amount:N2}").FontSize(16).Bold().FontColor(Colors.Green.Darken2);
                                });
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Donation Date:").Bold().FontSize(12);
                                    col.Item().PaddingLeft(10).Text(donation.DonationDate.ToString("dd MMMM yyyy")).FontSize(12);
                                });
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Donation ID:").Bold().FontSize(12);
                                    col.Item().PaddingLeft(10).Text(donation.Id.ToString()).FontSize(12);
                                });
                            });

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Receipt Number:").Bold().FontSize(12);
                                    col.Item().PaddingLeft(10).Text(donation.ReceiptNumber).FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                                });
                            });

                            if (!string.IsNullOrEmpty(donation.TransactionId))
                            {
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("Razorpay Transaction ID:").Bold().FontSize(12);
                                        col.Item().PaddingLeft(10).Text(donation.TransactionId).FontSize(10).FontColor(Colors.Grey.Darken1);
                                    });
                                });
                            }

                            column.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                            // Thank You Message
                            column.Item().AlignCenter().PaddingTop(20).Text("Thank you for your donation.")
                                .FontSize(14)
                                .Italic()
                                .FontColor(Colors.Grey.Darken2);

                            column.Item().AlignCenter().Text("Your contribution makes a difference.")
                                .FontSize(12)
                                .FontColor(Colors.Grey.Darken1);
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Generated on ").FontSize(9).FontColor(Colors.Grey.Medium);
                            text.Span(DateTime.Now.ToString("dd MMM yyyy HH:mm")).FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                });
            });

            return document.GeneratePdf();
        }
    }
}
