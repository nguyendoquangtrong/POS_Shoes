using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS_Shoes.Migrations
{
    /// <inheritdoc />
    public partial class updateReturnCreipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "ReturnReceipts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status",
                table: "ReturnReceipts");
        }
    }
}
