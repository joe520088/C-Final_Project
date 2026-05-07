using System;
using System.Collections.Generic;

namespace CreativeWrites.Models
{
    public class User
    {
        public string UserId { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // IDs of stories this user has liked
        public List<string> LikedStoryIds { get; set; } = new();

        public User() { }

        public User(string username, string bio = "")
        {
            Username = username;
            Bio = bio;
        }

        public override string ToString() => Username;
    }
}
