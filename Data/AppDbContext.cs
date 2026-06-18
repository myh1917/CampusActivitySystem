using Microsoft.EntityFrameworkCore;
using CampusActivitySystem.Models;

namespace CampusActivitySystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<SignInSession> SignInSessions { get; set; }
        public DbSet<SignIn> SignIns { get; set; }
        public DbSet<Notice> Notices { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Registration>()
                .HasIndex(r => new { r.ActivityId, r.UserId })
                .IsUnique()
                .HasFilter("[Status] NOT IN ('CANCELLED', 'REJECTED')");

            modelBuilder.Entity<SignIn>()
                .HasIndex(s => new { s.SessionId, s.RegistrationId })
                .IsUnique();

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);
        }
    }
}