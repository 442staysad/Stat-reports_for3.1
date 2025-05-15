using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;

namespace Core.Interfaces
{
    public interface IPostService
    {
        Task<List<Post>> GetRecentPostsForUserAsync();
        Task AddPostAsync(string header, string text, int posterId);
    }
}
