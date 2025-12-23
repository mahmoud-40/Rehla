using AutoMapper;
using BreastCancer.DTO.response;
using BreastCancer.Models;

namespace BreastCancer.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Map ApplicationUser to PatientResponseDTO (for IncludeMembers)
            // This allows AutoMapper to automatically map matching properties
            CreateMap<ApplicationUser, PatientResponseDTO>(MemberList.None)
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalHistory, opt => opt.Ignore())
                .ForMember(dest => dest.DoctorId, opt => opt.Ignore())
                .ForMember(dest => dest.DoctorName, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty));

            // Map Patient to PatientResponseDTO
            // IncludeMembers automatically maps all matching properties from User
            CreateMap<Patient, PatientResponseDTO>()
                .IncludeMembers(p => p.User)
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.Doctor != null && src.Doctor.User != null ? src.Doctor.User.FullName : null));
        }
    }
}

