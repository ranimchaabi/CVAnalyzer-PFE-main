using Microsoft.EntityFrameworkCore;
using CvParsing.Models;

namespace CvParsing.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Utilisateur> Utilisateurs { get; set; }
    public DbSet<Cv> Cvs { get; set; }
    public DbSet<OffreEmploi> OffresEmploi { get; set; }
    public DbSet<Competence> Competences { get; set; }
    public DbSet<CvCompetence> CvCompetences { get; set; }
    public DbSet<DonneesCv> DonneesCvs { get; set; }
    public DbSet<Match> Matches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Many-to-many relationship for CvCompetence
        modelBuilder.Entity<CvCompetence>()
            .HasKey(cc => new { cc.CvId, cc.CompetenceId });

        modelBuilder.Entity<CvCompetence>()
            .HasOne(cc => cc.Cv)
            .WithMany(c => c.CvCompetences)
            .HasForeignKey(cc => cc.CvId);

        // One-to-one between Cv and DonneesCv
        modelBuilder.Entity<DonneesCv>()
            .HasOne(d => d.Cv)
            .WithOne(c => c.DonneesCv)
            .HasForeignKey<DonneesCv>(d => d.CvId);

        // Cv -> Utilisateur
        modelBuilder.Entity<Cv>()
            .HasOne(c => c.Utilisateur)
            .WithMany()
            .HasForeignKey(c => c.UtilisateurId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}