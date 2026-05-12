using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BreastCancer.Models
{
    public class PatientDiagnosis
    {
        [Key]
        [ForeignKey(nameof(Patient))]
        public string UserId { get; set; }

        public int AgeAtDiagnosis { get; set; }
        public string? CancerType { get; set; }
        public string? CancerTypeDetailed { get; set; }
        public string? TumorStage { get; set; }
        public string? NeoplasmHistologicGrade { get; set; }

        public string? ErStatus { get; set; }
        public string? PrStatus { get; set; }
        public string? Her2Status { get; set; }

        public bool Chemotherapy { get; set; }
        public bool HormoneTherapy { get; set; }
        public bool RadioTherapy { get; set; }

        public virtual Patient Patient { get; set; } = null!;
    }
}