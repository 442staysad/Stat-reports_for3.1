using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

using Infrastructure.Data;
using System.Threading.Tasks;

namespace Infrastructure.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public IRepository<User> Users { get; }
        public IRepository<Branch> Branches { get; }
        public IRepository<ReportTemplate> ReportTemplates { get; }
        public IRepository<Report> Reports { get; }
        public IRepository<SubmissionDeadline> SubmissionDeadlines { get; }
        public IRepository<SummaryReport> SummaryReports { get; }
        public IRepository<SystemRole> SystemRoles { get; }
        public IRepository<Notification> Notifications { get; }
        public IRepository<CommentHistory> CommentHistory { get; }
        public IRepository<Post> Posts { get; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;

            Users = new Repository<User>(_context);
            Branches = new Repository<Branch>(_context);
            ReportTemplates = new Repository<ReportTemplate>(_context);
            Reports = new Repository<Report>(_context);
            SubmissionDeadlines = new Repository<SubmissionDeadline>(_context);
            SummaryReports = new Repository<SummaryReport>(_context);
            SystemRoles = new Repository<SystemRole>(_context);
            Notifications = new Repository<Notification>(_context);
            CommentHistory = new Repository<CommentHistory>(_context);
            Posts = new Repository<Post>(_context);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
