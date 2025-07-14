using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS_Shoes.Migrations
{
    /// <inheritdoc />
    public partial class updateUserAndPayslip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateEnd",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "DateStart",
                table: "PaySlips");

            migrationBuilder.RenameColumn(
                name: "TotalTime",
                table: "PaySlips",
                newName: "TotalHours");

            migrationBuilder.RenameColumn(
                name: "TotalPay",
                table: "PaySlips",
                newName: "NetSalary");

            migrationBuilder.RenameColumn(
                name: "DateCreated",
                table: "PaySlips",
                newName: "PayPeriodStart");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "HireDate",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "Users",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyBonus",
                table: "Users",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BasicSalary",
                table: "PaySlips",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Bonus",
                table: "PaySlips",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PaySlips",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserID",
                table: "PaySlips",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "Deduction",
                table: "PaySlips",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "PaySlips",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "PaySlips",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PayPeriodEnd",
                table: "PaySlips",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "PaySlips",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PaySlips",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HireDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MonthlyBonus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BasicSalary",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "Bonus",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "CreatedByUserID",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "Deduction",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "PayPeriodEnd",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PaySlips");

            migrationBuilder.RenameColumn(
                name: "TotalHours",
                table: "PaySlips",
                newName: "TotalTime");

            migrationBuilder.RenameColumn(
                name: "PayPeriodStart",
                table: "PaySlips",
                newName: "DateCreated");

            migrationBuilder.RenameColumn(
                name: "NetSalary",
                table: "PaySlips",
                newName: "TotalPay");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateEnd",
                table: "PaySlips",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateStart",
                table: "PaySlips",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }
    }
}
