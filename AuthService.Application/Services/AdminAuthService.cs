using AuthService.Application.DTOs.Request.Admin;
using AuthService.Application.DTOs.Response.Admin;
using AuthService.Application.Interfaces.Helpers;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enum;
using AutoMapper;
using Shared.Contracts.Common.Wrappers;

namespace AuthService.Application.Services;

public class AdminAuthService : IAdminAuthService
{
    private readonly IAuthUnitOfWork _unitOfWork;
    private readonly IBcryptHelper _bcryptHelper;
    private readonly IMapper _mapper;

    public AdminAuthService(IAuthUnitOfWork unitOfWork, IBcryptHelper bcryptHelper, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _bcryptHelper = bcryptHelper;
        _mapper = mapper;
    }

    public async Task<CommonResponse<TeacherResponseDto>> RegisterTeacherAsync(RegisterTeacherByAdminRequest request, Guid adminId, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<TeacherResponseDto> { IsSuccess = false };
        if (await _unitOfWork.Users.AnyAsync(u => u.Email == request.Email))
        {
            response.Message = "Email already exists";
            response.ListErrors.Add(new Errors { Field = nameof(request.Email), Detail = "Email already exists" });
            return response;
        }

        var hashedPassword = _bcryptHelper.HashPassword(request.Password);
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = request.Email,
            Password = hashedPassword,
            Fullname = request.FullName,
            Role = (int)RoleNameEnum.Teacher,
            AvatarUrl = request.AvatarUrl ?? string.Empty
        };

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = request.Email,
            Password = hashedPassword,
            FullName = request.FullName,
            Phone = request.Phone,
            AvatarUrl = request.AvatarUrl,
            Department = request.Department,
            Specialization = request.Specialization,
            IsActive = true,
            CreatedByAdminId = adminId
        };

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.Teachers.AddAsync(teacher);
            await _unitOfWork.CommitTransactionAsync();

            response.IsSuccess = true;
            response.Message = "Teacher registered successfully";
            response.Data = _mapper.Map<TeacherResponseDto>(teacher);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<CommonResponse<StudentResponseDto>> RegisterStudentAsync(RegisterStudentByAdminRequest request, Guid adminId, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<StudentResponseDto> { IsSuccess = false };
        if (await _unitOfWork.Users.AnyAsync(u => u.Email == request.Email))
        {
            response.Message = "Email already exists";
            response.ListErrors.Add(new Errors { Field = nameof(request.Email), Detail = "Email already exists" });
            return response;
        }

        var hashedPassword = _bcryptHelper.HashPassword(request.Password);
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = request.Email,
            Password = hashedPassword,
            Fullname = request.FullName,
            Role = (int)RoleNameEnum.Student,
            AvatarUrl = request.AvatarUrl ?? string.Empty
        };

        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = request.Email,
            Password = hashedPassword,
            FullName = request.FullName,
            AvatarUrl = request.AvatarUrl,
            StudentCode = request.StudentCode,
            IsActive = true,
            CreatedByAdminId = adminId
        };

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.Students.AddAsync(student);
            await _unitOfWork.CommitTransactionAsync();

            response.IsSuccess = true;
            response.Message = "Student registered successfully";
            response.Data = _mapper.Map<StudentResponseDto>(student);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
