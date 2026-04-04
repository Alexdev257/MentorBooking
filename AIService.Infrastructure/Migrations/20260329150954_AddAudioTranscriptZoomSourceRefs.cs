using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioTranscriptZoomSourceRefs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "source_booking_id",
                table: "audio_transcripts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_meeting_recording_id",
                table: "audio_transcripts",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "source_booking_id",
                table: "audio_transcripts");

            migrationBuilder.DropColumn(
                name: "source_meeting_recording_id",
                table: "audio_transcripts");
        }
    }
}
