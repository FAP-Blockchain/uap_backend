using Fap.Domain.Entities;  // ? Add this
using Fap.Domain.Repositories;
using Fap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Fap.Infrastructure.Repositories
{
    public class CredentialRepository : ICredentialRepository
    {
        private readonly FapDbContext _context;

        public CredentialRepository(FapDbContext context)
        {
            _context = context;
        }

        public async Task<Credential?> GetByIdAsync(Guid id)
        {
            return await _context.Credentials
                .Include(c => c.Student)
   .ThenInclude(s => s.User)  // ? Include Student.User for FullName
     .Include(c => c.CertificateTemplate)
      .Include(c => c.Subject)
       .Include(c => c.Semester)
    .Include(c => c.StudentRoadmap)
       .FirstOrDefaultAsync(c => c.Id == id);
      }

        public async Task<IEnumerable<Credential>> GetAllAsync()
  {
            return await _context.Credentials
 .Include(c => c.Student)
          .ThenInclude(s => s.User)  // ? Include Student.User for FullName
           .Include(c => c.CertificateTemplate)
       .ToListAsync();
   }

      public async Task<IEnumerable<Credential>> FindAsync(Expression<Func<Credential, bool>> predicate)
  {
     return await _context.Credentials
    .Include(c => c.Student)
          .ThenInclude(s => s.User)  // ? Include Student.User for FullName
          .Include(c => c.CertificateTemplate)
          .Include(c => c.Subject)
           .Include(c => c.Semester)
  .Include(c => c.StudentRoadmap)
          .Where(predicate)
    .ToListAsync();
        }

        public async Task<Credential> AddAsync(Credential entity)
        {
            await _context.Credentials.AddAsync(entity);
            return entity;
        }

        public void Update(Credential entity)
        {
            _context.Credentials.Update(entity);
        }

        public void Delete(Credential entity)
        {
            _context.Credentials.Remove(entity);
        }

        public async Task<int> CountAsync(Expression<Func<Credential, bool>>? predicate = null)
        {
            if (predicate == null)
                return await _context.Credentials.CountAsync();

            return await _context.Credentials.CountAsync(predicate);
        }
    }

    public class CredentialRequestRepository : ICredentialRequestRepository
    {
        private readonly FapDbContext _context;

        public CredentialRequestRepository(FapDbContext context)
        {
            _context = context;
        }

        public async Task<CredentialRequest?> GetByIdAsync(Guid id)
        {
            return await _context.CredentialRequests
    .Include(cr => cr.Student)
          .ThenInclude(s => s.User)  // ? Include Student.User for FullName
        .Include(cr => cr.Subject)
  .Include(cr => cr.Semester)
    .Include(cr => cr.StudentRoadmap)
  .Include(cr => cr.Credential)
  .FirstOrDefaultAsync(cr => cr.Id == id);
        }

        public async Task<IEnumerable<CredentialRequest>> GetAllAsync()
        {
            return await _context.CredentialRequests
       .Include(cr => cr.Student)
      .ThenInclude(s => s.User)  // ? Include Student.User for FullName
      .Include(cr => cr.Subject)
        .Include(cr => cr.Semester)
    .ToListAsync();
        }

        public async Task<IEnumerable<CredentialRequest>> FindAsync(Expression<Func<CredentialRequest, bool>> predicate)
        {
          return await _context.CredentialRequests
      .Include(cr => cr.Student)
          .ThenInclude(s => s.User)  // ? Include Student.User for FullName
      .Include(cr => cr.Subject)
  .Include(cr => cr.Semester)
   .Include(cr => cr.StudentRoadmap)
        .Where(predicate)
    .ToListAsync();
        }

        public async Task<CredentialRequest> AddAsync(CredentialRequest entity)
        {
            await _context.CredentialRequests.AddAsync(entity);
            return entity;
        }

        public void Update(CredentialRequest entity)
        {
            _context.CredentialRequests.Update(entity);
        }

        public void Delete(CredentialRequest entity)
        {
            _context.CredentialRequests.Remove(entity);
        }

        public async Task<int> CountAsync(Expression<Func<CredentialRequest, bool>>? predicate = null)
        {
            if (predicate == null)
                return await _context.CredentialRequests.CountAsync();

            return await _context.CredentialRequests.CountAsync(predicate);
        }
    }

    public class CertificateTemplateRepository : ICertificateTemplateRepository
    {
        private readonly FapDbContext _context;

        public CertificateTemplateRepository(FapDbContext context)
        {
            _context = context;
        }

        public async Task<CertificateTemplate?> GetByIdAsync(Guid id)
        {
            return await _context.CertificateTemplates.FindAsync(id);
        }

        public async Task<IEnumerable<CertificateTemplate>> GetAllAsync()
        {
            return await _context.CertificateTemplates.ToListAsync();
        }

        public async Task<IEnumerable<CertificateTemplate>> FindAsync(Expression<Func<CertificateTemplate, bool>> predicate)
        {
            return await _context.CertificateTemplates.Where(predicate).ToListAsync();
        }

        public async Task<CertificateTemplate> AddAsync(CertificateTemplate entity)
        {
            await _context.CertificateTemplates.AddAsync(entity);
            return entity;
        }

        public void Update(CertificateTemplate entity)
        {
            _context.CertificateTemplates.Update(entity);
        }

        public void Delete(CertificateTemplate entity)
        {
            _context.CertificateTemplates.Remove(entity);
        }
    }

    public class SubjectCriteriaRepository : ISubjectCriteriaRepository
    {
        private readonly FapDbContext _context;

        public SubjectCriteriaRepository(FapDbContext context)
        {
            _context = context;
        }

        public async Task<SubjectCriteria?> GetByIdAsync(Guid id)
        {
            return await _context.SubjectCriteria
                 .Include(sc => sc.Subject)
                 .FirstOrDefaultAsync(sc => sc.Id == id);
        }

        public async Task<IEnumerable<SubjectCriteria>> GetAllAsync()
        {
            return await _context.SubjectCriteria
      .Include(sc => sc.Subject)
           .ToListAsync();
        }

        public async Task<IEnumerable<SubjectCriteria>> FindAsync(Expression<Func<SubjectCriteria, bool>> predicate)
        {
            return await _context.SubjectCriteria
           .Include(sc => sc.Subject)
         .Where(predicate)
          .ToListAsync();
        }

        public async Task<SubjectCriteria> AddAsync(SubjectCriteria entity)
        {
            await _context.SubjectCriteria.AddAsync(entity);
            return entity;
        }

        public void Update(SubjectCriteria entity)
        {
            _context.SubjectCriteria.Update(entity);
        }

        public void Delete(SubjectCriteria entity)
        {
            _context.SubjectCriteria.Remove(entity);
        }
    }
}
