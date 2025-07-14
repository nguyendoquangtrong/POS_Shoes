using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS_Shoes.Migrations
{
    /// <inheritdoc />
    public partial class updateStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "status",
                table: "ReturnReceipts",
                newName: "Status");

            migrationBuilder.AddColumn<string>(
                name: "Quantity",
                table: "ReturnReceipts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "ReturnReceipts");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "ReturnReceipts",
                newName: "status");
        }
    }
}
