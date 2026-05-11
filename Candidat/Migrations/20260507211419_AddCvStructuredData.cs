using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvParsing.Migrations
{
    /// <inheritdoc />
    public partial class AddCvStructuredData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CvStructuredData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CvId = table.Column<int>(type: "int", nullable: false),
                    Competences = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Experiences = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Diplomes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvStructuredData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CvStructuredData_Cv_CvId",
                        column: x => x.CvId,
                        principalTable: "Cv",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CvStructuredData_CvId",
                table: "CvStructuredData",
                column: "CvId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CvStructuredData");
        }
    }
}
