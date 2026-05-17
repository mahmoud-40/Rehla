namespace BreastCancer.Service.Exceptions
{
    public class PatientMedicalDataNotFoundException : Exception
    {
        public PatientMedicalDataNotFoundException(string message)
            : base(message)
        {
        }
    }
}