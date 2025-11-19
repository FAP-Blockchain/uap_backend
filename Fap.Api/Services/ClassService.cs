using AutoMapper;
using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Class;
using Fap.Domain.DTOs.Common;
using Fap.Domain.Repositories;

namespace Fap.Api.Services
{
    public class ClassService : IClassService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<ClassService> _logger;

        public ClassService(IUnitOfWork uow, IMapper mapper, ILogger<ClassService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        // ========== GET CLASSES WITH PAGINATION ==========
        public async Task<PagedResult<ClassDto>> GetClassesAsync(GetClassesRequest request)
        {
            try
            {
                var (classes, totalCount) = await _uow.Classes.GetPagedClassesAsync(
                    request.Page,
                    request.PageSize,
                    request.SubjectId,
                    request.TeacherId,
                    request.SemesterId,
                    request.ClassCode,
                    request.SortBy,
                    request.SortOrder
                );

                var classDtos = _mapper.Map<List<ClassDto>>(classes);

                return new PagedResult<ClassDto>(
                    classDtos,
                    totalCount,
                    request.Page,
                    request.PageSize
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Error getting classes: {ex.Message}");
                throw;
            }
        }

        // ========== GET CLASS BY ID WITH DETAILS ==========
        public async Task<ClassDetailDto?> GetClassByIdAsync(Guid id)
        {
            try
            {
                var @class = await _uow.Classes.GetByIdWithDetailsAsync(id);
                if (@class == null)
                    return null;

                return _mapper.Map<ClassDetailDto>(@class);
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Error getting class {id}: {ex.Message}");
                throw;
            }
        }

        // ========== CREATE CLASS ==========
        public async Task<ClassResponse> CreateClassAsync(CreateClassRequest request)
        {
            var response = new ClassResponse
            {
                ClassCode = request.ClassCode
            };

            try
            {
                // 1. Validate ClassCode uniqueness
                var isUnique = await _uow.Classes.IsClassCodeUniqueAsync(request.ClassCode);
                if (!isUnique)
                {
                    response.Errors.Add($"ClassCode '{request.ClassCode}' already exists");
                    response.Message = "Class creation failed";
                    return response;
                }

                // ✅ FIXED: 2. Validate SubjectOffering exists (not Subject directly)
                var subjectOffering = await _uow.SubjectOfferings.GetByIdAsync(request.SubjectOfferingId);
                if (subjectOffering == null)
                {
                    response.Errors.Add($"Subject offering with ID '{request.SubjectOfferingId}' not found");
                    response.Message = "Class creation failed";
                    return response;
                }

                // 3. Validate Teacher exists
                var teacher = await _uow.Teachers.GetByIdAsync(request.TeacherId);
                if (teacher == null)
                {
                    response.Errors.Add($"Teacher with ID '{request.TeacherId}' not found");
                    response.Message = "Class creation failed";
                    return response;
                }

                // ✅ FIXED: 4. Create new Class with SubjectOfferingId
                var newClass = new Domain.Entities.Class
                {
                    Id = Guid.NewGuid(),
                    ClassCode = request.ClassCode,
                    SubjectOfferingId = request.SubjectOfferingId,
                    TeacherUserId = request.TeacherId
                };

                await _uow.Classes.AddAsync(newClass);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Class created successfully";
                response.ClassId = newClass.Id;
                _logger.LogInformation($"? Class {request.ClassCode} created successfully");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Error creating class: {ex.Message}");
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Class creation failed";
                return response;
            }
        }

        // ========== UPDATE CLASS ==========
        public async Task<ClassResponse> UpdateClassAsync(Guid id, UpdateClassRequest request)
        {
            var response = new ClassResponse
            {
                ClassId = id,
                ClassCode = request.ClassCode
            };

            try
            {
                // 1. Check if class exists
                var existingClass = await _uow.Classes.GetByIdAsync(id);
                if (existingClass == null)
                {
                    response.Errors.Add($"Class with ID '{id}' not found");
                    response.Message = "Class update failed";
                    return response;
                }

                // 2. Validate ClassCode uniqueness (excluding current class)
                var isUnique = await _uow.Classes.IsClassCodeUniqueAsync(request.ClassCode, id);
                if (!isUnique)
                {
                    response.Errors.Add($"ClassCode '{request.ClassCode}' already exists");
                    response.Message = "Class update failed";
                    return response;
                }

                // ✅ FIXED: 3. Validate SubjectOffering exists (not Subject directly)
                var subjectOffering = await _uow.SubjectOfferings.GetByIdAsync(request.SubjectOfferingId);
                if (subjectOffering == null)
                {
                    response.Errors.Add($"Subject offering with ID '{request.SubjectOfferingId}' not found");
                    response.Message = "Class update failed";
                    return response;
                }

                // 4. Validate Teacher exists
                var teacher = await _uow.Teachers.GetByIdAsync(request.TeacherId);
                if (teacher == null)
                {
                    response.Errors.Add($"Teacher with ID '{request.TeacherId}' not found");
                    response.Message = "Class update failed";
                    return response;
                }

                // ✅ FIXED: 5. Update class with SubjectOfferingId
                existingClass.ClassCode = request.ClassCode;
                existingClass.SubjectOfferingId = request.SubjectOfferingId;
                existingClass.TeacherUserId = request.TeacherId;

                _uow.Classes.Update(existingClass);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Class updated successfully";
                _logger.LogInformation($"✅ Class {id} updated successfully");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error updating class {id}: {ex.Message}");
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Class update failed";
                return response;
            }
        }

        // ========== DELETE CLASS ==========
        public async Task<ClassResponse> DeleteClassAsync(Guid id)
        {
            var response = new ClassResponse
            {
                ClassId = id
            };

            try
            {
                var existingClass = await _uow.Classes.GetByIdAsync(id);
                if (existingClass == null)
                {
                    response.Errors.Add($"Class with ID '{id}' not found");
                    response.Message = "Class deletion failed";
                    return response;
                }

                response.ClassCode = existingClass.ClassCode;

                _uow.Classes.Remove(existingClass);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = "Class deleted successfully";
                _logger.LogInformation($"? Class {id} deleted successfully");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Error deleting class {id}: {ex.Message}");
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Class deletion failed";
                return response;
            }
        }

        // ========== GET CLASS ROSTER ==========
        public async Task<ClassRosterDto> GetClassRosterAsync(Guid id, ClassRosterRequest request)
        {
            try
            {
                // ✅ FIX: Get class members directly from ClassMembers table (fresh data)
                var classMembers = await _uow.ClassMembers.GetClassMembersWithDetailsAsync(id);

                // Apply filtering
                var query = classMembers.AsQueryable();

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    query = query.Where(cm =>
                        cm.Student.StudentCode.Contains(request.SearchTerm) ||
                        cm.Student.User.FullName.Contains(request.SearchTerm) ||
                        cm.Student.User.Email.Contains(request.SearchTerm)
                    );
                }

                var totalCount = query.Count();

                // Apply pagination
                var paginatedMembers = query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                var students = _mapper.Map<List<ClassStudentInfo>>(paginatedMembers);

                return new ClassRosterDto
                {
                    Students = students,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error getting class roster for {id}: {ex.Message}");
                throw;
            }
        }

        // ==================== ASSIGN STUDENTS TO CLASS ====================
        public async Task<AssignStudentsResponse> AssignStudentsToClassAsync(Guid classId, AssignStudentsRequest request)
        {
            var response = new AssignStudentsResponse();

            try
            {
                // 1. Validate class exists
                var @class = await _uow.Classes.GetByIdWithDetailsAsync(classId);
                if (@class == null)
                {
                    response.Errors.Add($"Class with ID '{classId}' not found");
                    response.Message = "Assignment failed";
                    return response;
                }

                // 2. Check max enrollment limit using ClassMembers
                var currentStudentCount = await _uow.ClassMembers.GetClassMemberCountAsync(classId);
                var availableSlots = @class.MaxEnrollment - currentStudentCount;

                if (request.StudentIds.Count > availableSlots)
                {
                    response.Errors.Add($"Cannot assign {request.StudentIds.Count} students. Only {availableSlots} slots available (Max: {@class.MaxEnrollment}, Current: {currentStudentCount})");
                    response.Message = "Assignment failed - exceeds max enrollment";
                    return response;
                }

                // 3. Process each student
                var assignedStudents = new List<AssignedStudentInfo>();
                var failedCount = 0;

                foreach (var studentId in request.StudentIds)
                {
                    // Check if student exists
                    var student = await _uow.Students.GetByIdAsync(studentId);
                    if (student == null)
                    {
                        response.Errors.Add($"Student with ID '{studentId}' not found");
                        failedCount++;
                        continue;
                    }

                    // ✅ Check if student is already in class using ClassMembers
                    var isAlreadyInClass = await _uow.ClassMembers.IsStudentInClassAsync(classId, studentId);
                    if (isAlreadyInClass)
                    {
                        response.Errors.Add($"Student '{student.StudentCode}' is already in this class");
                        failedCount++;
                        continue;
                    }

                    // ✅ Add student to class via ClassMembers
                    var classMember = new Domain.Entities.ClassMember
                    {
                        Id = Guid.NewGuid(),
                        ClassId = classId,
                        StudentId = studentId,
                        JoinedAt = DateTime.UtcNow
                    };

                    await _uow.ClassMembers.AddAsync(classMember);

                    // Add to response
                    assignedStudents.Add(new AssignedStudentInfo
                    {
                        StudentId = studentId,
                        StudentCode = student.StudentCode,
                        StudentName = student.User?.FullName ?? "Unknown",
                        JoinedAt = classMember.JoinedAt
                    });
                }

                // 4. Save changes
                await _uow.SaveChangesAsync();

                response.Success = assignedStudents.Count > 0;
                response.TotalAssigned = assignedStudents.Count;
                response.TotalFailed = failedCount;
                response.AssignedStudents = assignedStudents;
                response.Message = assignedStudents.Count > 0
                    ? $"Successfully assigned {assignedStudents.Count} student(s) to class {@class.ClassCode}"
                    : "No students were assigned";

                _logger.LogInformation($"✅ Assigned {assignedStudents.Count} students to class {classId}");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error assigning students to class {classId}: {ex.Message}");
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Assignment failed";
                return response;
            }
        }

        // ==================== REMOVE STUDENT FROM CLASS ====================
        public async Task<RemoveStudentResponse> RemoveStudentFromClassAsync(Guid classId, Guid studentId)
        {
            var response = new RemoveStudentResponse();

            try
            {
                // 1. Validate class exists
                var @class = await _uow.Classes.GetByIdAsync(classId);
                if (@class == null)
                {
                    response.Errors.Add($"Class with ID '{classId}' not found");
                    response.Message = "Removal failed";
                    return response;
                }

                // 2. ✅ Check if student is in class using ClassMembers
                var isInClass = await _uow.ClassMembers.IsStudentInClassAsync(classId, studentId);
                if (!isInClass)
                {
                    response.Errors.Add($"Student is not enrolled in this class");
                    response.Message = "Removal failed";
                    return response;
                }

                // 3. ✅ Remove student from class via ClassMembers
                await _uow.ClassMembers.RemoveStudentFromClassAsync(classId, studentId);
                await _uow.SaveChangesAsync();

                response.Success = true;
                response.Message = $"Successfully removed student from class {@class.ClassCode}";

                _logger.LogInformation($"✅ Removed student {studentId} from class {classId}");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error removing student {studentId} from class {classId}: {ex.Message}");
                response.Errors.Add($"Internal error: {ex.Message}");
                response.Message = "Removal failed";
                return response;
            }
        }
    }
}
