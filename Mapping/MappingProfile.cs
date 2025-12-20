using AutoMapper;
using BreastCancer.DTO.response;
using BreastCancer.Models;

namespace BreastCancer.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Patient, PatientResponseDTO>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email ?? string.Empty))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.Doctor != null ? src.User.FullName : null));
        }
    }
}

