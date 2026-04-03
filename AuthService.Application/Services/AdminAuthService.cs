using AuthService.Application.DTOs.Request.Admin;
using AuthService.Application.DTOs.Response.Admin;
using AuthService.Application.Interfaces.Helpers;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enum;
using AutoMapper;
using Shared.Contracts.Common.Wrappers;
using Shared.Contracts.Interfaces;
using SharedContracts.Common.Wrappers.Requests;

namespace AuthService.Application.Services;

public class AdminAuthService : IAdminAuthService
{
    private readonly IAuthUnitOfWork _unitOfWork;
    private readonly IBcryptHelper _bcryptHelper;
    private readonly IMapper _mapper;
    private readonly IQueryablePager _queryablePager;
    private readonly IStorageService _storageService;

    public AdminAuthService(IAuthUnitOfWork unitOfWork, IBcryptHelper bcryptHelper, IMapper mapper, IQueryablePager queryablePager, IStorageService storageService)
    {
        _unitOfWork = unitOfWork;
        _bcryptHelper = bcryptHelper;
        _mapper = mapper;
        _queryablePager = queryablePager;
        _storageService = storageService;
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
        var fileName = $"avatars/{Guid.NewGuid()}_{request.Avatar.FileName}";

        using var stream = request.Avatar.OpenReadStream();

        var avatarUrl = await _storageService.UploadFileAsync(
            fileName,
            stream);
        var user = new User
        {
            Id = userId,
            Email = request.Email,
            Password = hashedPassword,
            Fullname = request.FullName,
            Role = (int)RoleNameEnum.Teacher,
            AvatarUrl = avatarUrl ?? string.Empty
        };

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = request.Email,
            Password = hashedPassword,
            FullName = request.FullName,
            Phone = request.Phone,
            AvatarUrl = avatarUrl,
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
        var fileName = $"avatars/{Guid.NewGuid()}_{request.Avatar.FileName}";

        using var stream = request.Avatar.OpenReadStream();

        var avatarUrl = await _storageService.UploadFileAsync(
            fileName,
            stream);
        var user = new User
        {
            Id = userId,
            Email = request.Email,
            Password = hashedPassword,
            Fullname = request.FullName,
            Role = (int)RoleNameEnum.Student,
            AvatarUrl = avatarUrl ?? string.Empty
        };

        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = request.Email,
            Password = hashedPassword,
            FullName = request.FullName,
            AvatarUrl = avatarUrl,
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

        if (request.Avatar is { Length: > 0 } av)
        {
            if (!string.IsNullOrWhiteSpace(student.AvatarUrl))
                await _storageService.DeleteFileFromUrlAsync(student.AvatarUrl);
            var fileName = $"avatars/{Guid.NewGuid()}_{av.FileName}";
            await using var stream = av.OpenReadStream();
            var avatarUrl = await _storageService.UploadFileAsync(fileName, stream);
            student.AvatarUrl = avatarUrl;
        }

        student.FullName = request.FullName;
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

    public async Task<CommonResponse<PaginationResponse<TeacherResponseDto>>> GetAllTeachersAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Teachers.GetAllAsync();
        var paged = await _queryablePager.ToPagedListAsync(query, request.PageNumber, request.PageSize, cancellationToken);
        var dtoItems = _mapper.Map<List<TeacherResponseDto>>(paged.Items);
        var result = new PaginationResponse<TeacherResponseDto>
        {
            Items = dtoItems,
            TotalItems = paged.TotalItems,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize
        };
        return new CommonResponse<PaginationResponse<TeacherResponseDto>>
        {
            IsSuccess = true,
            Message = "Success",
            Data = result
        };
    }

