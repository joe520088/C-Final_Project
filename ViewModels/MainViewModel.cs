using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CreativeWrites.Models;
using CreativeWrites.Services;

namespace CreativeWrites.ViewModels
{
    /// <summary>
    /// Central ViewModel that owns all runtime state:
    ///   - User list and active user
    ///   - Full story collection and filtered feed
    ///   - Save/load via DataService
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        // ── Services ────────────────────────────────────────────────────────────
        private readonly DataService _dataService = new();

        // ── Backing fields ───────────────────────────────────────────────────────
        private User? _activeUser;
        private Story? _selectedStory;
        private Genre? _selectedGenreFilter;
        private string _searchText = string.Empty;

        // ── Collections ──────────────────────────────────────────────────────────

        public ObservableCollection<User> Users { get; } = new();
        public ObservableCollection<Story> AllStories { get; } = new();

        /// <summary>Filtered view shown in the main feed.</summary>
        public ObservableCollection<Story> FeedStories { get; } = new();

        public IEnumerable<Genre?> GenreFilters { get; } =
            new Genre?[] { null }.Concat(Enum.GetValues<Genre>().Cast<Genre?>());

        // ── Properties ───────────────────────────────────────────────────────────

        public User? ActiveUser
        {
            get => _activeUser;
            set { SetField(ref _activeUser, value); OnPropertyChanged(nameof(IsLoggedIn)); }
        }

        public bool IsLoggedIn => ActiveUser != null;

        public Story? SelectedStory
        {
            get => _selectedStory;
            set => SetField(ref _selectedStory, value);
        }

        public Genre? SelectedGenreFilter
        {
            get => _selectedGenreFilter;
            set { SetField(ref _selectedGenreFilter, value); ApplyFilter(); }
        }

        public string SearchText
        {
            get => _searchText;
            set { SetField(ref _searchText, value); ApplyFilter(); }
        }

        // ── Commands ─────────────────────────────────────────────────────────────

        public ICommand SwitchUserCommand { get; }
        public ICommand PublishStoryCommand { get; }
        public ICommand DeleteStoryCommand { get; }
        public ICommand EditStoryCommand { get; }
        public ICommand LikeStoryCommand { get; }
        public ICommand AddCommentCommand { get; }
        public ICommand DeleteCommentCommand { get; }
        public ICommand EditCommentCommand { get; }
        public ICommand SaveDataCommand { get; }
        public ICommand ClearFilterCommand { get; }

        // ── Constructor ──────────────────────────────────────────────────────────

        public MainViewModel()
        {
            SwitchUserCommand   = new RelayCommand<User>(SwitchUser);
            PublishStoryCommand = new RelayCommand<StoryDraft>(PublishStory, d => IsLoggedIn && d != null);
            DeleteStoryCommand  = new RelayCommand<Story>(DeleteStory, s => CanEditStory(s));
            EditStoryCommand    = new RelayCommand<StoryEditArgs>(EditStory, a => CanEditStory(a?.Story));
            LikeStoryCommand    = new RelayCommand<Story>(ToggleLike, s => IsLoggedIn && s != null);
            AddCommentCommand   = new RelayCommand<CommentArgs>(AddComment, a => IsLoggedIn && a != null);
            DeleteCommentCommand = new RelayCommand<CommentDeleteArgs>(DeleteComment);
            EditCommentCommand  = new RelayCommand<CommentEditArgs>(EditComment);
            SaveDataCommand     = new RelayCommand(SaveData);
            ClearFilterCommand  = new RelayCommand(() => { SelectedGenreFilter = null; SearchText = string.Empty; });

            LoadData();
        }

        // ── Data persistence ─────────────────────────────────────────────────────

        private void LoadData()
        {
            var appData = _dataService.Load();

            foreach (var user in appData.Users)
                Users.Add(user);

            foreach (var story in appData.Stories)
                AllStories.Add(story);

            // Restore last active user
            ActiveUser = Users.FirstOrDefault(u => u.UserId == appData.LastActiveUserId)
                         ?? Users.FirstOrDefault();

            ApplyFilter();
        }

        public void SaveData()
        {
            var appData = new AppData
            {
                Users = Users.ToList(),
                Stories = AllStories.ToList(),
                LastActiveUserId = ActiveUser?.UserId
            };
            _dataService.Save(appData);
        }

        // ── User management ──────────────────────────────────────────────────────

