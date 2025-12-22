using System;
using System.Collections.Generic;
using System.Linq;
using Fap.Domain.DTOs.Attendance;
using Fap.Domain.DTOs.Grade;

namespace Fap.Domain.DTOs.Credential
{
    public class CredentialRequestPreIssueVerifyDto
    {
        public Guid RequestId { get; set; }
        public Guid StudentId { get; set; }
        public string CertificateType { get; set; } = string.Empty;

        public Guid? SubjectId { get; set; }
        public Guid? SemesterId { get; set; }
        public Guid? StudentRoadmapId { get; set; }

        // Resolved context for verification
        public Guid? ClassId { get; set; }
        public string? ClassCode { get; set; }
        public long? OnChainClassId { get; set; }

        public List<AttendanceVerifyItemDto> Attendance { get; set; } = new();
        public List<GradeVerifyItemDto> Grades { get; set; } = new();

        public bool AttendanceAllVerified => Attendance.Count > 0 && Attendance.All(x => x.Verified);
        public bool GradesAllVerified => Grades.Count > 0 && Grades.All(x => x.Verified);

        public string? Message { get; set; }
    }
}
