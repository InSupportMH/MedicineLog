using MedicineLog.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // 🔑 Skip owned / keyless / non-table mapped types (e.g., IdentityPasskeyData)
                if (entityType.IsOwned())
                    continue;

                if (entityType.FindPrimaryKey() is null)
                    continue;

                var currentTableName = entityType.GetTableName();
                if (currentTableName is null)
                    continue;

                // Decide table name
                string tableName;
                if (dbSets.TryGetValue(entityType.ClrType, out var dbSetName))
                    tableName = dbSetName.ToSnakeCase();
                else
                    tableName = currentTableName.SanitizeTableName().ToSnakeCase();

                // Apply table name
                modelBuilder.Entity(entityType.ClrType).ToTable(tableName);

                // Rename columns ONLY for actual table columns
                var storeObject = StoreObjectIdentifier.Table(tableName, entityType.GetSchema() ?? "public");

                foreach (var prop in entityType.GetProperties())
                {
                    var colName = prop.GetColumnName(storeObject);
                    if (colName is null)
                        continue;

                    modelBuilder.Entity(entityType.ClrType)
                        .Property(prop.Name)
                        .HasColumnName(colName.ToSnakeCase());
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
