using Fap.Api.Interfaces;
using Fap.Domain.DTOs.Slot;
using Fap.Domain.Entities;
using Fap.Domain.Repositories;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fap.Api.Services
{
    public class SlotService : ISlotService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SlotService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        #region Basic CRUD

        public async Task<SlotDto?> GetSlotByIdAsync(Guid id)
        {
            var slot = await _unitOfWork.Slots.GetByIdWithDetailsAsync(id);
            if (slot == null) return null;

            var dto = _mapper.Map<SlotDto>(slot);
            dto.HasAttendance = slot.Attendances?.Any() ?? false;
            dto.TotalAttendances = slot.Attendances?.Count ?? 0;
            dto.PresentCount = slot.Attendances?.Count(a => a.IsPresent) ?? 0;
            dto.AbsentCount = slot.Attendances?.Count(a => !a.IsPresent) ?? 0;

            return dto;
        }

        public async Task<IEnumerable<SlotDto>> GetSlotsByClassIdAsync(Guid classId)
        {
            var slots = await _unitOfWork.Slots.GetByClassIdAsync(classId);
            return slots.Select(slot =>
            {
                var dto = _mapper.Map<SlotDto>(slot);
                dto.HasAttendance = slot.Attendances?.Any() ?? false;
                dto.TotalAttendances = slot.Attendances?.Count ?? 0;
                dto.PresentCount = slot.Attendances?.Count(a => a.IsPresent) ?? 0;
                dto.AbsentCount = slot.Attendances?.Count(a => !a.IsPresent) ?? 0;
                return dto;
            });
        }

        public async Task<IEnumerable<SlotDto>> GetSlotsByTeacherIdAsync(Guid teacherId)
        {
            var slots = await _unitOfWork.Slots.GetByTeacherIdAsync(teacherId);
            return slots.Select(slot =>
            {
                var dto = _mapper.Map<SlotDto>(slot);
                dto.HasAttendance = slot.Attendances?.Any() ?? false;
                dto.TotalAttendances = slot.Attendances?.Count ?? 0;
                dto.PresentCount = slot.Attendances?.Count(a => a.IsPresent) ?? 0;
                dto.AbsentCount = slot.Attendances?.Count(a => !a.IsPresent) ?? 0;
                return dto;
            });
        }

        public async Task<SlotDto> CreateSlotAsync(CreateSlotRequest request)
        {
            // Validate class exists
            var classEntity = await _unitOfWork.Classes.GetByIdAsync(request.ClassId);
            if (classEntity == null)
            {
                throw new InvalidOperationException($"Class with ID {request.ClassId} not found");
            }

            // Validate timeSlot if provided
            if (request.TimeSlotId.HasValue)
            {
                var timeSlots = await _unitOfWork.TimeSlots.FindAsync(ts => ts.Id == request.TimeSlotId.Value);
                if (!timeSlots.Any())
                {
                    throw new InvalidOperationException($"TimeSlot with ID {request.TimeSlotId} not found");
                }
            }

            // Validate substitute teacher if provided
            if (request.SubstituteTeacherId.HasValue)
            {
                var teacher = await _unitOfWork.Teachers.GetByIdAsync(request.SubstituteTeacherId.Value);
                if (teacher == null)
                {
                    throw new InvalidOperationException($"Substitute teacher with ID {request.SubstituteTeacherId} not found");
                }

                // Require substitution reason when substitute teacher is assigned
                if (string.IsNullOrWhiteSpace(request.SubstitutionReason))
                {
                    throw new InvalidOperationException("Substitution reason is required when assigning a substitute teacher");
                }
            }

            // Check for overlapping slots
            if (await _unitOfWork.Slots.HasOverlappingSlotAsync(request.ClassId, request.Date, request.TimeSlotId))
            {
                throw new InvalidOperationException("A slot already exists for this class at the same date and time");
            }

            var teacherForSlot = request.SubstituteTeacherId ?? classEntity.TeacherUserId;
            await EnsureTeacherAvailabilityAsync(teacherForSlot, request.Date, request.TimeSlotId);

            var slot = new Slot
            {
                Id = Guid.NewGuid(),
                ClassId = request.ClassId,
                Date = request.Date,
                TimeSlotId = request.TimeSlotId,
                SubstituteTeacherId = request.SubstituteTeacherId,
                SubstitutionReason = request.SubstitutionReason,
                Status = "Scheduled",
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Slots.AddAsync(slot);
            await _unitOfWork.SaveChangesAsync();

            var createdSlot = await GetSlotByIdAsync(slot.Id);
            return createdSlot ?? throw new InvalidOperationException("Failed to retrieve created slot");
        }

        public async Task<SlotDto?> UpdateSlotAsync(Guid id, UpdateSlotRequest request)
        {
            var slot = await _unitOfWork.Slots.GetByIdWithDetailsAsync(id);
            if (slot == null) return null;

            // Validation: Cannot update a slot that has already taken place
            if (slot.Date < DateTime.UtcNow.Date)
            {
                throw new InvalidOperationException("Cannot update a slot that has already taken place.");
            }

            // Check if slot has attendance - if yes, only allow status and notes update
            if (slot.Attendances?.Any() == true)
            {
                if (slot.Date != request.Date || slot.TimeSlotId != request.TimeSlotId)
                {
                    throw new InvalidOperationException("Cannot change date or time for a slot that already has attendance records");
                }
            }

            // Validate timeSlot if changed
            if (request.TimeSlotId != slot.TimeSlotId && request.TimeSlotId.HasValue)
            {
                var timeSlots = await _unitOfWork.TimeSlots.FindAsync(ts => ts.Id == request.TimeSlotId.Value);
                if (!timeSlots.Any())
                {
                    throw new InvalidOperationException($"TimeSlot with ID {request.TimeSlotId} not found");
                }
            }

            // Validate substitute teacher if changed
            if (request.SubstituteTeacherId.HasValue)
            {
                var teacher = await _unitOfWork.Teachers.GetByIdAsync(request.SubstituteTeacherId.Value);
                if (teacher == null)
                {
                    throw new InvalidOperationException($"Substitute teacher with ID {request.SubstituteTeacherId} not found");
                }

                if (string.IsNullOrWhiteSpace(request.SubstitutionReason))
                {
                    throw new InvalidOperationException("Substitution reason is required when assigning a substitute teacher");
                }
            }

            // Check for overlapping if date or time changed
            if (slot.Date != request.Date || slot.TimeSlotId != request.TimeSlotId)
            {
                if (await _unitOfWork.Slots.HasOverlappingSlotAsync(slot.ClassId, request.Date, request.TimeSlotId, id))
                {
                    throw new InvalidOperationException("A slot already exists for this class at the same date and time");
                }
            }

            var teacherForSlot = request.SubstituteTeacherId ?? slot.Class.TeacherUserId;
            await EnsureTeacherAvailabilityAsync(teacherForSlot, request.Date, request.TimeSlotId, id);

            slot.Date = request.Date;
            slot.TimeSlotId = request.TimeSlotId;
            slot.SubstituteTeacherId = request.SubstituteTeacherId;
            slot.SubstitutionReason = request.SubstitutionReason;
            slot.Status = request.Status;
            slot.Notes = request.Notes;
            slot.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Slots.Update(slot);
            await _unitOfWork.SaveChangesAsync();

            return await GetSlotByIdAsync(id);
        }

        public async Task<bool> DeleteSlotAsync(Guid id)
        {
            var slot = await _unitOfWork.Slots.GetByIdWithDetailsAsync(id);
            if (slot == null) return false;

            // Validation: Cannot delete a past slot
            if (slot.Date < DateTime.UtcNow.Date)
            {
                throw new InvalidOperationException("Cannot delete a past slot.");
            }

            // Cannot delete slot with attendance records
            if (slot.Attendances?.Any() == true)
            {
                throw new InvalidOperationException("Cannot delete a slot that has attendance records. Consider cancelling it instead.");
            }

            _unitOfWork.Slots.Remove(slot);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Status Management

        public async Task<SlotDto?> UpdateSlotStatusAsync(Guid id, UpdateSlotStatusRequest request)
        {
            var slot = await _unitOfWork.Slots.GetByIdWithDetailsAsync(id);
            if (slot == null) return null;

            slot.Status = request.Status;
            slot.Notes = request.Notes ?? slot.Notes;
            slot.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Slots.Update(slot);
            await _unitOfWork.SaveChangesAsync();

            return await GetSlotByIdAsync(id);
        }

        public async Task<SlotDto?> CompleteSlotAsync(Guid id)
        {
            var slot = await _unitOfWork.Slots.GetByIdWithDetailsAsync(id);
            if (slot == null) return null;

            if (slot.Status == "Cancelled")
            {
                throw new InvalidOperationException("Cannot complete a cancelled slot");
            }

            slot.Status = "Completed";
            slot.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Slots.Update(slot);
            await _unitOfWork.SaveChangesAsync();

            return await GetSlotByIdAsync(id);
        }

        public async Task<SlotDto?> CancelSlotAsync(Guid id, string reason)
        {
            var slot = await _unitOfWork.Slots.GetByIdWithDetailsAsync(id);
            if (slot == null) return null;

            if (slot.Attendances?.Any() == true)
            {
                throw new InvalidOperationException("Cannot cancel a slot that already has attendance records");
            }

            slot.Status = "Cancelled";
            slot.Notes = reason;
            slot.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Slots.Update(slot);
            await _unitOfWork.SaveChangesAsync();

            return await GetSlotByIdAsync(id);
        }

        #endregion

        #region Query & Filter

        public async Task<IEnumerable<SlotDto>> GetSlotsByFilterAsync(SlotFilterRequest filter)
        {
            IEnumerable<Slot> slots;

            if (filter.ClassId.HasValue)
            {
                slots = await _unitOfWork.Slots.GetByClassIdAsync(filter.ClassId.Value);
            }
            else if (filter.TeacherId.HasValue)
            {
                slots = await _unitOfWork.Slots.GetByTeacherIdAsync(filter.TeacherId.Value);
            }
            else if (filter.FromDate.HasValue && filter.ToDate.HasValue)
            {
                slots = await _unitOfWork.Slots.GetByDateRangeAsync(filter.FromDate.Value, filter.ToDate.Value);
            }
            else if (!string.IsNullOrEmpty(filter.Status))
            {
                slots = await _unitOfWork.Slots.GetByStatusAsync(filter.Status);
            }
            else
            {
                slots = await _unitOfWork.Slots.GetAllAsync();
            }

            var query = slots.AsQueryable();

            // Apply additional filters
            if (filter.FromDate.HasValue && !filter.ToDate.HasValue)
            {
                query = query.Where(s => s.Date >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue && !filter.FromDate.HasValue)
            {
                query = query.Where(s => s.Date <= filter.ToDate.Value);
            }

            if (!string.IsNullOrEmpty(filter.Status))
            {
                query = query.Where(s => s.Status == filter.Status);
            }

            if (filter.HasAttendance.HasValue)
            {
                if (filter.HasAttendance.Value)
                {
                    query = query.Where(s => s.Attendances != null && s.Attendances.Any());
                }
                else
                {
                    query = query.Where(s => s.Attendances == null || !s.Attendances.Any());
                }
            }

            // Apply paging
            var pagedResults = query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return pagedResults.Select(slot =>
            {
                var dto = _mapper.Map<SlotDto>(slot);
                dto.HasAttendance = slot.Attendances?.Any() ?? false;
                dto.TotalAttendances = slot.Attendances?.Count ?? 0;
                dto.PresentCount = slot.Attendances?.Count(a => a.IsPresent) ?? 0;
                dto.AbsentCount = slot.Attendances?.Count(a => !a.IsPresent) ?? 0;
                return dto;
            });
        }

        public async Task<IEnumerable<SlotDto>> GetUpcomingSlotsAsync(Guid teacherId)
        {
            var slots = await _unitOfWork.Slots.GetUpcomingSlotsAsync(teacherId);
            return slots.Select(slot =>
            {
                var dto = _mapper.Map<SlotDto>(slot);
                dto.HasAttendance = slot.Attendances?.Any() ?? false;
                dto.TotalAttendances = slot.Attendances?.Count ?? 0;
                dto.PresentCount = slot.Attendances?.Count(a => a.IsPresent) ?? 0;
                dto.AbsentCount = slot.Attendances?.Count(a => !a.IsPresent) ?? 0;
                return dto;
            });
        }

        public async Task<IEnumerable<SlotDto>> GetSlotsNeedingAttendanceAsync(Guid teacherId)
        {
            var today = DateTime.UtcNow.Date;
            var slots = await _unitOfWork.Slots.GetByTeacherIdAsync(teacherId);

            var slotsNeedingAttendance = slots
                .Where(s => s.Date <= today
                    && s.Status == "Scheduled"
                    && (s.Attendances == null || !s.Attendances.Any()))
                .OrderBy(s => s.Date)
                .ToList();

            return slotsNeedingAttendance.Select(slot =>
            {
                var dto = _mapper.Map<SlotDto>(slot);
                dto.HasAttendance = false;
                dto.TotalAttendances = 0;
                dto.PresentCount = 0;
                dto.AbsentCount = 0;
                return dto;
            });
        }

        #endregion

        #region Validation

        public async Task<bool> CanModifySlotAsync(Guid slotId, Guid teacherUserId)
        {
            var slot = await _unitOfWork.Slots.GetByIdWithDetailsAsync(slotId);
            if (slot == null) return false;

            // Teacher can modify if they are the class teacher or substitute teacher
            return slot.Class.TeacherUserId == teacherUserId ||
                   (slot.SubstituteTeacherId.HasValue && slot.SubstituteTeacherId.Value == teacherUserId);
        }

        public async Task<bool> CanDeleteSlotAsync(Guid slotId)
        {
            var slot = await _unitOfWork.Slots.GetByIdWithDetailsAsync(slotId);
            if (slot == null) return false;

            // Cannot delete if has attendance
            return slot.Attendances == null || !slot.Attendances.Any();
        }

        #endregion

        #region Helpers

        private async Task EnsureTeacherAvailabilityAsync(Guid teacherId, DateTime date, Guid? timeSlotId, Guid? excludeSlotId = null)
        {
            if (await _unitOfWork.Slots.HasTeacherConflictAsync(teacherId, date, timeSlotId, excludeSlotId))
            {
                throw new InvalidOperationException("Teacher already has another slot scheduled at the same time");
            }
        }

        #endregion
    }
}
