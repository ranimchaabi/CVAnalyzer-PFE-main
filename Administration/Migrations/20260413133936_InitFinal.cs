using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Administration.Migrations
{
    /// <inheritdoc />
    public partial class InitFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Competences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utilisateur",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NomUtilisateur = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MotPasse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateDerniereConnexion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateur", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OffreEmploi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Departement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Experience = table.Column<int>(type: "int", nullable: false),
                    NiveauEducation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdResponsable = table.Column<int>(type: "int", nullable: false),
                    ResponsableRHId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OffreEmploi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OffreEmploi_Utilisateur_ResponsableRHId",
                        column: x => x.ResponsableRHId,
                        principalTable: "Utilisateur",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Cv",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OffreId = table.Column<int>(type: "int", nullable: false),
                    UtilisateurId = table.Column<int>(type: "int", nullable: false),
                    CheminFichier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cv", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cv_OffreEmploi_OffreId",
                        column: x => x.OffreId,
                        principalTable: "OffreEmploi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cv_Utilisateur_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateur",
                        principalColumn: "Id");
                });

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
                    NomCandidat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "Match",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CvId = table.Column<int>(type: "int", nullable: false),
                    OffreId = table.Column<int>(type: "int", nullable: false),
                    CompetenceScore = table.Column<float>(type: "real", nullable: false),
                    DiplomeScore = table.Column<float>(type: "real", nullable: false),
                    ExperienceScore = table.Column<float>(type: "real", nullable: false),
                    GlobalScore = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Match", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Match_Cv_CvId",
                        column: x => x.CvId,
                        principalTable: "Cv",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Match_OffreEmploi_OffreId",
                        column: x => x.OffreId,
                        principalTable: "OffreEmploi",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Utilisateur",
                columns: new[] { "Id", "DateCreation", "DateDerniereConnexion", "Email", "IsActive", "MotPasse", "NomUtilisateur", "Role" },
                values: new object[] { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "admin@gmail.com", true, "$2a$11$8K1pQYlG9lYk1ExampleHashReplaceThis1234567890", "admin", "Admin" });

            migrationBuilder.CreateIndex(
                name: "IX_Cv_OffreId",
                table: "Cv",
                column: "OffreId");

            migrationBuilder.CreateIndex(
                name: "IX_Cv_UtilisateurId",
                table: "Cv",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_CvCompetences_CompetenceId",
                table: "CvCompetences",
                column: "CompetenceId");

            migrationBuilder.CreateIndex(
                name: "IX_DonneesCvs_CvId",
                table: "DonneesCvs",
                column: "CvId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Match_CvId",
                table: "Match",
                column: "CvId");

            migrationBuilder.CreateIndex(
                name: "IX_Match_OffreId",
                table: "Match",
                column: "OffreId");

            migrationBuilder.CreateIndex(
                name: "IX_OffreEmploi_ResponsableRHId",
                table: "OffreEmploi",
                column: "ResponsableRHId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CvCompetences");

            migrationBuilder.DropTable(
                name: "DonneesCvs");

            migrationBuilder.DropTable(
                name: "Match");

            migrationBuilder.DropTable(
                name: "Competences");

            migrationBuilder.DropTable(
                name: "Cv");

            migrationBuilder.DropTable(
                name: "OffreEmploi");

            migrationBuilder.DropTable(
                name: "Utilisateur");
        }
    }
}
