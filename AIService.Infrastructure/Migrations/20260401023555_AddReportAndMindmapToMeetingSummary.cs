using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportAndMindmapToMeetingSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mindmap",
                table: "MeetingSummary",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "report",
                table: "MeetingSummary",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mindmap",
                table: "MeetingSummary");

            migrationBuilder.DropColumn(
                name: "report",
                table: "MeetingSummary");
        }
    }
}
