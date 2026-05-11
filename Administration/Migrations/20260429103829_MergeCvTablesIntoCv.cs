using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Administration.Migrations
{
    /// <inheritdoc />
    public partial class MergeCvTablesIntoCv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AutresInfos",
                table: "Cv",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Competences",
                table: "Cv",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Cv",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Experience",
                table: "Cv",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NiveauEducation",
                table: "Cv",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomCandidat",
                table: "Cv",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telephone",
                table: "Cv",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE c
SET
    c.NomCandidat = d.NomCandidat,
    c.Email = d.Email,
    c.Telephone = d.Telephone
FROM Cv AS c
INNER JOIN DonneesCvs AS d ON d.CvId = c.Id
");

            migrationBuilder.Sql(@"
UPDATE c
SET c.Competences = s.Skills
FROM Cv AS c
INNER JOIN (
    SELECT cc.CvId, STRING_AGG(cp.Nom, ', ') AS Skills
    FROM CvCompetences AS cc
    INNER JOIN Competences AS cp ON cp.Id = cc.CompetenceId
    GROUP BY cc.CvId
) AS s ON s.CvId = c.Id
WHERE c.Competences IS NULL OR LTRIM(RTRIM(c.Competences)) = ''
");

            migrationBuilder.DropTable(
                name: "CvCompetences");

            migrationBuilder.DropTable(
                name: "DonneesCvs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutresInfos",
                table: "Cv");

            migrationBuilder.DropColumn(
                name: "Competences",
                table: "Cv");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Cv");

            migrationBuilder.DropColumn(
                name: "Experience",
                table: "Cv");

            migrationBuilder.DropColumn(
                name: "NiveauEducation",
                table: "Cv");

            migrationBuilder.DropColumn(
                name: "NomCandidat",
                table: "Cv");

            migrationBuilder.DropColumn(
                name: "Telephone",
                table: "Cv");

            migrationBuilder.CreateTable(
                name: "CvCompetences",
                columns: table => new
                {
                    CvId = table.Column<int>(type: "int", nullable: false),
                    CompetenceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvCompetences", x => new { x.CvId, x.CompetenceId });
                    table.ForeignKey(
                        name: "FK_CvCompetences_Competences_CompetenceId",
                        column: x => x.CompetenceId,
                        principalTable: "Competences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CvCompetences_Cv_CvId",
                        column: x => x.CvId,
                        principalTable: "Cv",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DonneesCvs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CvId = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NomCandidat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telephone = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonneesCvs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonneesCvs_Cv_CvId",
                        column: x => x.CvId,
                        principalTable: "Cv",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CvCompetences_CompetenceId",
                table: "CvCompetences",
                column: "CompetenceId");

            migrationBuilder.CreateIndex(
                name: "IX_DonneesCvs_CvId",
                table: "DonneesCvs",
                column: "CvId",
                unique: true);
        }
    }
}
