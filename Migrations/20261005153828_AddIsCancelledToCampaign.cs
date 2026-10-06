using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NGODonationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCancelledToCampaign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCancelled",
                table: "Campaigns",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCancelled",
                table: "Campaigns");
        }
    }
}
