using Core.Entities;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<User> Users { get; }
        IRepository<Branch> Branches { get; }
        IRepository<ReportTemplate> ReportTemplates { get; }
        IRepository<Report> Reports { get; }
        IRepository<SubmissionDeadline> SubmissionDeadlines { get; }
        IRepository<SummaryReport> SummaryReports { get; }
        IRepository<SystemRole> SystemRoles { get; }
        IRepository<Notification> Notifications { get; }
        IRepository<CommentHistory> CommentHistory { get; }
        IRepository<Post> Posts { get; }

        Task<int> SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
