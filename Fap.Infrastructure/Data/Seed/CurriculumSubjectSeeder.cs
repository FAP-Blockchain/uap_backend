using System.Collections.Generic;
using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds the subject mappings for each curriculum with prerequisite relationships.
    /// </summary>
    public class CurriculumSubjectSeeder : BaseSeeder
    {
        public CurriculumSubjectSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.CurriculumSubjects.AnyAsync())
            {
                Console.WriteLine("Curriculum subjects already exist. Skipping...");
                return;
            }

            var hasCurriculums = await _context.Curriculums.AnyAsync();
            var hasSubjects = await _context.Subjects.AnyAsync();

            if (!hasCurriculums || !hasSubjects)
            {
                Console.WriteLine("Missing curriculum or subject data. Curriculum subject seeding skipped.");
                return;
            }

            var items = new List<CurriculumSubject>
            {
                // Software Engineering 2024
                // Semester 1
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.SoftwareEngineering2024Id,
                    SubjectId = SubjectOfferingSeeder.PRF192Id,
                    SemesterNumber = 1
                },
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.SoftwareEngineering2024Id,
                    SubjectId = SubjectOfferingSeeder.CEA201Id,
                    SemesterNumber = 1
                },

                // Semester 2
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.SoftwareEngineering2024Id,
                    SubjectId = SubjectOfferingSeeder.PRO192Id,
                    SemesterNumber = 2,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.PRF192Id
                },
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.SoftwareEngineering2024Id,
                    SubjectId = SubjectOfferingSeeder.MAD101Id,
                    SemesterNumber = 2
                },

                // Semester 3
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.SoftwareEngineering2024Id,
                    SubjectId = SubjectOfferingSeeder.CSD201Id,
                    SemesterNumber = 3,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.PRO192Id
                },
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.SoftwareEngineering2024Id,
                    SubjectId = SubjectOfferingSeeder.DBI202Id,
                    SemesterNumber = 3,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.PRO192Id
                },

                // Semester 4
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.SoftwareEngineering2024Id,
                    SubjectId = SubjectOfferingSeeder.PRJ301Id,
                    SemesterNumber = 4,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.DBI202Id // Also PRO192 but we can only set one here for now or logic handles multiple? Entity has one PrerequisiteSubjectId.
                },

                // Semester 5
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.SoftwareEngineering2024Id,
                    SubjectId = SubjectOfferingSeeder.SWP391Id,
                    SemesterNumber = 5,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.PRJ301Id
                },
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.SoftwareEngineering2024Id,
                    SubjectId = SubjectOfferingSeeder.SWT301Id,
                    SemesterNumber = 5,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.SWP391Id
                },

                // Semester 9 (Capstone)
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.SoftwareEngineering2024Id,
                    SubjectId = SubjectOfferingSeeder.SEP490Id,
                    SemesterNumber = 9,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.SWP391Id
                },



                // Graphic Design 2024
                // Semester 1
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.GraphicDesign2024Id,
                    SubjectId = SubjectOfferingSeeder.DRP101Id,
                    SemesterNumber = 1
                },
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.GraphicDesign2024Id,
                    SubjectId = SubjectOfferingSeeder.DTG102Id,
                    SemesterNumber = 1
                },

                // Semester 2
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.GraphicDesign2024Id,
                    SubjectId = SubjectOfferingSeeder.DRS102Id,
                    SemesterNumber = 2,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.DRP101Id
                },
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.GraphicDesign2024Id,
                    SubjectId = SubjectOfferingSeeder.VCM202Id,
                    SemesterNumber = 2
                },

                // Semester 3
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.GraphicDesign2024Id,
                    SubjectId = SubjectOfferingSeeder.TPG203Id,
                    SemesterNumber = 3,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.DTG102Id
                },
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.GraphicDesign2024Id,
                    SubjectId = SubjectOfferingSeeder.DGP201Id,
                    SemesterNumber = 3,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.DRS102Id
                },

                // Semester 4
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.GraphicDesign2024Id,
                    SubjectId = SubjectOfferingSeeder.WDU202cId,
                    SemesterNumber = 4,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.DTG102Id
                },
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.GraphicDesign2024Id,
                    SubjectId = SubjectOfferingSeeder.ANS201Id,
                    SemesterNumber = 4
                },

                // Semester 5
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.GraphicDesign2024Id,
                    SubjectId = SubjectOfferingSeeder.ANC302Id,
                    SemesterNumber = 5,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.DGP201Id
                },

                // Semester 9 (Capstone)
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.GraphicDesign2024Id,
                    SubjectId = SubjectOfferingSeeder.GRP490Id,
                    SemesterNumber = 9,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.ANC302Id
                },

                // ===== INFORMATION ASSURANCE =====
                // Semester 1
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.InformationAssurance2024Id,
                    SubjectId = SubjectOfferingSeeder.CSI106Id,
                    SemesterNumber = 1
                },
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.InformationAssurance2024Id,
                    SubjectId = SubjectOfferingSeeder.MAE101Id,
                    SemesterNumber = 1
                },

                // Semester 2
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.InformationAssurance2024Id,
                    SubjectId = SubjectOfferingSeeder.PFP191Id,
                    SemesterNumber = 2
                },
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.InformationAssurance2024Id,
                    SubjectId = SubjectOfferingSeeder.MAS291Id,
                    SemesterNumber = 2,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.MAE101Id
                },

                // Semester 3
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.InformationAssurance2024Id,
                    SubjectId = SubjectOfferingSeeder.NWC204Id,
                    SemesterNumber = 3,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.CSI106Id
                },

                // Semester 4
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.InformationAssurance2024Id,
                    SubjectId = SubjectOfferingSeeder.OSG202Id,
                    SemesterNumber = 4,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.CSI106Id
                },
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.InformationAssurance2024Id,
                    SubjectId = SubjectOfferingSeeder.CRY303cId,
                    SemesterNumber = 4,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.MAS291Id
                },

                // Semester 5
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.InformationAssurance2024Id,
                    SubjectId = SubjectOfferingSeeder.FRS301Id,
                    SemesterNumber = 5,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.OSG202Id
                },
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.InformationAssurance2024Id,
                    SubjectId = SubjectOfferingSeeder.HOD402Id,
                    SemesterNumber = 5,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.NWC204Id
                },

                // Semester 9 (Capstone)
                new CurriculumSubject
                {
                    CurriculumId = CurriculumSeeder.InformationAssurance2024Id,
                    SubjectId = SubjectOfferingSeeder.IAP490Id,
                    SemesterNumber = 9,
                    PrerequisiteSubjectId = SubjectOfferingSeeder.HOD402Id
                }
            };

            await _context.CurriculumSubjects.AddRangeAsync(items);
            await SaveAsync("Curriculum Subjects");
        }
    }
}
