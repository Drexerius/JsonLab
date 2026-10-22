using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JsonLab.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDocumentVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Documents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Documents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
