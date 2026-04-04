using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceUrlToAudioTranscript : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "source_url",
                table: "audio_transcripts",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "source_url",
                table: "audio_transcripts");
        }
    }
}
