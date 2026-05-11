using Microsoft.EntityFrameworkCore;
using CvParsing.Models;

namespace CvParsing.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Utilisateur> Utilisateurs { get; set; }
    public DbSet<Cv> Cvs { get; set; }
<<<<<<< HEAD
    public DbSet<CvStructuredData> CvStructuredDatas { get; set; }
=======
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
    public DbSet<OffreEmploi> OffresEmploi { get; set; }
    public DbSet<Competence> Competences { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<Notification> Notifications { get; set; }
<<<<<<< HEAD
    public object Offres { get; internal set; }
=======
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Cv -> Utilisateur
        modelBuilder.Entity<Cv>()
            .HasOne(c => c.Utilisateur)
            .WithMany()
            .HasForeignKey(c => c.UtilisateurId)
            .OnDelete(DeleteBehavior.NoAction);
<<<<<<< HEAD

        // Cv -> CvStructuredData (one-to-one)
        modelBuilder.Entity<Cv>()
            .HasOne(c => c.StructuredData)
            .WithOne(d => d.Cv)
            .HasForeignKey<CvStructuredData>(d => d.CvId)
            .OnDelete(DeleteBehavior.Cascade);
=======
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
    }
}