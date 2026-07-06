using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace V3RII.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportOperationsAndRag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HandoffReason",
                table: "SupportTickets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresHandoff",
                table: "SupportTickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResolvedAt",
                table: "SupportTickets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "SupportTickets",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HandoffReason",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "RequiresHandoff",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "SupportTickets");
        }
    }
}
