using System;
using System.Collections.Generic;
using System.Linq;
using Fap.Domain.Entities;

namespace Fap.Domain.Helpers
{
    /// <summary>
    /// Utility helper to derive curriculum progress snapshots from a fully-loaded student aggregate.
    /// </summary>
    public static class CurriculumProgressHelper
    {
        /// <summary>
        /// Build a curriculum progress snapshot for the supplied student.
        /// The student instance must include Curriculum -> CurriculumSubjects (with Subject and PrerequisiteSubject),
        /// Grades (with GradeComponent), and Enrollments (with Class -> SubjectOffering -> Subject/Semester).
        /// </summary>
        public static CurriculumProgressSnapshot BuildSnapshot(Student student, DateTime? referenceTimeUtc = null)
        {
            if (student == null)
            {
                throw new ArgumentNullException(nameof(student));
            }

            if (student.Curriculum == null || student.Curriculum.CurriculumSubjects == null)
            {
                return new CurriculumProgressSnapshot(student, Enumerable.Empty<CurriculumSubject>());
            }

            var now = referenceTimeUtc ?? DateTime.UtcNow;
            var orderedCurriculumSubjects = student.Curriculum.CurriculumSubjects
                .OrderBy(cs => cs.SemesterNumber)
                .ThenBy(cs => cs.Subject.SubjectCode)
                .ToList();

            if (!orderedCurriculumSubjects.Any())
            {
                return new CurriculumProgressSnapshot(student, orderedCurriculumSubjects);
            }

            var snapshot = new CurriculumProgressSnapshot(student, orderedCurriculumSubjects);

            var gradeSummary = BuildGradeSummary(student);

            var completedSubjectIds = gradeSummary
                .Where(kvp => kvp.Value.WeightPercent >= 100 && kvp.Value.FinalScore.HasValue && kvp.Value.FinalScore.Value >= 5m)
                .Select(kvp => kvp.Key)
                .ToHashSet();

            var failedSubjectIds = gradeSummary
                .Where(kvp => kvp.Value.WeightPercent >= 100 && kvp.Value.FinalScore.HasValue && kvp.Value.FinalScore.Value < 5m)
                .Select(kvp => kvp.Key)
                .ToHashSet();

            var enrollmentLookup = BuildEnrollmentLookup(student);

            foreach (var cs in orderedCurriculumSubjects)
            {
                gradeSummary.TryGetValue(cs.SubjectId, out var summary);
                var finalScore = summary.FinalScore;
                var totalWeight = summary.WeightPercent;

                enrollmentLookup.TryGetValue(cs.SubjectId, out var enrollmentsForSubject);
                var latestEnrollment = enrollmentsForSubject?.FirstOrDefault();

                var (isCurrentEnrollment, currentClassId, currentClassCode, currentSemesterId, currentSemesterName) =
                    ResolveEnrollmentMetadata(latestEnrollment, now);

                var prerequisiteCode = cs.PrerequisiteSubject?.SubjectCode;
                var prerequisitesMet = cs.PrerequisiteSubjectId == null || completedSubjectIds.Contains(cs.PrerequisiteSubjectId.Value);
                var hasApprovedEnrollment = latestEnrollment != null;

                var status = ResolveSubjectStatus(
                    cs.SubjectId,
                    completedSubjectIds,
                    failedSubjectIds,
                    isCurrentEnrollment,
                    hasApprovedEnrollment,
                    prerequisitesMet,
                    finalScore,
                    totalWeight);

                snapshot.IncrementCounters(status, cs.Subject.Credits);

                // Only assign current class info when subject is in progress
                Guid? assignedClassId = null;
                string? assignedClassCode = null;
                Guid? assignedSemesterId = null;
                string? assignedSemesterName = null;
                decimal? assignedFinalScore = null;

                if (status == "InProgress")
                {
                    // Subject currently in progress: keep latest class and provisional score
                    assignedClassId = currentClassId;
                    assignedClassCode = currentClassCode;
                    assignedSemesterId = currentSemesterId;
                    assignedSemesterName = currentSemesterName;
                    assignedFinalScore = finalScore;
                }
                else if (status == "Completed" || status == "Failed")
                {
                    // Subject finished or failed: keep final score only
                    assignedFinalScore = finalScore;
                }
                // Other states do not carry class info

                var subjectProgress = new SubjectProgressInfo
                {
                    SubjectId = cs.SubjectId,
                    CurriculumSubject = cs,
                    Status = status,
                    FinalScore = assignedFinalScore,
                    PrerequisitesMet = prerequisitesMet,
                    PrerequisiteSubjectCode = prerequisiteCode,
                    CurrentClassId = assignedClassId,
                    CurrentClassCode = assignedClassCode,
                    CurrentSemesterId = assignedSemesterId,
                    CurrentSemesterName = assignedSemesterName,
                    IsCurrentEnrollment = isCurrentEnrollment,
                    Credits = cs.Subject.Credits
                };

                snapshot.Subjects[cs.SubjectId] = subjectProgress;
            }

            return snapshot;
        }

