using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emissions.Data.Migrations
{
    public partial class UserLimitsAndRefs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CarbonEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreationTimestamp",
                table: "CarbonEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "CarbonEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "DailyEmissionsWarningThreshold",
                table: "AspNetUsers",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<decimal>(
                name: "MontlyExpensesWarningThreshold",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_CarbonEntries_UserId",
                table: "CarbonEntries",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CarbonEntries_AspNetUsers_UserId",
                table: "CarbonEntries",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarbonEntries_AspNetUsers_UserId",
                table: "CarbonEntries");

            migrationBuilder.DropIndex(
                name: "IX_CarbonEntries_UserId",
                table: "CarbonEntries");

            migrationBuilder.DropColumn(
                name: "CreationTimestamp",
                table: "CarbonEntries");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CarbonEntries");

            migrationBuilder.DropColumn(
                name: "DailyEmissionsWarningThreshold",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MontlyExpensesWarningThreshold",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CarbonEntries",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
