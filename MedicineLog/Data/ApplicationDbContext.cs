using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MedicineLog.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("public");

            MakeTableNamesCompatibleWithPostgres(modelBuilder);
        }

        void MakeTableNamesCompatibleWithPostgres(ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                // Convert table name to snake case
                var currentTableName = modelBuilder.Entity(entity.ClrType).Metadata.GetDefaultTableName();
                if (currentTableName is not null)
                {
                    var tableName = currentTableName.SanitizeTableName().ToSnakeCase();
                    modelBuilder.Entity(entity.Name).ToTable(tableName);
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
