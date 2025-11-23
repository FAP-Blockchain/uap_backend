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
        private readonly IStudentRoadmapService _roadmapService;

        public EnrollmentService(
                  IUnitOfWork uow,
           IMapper mapper,
              ILogger<EnrollmentService> logger,
         IStudentRoadmapService roadmapService)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
            _roadmapService = roadmapService;
        }

        public async Task<EnrollmentResponse> CreateEnrollmentAsync(CreateEnrollmentRequest request)
        {
            var response = new EnrollmentResponse();

            try
            {
                // 1. Validate student exists
                var student = await _uow.Students.GetByIdAsync(request.StudentId);
                if (student == null)
                {
                    response.Errors.Add($"Student with ID '{request.StudentId}' not found");
                    response.Message = "Enrollment creation failed";
                    return response;
                }

                // 2. Validate class exists
                var classEntity = await _uow.Classes.GetByIdAsync(request.ClassId);
                if (classEntity == null)
                {
                    response.Errors.Add($"Class with ID '{request.ClassId}' not found");
                    response.Message = "Enrollment creation failed";
                    return response;
                }

                // 3. Get subject information via SubjectOffering
                var subjectOffering = await _uow.SubjectOfferings.GetByIdAsync(classEntity.SubjectOfferingId);
                if (subjectOffering == null)
                {
                    response.Errors.Add($"Subject offering not found for class '{request.ClassId}'");
                    response.Message = "Enrollment creation failed";
                    return response;
                }

                var subject = await _uow.Subjects.GetByIdAsync(subjectOffering.SubjectId);
                if (subject == null)
                {
                    response.Errors.Add($"Subject not found for class '{request.ClassId}'");
                    response.Message = "Enrollment creation failed";
                    return response;
                }

                var semesterId = subjectOffering.SemesterId;
                var subjectId = subject.Id;

                // 4. ✅ NEW: Check if student is already a member of the class (in ClassMembers table)
                var isAlreadyMember = await _uow.ClassMembers.IsStudentInClassAsync(
              request.ClassId,
                 request.StudentId);

                if (isAlreadyMember)
                {
                    response.Errors.Add("Student is already a member of this class");
                    response.Message = "Enrollment creation failed - Student already enrolled";
                    _logger.LogWarning(
                          "Enrollment attempt rejected: Student {StudentId} is already a member of class {ClassId}",
                     request.StudentId, request.ClassId);
                    return response;
                }

                // 5. Check if student has pending enrollment request
                var isAlreadyEnrolled = await _uow.Enrolls.IsStudentEnrolledInClassAsync(
                      request.StudentId,
                  request.ClassId);

                if (isAlreadyEnrolled)
                {
                    response.Errors.Add("Student already has a pending enrollment request for this class");
                    response.Message = "Enrollment creation failed";
                    return response;
                }

                // 6. ✅ Ensure student is not already enrolled in another class for this subject this semester
                var alreadyInSubjectThisSemester = await _uow.Enrolls.IsStudentEnrolledInSubjectAsync(
                    request.StudentId,
                    subjectId,
                    semesterId);

                if (alreadyInSubjectThisSemester)
                {
                    response.Errors.Add($"Student already enrolled in another class for '{subject.SubjectCode}' this semester");
                    response.Message = "Enrollment creation failed";
                    return response;
                }

                // 7. ✅ Curriculum-based eligibility (subject in curriculum, prerequisites, completion)
                var prerequisiteValidation = await ValidateCurriculumEligibilityAsync(request.StudentId, subjectId, subject.SubjectCode);
                if (!prerequisiteValidation.IsValid)
                {
                    response.Errors.AddRange(prerequisiteValidation.Errors);
                    response.Message = "Enrollment creation failed - Prerequisites not met";
                    _logger.LogWarning(
                         "Enrollment rejected: Curriculum eligibility failed for student {StudentId}, subject {SubjectCode}",
                               request.StudentId, subject.SubjectCode);
                    return response;
                }

                // Fetch roadmap entry for optional sequence warning
                var roadmapEntry = await _uow.StudentRoadmaps.GetByStudentAndSubjectAsync(
                    request.StudentId,
                    subjectId);

                // 8. ✅✅ NEW: Check sequence order (warning only, not blocking)
                if (roadmapEntry != null)
                {
                    var sequenceWarning = await CheckSequenceOrderAsync(request.StudentId, roadmapEntry);
                    if (!string.IsNullOrEmpty(sequenceWarning))
                    {
                        response.Warnings.Add(sequenceWarning);
                        _logger.LogInformation(
                            "Sequence warning for student {StudentId}: {Warning}",
                            request.StudentId, sequenceWarning);
                    }
                }

                // 10. Create new enrollment request
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
                response.Message = "Enrollment request created successfully. Waiting for approval.";
                response.EnrollmentId = newEnrollment.Id;

                _logger.LogInformation(
          "Student {StudentId} submitted enrollment request for class {ClassId} (Subject: {SubjectCode})",
   request.StudentId, request.ClassId, subject.SubjectCode);

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

                // ✅ Check if student is already in class roster (ClassMember)
                var isAlreadyInRoster = await _uow.ClassMembers.IsStudentInClassAsync(
    enrollment.ClassId, enrollment.StudentId);

                if (isAlreadyInRoster)
                {
                    response.Errors.Add("Student is already in the class roster");
                    response.Message = "Enrollment approval failed";
                    return response;
                }

                // ✅ Check class capacity
                var classEntity = await _uow.Classes.GetByIdAsync(enrollment.ClassId);
                if (classEntity == null)
                {
                    response.Errors.Add($"Class with ID '{enrollment.ClassId}' not found");
                    response.Message = "Enrollment approval failed";
                    return response;
                }

                var currentEnrollmentCount = await _uow.ClassMembers.GetClassMemberCountAsync(
                 enrollment.ClassId);

                if (currentEnrollmentCount >= classEntity.MaxEnrollment)
                {
                    response.Errors.Add($"Class is full. Maximum enrollment: {classEntity.MaxEnrollment}");
                    response.Message = "Enrollment approval failed";
                    return response;
                }

                // ✅ Approve enrollment
                enrollment.IsApproved = true;
                _uow.Enrolls.Update(enrollment);

                // ✅✅ CREATE ClassMember - Add student to class roster
                var classMember = new ClassMember
                {
                    Id = Guid.NewGuid(),
                    ClassId = enrollment.ClassId,
                    StudentId = enrollment.StudentId,
                    JoinedAt = DateTime.UtcNow
                };

                await _uow.ClassMembers.AddAsync(classMember);

                // ✅✅✅ NEW: Auto-create grade records with null scores for all grade components of the subject
                var subjectOffering = await _uow.SubjectOfferings.GetByIdAsync(classEntity.SubjectOfferingId);
                if (subjectOffering != null)
                {
                    // Get all grade components for this subject
                    var gradeComponents = await _uow.GradeComponents.FindAsync(gc => gc.SubjectId == subjectOffering.SubjectId);
                    var gradesCreated = 0;

                    foreach (var component in gradeComponents)
                    {
                        // Check if grade already exists (shouldn't happen, but safety check)
                        var existingGrade = await _uow.Grades.GetGradeByStudentSubjectComponentAsync(
                            enrollment.StudentId,
                            subjectOffering.SubjectId,
                            component.Id);

                        if (existingGrade == null)
                        {
                            var grade = new Grade
                            {
                                Id = Guid.NewGuid(),
                                StudentId = enrollment.StudentId,
                                SubjectId = subjectOffering.SubjectId,
                                GradeComponentId = component.Id,
                                Score = null,  // ✅ Initialize with null
                                LetterGrade = null,
                                UpdatedAt = DateTime.UtcNow
                            };

                            await _uow.Grades.AddAsync(grade);
                            gradesCreated++;
                        }
                    }

                    _logger.LogInformation(
                        "Auto-created {Count} grade records (null scores) for student {StudentId} in subject {SubjectId}",
                        gradesCreated, enrollment.StudentId, subjectOffering.SubjectId);

                    // ✅ Update roadmap with actual semester and set status to InProgress
                    await _roadmapService.UpdateRoadmapWithActualSemesterAsync(
                        enrollment.StudentId,
                        subjectOffering.SubjectId,
                        subjectOffering.SemesterId);
                }

                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Enrollment approved successfully. Student added to class roster, grade records initialized, and roadmap updated.";

                _logger.LogInformation(
            "Enrollment {EnrollmentId} approved. Student {StudentId} added to class {ClassId} roster. Roadmap updated to InProgress.",
                       id, enrollment.StudentId, enrollment.ClassId);

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

        // ==================== NEW HELPER METHODS ====================

        /// <summary>
        /// Validate curriculum-based eligibility (subject in curriculum, prerequisites met, not already completed)
        /// </summary>
        private async Task<(bool IsValid, List<string> Errors)> ValidateCurriculumEligibilityAsync(
            Guid studentId,
            Guid subjectId,
            string? subjectCode)
        {
            var eligibility = await _roadmapService.CheckCurriculumSubjectEligibilityAsync(studentId, subjectId);

            if (eligibility.IsEligible)
            {
                return (true, new List<string>());
            }

            var fallbackMessage = !string.IsNullOrWhiteSpace(subjectCode)
                ? $"Curriculum eligibility failed for '{subjectCode}'"
                : "Curriculum eligibility failed";

            var errors = eligibility.Reasons.Any()
                ? new List<string>(eligibility.Reasons)
                : new List<string> { fallbackMessage };

            if (!string.IsNullOrWhiteSpace(eligibility.BlockingReason) && !errors.Contains(eligibility.BlockingReason))
            {
                errors.Add(eligibility.BlockingReason);
            }

            return (false, errors);
        }

        /// <summary>
        /// Check if student is enrolling subjects in recommended sequence order
        /// Returns warning message if out of sequence (non-blocking)
        /// </summary>
                private async Task<string?> CheckSequenceOrderAsync(
                    Guid studentId,
                        StudentRoadmap? currentRoadmapEntry)
        {
                        if (currentRoadmapEntry == null)
                        {
                                return null;
                        }

            // Get all planned subjects with lower sequence order
            var earlierPlannedSubjects = await _uow.StudentRoadmaps
                           .GetStudentRoadmapAsync(studentId);

            var skippedSubjects = earlierPlannedSubjects
         .Where(r => r.SequenceOrder < currentRoadmapEntry.SequenceOrder
          && r.Status == "Planned"
              && r.SemesterId == currentRoadmapEntry.SemesterId)
             .OrderBy(r => r.SequenceOrder)
     .ToList();

            if (skippedSubjects.Any())
            {
                var skippedCodes = string.Join(", ", skippedSubjects.Select(s => s.Subject.SubjectCode));
                return $"⚠️ Note: You are skipping earlier subjects in your roadmap: {skippedCodes}. " +
                 "Consider enrolling in recommended sequence order.";
            }

            return null;
        }
    }
}
