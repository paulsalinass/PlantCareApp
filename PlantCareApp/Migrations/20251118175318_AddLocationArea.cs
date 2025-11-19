using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCareApp.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocationArea",
                table: "Plants",
                type: "TEXT",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationArea",
                table: "Plants");
        }
    }
}
