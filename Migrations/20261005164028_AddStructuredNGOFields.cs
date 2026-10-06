using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NGODonationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredNGOFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ContactInformation",
                table: "NGOs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "NGOs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "NGOs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Pincode",
                table: "NGOs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "NGOs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "NGOs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "NGOs");

            migrationBuilder.DropColumn(
                name: "City",
                table: "NGOs");

            migrationBuilder.DropColumn(
                name: "Pincode",
                table: "NGOs");

            migrationBuilder.DropColumn(
                name: "State",
                table: "NGOs");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "NGOs");

            migrationBuilder.AlterColumn<string>(
                name: "ContactInformation",
                table: "NGOs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
