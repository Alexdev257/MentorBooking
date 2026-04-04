using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSummaryQueueToAudioTranscript : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "summary_queue_error",
                table: "audio_transcripts",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "summary_queue_status",
                table: "audio_transcripts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "summary_requested_by",
                table: "audio_transcripts",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "summary_queue_error",
                table: "audio_transcripts");

            migrationBuilder.DropColumn(
                name: "summary_queue_status",
                table: "audio_transcripts");

            migrationBuilder.DropColumn(
                name: "summary_requested_by",
                table: "audio_transcripts");
        }
    }
}
