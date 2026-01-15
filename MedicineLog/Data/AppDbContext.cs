using MedicineLog.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace MedicineLog.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Site> Sites => Set<Site>();
        public DbSet<Terminal> Terminals => Set<Terminal>();
        public DbSet<MedicineLogEntry> MedicineLogEntries => Set<MedicineLogEntry>();
        public DbSet<MedicineLogEntryItem> MedicineLogEntryItems => Set<MedicineLogEntryItem>();
        public DbSet<AuditorSiteAccess> AuditorSiteAccesses => Set<AuditorSiteAccess>();
        public DbSet<TerminalPairingCode> TerminalPairingCodes => Set<TerminalPairingCode>();
        public DbSet<TerminalSession> TerminalSessions => Set<TerminalSession>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("public");
            MakeTableNamesCompatibleWithPostgres(modelBuilder);
        }

        void MakeTableNamesCompatibleWithPostgres(ModelBuilder modelBuilder)
        {
            var dbSets = GetType()
                .GetProperties()
                .Where(p => p.PropertyType.IsGenericType &&
                       p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .ToDictionary(p => p.PropertyType.GetGenericArguments()[0], p => p.Name);

            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                if (dbSets.TryGetValue(entity.ClrType, out var dbSetName))
                {
                    // Convert the DbSet name to snake_case
                    var tableName = dbSetName.ToSnakeCase();
                    modelBuilder.Entity(entity.Name).ToTable(tableName);
                }
                else
                {
                    // Fallback to default table name if no DbSet property found
                    var currentTableName = modelBuilder.Entity(entity.ClrType).Metadata.GetDefaultTableName();
                    if (currentTableName is not null)
                    {
                        var tableName = currentTableName.SanitizeTableName().ToSnakeCase();
                        modelBuilder.Entity(entity.Name).ToTable(tableName);
                    }
                }

                // Convert each column name within the table to snake case
                foreach (var property in entity.GetProperties())
                {
                    var currentColumnName = property.GetDefaultColumnName();
                    if (currentColumnName is not null)
                    {
                        var columnName = currentColumnName.ToSnakeCase();
                        modelBuilder.Entity(entity.Name).Property(property.Name).HasColumnName(columnName);
                    }
                }
            }
        }
    }

    static class StringExtensions
    {
        public static string SanitizeTableName(this string tableName)
        {
            return tableName.Contains("<") ? tableName.Split('<')[0] : tableName;
        }

        public static string ToSnakeCase(this string str)
        {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }
    }
}
