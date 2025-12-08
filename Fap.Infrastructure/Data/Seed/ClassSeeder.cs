using System;
using System.Collections.Generic;
using System.Linq;
using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds Classes using SubjectOffering pattern with fixed, conflict-free schedules.
    /// </summary>
    public class ClassSeeder : BaseSeeder
    {
        public static readonly Guid PRF192_Winter2025_A = Guid.Parse("40000000-0000-0000-0000-000000000101");
        public static readonly Guid PRO192_Spring2026_A = Guid.Parse("40000000-0000-0000-0000-000000000102");
        public static readonly Guid PRJ301_Fall2026_A = Guid.Parse("40000000-0000-0000-0000-000000000103");
        public static readonly Guid MAD101_Spring2026_A = Guid.Parse("40000000-0000-0000-0000-000000000104");
        public static readonly Guid SWP391_Winter2026_A = Guid.Parse("40000000-0000-0000-0000-000000000105");
        public static readonly Guid CEA201_Winter2025_Evening = Guid.Parse("40000000-0000-0000-0000-000000000106");
        public static readonly Guid DBI202_Summer2026_A = Guid.Parse("40000000-0000-0000-0000-000000000107");
        public static readonly Guid CSD201_Summer2026_A = Guid.Parse("40000000-0000-0000-0000-000000000108");
        public static readonly Guid OSG202_Fall2026_A = Guid.Parse("40000000-0000-0000-0000-000000000109");
        public static readonly Guid MAE101_Winter2025_A = Guid.Parse("40000000-0000-0000-0000-00000000010a");
        public static readonly Guid MAS291_Spring2026_A = Guid.Parse("40000000-0000-0000-0000-00000000010b");
        public static readonly Guid CRY303c_Fall2026_A = Guid.Parse("40000000-0000-0000-0000-00000000010c");
        public static readonly Guid CSI106_Winter2025_A = Guid.Parse("40000000-0000-0000-0000-00000000010d");
        public static readonly Guid PFP191_Spring2026_A = Guid.Parse("40000000-0000-0000-0000-00000000010e");
        public static readonly Guid NWC204_Summer2026_A = Guid.Parse("40000000-0000-0000-0000-00000000010f");
        public static readonly Guid DRP101_Winter2025_A = Guid.Parse("40000000-0000-0000-0000-000000000110");
        public static readonly Guid DTG102_Winter2025_A = Guid.Parse("40000000-0000-0000-0000-000000000111");
        public static readonly Guid DRS102_Spring2026_A = Guid.Parse("40000000-0000-0000-0000-000000000112");
        public static readonly Guid VCM202_Spring2026_A = Guid.Parse("40000000-0000-0000-0000-000000000113");

        private static readonly IReadOnlyList<ClassDefinition> ClassDefinitions = new List<ClassDefinition>
        {
            new ClassDefinition(PRF192_Winter2025_A, "PRF192.W25.A", SubjectOfferingSeeder.PRF192_Winter2025, TeacherStudentSeeder.Teacher1Id, 42),
            new ClassDefinition(PRO192_Spring2026_A, "PRO192.SP26.A", SubjectOfferingSeeder.PRO192_Spring2026, TeacherStudentSeeder.Teacher1Id, 48),
            new ClassDefinition(PRJ301_Fall2026_A, "PRJ301.F26.A", SubjectOfferingSeeder.PRJ301_Fall2026, TeacherStudentSeeder.Teacher2Id, 48),

            new ClassDefinition(MAD101_Spring2026_A, "MAD101.SP26.A", SubjectOfferingSeeder.MAD101_Spring2026, TeacherStudentSeeder.Teacher2Id, 36),
            new ClassDefinition(SWP391_Winter2026_A, "SWP391.W26.A", SubjectOfferingSeeder.SWP391_Winter2026, TeacherStudentSeeder.Teacher1Id, 36),

            new ClassDefinition(CEA201_Winter2025_Evening, "CEA201.W25.E", SubjectOfferingSeeder.CEA201_Winter2025, TeacherStudentSeeder.Teacher2Id, 32),
            new ClassDefinition(DBI202_Summer2026_A, "DBI202.SU26.A", SubjectOfferingSeeder.DBI202_Summer2026, TeacherStudentSeeder.Teacher3Id, 40),

            new ClassDefinition(CSD201_Summer2026_A, "CSD201.SU26.A", SubjectOfferingSeeder.CSD201_Summer2026, TeacherStudentSeeder.Teacher4Id, 30),
            new ClassDefinition(OSG202_Fall2026_A, "OSG202.F26.A", SubjectOfferingSeeder.OSG202_Fall2026, TeacherStudentSeeder.Teacher4Id, 28),

            new ClassDefinition(MAE101_Winter2025_A, "MAE101.W25.A", SubjectOfferingSeeder.MAE101_Winter2025, TeacherStudentSeeder.Teacher3Id, 50),
            new ClassDefinition(MAS291_Spring2026_A, "MAS291.SP26.A", SubjectOfferingSeeder.MAS291_Spring2026, TeacherStudentSeeder.Teacher3Id, 50),
            new ClassDefinition(CRY303c_Fall2026_A, "CRY303c.F26.A", SubjectOfferingSeeder.CRY303c_Fall2026, TeacherStudentSeeder.Teacher3Id, 45),

            new ClassDefinition(CSI106_Winter2025_A, "CSI106.W25.A", SubjectOfferingSeeder.CSI106_Winter2025, TeacherStudentSeeder.Teacher2Id, 45),
            new ClassDefinition(PFP191_Spring2026_A, "PFP191.SP26.A", SubjectOfferingSeeder.PFP191_Spring2026, TeacherStudentSeeder.Teacher2Id, 45),
            new ClassDefinition(NWC204_Summer2026_A, "NWC204.SU26.A", SubjectOfferingSeeder.NWC204_Summer2026, TeacherStudentSeeder.Teacher1Id, 35),

            new ClassDefinition(DRP101_Winter2025_A, "DRP101.W25.A", SubjectOfferingSeeder.DRP101_Winter2025, TeacherStudentSeeder.Teacher4Id, 30),
            new ClassDefinition(DTG102_Winter2025_A, "DTG102.W25.A", SubjectOfferingSeeder.DTG102_Winter2025, TeacherStudentSeeder.Teacher4Id, 30),
            new ClassDefinition(DRS102_Spring2026_A, "DRS102.SP26.A", SubjectOfferingSeeder.DRS102_Spring2026, TeacherStudentSeeder.Teacher4Id, 30),
            new ClassDefinition(VCM202_Spring2026_A, "VCM202.SP26.A", SubjectOfferingSeeder.VCM202_Spring2026, TeacherStudentSeeder.Teacher4Id, 30)
        };

        public ClassSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.Classes.AnyAsync())
            {
                Console.WriteLine("Classes already exist. Skipping seeding...");
                return;
            }

            var timestamp = DateTime.UtcNow;
            var classes = ClassDefinitions
                .Select(def => new Class
                {
                    Id = def.Id,
                    ClassCode = def.ClassCode,
                    SubjectOfferingId = def.SubjectOfferingId,
                    TeacherUserId = def.TeacherUserId,
                    MaxEnrollment = def.MaxEnrollment,
                    IsActive = true,
                    CreatedAt = timestamp,
                    UpdatedAt = timestamp
                })
                .ToList();

            await _context.Classes.AddRangeAsync(classes);
            await SaveAsync("Classes");

            Console.WriteLine($"Created {classes.Count} classes linked to semester-specific offerings");
        }

        private sealed record ClassDefinition(Guid Id, string ClassCode, Guid SubjectOfferingId, Guid TeacherUserId, int MaxEnrollment);
    }
}
