using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OECSMS.Domain.Entities
{
    public class EmailVerification
    {
        [Key]
        public Guid Token { get; set; } = Guid.NewGuid();

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);

        public bool IsUsed { get; set; } = false;
    }

    public class PasswordResetToken
    {
        [Key]
        public Guid Token { get; set; } = Guid.NewGuid();

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(2);

        public bool IsUsed { get; set; } = false;
    }
}
