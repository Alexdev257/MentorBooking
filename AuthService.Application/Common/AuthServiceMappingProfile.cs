using AuthService.Application.DTOs.Response.Admin;
using AuthService.Domain.Entities;
using AutoMapper;

namespace AuthService.Application.Common;

public class AuthServiceMappingProfile : Profile
{
    public AuthServiceMappingProfile()
    {
        CreateMap<Teacher, TeacherResponseDto>();
        CreateMap<Student, StudentResponseDto>();
    }
}
