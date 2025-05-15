using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;

namespace Core.Services
{
    public class PostService : IPostService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PostService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        
        public async Task<List<Post>> GetRecentPostsForUserAsync()
        {

            var posts = await _unitOfWork.Posts.GetAllAsync();
            return posts
                .Where(p => p.Poster != null)
                .GroupBy(p => p.Poster.Role)
                .Select(g => g.OrderByDescending(p => p.PostDate).First())
                .OrderByDescending(p => p.PostDate)
                .ToList();
        }

        public async Task AddPostAsync(string header, string text, int posterId)
        {
            var post = new Post
            {
                PostHeader = header,
                PostText = text,
                PosterId = posterId,
                PostDate = DateTime.Now
            };

            await _unitOfWork.Posts.AddAsync(post);
        }
    }
}
