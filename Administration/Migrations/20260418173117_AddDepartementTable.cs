using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Administration.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartementTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departement", x => x.Id);
                });

            // Seed initial departments from organizational chart
            migrationBuilder.InsertData(
                table: "Departement",
                columns: new[] { "Nom", "Description", "DateCreation", "IsActive" },
                values: new object[,]
                {
                    { "Structure visites de risques", "Structure des visites de risques", DateTime.Now, true },
                    { "Département Santé et Prévoyance Collective", "Département Santé et Prévoyance Collective", DateTime.Now, true },
                    { "Département Vie et Capitalisation", "Département Vie et Capitalisation", DateTime.Now, true },
                    { "Département Réassurance", "Département Réassurance", DateTime.Now, true },
                    { "Direction Automobile et Assurances des Particuliers", "Direction Automobile et Assurances des Particuliers", DateTime.Now, true },
                    { "Direction Assurances d'Entreprises", "Direction Assurances d'Entreprises", DateTime.Now, true },
                    { "Direction des Affaires Administratives et GRH", "Direction des Affaires Administratives et GRH", DateTime.Now, true },
                    { "Direction Financière", "Direction Financière", DateTime.Now, true },
                    { "Direction Comptabilité et Contrôle de Gestion", "Direction Comptabilité et Contrôle de Gestion", DateTime.Now, true },
                    { "Direction Commerciale", "Direction Commerciale", DateTime.Now, true },
                    { "Direction des Systèmes d'Information", "Direction des Systèmes d'Information", DateTime.Now, true },
                    { "Direction Audit Stratégie et Risques", "Direction Audit Stratégie et Risques", DateTime.Now, true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Departement");
        }
    }
}
