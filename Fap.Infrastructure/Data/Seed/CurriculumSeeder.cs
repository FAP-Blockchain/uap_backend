using System.Collections.Generic;
using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds academic curriculums that tie students to structured study plans.
    /// </summary>
    public class CurriculumSeeder : BaseSeeder
    {
        public static int SoftwareEngineering2024Id = 1;
        public static int InformationAssurance2024Id = 2;
        public static int GraphicDesign2024Id = 3;

        public CurriculumSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.Curriculums.AnyAsync())
            {
                Console.WriteLine("Curriculums already exist. Skipping...");
                return;
            }

            var curriculums = new List<Curriculum>
            {
                new Curriculum
                {
                    Code = "SE-2024",
                    Name = "Software Engineering 2024",
                    Description = "Four-year curriculum focused on software development, quality, and architecture.",
                    TotalCredits = 120
                },
                new Curriculum
                {
                    Code = "IA-2024",
                    Name = "Information Assurance 2024",
                    Description = "Focuses on cybersecurity, network defense, digital forensics, and secure system administration.",
                    TotalCredits = 120
                },
                new Curriculum
                {
                    Code = "GD-2024",
                    Name = "Graphic Design 2024",
                    Description = "Comprehensive graphic design program covering visual arts, digital tools, and multimedia.",
                    TotalCredits = 120
                }
            };

            await _context.Curriculums.AddRangeAsync(curriculums);
            await SaveAsync("Curriculums");
            
            // Store generated IDs for reference by other seeders
            var seCurriculum = await _context.Curriculums.FirstOrDefaultAsync(c => c.Code == "SE-2024");
            var iaCurriculum = await _context.Curriculums.FirstOrDefaultAsync(c => c.Code == "IA-2024");
            var gdCurriculum = await _context.Curriculums.FirstOrDefaultAsync(c => c.Code == "GD-2024");
            
            if (seCurriculum != null) SoftwareEngineering2024Id = seCurriculum.Id;
            if (iaCurriculum != null) InformationAssurance2024Id = iaCurriculum.Id;
            if (gdCurriculum != null) GraphicDesign2024Id = gdCurriculum.Id;
        }
    }
}
