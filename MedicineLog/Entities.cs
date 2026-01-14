namespace MedicineLog
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class MedicationRegistration
    {
        [Key]
        public long Id { get; set; }

        [Required, StringLength(100)]
        public string FirstName { get; set; } = "";

        [Required, StringLength(100)]
        public string LastName { get; set; } = "";

        [Required, StringLength(200)]
        public string MedicationName { get; set; } = "";

        [Range(1, 100000)]
        public int Quantity { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

}
