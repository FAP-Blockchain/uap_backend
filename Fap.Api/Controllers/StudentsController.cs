using Fap.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Fap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // T?t c? roles ??u có th? truy c?p (có th? customize later)
    public class StudentsController : ControllerBase
    {
        private readonly StudentService _studentService;

        public StudentsController(StudentService studentService)
        {
            _studentService = studentService;
        }

        // ========================================
        // ?? API #18: GET /api/students
        // L?y danh sách t?t c? sinh viên
        // ========================================
        [HttpGet]
        public async Task<IActionResult> GetAllStudents()
        {
            var students = await _studentService.GetAllStudentsAsync();
            
            return Ok(new
            {
                success = true,
                data = students,
                count = students.Count
            });
        }

        // ========================================
        // ?? API #19: GET /api/students/{id}
        // L?y chi ti?t sinh viên theo ID
        // ========================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudentById(Guid id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            
            if (student == null)
                return NotFound(new 
                { 
                    success = false,
                    message = $"Student with ID '{id}' not found" 
                });

            return Ok(new
            {
                success = true,
                data = student
            });
        }
    }
}
