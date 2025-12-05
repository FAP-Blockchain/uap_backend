using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds CertificateTemplates for different types of credentials
    /// </summary>
    public class CertificateTemplateSeeder : BaseSeeder
    {
        // Fixed GUIDs for certificate templates
        public static readonly Guid CompletionCertificateId = Guid.Parse("60000000-0000-0000-0000-000000000001");
        public static readonly Guid ExcellenceCertificateId = Guid.Parse("60000000-0000-0000-0000-000000000002");
        public static readonly Guid DiplomaId = Guid.Parse("60000000-0000-0000-0000-000000000003");
        public static readonly Guid TranscriptId = Guid.Parse("60000000-0000-0000-0000-000000000004");
        public static readonly Guid ParticipationId = Guid.Parse("60000000-0000-0000-0000-000000000005");

        public CertificateTemplateSeeder(FapDbContext context) : base(context) { }

        public override async Task SeedAsync()
        {
            if (await _context.CertificateTemplates.AnyAsync())
            {
                Console.WriteLine("⏭️ Certificate Templates already exist. Skipping...");
                return;
            }

            var templates = new List<CertificateTemplate>
            {
                new CertificateTemplate
                {
                    Id = CompletionCertificateId,
                    Name = "Course Completion Certificate",
                    Description = "Awarded to students who successfully complete a course with passing grade",
                    TemplateFileUrl = "ipfs://QmTemplateCompletionCert123/template.pdf",
                    TemplateContent = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333; background-color: #f0f8ff; }
        .container { width: 800px; margin: 0 auto; padding: 40px; border: 10px solid #0056b3; background-color: #fff; position: relative; box-shadow: 0 0 20px rgba(0,0,0,0.1); }
        .header { text-align: center; color: #0056b3; }
        .title { font-size: 42px; font-weight: bold; margin-bottom: 10px; text-transform: uppercase; letter-spacing: 2px; }
        .subtitle { font-size: 20px; color: #555; }
        .content { margin-top: 60px; text-align: center; line-height: 1.8; font-size: 18px; }
        .student-name { font-size: 32px; font-weight: bold; color: #004494; margin: 20px 0; border-bottom: 2px solid #0056b3; display: inline-block; padding: 0 20px 5px; }
        .course-name { font-size: 24px; font-weight: bold; color: #333; }
        .date { margin-top: 40px; font-style: italic; color: #666; }
        .footer { margin-top: 80px; display: flex; justify-content: space-between; padding: 0 50px; }
        .signature { text-align: center; border-top: 1px solid #333; padding-top: 10px; width: 200px; }
        .watermark { position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); opacity: 0.05; font-size: 100px; color: #0056b3; font-weight: bold; z-index: 0; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='watermark'>UAP</div>
        <div class='header'>
            <div class='title'>Certificate of Completion</div>
            <div class='subtitle'>This is to certify that</div>
        </div>
        <div class='content'>
            <div class='student-name'>{{StudentName}}</div>
            <div>has successfully completed the course</div>
            <div class='course-name'>{{SubjectName}}</div>
            <div>with a grade of <b>{{Grade}}</b></div>
        </div>
        <div class='date'>Given this day, {{Date}}</div>
        <div class='footer'>
            <div class='signature'>
                <p>Instructor</p>
            </div>
            <div class='signature'>
                <p>Dean of Faculty</p>
            </div>
        </div>
    </div>
</body>
</html>"
                },
                new CertificateTemplate
                {
                    Id = ExcellenceCertificateId,
                    Name = "Certificate of Excellence",
                    Description = "Awarded to students with outstanding academic performance (GPA >= 8.5)",
                    TemplateFileUrl = "ipfs://QmTemplateExcellenceCert456/template.pdf",
                    TemplateContent = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: 'Georgia', serif; color: #333; background-color: #e6f2ff; }
        .container { width: 800px; margin: 0 auto; padding: 40px; border: 15px double #003366; background-color: #fff; position: relative; }
        .header { text-align: center; color: #003366; }
        .title { font-size: 48px; font-weight: bold; margin-bottom: 15px; text-transform: uppercase; }
        .subtitle { font-size: 22px; color: #444; font-style: italic; }
        .content { margin-top: 50px; text-align: center; line-height: 1.8; font-size: 20px; }
        .student-name { font-size: 36px; font-weight: bold; color: #004494; margin: 25px 0; font-family: 'Arial', sans-serif; }
        .achievement { font-size: 24px; color: #003366; font-weight: bold; }
        .date { margin-top: 50px; font-style: italic; }
        .footer { margin-top: 70px; display: flex; justify-content: center; gap: 100px; }
        .signature { text-align: center; border-top: 2px solid #003366; padding-top: 10px; width: 250px; }
        .badge { position: absolute; top: 40px; right: 40px; width: 100px; height: 100px; background-color: #ffd700; border-radius: 50%; display: flex; align-items: center; justify-content: center; color: #003366; font-weight: bold; box-shadow: 0 4px 8px rgba(0,0,0,0.2); }
    </style>
</head>
<body>
    <div class='container'>
        <div class='badge'>Excellence</div>
        <div class='header'>
            <div class='title'>Certificate of Excellence</div>
            <div class='subtitle'>Presented to</div>
        </div>
        <div class='content'>
            <div class='student-name'>{{StudentName}}</div>
            <div>For outstanding academic performance in</div>
            <div class='achievement'>{{Semester}}</div>
            <div>Achieving a GPA of <b>{{GPA}}</b></div>
        </div>
        <div class='date'>Awarded on {{Date}}</div>
        <div class='footer'>
            <div class='signature'>
                <p>University President</p>
            </div>
        </div>
    </div>
</body>
</html>"
                },
                new CertificateTemplate
                {
                    Id = DiplomaId,
                    Name = "Graduation Diploma",
                    Description = "Official diploma awarded upon successful completion of degree program",
                    TemplateFileUrl = "ipfs://QmTemplateDiploma789/template.pdf",
                    TemplateContent = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: 'Times New Roman', serif; color: #000; background-color: #fff; }
        .container { width: 900px; margin: 0 auto; padding: 60px; border: 20px solid #002244; position: relative; }
        .university-name { text-align: center; font-size: 40px; font-weight: bold; color: #002244; text-transform: uppercase; margin-bottom: 40px; }
        .content { text-align: center; line-height: 2.0; font-size: 22px; }
        .student-name { font-size: 40px; font-weight: bold; color: #003366; margin: 20px 0; font-family: 'Arial', sans-serif; }
        .degree { font-size: 30px; font-weight: bold; color: #002244; margin: 20px 0; }
        .footer { margin-top: 100px; display: flex; justify-content: space-between; padding: 0 50px; }
        .signature { text-align: center; border-top: 1px solid #000; padding-top: 10px; width: 250px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='university-name'>UAP Blockchain University</div>
        <div class='content'>
            <div>Upon the recommendation of the Faculty and by the authority of the Board of Trustees</div>
            <div>has conferred upon</div>
            <div class='student-name'>{{StudentName}}</div>
            <div>the degree of</div>
            <div class='degree'>{{DegreeName}}</div>
            <div>with all the rights, privileges, and honors thereunto appertaining.</div>
        </div>
        <div class='footer'>
            <div class='signature'>
                <p>President</p>
            </div>
            <div class='signature'>
                <p>Dean</p>
            </div>
        </div>
    </div>
</body>
</html>"
                },
                new CertificateTemplate
                {
                    Id = TranscriptId,
                    Name = "Academic Transcript",
                    Description = "Official record of all courses and grades",
                    TemplateFileUrl = "ipfs://QmTemplateTranscript012/template.pdf",
                    TemplateContent = "<html><body><h1>Transcript Placeholder</h1></body></html>"
                },
                new CertificateTemplate
                {
                    Id = ParticipationId,
                    Name = "Participation Certificate",
                    Description = "Awarded for active participation in courses or events",
                    TemplateFileUrl = "ipfs://QmTemplateParticipation345/template.pdf",
                    TemplateContent = "<html><body><h1>Participation Placeholder</h1></body></html>"
                }
            };

            await _context.CertificateTemplates.AddRangeAsync(templates);
            await SaveAsync("Certificate Templates");

            Console.WriteLine($"   ✅ Created {templates.Count} certificate templates");
        }
    }
}
