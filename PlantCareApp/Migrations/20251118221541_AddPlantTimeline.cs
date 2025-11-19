using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantCareApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantTimeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlantTimelineEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlantId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PhotoId = table.Column<int>(type: "INTEGER", nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    AccentCss = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantTimelineEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantTimelineEvents_PlantPhotos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "PlantPhotos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlantTimelineEvents_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlantTimelineEvents_PhotoId",
                table: "PlantTimelineEvents",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantTimelineEvents_PlantId",
                table: "PlantTimelineEvents",
                column: "PlantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlantTimelineEvents");
        }
    }
}
