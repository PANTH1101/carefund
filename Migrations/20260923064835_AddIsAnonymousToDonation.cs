using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NGODonationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAnonymousToDonation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymous",
                table: "Donations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAnonymous",
                table: "Donations");
        }
    }
}
