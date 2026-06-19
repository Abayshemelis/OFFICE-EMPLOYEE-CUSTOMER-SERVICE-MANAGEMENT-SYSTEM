using System;
using Microsoft.EntityFrameworkCore;
using OECSMS.Domain.Entities;
using OECSMS.Domain.Enums;
using Task = OECSMS.Domain.Entities.Task;

namespace OECSMS.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Task> Tasks { get; set; } = null!;
        public DbSet<TaskAuditLog> TaskAuditLogs { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<ServiceRequest> ServiceRequests { get; set; } = null!;
        public DbSet<ContactManagerRequest> ContactManagerRequests { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<AssistantConductScore> AssistantConductScores { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.UserId).ValueGeneratedOnAdd();
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
                entity.Property(e => e.PasswordHash).HasMaxLength(512).IsRequired();
                entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Role).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Phone).HasMaxLength(20);

                entity.HasOne(d => d.Manager)
                    .WithMany(p => p.Assistants)
                    .HasForeignKey(d => d.ManagerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Task configuration
            modelBuilder.Entity<Task>(entity =>
            {
                entity.HasKey(e => e.TaskId);
                entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
                entity.Property(e => e.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

                entity.HasOne(d => d.AssignedTo)
                    .WithMany(p => p.AssignedTasks)
                    .HasForeignKey(d => d.AssignedToId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.AssignedBy)
                    .WithMany(p => p.CreatedTasks)
                    .HasForeignKey(d => d.AssignedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // TaskAuditLog configuration
            modelBuilder.Entity<TaskAuditLog>(entity =>
            {
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.OldStatus).HasConversion<string>().HasMaxLength(30);
                entity.Property(e => e.NewStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
                entity.Property(e => e.ChangeNote).HasMaxLength(500);

                entity.HasOne(d => d.Task)
                    .WithMany(p => p.AuditLogs)
                    .HasForeignKey(d => d.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.ChangedByUser)
                    .WithMany()
                    .HasForeignKey(d => d.ChangedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Customer configuration
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustomerId);
                entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Phone).HasMaxLength(30);
                entity.Property(e => e.Email).HasMaxLength(200);
            });

            // ServiceRequest configuration
            modelBuilder.Entity<ServiceRequest>(entity =>
            {
                entity.HasKey(e => e.RequestId);
                entity.Property(e => e.ServiceDescription).IsRequired();
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
                entity.Property(e => e.ResolutionNote);
                entity.Property(e => e.CustomerFeedback).HasMaxLength(500);

                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.ServiceRequests)
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Assistant)
                    .WithMany(p => p.HandledRequests)
                    .HasForeignKey(d => d.AssistantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ContactManagerRequest configuration
            modelBuilder.Entity<ContactManagerRequest>(entity =>
            {
                entity.HasKey(e => e.ContactRequestId);
                entity.Property(e => e.CustomerMessage).IsRequired();
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

                entity.HasOne(d => d.ServiceRequest)
                    .WithMany(p => p.ContactManagerRequests)
                    .HasForeignKey(d => d.RequestId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Notification configuration
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.NotificationId);
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(50).IsRequired();

                entity.HasOne(d => d.RecipientUser)
                    .WithMany(p => p.Notifications)
                    .HasForeignKey(d => d.RecipientUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // AssistantConductScore configuration
            modelBuilder.Entity<AssistantConductScore>(entity =>
            {
                entity.HasKey(e => e.ScoreId);
                entity.Property(e => e.ManagerNote).HasMaxLength(500);

                entity.HasOne(d => d.Assistant)
                    .WithMany(p => p.ConductScores)
                    .HasForeignKey(d => d.AssistantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.ServiceRequest)
                    .WithMany(p => p.ConductScores)
                    .HasForeignKey(d => d.RequestId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
