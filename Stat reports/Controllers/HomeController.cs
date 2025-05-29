using System.Diagnostics;
using System.Threading.Tasks;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stat_reports.Models;
using Stat_reportsnt.Filters;

namespace Stat_reports.Controllers
{
    [AuthorizeBranchAndUser]
    public class HomeController : Controller
    {
        private readonly IPostService _postService;


        public HomeController(IPostService postService)
        {
            _postService = postService;
       
        }

        public async Task<IActionResult> Index()
        {
            var posts = await _postService.GetRecentPostsForUserAsync();
            return View(posts); // Передаем список Post
        }

        [Authorize(Roles = "Admin,OBUnF,PEB,AdminTrest")]
        [HttpPost]
        public async Task<IActionResult> AddPost(string header, string text)
        {
            var user = HttpContext.Session.GetInt32("UserId");
            if (user != null)
            {
                await _postService.AddPostAsync(header, text, (int)user);
            }
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin,OBUnF,PEB,AdminTrest")]
        [HttpPost]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var user = HttpContext.Session.GetInt32("UserId");
            if (user != null)
            {
                await _postService.DeletePostAsync(postId);
            }
            return RedirectToAction("Index");
        }
    }
}