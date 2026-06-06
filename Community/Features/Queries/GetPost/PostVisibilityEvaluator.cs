using BreastCancer.Enum;

namespace BreastCancer.Community.Features.GetPost;

internal static class PostVisibilityEvaluator
{
    public static bool CanView(PostVisibility visibility, IReadOnlyCollection<string> roles)
    {
        var isDoctor = roles.Any(r => string.Equals(r, "Doctor", StringComparison.OrdinalIgnoreCase));
        var isPatient = roles.Any(r => string.Equals(r, "Patient", StringComparison.OrdinalIgnoreCase));
        var isCaregiver = roles.Any(r => string.Equals(r, "Caregiver", StringComparison.OrdinalIgnoreCase));

        return visibility switch
        {
            PostVisibility.Public => true,
            PostVisibility.PatientsOnly => isPatient || isDoctor,
            PostVisibility.DoctorOnly => isDoctor,
            PostVisibility.CaregiverOnly => isCaregiver || isDoctor,
            _ => false
        };
    }
}
