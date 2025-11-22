using Fap.Domain.DTOs.Curriculum;
using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fap.Api.Controllers
{
    /// <summary>
    /// Curriculum Management API for Admin
    /// Manages curriculum definitions and their subject mappings
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class CurriculumController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<CurriculumController> _logger;

        public CurriculumController(IUnitOfWork uow, ILogger<CurriculumController> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/curriculum - Get all curriculums
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllCurriculums()
        {
            try
            {
                var curriculums = await _uow.Curriculums.GetAllWithDetailsAsync();

                var result = curriculums.Select(c => new CurriculumListDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Description = c.Description,
                    TotalCredits = c.TotalCredits,
                    SubjectCount = c.CurriculumSubjects?.Count ?? 0,
                    StudentCount = c.Students?.Count ?? 0
                }).ToList();

                return Ok(new
                {
                    Success = true,
                    Message = $"Retrieved {result.Count} curriculums",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all curriculums");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Failed to retrieve curriculums",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// GET /api/curriculum/{id} - Get curriculum details with subjects
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCurriculumById(int id)
        {
            try
            {
                var curriculum = await _uow.Curriculums.GetByIdAsync(id);

                if (curriculum == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = $"Curriculum with ID {id} not found"
                    });
                }

                var result = new CurriculumDetailDto
                {
                    Id = curriculum.Id,
                    Code = curriculum.Code,
                    Name = curriculum.Name,
                    Description = curriculum.Description,
                    TotalCredits = curriculum.TotalCredits,
                    StudentCount = curriculum.Students?.Count ?? 0,
                    Subjects = curriculum.CurriculumSubjects?
                        .OrderBy(cs => cs.SemesterNumber)
                        .ThenBy(cs => cs.Subject?.SubjectCode)
                        .Select(cs => new CurriculumSubjectDto
                        {
                            Id = cs.Id,
                            SubjectId = cs.SubjectId,
                            SubjectCode = cs.Subject?.SubjectCode ?? "",
                            SubjectName = cs.Subject?.SubjectName ?? "",
                            Credits = cs.Subject?.Credits ?? 0,
                            SemesterNumber = cs.SemesterNumber,
                            PrerequisiteSubjectId = cs.PrerequisiteSubjectId,
                            PrerequisiteSubjectCode = cs.PrerequisiteSubject?.SubjectCode,
                            PrerequisiteSubjectName = cs.PrerequisiteSubject?.SubjectName
                        }).ToList() ?? new List<CurriculumSubjectDto>()
                };

                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get curriculum {Id}", id);
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Failed to retrieve curriculum",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// POST /api/curriculum - Create new curriculum
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCurriculum([FromBody] CreateCurriculumRequest request)
        {
            try
            {
                // Check if code already exists
                if (await _uow.Curriculums.CodeExistsAsync(request.Code))
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = $"Curriculum with code '{request.Code}' already exists"
                    });
                }

                var curriculum = new Curriculum
                {
                    Code = request.Code,
                    Name = request.Name,
                    Description = request.Description,
                    TotalCredits = request.TotalCredits
                };

                await _uow.Curriculums.AddAsync(curriculum);
                await _uow.SaveChangesAsync();

                _logger.LogInformation("Created curriculum: {Code} - {Name}", curriculum.Code, curriculum.Name);

                return CreatedAtAction(nameof(GetCurriculumById), new { id = curriculum.Id }, new
                {
                    Success = true,
                    Message = "Curriculum created successfully",
                    Data = new CurriculumListDto
                    {
                        Id = curriculum.Id,
                        Code = curriculum.Code,
                        Name = curriculum.Name,
                        Description = curriculum.Description,
                        TotalCredits = curriculum.TotalCredits,
                        SubjectCount = 0,
                        StudentCount = 0
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create curriculum");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Failed to create curriculum",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// PUT /api/curriculum/{id} - Update curriculum
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCurriculum(int id, [FromBody] UpdateCurriculumRequest request)
        {
            try
            {
                var curriculum = await _uow.Curriculums.GetByIdAsync(id);

                if (curriculum == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = $"Curriculum with ID {id} not found"
                    });
                }

                // Check if code already exists (excluding current curriculum)
                if (await _uow.Curriculums.CodeExistsAsync(request.Code, id))
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = $"Curriculum with code '{request.Code}' already exists"
                    });
                }

                curriculum.Code = request.Code;
                curriculum.Name = request.Name;
                curriculum.Description = request.Description;
                curriculum.TotalCredits = request.TotalCredits;

                _uow.Curriculums.Update(curriculum);
                await _uow.SaveChangesAsync();

                _logger.LogInformation("Updated curriculum {Id}: {Code} - {Name}", id, curriculum.Code, curriculum.Name);

                return Ok(new
                {
                    Success = true,
                    Message = "Curriculum updated successfully",
                    Data = new CurriculumListDto
                    {
                        Id = curriculum.Id,
                        Code = curriculum.Code,
                        Name = curriculum.Name,
                        Description = curriculum.Description,
                        TotalCredits = curriculum.TotalCredits,
                        SubjectCount = curriculum.CurriculumSubjects?.Count ?? 0,
                        StudentCount = curriculum.Students?.Count ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update curriculum {Id}", id);
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Failed to update curriculum",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// DELETE /api/curriculum/{id} - Delete curriculum
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCurriculum(int id)
        {
            try
            {
                var curriculum = await _uow.Curriculums.GetByIdAsync(id);

                if (curriculum == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = $"Curriculum with ID {id} not found"
                    });
                }

                // Check if curriculum is in use
                if (curriculum.Students != null && curriculum.Students.Any())
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = $"Cannot delete curriculum. It is currently assigned to {curriculum.Students.Count} student(s)"
                    });
                }

                _uow.Curriculums.Remove(curriculum);
                await _uow.SaveChangesAsync();

                _logger.LogInformation("Deleted curriculum {Id}: {Code} - {Name}", id, curriculum.Code, curriculum.Name);

                return Ok(new
                {
                    Success = true,
                    Message = "Curriculum deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete curriculum {Id}", id);
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Failed to delete curriculum",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// POST /api/curriculum/{id}/subjects - Add subject to curriculum
        /// </summary>
        [HttpPost("{id}/subjects")]
        public async Task<IActionResult> AddSubjectToCurriculum(int id, [FromBody] AddSubjectToCurriculumRequest request)
        {
            try
            {
                var curriculum = await _uow.Curriculums.GetByIdAsync(id);
                if (curriculum == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = $"Curriculum with ID {id} not found"
                    });
                }

                // Check if subject exists
                var subject = await _uow.Subjects.GetByIdAsync(request.SubjectId);
                if (subject == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = $"Subject with ID {request.SubjectId} not found"
                    });
                }

                // Check if subject already exists in curriculum
                if (curriculum.CurriculumSubjects?.Any(cs => cs.SubjectId == request.SubjectId) == true)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = $"Subject '{subject.SubjectCode}' is already in this curriculum"
                    });
                }

                // Validate prerequisite if provided
                if (request.PrerequisiteSubjectId.HasValue)
                {
                    var prerequisite = await _uow.Subjects.GetByIdAsync(request.PrerequisiteSubjectId.Value);
                    if (prerequisite == null)
                    {
                        return NotFound(new
                        {
                            Success = false,
                            Message = $"Prerequisite subject with ID {request.PrerequisiteSubjectId} not found"
                        });
                    }

                    // Check if prerequisite is in curriculum
                    if (curriculum.CurriculumSubjects?.Any(cs => cs.SubjectId == request.PrerequisiteSubjectId) != true)
                    {
                        return BadRequest(new
                        {
                            Success = false,
                            Message = $"Prerequisite subject '{prerequisite.SubjectCode}' must be added to curriculum first"
                        });
                    }
                }

                var curriculumSubject = new CurriculumSubject
                {
                    CurriculumId = id,
                    SubjectId = request.SubjectId,
                    SemesterNumber = request.SemesterNumber,
                    PrerequisiteSubjectId = request.PrerequisiteSubjectId
                };

                await _uow.CurriculumSubjects.AddAsync(curriculumSubject);
                await _uow.SaveChangesAsync();

                _logger.LogInformation("Added subject {SubjectCode} to curriculum {CurriculumCode}",
                    subject.SubjectCode, curriculum.Code);

                return Ok(new
                {
                    Success = true,
                    Message = $"Subject '{subject.SubjectCode}' added to curriculum successfully",
                    Data = new CurriculumSubjectDto
                    {
                        Id = curriculumSubject.Id,
                        SubjectId = subject.Id,
                        SubjectCode = subject.SubjectCode ?? "",
                        SubjectName = subject.SubjectName ?? "",
                        Credits = subject.Credits,
                        SemesterNumber = curriculumSubject.SemesterNumber,
                        PrerequisiteSubjectId = request.PrerequisiteSubjectId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add subject to curriculum {Id}", id);
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Failed to add subject to curriculum",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// DELETE /api/curriculum/{id}/subjects/{subjectId} - Remove subject from curriculum
        /// </summary>
        [HttpDelete("{id}/subjects/{subjectId}")]
        public async Task<IActionResult> RemoveSubjectFromCurriculum(int id, Guid subjectId)
        {
            try
            {
                var curriculum = await _uow.Curriculums.GetByIdAsync(id);
                if (curriculum == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = $"Curriculum with ID {id} not found"
                    });
                }

                var curriculumSubject = curriculum.CurriculumSubjects?
                    .FirstOrDefault(cs => cs.SubjectId == subjectId);

                if (curriculumSubject == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Subject not found in this curriculum"
                    });
                }

                // Check if this subject is a prerequisite for other subjects
                var dependentSubjects = curriculum.CurriculumSubjects?
                    .Where(cs => cs.PrerequisiteSubjectId == subjectId)
                    .Select(cs => cs.Subject?.SubjectCode)
                    .ToList();

                if (dependentSubjects != null && dependentSubjects.Any())
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = $"Cannot remove subject. It is a prerequisite for: {string.Join(", ", dependentSubjects)}"
                    });
                }

                _uow.CurriculumSubjects.Remove(curriculumSubject);
                await _uow.SaveChangesAsync();

                _logger.LogInformation("Removed subject {SubjectId} from curriculum {CurriculumId}", subjectId, id);

                return Ok(new
                {
                    Success = true,
                    Message = "Subject removed from curriculum successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove subject from curriculum {Id}", id);
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Failed to remove subject from curriculum",
                    Error = ex.Message
                });
            }
        }
    }
}
