using NGODonationSystem.Models;

namespace NGODonationSystem.Extensions
{
    /// <summary>
    /// Extension methods for Campaign model to centralize status calculation logic
    /// </summary>
    public static class CampaignExtensions
    {
        /// <summary>
        /// Calculate the current status of a campaign based on:
        /// - Raised amount vs target
        /// - Current date vs start/deadline dates
        /// 
        /// Status Priority:
        /// 1. Completed (if raised >= target)
        /// 2. Expired (if deadline passed)
        /// 3. Active (if between start and deadline)
        /// 4. Scheduled (if before start date)
        /// </summary>
        public static string GetStatus(this Campaign campaign, decimal raisedAmount)
        {
            var today = DateTime.Today;

            // Check if completed (target reached)
            if (raisedAmount >= campaign.TargetAmount)
            {
                return "Completed";
            }

            // Check if expired (deadline passed)
            if (campaign.Deadline < today)
            {
                return "Expired";
            }

            // Check if active (between start and deadline)
            if (campaign.StartDate <= today && campaign.Deadline >= today)
            {
                return "Active";
            }

            // Not yet started
            return "Scheduled";
        }

        /// <summary>
        /// Check if a campaign can accept donations
        /// </summary>
        public static bool CanAcceptDonations(this Campaign campaign, decimal raisedAmount)
        {
            var status = campaign.GetStatus(raisedAmount);
            return status == "Active";
        }
    }
}
