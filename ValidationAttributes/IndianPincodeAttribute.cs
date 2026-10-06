using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NGODonationSystem.ValidationAttributes
{
    public class IndianPincodeAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                // Let [Required] handle null/empty validation
                return ValidationResult.Success;
            }

            string pincode = value.ToString()!;

            // Indian PIN code: exactly 6 digits
            if (Regex.IsMatch(pincode, @"^\d{6}$"))
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(ErrorMessage ?? "Pincode must be a valid 6-digit Indian PIN code");
        }
    }
}
