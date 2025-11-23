using System;
using System.ComponentModel.DataAnnotations;

namespace Fap.Domain.DTOs.Credential
{
    public class IssueCredentialDto
    {
        [Required]
        public Guid StudentId { get; set; }

        public Guid? SubjectId { get; set; }
        
        public Guid? StudentRoadmapId { get; set; }

        [Required]
        public string Type { get; set; } = "SubjectCompletion"; // SubjectCompletion, RoadmapCompletion
    }
}