        private static Dictionary<Guid, GradeAggregate> BuildGradeSummary(Student student)
        {
            var result = new Dictionary<Guid, GradeAggregate>();

            if (student.Grades == null || !student.Grades.Any())
            {
                return result;
            }

            var groups = student.Grades
                .Where(g => g.SubjectId != Guid.Empty)
                .GroupBy(g => g.SubjectId);

            foreach (var group in groups)
            {
                decimal weightedTotal = 0m;
                int totalWeight = 0;

            foreach (var grade in group)
            {
                var weight = grade.GradeComponent?.WeightPercent ?? 0;
                if (weight <= 0 || !grade.Score.HasValue)
                {
                    continue;
                }

                weightedTotal += grade.Score.Value * weight;
                totalWeight += weight;
            }                decimal? finalScore = null;
                if (totalWeight > 0)
                {
                    finalScore = Math.Round(weightedTotal / 100m, 2);
                }

                result[group.Key] = new GradeAggregate(finalScore, totalWeight);
            }

            return result;
        }

        private static Dictionary<Guid, List<Enroll>> BuildEnrollmentLookup(Student student)
        {
            if (student.Enrolls == null)
            {
                return new Dictionary<Guid, List<Enroll>>();
            }

            return student.Enrolls
                .Where(e => e.IsApproved && e.Class?.SubjectOffering != null)
                .GroupBy(e => e.Class!.SubjectOffering!.SubjectId)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .OrderByDescending(e => e.RegisteredAt)
                        .ToList());
        }

        private static (bool isCurrent, Guid? classId, string? classCode, Guid? semesterId, string? semesterName)
            ResolveEnrollmentMetadata(Enroll? enrollment, DateTime now)
        {
            if (enrollment?.Class == null)
            {
                return (false, null, null, null, null);
            }

            var subjectClass = enrollment.Class;
            var classCode = subjectClass.ClassCode;
            var classId = subjectClass.Id;
            var offering = subjectClass.SubjectOffering;
            var semesterId = offering?.SemesterId;
            var semesterName = offering?.Semester?.Name;

            if (offering?.Semester == null)
            {
                return (true, classId, classCode, semesterId, semesterName);
            }

            var start = offering.Semester.StartDate;
            var end = offering.Semester.EndDate;

            if (start == default || end == default)
            {
                return (true, classId, classCode, semesterId, semesterName);
            }

            if (now >= start && now <= end)
            {
                return (true, classId, classCode, semesterId, semesterName);
            }

            if (now < start)
            {
                // Enrollment for upcoming semester – treat as in progress for planning purposes.
                return (true, classId, classCode, semesterId, semesterName);
            }

            return (false, classId, classCode, semesterId, semesterName);
        }

        private static string ResolveSubjectStatus(
            Guid subjectId,
            HashSet<Guid> completedSubjectIds,
            HashSet<Guid> failedSubjectIds,
            bool isCurrentEnrollment,
            bool hasApprovedEnrollment,
            bool prerequisitesMet,
            decimal? finalScore,
            int totalWeight)
        {
            if (completedSubjectIds.Contains(subjectId))
            {
                return "Completed";
            }

            if (failedSubjectIds.Contains(subjectId))
            {
                return "Failed";
            }

            if (isCurrentEnrollment || hasApprovedEnrollment)
            {
                return "InProgress";
            }

            if (!prerequisitesMet)
            {
                return "Locked";
            }

            if (finalScore.HasValue && totalWeight > 0)
            {
                return "InProgress";
            }

            return "Open";
        }

        private readonly record struct GradeAggregate(decimal? FinalScore, int WeightPercent);
    }

    public class CurriculumProgressSnapshot
    {
        public CurriculumProgressSnapshot(Student student, IEnumerable<CurriculumSubject> curriculumSubjects)
        {
            Student = student;
            CurriculumSubjects = curriculumSubjects.ToList();
            Subjects = new Dictionary<Guid, SubjectProgressInfo>();
        }

        public Student Student { get; }

        public IReadOnlyList<CurriculumSubject> CurriculumSubjects { get; }

        public Dictionary<Guid, SubjectProgressInfo> Subjects { get; }

        public int TotalSubjects { get; private set; }
        public int CompletedSubjects { get; private set; }
        public int FailedSubjects { get; private set; }
        public int InProgressSubjects { get; private set; }
        public int OpenSubjects { get; private set; }
        public int LockedSubjects { get; private set; }
        public int RequiredCredits { get; private set; }
        public int CompletedCredits { get; private set; }

        public void IncrementCounters(string status, int credits)
        {
            TotalSubjects++;
            RequiredCredits += credits;

            switch (status)
            {
                case "Completed":
                    CompletedSubjects++;
                    CompletedCredits += credits;
                    break;
                case "Failed":
                    FailedSubjects++;
                    break;
                case "InProgress":
                    InProgressSubjects++;
                    break;
                case "Locked":
                    LockedSubjects++;
                    break;
                default:
                    OpenSubjects++;
                    break;
            }
        }
    }

    public class SubjectProgressInfo
    {
        public Guid SubjectId { get; set; }
        public CurriculumSubject CurriculumSubject { get; set; } = null!;
        public string Status { get; set; } = string.Empty;
        public decimal? FinalScore { get; set; }
        public bool PrerequisitesMet { get; set; }
        public string? PrerequisiteSubjectCode { get; set; }
        public Guid? CurrentClassId { get; set; }
        public string? CurrentClassCode { get; set; }
        public Guid? CurrentSemesterId { get; set; }
        public string? CurrentSemesterName { get; set; }
        public bool IsCurrentEnrollment { get; set; }
        public int Credits { get; set; }
    }
}
