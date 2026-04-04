using AuthService.Application.DTOs.Response.Admin;
using AuthService.Application.DTOs.Response.Review;
using AuthService.Domain.Entities;
using AutoMapper;

namespace AuthService.Application.Common;

public class AuthServiceMappingProfile : Profile
{
    public AuthServiceMappingProfile()
    {
        CreateMap<Teacher, TeacherResponseDto>();
        CreateMap<Student, StudentResponseDto>();
        CreateMap<User, UserDto>();
        CreateMap<Review, ReviewResponseDto>()
            .ForMember(dest => dest.Mentor,
                opt => opt.MapFrom(src => src.Mentor))
            .ForMember(dest => dest.Mentee,
                opt => opt.MapFrom(src => src.Mentee));
    }
}
