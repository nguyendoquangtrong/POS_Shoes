using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS_Shoes.Migrations
{
    /// <inheritdoc />
    public partial class updateReturnReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "ReturnReceipts");

            migrationBuilder.AddColumn<double>(
                name: "TotalRefundAmount",
                table: "ReturnReceipts",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<Guid>(
                name: "UserID",
                table: "ReturnReceipts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ReturnReceiptDetail",
                columns: table => new
                {
                    ReturnReceiptDetailID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnReceiptID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Size = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnReceiptDetail", x => x.ReturnReceiptDetailID);
                    table.ForeignKey(
                        name: "FK_ReturnReceiptDetail_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReturnReceiptDetail_ReturnReceipts_ReturnReceiptID",
                        column: x => x.ReturnReceiptID,
                        principalTable: "ReturnReceipts",
                        principalColumn: "ReturnReceiptID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnReceipts_UserID",
                table: "ReturnReceipts",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnReceiptDetail_ProductID",
                table: "ReturnReceiptDetail",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnReceiptDetail_ReturnReceiptID",
                table: "ReturnReceiptDetail",
                column: "ReturnReceiptID");

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnReceipts_Users_UserID",
                table: "ReturnReceipts",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReturnReceipts_Users_UserID",
                table: "ReturnReceipts");

            migrationBuilder.DropTable(
                name: "ReturnReceiptDetail");

            migrationBuilder.DropIndex(
                name: "IX_ReturnReceipts_UserID",
                table: "ReturnReceipts");

            migrationBuilder.DropColumn(
                name: "TotalRefundAmount",
                table: "ReturnReceipts");

            migrationBuilder.DropColumn(
                name: "UserID",
                table: "ReturnReceipts");

            migrationBuilder.AddColumn<string>(
                name: "Quantity",
                table: "ReturnReceipts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
