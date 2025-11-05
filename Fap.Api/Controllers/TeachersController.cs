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
    public class TeachersController : ControllerBase
    {
        private readonly TeacherService _teacherService;

        public TeachersController(TeacherService teacherService)
        {
            _teacherService = teacherService;
        }

        // ========================================
        // ?? API #20: GET /api/teachers
        // L?y danh sách t?t c? gi?ng viên
        // ========================================
        [HttpGet]
        public async Task<IActionResult> GetAllTeachers()
        {
            var teachers = await _teacherService.GetAllTeachersAsync();
            
            return Ok(new
            {
                success = true,
                data = teachers,
                count = teachers.Count
            });
        }

        // ========================================
        // ?? API #21: GET /api/teachers/{id}
        // L?y chi ti?t gi?ng viên theo ID
        // ========================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTeacherById(Guid id)
        {
            var teacher = await _teacherService.GetTeacherByIdAsync(id);
            
            if (teacher == null)
                return NotFound(new 
                { 
                    success = false,
                    message = $"Teacher with ID '{id}' not found" 
                });

            return Ok(new
            {
                success = true,
                data = teacher
            });
        }
    }
}
