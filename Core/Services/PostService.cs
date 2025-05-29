using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;

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

            var posts = await _unitOfWork.Posts.GetAll(p=>p.Include(r=>r.Poster)).ToListAsync();
            return posts
                .Where(p => p.Poster != null)
                .GroupBy(p => p.Poster.Role)
                .Select(g => g.OrderByDescending(p => p.PostDate).First())
                .OrderByDescending(p => p.PostDate)
                .ToList();
        }

        public async Task<Post> AddPostAsync(string header, string text, int posterId)
        {
            var post = new Post
            {
                PostHeader = header,
                PostText = text,
                PosterId = posterId,
                PostDate = DateTime.Now
            };
            await _unitOfWork.Posts.AddAsync(post);
            await _unitOfWork.SaveChangesAsync();
            return post;
        }

        public async Task DeletePostAsync(int postId)
        {
            var post = await _unitOfWork.Posts.FindAsync(p => p.Id == postId);
            await _unitOfWork.Posts.DeleteAsync(post);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
