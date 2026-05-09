using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace CreativeWrites.Models
{
    public class Comment : INotifyPropertyChanged
    {
        private string _body = string.Empty;
        private bool _isEditing;
        private string _editDraft = string.Empty;

        public string CommentId { get; set; } = Guid.NewGuid().ToString();
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;

        public string Body
        {
            get => _body;
            set { if (_body != value) { _body = value; OnPropertyChanged(); } }
        }

        public DateTime PostedAt { get; set; } = DateTime.Now;

        [JsonIgnore]
        public bool IsEditing
        {
            get => _isEditing;
            set { if (_isEditing != value) { _isEditing = value; OnPropertyChanged(); } }
        }

        [JsonIgnore]
        public string EditDraft
        {
            get => _editDraft;
            set { if (_editDraft != value) { _editDraft = value; OnPropertyChanged(); } }
        }

        public Comment() { }

        public Comment(User author, string body)
        {
            AuthorId = author.UserId;
            AuthorName = author.Username;
            Body = body;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
