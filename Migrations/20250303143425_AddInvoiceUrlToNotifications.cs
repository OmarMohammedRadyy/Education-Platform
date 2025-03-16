using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationPlatformN.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceUrlToNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceUrl",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceUrl",
                table: "Notifications");
        }
    }
}
