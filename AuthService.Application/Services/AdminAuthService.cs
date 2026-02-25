using AuthService.Application.DTOs.Request.Admin;
using AuthService.Application.DTOs.Response.Admin;
using AuthService.Application.Interfaces.Helpers;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enum;
using AutoMapper;
using Shared.Contracts.Common.Wrappers;
using SharedContracts.Common.Wrappers.Requests;

namespace AuthService.Application.Services;

public class AdminAuthService : IAdminAuthService
{
    private readonly IAuthUnitOfWork _unitOfWork;
    private readonly IBcryptHelper _bcryptHelper;
    private readonly IMapper _mapper;
    private readonly IQueryablePager _queryablePager;

    public AdminAuthService(IAuthUnitOfWork unitOfWork, IBcryptHelper bcryptHelper, IMapper mapper, IQueryablePager queryablePager)
    {
        _unitOfWork = unitOfWork;
        _bcryptHelper = bcryptHelper;
        _mapper = mapper;
        _queryablePager = queryablePager;
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
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            response.Message = "Registration failed. " + (ex.InnerException?.Message ?? ex.Message);
            response.ListErrors.Add(new Errors { Field = "", Detail = ex.InnerException?.Message ?? ex.Message });
            return response;
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

    public async Task<CommonResponse<PaginationResponse<StudentResponseDto>>> GetAllStudentsAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Students.GetAllAsync();
        var paged = await _queryablePager.ToPagedListAsync(query, request.PageNumber, request.PageSize, cancellationToken);
        var dtoItems = _mapper.Map<List<StudentResponseDto>>(paged.Items);
        var result = new PaginationResponse<StudentResponseDto>
        {
            Items = dtoItems,
            TotalItems = paged.TotalItems,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize
        };
        return new CommonResponse<PaginationResponse<StudentResponseDto>>
        {
            IsSuccess = true,
            Message = "Success",
            Data = result
        };
    }

    public async Task<CommonResponse<StudentResponseDto?>> GetStudentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(id);
        if (student == null)
            return new CommonResponse<StudentResponseDto?> { IsSuccess = false, Message = "Student not found", Data = null };
        var dto = _mapper.Map<StudentResponseDto>(student);
        return new CommonResponse<StudentResponseDto?> { IsSuccess = true, Message = "Success", Data = dto };
    }

    public async Task<CommonResponse<StudentResponseDto>> UpdateStudentAsync(Guid id, UpdateStudentByAdminRequest request, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<StudentResponseDto> { IsSuccess = false };
        var student = await _unitOfWork.Students.GetByIdAsync(id);
        if (student == null)
        {
            response.Message = "Student not found";
            return response;
        }
        student.FullName = request.FullName;
        student.AvatarUrl = request.AvatarUrl;
        student.StudentCode = request.StudentCode;
        student.IsActive = request.IsActive;
        _unitOfWork.Students.UpdateAsync(student);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        response.IsSuccess = true;
        response.Message = "Student updated successfully";
        response.Data = _mapper.Map<StudentResponseDto>(student);
        return response;
    }

    public async Task<CommonResponse<StudentResponseDto>> UpdateStudentStatusAsync(Guid id, UpdateStudentStatusRequest request, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<StudentResponseDto> { IsSuccess = false };
        var student = await _unitOfWork.Students.GetByIdAsync(id);
        if (student == null)
        {
            response.Message = "Student not found";
            return response;
        }
        student.IsActive = request.IsActive;
        _unitOfWork.Students.UpdateAsync(student);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        response.IsSuccess = true;
        response.Message = "Student status updated successfully";
        response.Data = _mapper.Map<StudentResponseDto>(student);
        return response;
    }

    /// <summary>Soft delete: sets IsActive = false. Does not remove the record.</summary>
    public async Task<CommonResponse<bool>> DeleteStudentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<bool> { IsSuccess = false, Data = false };
        var student = await _unitOfWork.Students.GetByIdAsync(id);
        if (student == null)
        {
            response.Message = "Student not found";
            return response;
        }
        student.IsActive = false;
        _unitOfWork.Students.UpdateAsync(student);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        response.IsSuccess = true;
        response.Message = "Student deactivated successfully (soft delete)";
        response.Data = true;
        return response;
    }
}
