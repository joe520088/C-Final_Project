using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CreativeWrites.Models
{
    public class Story : INotifyPropertyChanged
    {
        private int _likeCount;

        public string StoryId { get; set; } = Guid.NewGuid().ToString();
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public Genre Genre { get; set; } = Genre.Fantasy;
        public DateTime PublishedAt { get; set; } = DateTime.Now;
        public DateTime LastEditedAt { get; set; } = DateTime.Now;

        public int LikeCount
        {
            get => _likeCount;
            set { _likeCount = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Comment> Comments { get; set; } = new();

        public Story() { }

        public Story(User author, string title, string body, Genre genre)
        {
            AuthorId = author.UserId;
            AuthorName = author.Username;
            Title = title;
            Body = body;
            Genre = genre;
        }

        // --- Events ---
        public event EventHandler<Comment>? CommentAdded;
        public event EventHandler<Comment>? CommentDeleted;
        public event EventHandler<int>? LikeChanged;

        // --- Actions ---

        public void AddComment(Comment comment)
        {
            Comments.Add(comment);
            CommentAdded?.Invoke(this, comment);
        }

        public bool DeleteComment(string commentId, string requestingUserId)
        {
            var comment = Comments.FirstOrDefault(c => c.CommentId == commentId);
            if (comment == null) return false;

            // Only the comment author can delete their own comment
            if (comment.AuthorId != requestingUserId) return false;

            Comments.Remove(comment);
            CommentDeleted?.Invoke(this, comment);
            return true;
        }

        public bool EditComment(string commentId, string requestingUserId, string newBody)
        {
            var comment = Comments.FirstOrDefault(c => c.CommentId == commentId);
            if (comment == null || comment.AuthorId != requestingUserId) return false;

            comment.Body = newBody;
            return true;
        }

        public void IncrementLike()
        {
            LikeCount++;
            LikeChanged?.Invoke(this, LikeCount);
        }

        public void DecrementLike()
        {
            if (LikeCount > 0) LikeCount--;
            LikeChanged?.Invoke(this, LikeCount);
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
