using AutoMapper;
using BreastCancer.DTO.request;
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


            CreateMap<BaseRegisterDTO, ApplicationUser>()
                .ForMember(dest => dest.UserName , opt => opt.MapFrom(src => src.Username));

            CreateMap<DoctorRegisterDTO, ApplicationUser>()
                .IncludeBase<BaseRegisterDTO, ApplicationUser>();

            CreateMap<PatientRegisterDTO, ApplicationUser>()
                .IncludeBase<BaseRegisterDTO, ApplicationUser>();

            CreateMap<CaregiverRegisterDTO, ApplicationUser>()
                .IncludeBase<BaseRegisterDTO, ApplicationUser>();

            CreateMap<DoctorRegisterDTO, Doctor>();
            CreateMap<PatientRegisterDTO, Patient>();
            CreateMap<CaregiverRegisterDTO, Caregiver>();

            CreateMap<ApplicationUser, BaseRegisterDTO>();
            CreateMap<Patient, PatientRegisterDTO>();
            CreateMap<Caregiver, CaregiverRegisterDTO>();
            CreateMap<Doctor, DoctorRegisterDTO>();


        }
    }
}

