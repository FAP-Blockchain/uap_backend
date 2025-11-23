using Fap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fap.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds Permissions for each Role (Admin, Teacher, Student)
    /// Essential for API authorization and access control
    /// </summary>
    public class PermissionSeeder : BaseSeeder
    {
  public PermissionSeeder(FapDbContext context) : base(context) { }

      public override async Task SeedAsync()
        {
    if (await _context.Permissions.AnyAsync())
       {
  Console.WriteLine("Permissions already exist. Skipping seeding...");
           return;
        }

            var permissions = new List<Permission>();

            // ==================== ADMIN PERMISSIONS ====================
          var adminPermissions = new[]
  {
   // User Management
     ("MANAGE_USERS", "Create, edit, delete users"),
                ("MANAGE_ROLES", "Create, edit, delete roles"),
 ("MANAGE_PERMISSIONS", "Assign/revoke permissions"),
     ("VIEW_ALL_USERS", "View all user profiles"),
       
      // Grade Management
 ("VIEW_ALL_GRADES", "View grades of all students"),
     ("EDIT_ALL_GRADES", "Edit any student's grades"),
   ("DELETE_GRADES", "Delete grade records"),
      ("OVERRIDE_GRADES", "Override system-calculated grades"),
        
        // Class & Schedule Management
("MANAGE_CLASSES", "Create, edit, delete classes"),
      ("MANAGE_SCHEDULES", "Manage all class schedules"),
       ("CANCEL_SLOTS", "Cancel any class slot"),
   ("ASSIGN_TEACHERS", "Assign/change teachers for classes"),
       
     // Attendance Management
        ("VIEW_ALL_ATTENDANCE", "View all attendance records"),
           ("EDIT_ALL_ATTENDANCE", "Edit any attendance record"),
      ("GENERATE_ATTENDANCE_REPORTS", "Generate attendance reports"),
          
           // Credential Management
     ("ISSUE_CREDENTIALS", "Issue blockchain credentials"),
                ("REVOKE_CREDENTIALS", "Revoke credentials"),
           ("VERIFY_CREDENTIALS", "Verify credential authenticity"),
         ("MANAGE_CERTIFICATE_TEMPLATES", "Manage certificate templates"),
           
         // Subject & Semester Management
 ("MANAGE_SUBJECTS", "Create, edit, delete subjects"),
    ("MANAGE_SEMESTERS", "Create, edit, delete semesters"),
       ("MANAGE_SUBJECT_CRITERIA", "Manage subject pass/fail criteria"),
        ("MANAGE_GRADE_COMPONENTS", "Manage grading components"),
   
        // Reporting & Analytics
          ("VIEW_ALL_REPORTS", "Access all system reports"),
       ("EXPORT_DATA", "Export data to external formats"),
    ("VIEW_AUDIT_LOGS", "View system audit logs"),
    ("VIEW_ANALYTICS", "Access analytics dashboard"),
                
    // System Administration
      ("MANAGE_SYSTEM_SETTINGS", "Configure system settings"),
     ("DATABASE_BACKUP", "Perform database backups"),
      ("BLOCKCHAIN_ADMIN", "Manage blockchain integration")
};

          foreach (var (code, description) in adminPermissions)
      {
       permissions.Add(new Permission
        {
                Id = Guid.NewGuid(),
      Code = code,
 Description = description,
   RoleId = RoleSeeder.AdminRoleId
       });
        }

            // ==================== TEACHER PERMISSIONS ====================
            var teacherPermissions = new[]
            {
        // Class Management
("VIEW_OWN_CLASSES", "View classes they teach"),
 ("EDIT_OWN_CLASSES", "Edit their class details"),
      ("VIEW_CLASS_STUDENTS", "View student list in their classes"),
         ("MANAGE_CLASS_MEMBERS", "Add/remove students from their classes"),
         
   // Grade Management
     ("VIEW_CLASS_GRADES", "View grades of their students"),
        ("EDIT_CLASS_GRADES", "Edit grades of their students"),
  ("SUBMIT_GRADES", "Submit grades for assignments/exams"),
            ("CALCULATE_FINAL_GRADES", "Calculate final semester grades"),
    ("VIEW_GRADE_STATISTICS", "View grade distribution statistics"),
    
     // Attendance Management
    ("MARK_ATTENDANCE", "Mark student attendance"),
                ("VIEW_CLASS_ATTENDANCE", "View attendance of their students"),
            ("EDIT_CLASS_ATTENDANCE", "Edit attendance records for their classes"),
         ("EXCUSE_ABSENCES", "Approve excused absences"),
   
  // Schedule Management
      ("VIEW_OWN_SCHEDULE", "View their teaching schedule"),
          ("UPDATE_CLASS_NOTES", "Update notes for class slots"),
                ("REQUEST_SUBSTITUTE", "Request substitute teacher"),
        ("VIEW_SLOT_DETAILS", "View detailed slot information"),
       
 // Student Management
                ("VIEW_STUDENT_PROFILES", "View profiles of their students"),
       ("VIEW_STUDENT_PROGRESS", "View student academic progress"),
   ("ADD_STUDENT_NOTES", "Add notes to student records"),
    
         // Reporting
     ("VIEW_CLASS_REPORTS", "Generate reports for their classes"),
        ("EXPORT_CLASS_GRADES", "Export grade sheets"),
        ("VIEW_ATTENDANCE_REPORTS", "View attendance reports for their classes")
   };

       foreach (var (code, description) in teacherPermissions)
   {
             permissions.Add(new Permission
     {
   Id = Guid.NewGuid(),
      Code = code,
      Description = description,
      RoleId = RoleSeeder.TeacherRoleId
                });
    }

     // ==================== STUDENT PERMISSIONS ====================
            var studentPermissions = new[]
            {
    // Profile Management
      ("VIEW_OWN_PROFILE", "View their own profile"),
            ("EDIT_OWN_PROFILE", "Edit their profile information"),
          ("CHANGE_PASSWORD", "Change their password"),
     
                // Grade Access
    ("VIEW_OWN_GRADES", "View their own grades"),
     ("VIEW_GRADE_BREAKDOWN", "View detailed grade breakdown"),
     ("EXPORT_OWN_TRANSCRIPT", "Export their academic transcript"),
         ("VIEW_GPA", "View their GPA"),
 
                // Schedule Access
         ("VIEW_OWN_SCHEDULE", "View their class schedule"),
                ("VIEW_OWN_CLASSES", "View their enrolled classes"),
  ("VIEW_CLASS_DETAILS", "View class information"),
         ("VIEW_TEACHER_INFO", "View teacher contact information"),
                
          // Attendance Access
       ("VIEW_OWN_ATTENDANCE", "View their attendance records"),
           ("VIEW_ATTENDANCE_SUMMARY", "View attendance summary/statistics"),
       
        // Credential Access
             ("VIEW_OWN_CREDENTIALS", "View their credentials/certificates"),
      ("REQUEST_CREDENTIALS", "Request new credentials"),
          ("DOWNLOAD_CREDENTIALS", "Download credential files"),
        ("VERIFY_OWN_CREDENTIALS", "Verify their credentials"),

                // Roadmap Access
      ("VIEW_OWN_ROADMAP", "View their academic roadmap"),
  ("UPDATE_OWN_ROADMAP", "Update their course planning"),
       ("VIEW_PREREQUISITES", "View course prerequisites"),
     
                // Enrollment
       ("VIEW_AVAILABLE_CLASSES", "View available classes for enrollment"),
  ("REQUEST_ENROLLMENT", "Request to enroll in classes"),
         
   // Communication
   ("VIEW_ANNOUNCEMENTS", "View class announcements"),
         ("SUBMIT_FEEDBACK", "Submit course feedback")
   };

            foreach (var (code, description) in studentPermissions)
      {
             permissions.Add(new Permission
            {
      Id = Guid.NewGuid(),
        Code = code,
          Description = description,
        RoleId = RoleSeeder.StudentRoleId
     });
            }

    await _context.Permissions.AddRangeAsync(permissions);
  await SaveAsync("Permissions");

  Console.WriteLine($"   Created {permissions.Count} permissions:");
            Console.WriteLine($"      • Admin permissions: {permissions.Count(p => p.RoleId == RoleSeeder.AdminRoleId)}");
            Console.WriteLine($"      • Teacher permissions: {permissions.Count(p => p.RoleId == RoleSeeder.TeacherRoleId)}");
     Console.WriteLine($"      • Student permissions: {permissions.Count(p => p.RoleId == RoleSeeder.StudentRoleId)}");
        }
  }
}
