using System;

namespace CreativeWrites.Models
{
    public class Comment
    {
        public string CommentId { get; set; } = Guid.NewGuid().ToString();
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime PostedAt { get; set; } = DateTime.Now;

        public Comment() { }

        public Comment(User author, string body)
        {
            AuthorId = author.UserId;
            AuthorName = author.Username;
            Body = body;
        }
    }
}
