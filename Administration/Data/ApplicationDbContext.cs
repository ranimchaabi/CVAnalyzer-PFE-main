using Microsoft.EntityFrameworkCore;
using Administration.Models;

namespace Administration.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<OffreEmploi> OffresEmploi { get; set; }
        public DbSet<Cv> Cvs { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Competence> Competences { get; set; }
        public DbSet<Departement> Departements { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TABLES
            modelBuilder.Entity<Utilisateur>().ToTable("Utilisateur");
            modelBuilder.Entity<OffreEmploi>().ToTable("OffreEmploi");
            modelBuilder.Entity<Cv>().ToTable("Cv");
            modelBuilder.Entity<Match>().ToTable("Match");
            modelBuilder.Entity<Departement>().ToTable("Departement");
            modelBuilder.Entity<Notification>().ToTable("Notification");

            // CV
            modelBuilder.Entity<Cv>()
                .HasOne(c => c.Offre)
                .WithMany(o => o.Cvs)
                .HasForeignKey(c => c.OffreId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Cv>()
                .HasOne(c => c.Utilisateur)
                .WithMany()
                .HasForeignKey(c => c.UtilisateurId)
                .OnDelete(DeleteBehavior.NoAction);

            // MATCH (🔥 المهم هنا)
            modelBuilder.Entity<Match>()
                .HasOne(m => m.Cv)
                .WithMany(c => c.Matches)
                .HasForeignKey(m => m.CvId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.Offre)
                .WithMany()
                .HasForeignKey(m => m.OffreId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.RecipientUser)
                .WithMany()
                .HasForeignKey(n => n.RecipientUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // SEED
            modelBuilder.Entity<Utilisateur>().HasData(new Utilisateur
            {
                Id = 1,
                NomUtilisateur = "admin",
                Email = "admin@gmail.com",
                MotPasse = "$2a$11$8K1pQYlG9lYk1ExampleHashReplaceThis1234567890",
                Role = "Admin",
                IsActive = true,
                DateCreation = new DateTime(2024, 1, 1)
            });
        }
    }
}