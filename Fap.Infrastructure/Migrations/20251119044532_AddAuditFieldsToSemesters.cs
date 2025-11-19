using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFieldsToSemesters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Subjects_SubjectId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Slots_Subjects_SubjectId",
                table: "Slots");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Semesters_SemesterId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_SemesterId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Slots_SubjectId",
                table: "Slots");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "Slots");

            // ⚠️ IMPORTANT: Rename BEFORE data migration
            migrationBuilder.RenameColumn(
                name: "SubjectId",
                table: "Classes",
                newName: "SubjectOfferingId");

            migrationBuilder.RenameIndex(
                name: "IX_Classes_SubjectId",
                table: "Classes",
                newName: "IX_Classes_SubjectOfferingId");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Subjects",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Subjects",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Subjects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Subjects",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Subjects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Prerequisites",
                table: "Subjects",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Subjects",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Semesters",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Semesters",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Classes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Classes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Classes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "SubjectOfferings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaxClasses = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SemesterCapacity = table.Column<int>(type: "int", nullable: true),
                    RegistrationStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RegistrationEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectOfferings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectOfferings_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubjectOfferings_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // ✅ DATA MIGRATION: Create SubjectOffering for each unique Subject in existing Classes
            // This ensures Classes.SubjectOfferingId will reference valid SubjectOffering records
            migrationBuilder.Sql(@"
  -- Step 1: Get or create default semester
          DECLARE @DefaultSemesterId uniqueidentifier;
                SELECT TOP 1 @DefaultSemesterId = Id FROM Semesters ORDER BY StartDate DESC;
    
     -- Step 2: Create SubjectOfferings for each UNIQUE Subject (not per class!)
    -- SubjectOfferingId currently contains the OLD SubjectId values
         INSERT INTO SubjectOfferings (Id, SubjectId, SemesterId, MaxClasses, IsActive, CreatedAt, UpdatedAt)
  SELECT 
        NEWID() as Id,        -- ✅ Generate NEW unique ID
      SubjectOfferingId as SubjectId,    -- ✅ This is the actual Subject ID
         @DefaultSemesterId as SemesterId,
       10 as MaxClasses,
      1 as IsActive,
             GETUTCDATE() as CreatedAt,
              GETUTCDATE() as UpdatedAt
                FROM (
     SELECT DISTINCT SubjectOfferingId 
          FROM Classes 
  WHERE SubjectOfferingId IS NOT NULL
      ) AS UniqueSubjects
             WHERE @DefaultSemesterId IS NOT NULL;

  -- Step 3: Create temp mapping table to track old SubjectId -> new SubjectOfferingId
        CREATE TABLE #SubjectOfferingMapping (
OldSubjectId uniqueidentifier,
     NewSubjectOfferingId uniqueidentifier
             );

            -- Step 4: Populate mapping
          INSERT INTO #SubjectOfferingMapping (OldSubjectId, NewSubjectOfferingId)
                SELECT DISTINCT 
           SubjectOfferingId as OldSubjectId,
          so.Id as NewSubjectOfferingId
    FROM Classes c
      INNER JOIN SubjectOfferings so ON c.SubjectOfferingId = so.SubjectId
        WHERE so.SemesterId = (SELECT TOP 1 Id FROM Semesters ORDER BY StartDate DESC);

      -- Step 5: Update Classes.SubjectOfferingId with new IDs
       UPDATE c
       SET SubjectOfferingId = m.NewSubjectOfferingId
    FROM Classes c
     INNER JOIN #SubjectOfferingMapping m ON c.SubjectOfferingId = m.OldSubjectId;

       -- Step 6: Cleanup
  DROP TABLE #SubjectOfferingMapping;
         ");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectOfferings_SemesterId",
                table: "SubjectOfferings",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "UK_SubjectOffering_Subject_Semester",
                table: "SubjectOfferings",
                columns: new[] { "SubjectId", "SemesterId" },
                unique: true);

            // ✅ NOW it's safe to add FK - SubjectOffering records exist!
            migrationBuilder.AddForeignKey(
                name: "FK_Classes_SubjectOfferings_SubjectOfferingId",
                table: "Classes",
                column: "SubjectOfferingId",
                principalTable: "SubjectOfferings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_SubjectOfferings_SubjectOfferingId",
                table: "Classes");

            migrationBuilder.DropTable(
                name: "SubjectOfferings");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Prerequisites",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Semesters");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Semesters");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Classes");

            migrationBuilder.RenameColumn(
                name: "SubjectOfferingId",
                table: "Classes",
                newName: "SubjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Classes_SubjectOfferingId",
                table: "Classes",
                newName: "IX_Classes_SubjectId");

            migrationBuilder.AddColumn<Guid>(
                name: "SemesterId",
                table: "Subjects",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SubjectId",
                table: "Slots",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SemesterId",
                table: "Subjects",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Slots_SubjectId",
                table: "Slots",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Subjects_SubjectId",
                table: "Classes",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Slots_Subjects_SubjectId",
                table: "Slots",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Semesters_SemesterId",
                table: "Subjects",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
