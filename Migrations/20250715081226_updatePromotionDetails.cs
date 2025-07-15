using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS_Shoes.Migrations
{
    /// <inheritdoc />
    public partial class updatePromotionDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Promotions_PromotionID",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_PromotionID",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PromotionID",
                table: "Products");

            migrationBuilder.CreateTable(
                name: "PromotionDetails",
                columns: table => new
                {
                    PromotionDetailsID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromotionID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductID = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionDetails", x => x.PromotionDetailsID);
                    table.ForeignKey(
                        name: "FK_PromotionDetails_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromotionDetails_Promotions_PromotionID",
                        column: x => x.PromotionID,
                        principalTable: "Promotions",
                        principalColumn: "PromotionID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionDetails_ProductID",
                table: "PromotionDetails",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionDetails_PromotionID",
                table: "PromotionDetails",
                column: "PromotionID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromotionDetails");

            migrationBuilder.AddColumn<Guid>(
                name: "PromotionID",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_PromotionID",
                table: "Products",
                column: "PromotionID");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Promotions_PromotionID",
                table: "Products",
                column: "PromotionID",
                principalTable: "Promotions",
                principalColumn: "PromotionID");
        }
    }
}
