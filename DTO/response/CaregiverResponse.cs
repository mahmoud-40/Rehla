namespace BreastCancer.DTO.response
{
    public class CaregiverResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PatientId { get; set; }
        public string? RelationshipType { get; set; }
    }
}