        public void CreateUser(string username, string bio = "")
        {
            if (string.IsNullOrWhiteSpace(username)) return;
            if (Users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase))) return;

            var user = new User(username, bio);
            Users.Add(user);
            ActiveUser = user;
            SaveData();
        }

        private void SwitchUser(User? user)
        {
            if (user == null) return;
            ActiveUser = user;
            SaveData(); // persist LastActiveUserId
        }

        // ── Story management ─────────────────────────────────────────────────────

        private void PublishStory(StoryDraft? draft)
        {
            if (draft == null || ActiveUser == null) return;

            var story = new Story(ActiveUser, draft.Title, draft.Body, draft.Genre);
            AllStories.Insert(0, story); // newest first
            ApplyFilter();
            SaveData();
        }

        private void DeleteStory(Story? story)
        {
            if (story == null || !CanEditStory(story)) return;

            AllStories.Remove(story);
            FeedStories.Remove(story);
            if (SelectedStory == story) SelectedStory = null;
            SaveData();
        }

        private void EditStory(StoryEditArgs? args)
        {
            if (args == null || !CanEditStory(args.Story)) return;

            args.Story.Title = args.NewTitle;
            args.Story.Body = args.NewBody;
            args.Story.Genre = args.NewGenre;
            args.Story.LastEditedAt = DateTime.Now;
            SaveData();
        }

        private bool CanEditStory(Story? story) =>
            story != null && ActiveUser != null && story.AuthorId == ActiveUser.UserId;

        // ── Likes ────────────────────────────────────────────────────────────────

        private void ToggleLike(Story? story)
        {
            if (story == null || ActiveUser == null) return;

            if (ActiveUser.LikedStoryIds.Contains(story.StoryId))
            {
                ActiveUser.LikedStoryIds.Remove(story.StoryId);
                story.DecrementLike();
            }
            else
            {
                ActiveUser.LikedStoryIds.Add(story.StoryId);
                story.IncrementLike();
            }
            SaveData();
        }

        public bool HasLiked(Story story) =>
            ActiveUser?.LikedStoryIds.Contains(story.StoryId) ?? false;

        // ── Comments ─────────────────────────────────────────────────────────────

        private void AddComment(CommentArgs? args)
        {
            if (args == null || ActiveUser == null) return;

            var comment = new Comment(ActiveUser, args.Body);
            args.Story.AddComment(comment);
            SaveData();
        }

        private void DeleteComment(CommentDeleteArgs? args)
        {
            if (args == null || ActiveUser == null) return;

            bool deleted = args.Story.DeleteComment(args.CommentId, ActiveUser.UserId);
            if (deleted) SaveData();
        }

        private void EditComment(CommentEditArgs? args)
        {
            if (args == null || ActiveUser == null) return;

            bool edited = args.Story.EditComment(args.CommentId, ActiveUser.UserId, args.NewBody);
            if (edited) SaveData();
        }

        // ── Filtering ────────────────────────────────────────────────────────────

        private void ApplyFilter()
        {
            FeedStories.Clear();

            var filtered = AllStories.AsEnumerable();

            if (SelectedGenreFilter.HasValue)
                filtered = filtered.Where(s => s.Genre == SelectedGenreFilter.Value);

            if (!string.IsNullOrWhiteSpace(SearchText))
                filtered = filtered.Where(s =>
                    s.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    s.AuthorName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var story in filtered)
                FeedStories.Add(story);
        }
    }

    // ── Argument DTOs ────────────────────────────────────────────────────────────

    public class StoryDraft
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public Genre Genre { get; set; } = Genre.Fantasy;
    }

    public class StoryEditArgs
    {
        public Story Story { get; set; } = null!;
        public string NewTitle { get; set; } = string.Empty;
        public string NewBody { get; set; } = string.Empty;
        public Genre NewGenre { get; set; } = Genre.Fantasy;
    }

    public class CommentArgs
    {
        public Story Story { get; set; } = null!;
        public string Body { get; set; } = string.Empty;
    }

    public class CommentDeleteArgs
    {
        public Story Story { get; set; } = null!;
        public string CommentId { get; set; } = string.Empty;
    }

    public class CommentEditArgs
    {
        public Story Story { get; set; } = null!;
        public string CommentId { get; set; } = string.Empty;
        public string NewBody { get; set; } = string.Empty;
    }
}
