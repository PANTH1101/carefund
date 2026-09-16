namespace NGODonationSystem.Models
{
    public class NGODocument
    {
        public int Id { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public byte[] FileData { get; set; } = Array.Empty<byte>();

        public int NGOId { get; set; }

        public NGO NGO { get; set; } = null!;
    }
}