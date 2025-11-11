using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Common;
using Fap.Domain.DTOs.Enrollment;
using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Fap.Api.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<EnrollmentService> _logger;

        public EnrollmentService(
            IUnitOfWork uow,
            IMapper mapper,
            ILogger<EnrollmentService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<EnrollmentResponse> CreateEnrollmentAsync(CreateEnrollmentRequest request)
        {
            var response = new EnrollmentResponse();

            try
            {
                var student = await _uow.Students.GetByIdAsync(request.StudentId);
                if (student == null)
                {
                    response.Errors.Add($"Student with ID '{request.StudentId}' not found");
                    response.Message = "Enrollment creation failed";
                    return response;
                }

                var classEntity = await _uow.Classes.GetByIdAsync(request.ClassId);
                if (classEntity == null)
                {
                    response.Errors.Add($"Class with ID '{request.ClassId}' not found");
                    response.Message = "Enrollment creation failed";
                    return response;
                }

                var isAlreadyEnrolled = await _uow.Enrolls.IsStudentEnrolledInClassAsync(
                    request.StudentId,
                    request.ClassId);

                if (isAlreadyEnrolled)
                {
                    response.Errors.Add("Student is already enrolled in this class");
                    response.Message = "Enrollment creation failed";
                    return response;
                }

                var newEnrollment = new Enroll
                {
                    Id = Guid.NewGuid(),
                    StudentId = request.StudentId,
                    ClassId = request.ClassId,
                    RegisteredAt = DateTime.UtcNow,
                    IsApproved = false
                };

                await _uow.Enrolls.AddAsync(newEnrollment);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Enrollment created successfully. Waiting for approval.";
                response.EnrollmentId = newEnrollment.Id;

                _logger.LogInformation("Student {StudentId} enrolled in class {ClassId}", request.StudentId, request.ClassId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating enrollment");
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Enrollment creation failed";
                return response;
            }
        }

        public async Task<EnrollmentDetailDto?> GetEnrollmentByIdAsync(Guid id)
        {
            try
            {
                var enrollment = await _uow.Enrolls.GetByIdWithDetailsAsync(id);
                if (enrollment == null)
                    return null;

                return _mapper.Map<EnrollmentDetailDto>(enrollment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrollment {EnrollmentId}", id);
                throw;
            }
        }

        public async Task<PagedResult<EnrollmentDto>> GetEnrollmentsAsync(GetEnrollmentsRequest request)
        {
            try
            {
                var (enrollments, totalCount) = await _uow.Enrolls.GetPagedEnrollmentsAsync(
                    request.Page,
                    request.PageSize,
                    request.ClassId,
                    request.StudentId,
                    request.IsApproved,
                    request.RegisteredFrom,
                    request.RegisteredTo,
                    request.SortBy,
                    request.SortOrder
                );

                var enrollmentDtos = _mapper.Map<List<EnrollmentDto>>(enrollments);

                return new PagedResult<EnrollmentDto>(
                    enrollmentDtos,
                    totalCount,
                    request.Page,
                    request.PageSize
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrollments");
                throw;
            }
        }

        public async Task<EnrollmentResponse> ApproveEnrollmentAsync(Guid id)
        {
            var response = new EnrollmentResponse { EnrollmentId = id };

            try
            {
                var enrollment = await _uow.Enrolls.GetByIdAsync(id);
                if (enrollment == null)
                {
                    response.Errors.Add($"Enrollment with ID '{id}' not found");
                    response.Message = "Enrollment approval failed";
                    return response;
                }

                if (enrollment.IsApproved)
                {
                    response.Errors.Add("Enrollment is already approved");
                    response.Message = "Enrollment approval failed";
                    return response;
                }

                enrollment.IsApproved = true;
                _uow.Enrolls.Update(enrollment);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Enrollment approved successfully";

                _logger.LogInformation("Enrollment {EnrollmentId} approved", id);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving enrollment {EnrollmentId}", id);
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Enrollment approval failed";
                return response;
            }
        }

        public async Task<EnrollmentResponse> RejectEnrollmentAsync(Guid id, string? reason)
        {
            var response = new EnrollmentResponse { EnrollmentId = id };

            try
            {
                var enrollment = await _uow.Enrolls.GetByIdAsync(id);
                if (enrollment == null)
                {
                    response.Errors.Add($"Enrollment with ID '{id}' not found");
                    response.Message = "Enrollment rejection failed";
                    return response;
                }

                if (enrollment.IsApproved)
                {
                    response.Errors.Add("Cannot reject an already approved enrollment");
                    response.Message = "Enrollment rejection failed";
                    return response;
                }

                _uow.Enrolls.Remove(enrollment);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = !string.IsNullOrEmpty(reason)
                    ? $"Enrollment rejected: {reason}"
                    : "Enrollment rejected successfully";

                _logger.LogInformation("Enrollment {EnrollmentId} rejected", id);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting enrollment {EnrollmentId}", id);
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Enrollment rejection failed";
                return response;
            }
        }

        public async Task<EnrollmentResponse> DropEnrollmentAsync(Guid id, Guid studentId)
        {
            var response = new EnrollmentResponse { EnrollmentId = id };

            try
            {
                var enrollment = await _uow.Enrolls.GetByIdAsync(id);
                if (enrollment == null)
                {
                    response.Errors.Add($"Enrollment with ID '{id}' not found");
                    response.Message = "Drop enrollment failed";
                    return response;
                }

                if (enrollment.StudentId != studentId)
                {
                    response.Errors.Add("You can only drop your own enrollments");
                    response.Message = "Drop enrollment failed";
                    return response;
                }

                _uow.Enrolls.Remove(enrollment);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Enrollment dropped successfully";

                _logger.LogInformation("Student {StudentId} dropped enrollment {EnrollmentId}", studentId, id);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dropping enrollment {EnrollmentId}", id);
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Drop enrollment failed";
                return response;
            }
        }

        public async Task<PagedResult<StudentEnrollmentHistoryDto>> GetStudentEnrollmentHistoryAsync(
            Guid studentId,
            GetStudentEnrollmentsRequest request)
        {
            try
            {
                var student = await _uow.Students.GetByIdAsync(studentId);
                if (student == null)
                {
                    return new PagedResult<StudentEnrollmentHistoryDto>(
                        new List<StudentEnrollmentHistoryDto>(),
                        0,
                        request.Page,
                        request.PageSize
                    );
                }

                var (enrollments, totalCount) = await _uow.Enrolls.GetStudentEnrollmentHistoryAsync(
                    studentId,
                    request.Page,
                    request.PageSize,
                    request.SemesterId,
                    request.IsApproved,
                    request.SortBy,
                    request.SortOrder
                );

                var historyDtos = _mapper.Map<List<StudentEnrollmentHistoryDto>>(enrollments);

                return new PagedResult<StudentEnrollmentHistoryDto>(
                    historyDtos,
                    totalCount,
                    request.Page,
                    request.PageSize
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student enrollment history for {StudentId}", studentId);
                throw;
            }
        }
    }
}