    public async Task<CommonResponse<PaginationResponse<TeacherResponseDto>>> GetAllTeacherForMenteesAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Teachers.GetAllAsync().Where(x => !x.IsDeleted);
        var paged = await _queryablePager.ToPagedListAsync(query, request.PageNumber, request.PageSize, cancellationToken);
        var dtoItems = _mapper.Map<List<TeacherResponseDto>>(paged.Items);
        var result = new PaginationResponse<TeacherResponseDto>
        {
            Items = dtoItems,
            TotalItems = paged.TotalItems,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize
        };
        return new CommonResponse<PaginationResponse<TeacherResponseDto>>
        {
            IsSuccess = true,
            Message = "Success",
            Data = result
        };
    }

    public async Task<CommonResponse<TeacherResponseDto?>> GetTeacherByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var teacher = await _unitOfWork.Teachers.GetByIdAsync(id);
        if (teacher == null)
            return new CommonResponse<TeacherResponseDto?> { IsSuccess = false, Message = "Teacher not found", Data = null };
        var dto = _mapper.Map<TeacherResponseDto>(teacher);
        return new CommonResponse<TeacherResponseDto?> { IsSuccess = true, Message = "Success", Data = dto };
    }

    public async Task<CommonResponse<TeacherResponseDto>> UpdateTeacherAsync(Guid id, UpdateTeacherByAdminRequest request, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<TeacherResponseDto> { IsSuccess = false };
        var teacher = await _unitOfWork.Teachers.GetByIdAsync(id);
        if (teacher == null)
        {
            response.Message = "Teacher not found";
            return response;
        }

        if (request.Avatar is { Length: > 0 } av)
        {
            if (!string.IsNullOrWhiteSpace(teacher.AvatarUrl))
                await _storageService.DeleteFileFromUrlAsync(teacher.AvatarUrl);
            var fileName = $"avatars/{Guid.NewGuid()}_{av.FileName}";
            await using var stream = av.OpenReadStream();
            var avatarUrl = await _storageService.UploadFileAsync(fileName, stream);
            teacher.AvatarUrl = avatarUrl;
        }

        teacher.FullName = request.FullName;
        teacher.Department = request.Department;
        teacher.Specialization = request.Specialization;
        teacher.IsActive = request.IsActive;
        _unitOfWork.Teachers.UpdateAsync(teacher);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        response.IsSuccess = true;
        response.Message = "Teacher updated successfully";
        response.Data = _mapper.Map<TeacherResponseDto>(teacher);
        return response;
    }

    public async Task<CommonResponse<TeacherResponseDto>> UpdateTeacherStatusAsync(Guid id, UpdateTeacherStatusRequest request, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<TeacherResponseDto> { IsSuccess = false };
        var teacher = await _unitOfWork.Teachers.GetByIdAsync(id);
        if (teacher == null)
        {
            response.Message = "Teacher not found";
            return response;
        }
        teacher.IsActive = request.IsActive;
        _unitOfWork.Teachers.UpdateAsync(teacher);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        response.IsSuccess = true;
        response.Message = "Teacher status updated successfully";
        response.Data = _mapper.Map<TeacherResponseDto>(teacher);
        return response;
    }

    public async Task<CommonResponse<bool>> DeleteTeacherAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = new CommonResponse<bool> { IsSuccess = false, Data = false };
        var teacher = await _unitOfWork.Teachers.GetByIdAsync(id);
        if (teacher == null)
        {
            response.Message = "Teacher not found";
            return response;
        }
        teacher.IsActive = false;
        _unitOfWork.Teachers.UpdateAsync(teacher);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        response.IsSuccess = true;
        response.Message = "Teacher deactivated successfully (soft delete)";
        response.Data = true;
        return response;
    }

    public async Task<CommonResponse<UserInfoDto?>> GetUserInfoByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return new CommonResponse<UserInfoDto?> { IsSuccess = false, Message = "User not found", Data = null };

        return new CommonResponse<UserInfoDto?>
        {
            IsSuccess = true,
            Message = "Success",
            Data = new UserInfoDto
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.Fullname
            }
        };
    }
}
