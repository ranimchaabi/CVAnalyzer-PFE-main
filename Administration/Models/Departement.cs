// Models/Departement.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Administration.Models
{
    [Table("Departement")]
    public class Departement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Nom { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime DateCreation { get; set; } = DateTime.Now;
    }
}
