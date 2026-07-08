using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace V3RII.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportTicketLeadScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LeadScore",
                table: "SupportTickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LeadSegment",
                table: "SupportTickets",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "cold");

            migrationBuilder.AddColumn<string>(
                name: "LeadSignalsJson",
                table: "SupportTickets",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LeadScore",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "LeadSegment",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "LeadSignalsJson",
                table: "SupportTickets");
        }
    }
}
