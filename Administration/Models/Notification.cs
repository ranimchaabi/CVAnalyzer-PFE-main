using System.ComponentModel.DataAnnotations;

namespace Administration.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        public int RecipientUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Info";
        public string? LinkUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public virtual Utilisateur? RecipientUser { get; set; }
    }
}
