using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class Post:BaseEntity
    {
        public string? PostHeader { get; set; }
        public string PostText { get; set; }
        public int PosterId { get; set; }
        public User Poster { get; set; }
        public DateTime PostDate { get; set; }
        
    }
}
