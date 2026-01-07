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

            // Map ApplicationUser to DoctorResponseDTO (for IncludeMembers)
            // This allows AutoMapper to automatically map matching properties
            CreateMap<ApplicationUser, DoctorResponseDTO>(MemberList.None)
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Specialization, opt => opt.Ignore())
                .ForMember(dest => dest.LicenseNumber, opt => opt.Ignore())
                .ForMember(dest => dest.YearsOfExperience, opt => opt.Ignore())
                .ForMember(dest => dest.IsVerified, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty));

            // Map Patient to PatientResponseDTO
            // IncludeMembers automatically maps all matching properties from User
            CreateMap<Patient, PatientResponseDTO>()
                .IncludeMembers(p => p.User)
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.Doctor != null && src.Doctor.User != null ? src.Doctor.User.FullName : null));

            // Map Doctor to DoctorResponseDTO
            // IncludeMembers automatically maps all matching properties from User
            CreateMap<Doctor, DoctorResponseDTO>()
                .IncludeMembers(d => d.User)
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.UserId));


            CreateMap<BaseRegisterDTO, ApplicationUser>()
                .ForMember(dest => dest.UserName , opt => opt.MapFrom(src => src.Username));

            CreateMap<DoctorRegisterDTO, ApplicationUser>()
                .IncludeBase<BaseRegisterDTO, ApplicationUser>();

            CreateMap<PatientRegisterDTO, ApplicationUser>()
                .IncludeBase<BaseRegisterDTO, ApplicationUser>();

            CreateMap<CaregiverRegisterDTO, ApplicationUser>()
                .IncludeBase<BaseRegisterDTO, ApplicationUser>();

            // Map DoctorCreateDTO to ApplicationUser
            CreateMap<DoctorCreateDTO, ApplicationUser>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            // Map DoctorUpdateDTO to ApplicationUser (for updates - only maps non-null properties)
            var updateUserMap = CreateMap<DoctorUpdateDTO, ApplicationUser>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom((src, dest) => !string.IsNullOrEmpty(src.Email) ? src.Email : dest.UserName))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
            
            updateUserMap.ForAllMembers(opt => opt.Condition((src, dest, srcMember, destMember) =>
            {
                if (srcMember == null) return false;
                if (srcMember is string str) return !string.IsNullOrEmpty(str);
                return true;
            }));

            // Map DoctorCreateDTO to Doctor
            CreateMap<DoctorCreateDTO, Doctor>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.IsVerified, opt => opt.MapFrom(src => false));

            // Map DoctorUpdateDTO to Doctor (for updates - only maps non-null properties)
            var updateDoctorMap = CreateMap<DoctorUpdateDTO, Doctor>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore());
            
            updateDoctorMap.ForAllMembers(opt => opt.Condition((src, dest, srcMember, destMember) =>
            {
                if (srcMember == null) return false;
                if (srcMember is string str) return !string.IsNullOrEmpty(str);
                return true;
            }));

            CreateMap<DoctorRegisterDTO, Doctor>();
            CreateMap<PatientRegisterDTO, Patient>();
            CreateMap<CaregiverRegisterDTO, Caregiver>();

            CreateMap<ApplicationUser, BaseRegisterDTO>();
            CreateMap<Patient, PatientRegisterDTO>();
            CreateMap<Caregiver, CaregiverRegisterDTO>();
            CreateMap<Doctor, DoctorRegisterDTO>();


            #region Caregiver Mapping

            // Map CaregiverCreateDTO to Caregiver
            CreateMap<CaregiverCreateDTO, Caregiver>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.Patient, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore());

            // Map Caregiver to CaregiverResponse
            CreateMap<Caregiver, CaregiverResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email));

            CreateMap<CaregiverUpdateDTO, ApplicationUser>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            #endregion
        }
    }
}

