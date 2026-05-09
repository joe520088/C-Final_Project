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
        private GenreFilter? _selectedGenreFilter;
        private string _searchText = string.Empty;

        // Story inline-edit state
        private bool _isEditingStory;
        private string _editTitleDraft = string.Empty;
        private string _editBodyDraft = string.Empty;
        private Genre _editGenreDraft = Genre.Fantasy;

        // Write-panel state (new story composer)
        private string _writeTitle = string.Empty;
        private string _writeBody = string.Empty;
        private Genre? _writeGenre;

        // ── Collections ──────────────────────────────────────────────────────────

        public ObservableCollection<User> Users { get; } = new();
        public ObservableCollection<Story> AllStories { get; } = new();

        /// <summary>Filtered view shown in the main feed.</summary>
        public ObservableCollection<Story> FeedStories { get; } = new();

        public IReadOnlyList<GenreFilter> GenreFilters { get; } = BuildGenreFilters();

        private static IReadOnlyList<GenreFilter> BuildGenreFilters()
        {
            var list = new List<GenreFilter> { new GenreFilter("All Genres", null) };
            foreach (Genre g in Enum.GetValues<Genre>())
                list.Add(new GenreFilter(g.ToString(), g));
            return list;
        }

        // ── Properties ───────────────────────────────────────────────────────────

        public User? ActiveUser
        {
            get => _activeUser;
            set
            {
                SetField(ref _activeUser, value);
                OnPropertyChanged(nameof(IsLoggedIn));
                OnPropertyChanged(nameof(HasLikedSelectedStory));
                IsEditingStory = false;
                foreach (var story in AllStories)
                    foreach (var c in story.Comments)
                        if (c.IsEditing) c.IsEditing = false;
            }
        }

        public bool IsLoggedIn => ActiveUser != null;

        public Story? SelectedStory
        {
            get => _selectedStory;
            set
            {
                if (SetField(ref _selectedStory, value))
                {
                    IsEditingStory = false;
                    foreach (var story in AllStories)
                        foreach (var c in story.Comments)
                            if (c.IsEditing) c.IsEditing = false;
                }
                OnPropertyChanged(nameof(HasLikedSelectedStory));
            }
        }

        public bool HasLikedSelectedStory =>
            SelectedStory != null && HasLiked(SelectedStory);

        public bool IsEditingStory
        {
            get => _isEditingStory;
            set => SetField(ref _isEditingStory, value);
        }

        public string EditTitleDraft
        {
            get => _editTitleDraft;
            set => SetField(ref _editTitleDraft, value);
        }

        public string EditBodyDraft
        {
            get => _editBodyDraft;
            set => SetField(ref _editBodyDraft, value);
        }

        public Genre EditGenreDraft
        {
            get => _editGenreDraft;
            set => SetField(ref _editGenreDraft, value);
        }

        public string WriteTitle
        {
            get => _writeTitle;
            set => SetField(ref _writeTitle, value);
        }

        public string WriteBody
        {
            get => _writeBody;
            set => SetField(ref _writeBody, value);
        }

        public Genre? WriteGenre
        {
            get => _writeGenre;
            set => SetField(ref _writeGenre, value);
        }

        public GenreFilter? SelectedGenreFilter
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

        // Inline-edit flow commands (story + comments)
        public ICommand StartEditStoryCommand { get; }
        public ICommand SaveEditStoryCommand { get; }
        public ICommand CancelEditStoryCommand { get; }
        public ICommand StartEditCommentCommand { get; }
        public ICommand SaveEditCommentCommand { get; }
        public ICommand CancelEditCommentCommand { get; }

        // ── Constructor ──────────────────────────────────────────────────────────

        public MainViewModel()
        {
            SwitchUserCommand   = new RelayCommand<User>(SwitchUser);
            PublishStoryCommand = new RelayCommand(PublishStory, () =>
                IsLoggedIn
                && !string.IsNullOrWhiteSpace(WriteTitle)
                && !string.IsNullOrWhiteSpace(WriteBody)
                && WriteGenre.HasValue);
            DeleteStoryCommand  = new RelayCommand<Story>(DeleteStory, s => CanEditStory(s));
            EditStoryCommand    = new RelayCommand<StoryEditArgs>(EditStory, a => CanEditStory(a?.Story));
            LikeStoryCommand    = new RelayCommand<Story>(ToggleLike, s => IsLoggedIn && s != null);
            AddCommentCommand   = new RelayCommand<CommentArgs>(AddComment, a => IsLoggedIn && a != null);
            DeleteCommentCommand = new RelayCommand<CommentDeleteArgs>(DeleteComment);
            EditCommentCommand  = new RelayCommand<CommentEditArgs>(EditComment);
            SaveDataCommand     = new RelayCommand(SaveData);
            ClearFilterCommand  = new RelayCommand(() => { SelectedGenreFilter = GenreFilters[0]; SearchText = string.Empty; });

            SelectedGenreFilter = GenreFilters[0];

            StartEditStoryCommand   = new RelayCommand(StartEditStory, () => CanEditStory(SelectedStory));
            SaveEditStoryCommand    = new RelayCommand(SaveEditStory, () => IsEditingStory && CanEditStory(SelectedStory));
            CancelEditStoryCommand  = new RelayCommand(() => IsEditingStory = false);
            StartEditCommentCommand  = new RelayCommand<Comment>(StartEditComment);
            SaveEditCommentCommand   = new RelayCommand<Comment>(SaveEditComment);
            CancelEditCommentCommand = new RelayCommand<Comment>(CancelEditComment);

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

        private void PublishStory()
        {
            if (ActiveUser == null || WriteGenre is not { } genre) return;
            if (string.IsNullOrWhiteSpace(WriteTitle) || string.IsNullOrWhiteSpace(WriteBody)) return;

            var story = new Story(ActiveUser, WriteTitle.Trim(), WriteBody.Trim(), genre);
            AllStories.Insert(0, story); // newest first
            ApplyFilter();
            SaveData();

            WriteTitle = string.Empty;
            WriteBody = string.Empty;
            WriteGenre = null;
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

        private void StartEditStory()
        {
            if (SelectedStory == null || !CanEditStory(SelectedStory)) return;
            EditTitleDraft = SelectedStory.Title;
            EditBodyDraft  = SelectedStory.Body;
            EditGenreDraft = SelectedStory.Genre;
            IsEditingStory = true;
        }

        private void SaveEditStory()
        {
            if (SelectedStory == null || !CanEditStory(SelectedStory)) return;
            EditStory(new StoryEditArgs
            {
                Story    = SelectedStory,
                NewTitle = EditTitleDraft,
                NewBody  = EditBodyDraft,
                NewGenre = EditGenreDraft
            });
            IsEditingStory = false;
        }

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
            OnPropertyChanged(nameof(HasLikedSelectedStory));
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

        private void StartEditComment(Comment? comment)
        {
            if (comment == null) return;
            comment.EditDraft = comment.Body;
            comment.IsEditing = true;
        }

        private void SaveEditComment(Comment? comment)
        {
            if (comment == null || SelectedStory == null) return;
            EditComment(new CommentEditArgs
            {
                Story     = SelectedStory,
                CommentId = comment.CommentId,
                NewBody   = comment.EditDraft
            });
            comment.IsEditing = false;
        }

        private void CancelEditComment(Comment? comment)
        {
            if (comment == null) return;
            comment.IsEditing = false;
        }

        // ── Filtering ────────────────────────────────────────────────────────────

        private void ApplyFilter()
        {
            FeedStories.Clear();

            var filtered = AllStories.AsEnumerable();

            if (SelectedGenreFilter?.Value is { } genre)
                filtered = filtered.Where(s => s.Genre == genre);

            if (!string.IsNullOrWhiteSpace(SearchText))
                filtered = filtered.Where(s =>
                    s.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    s.AuthorName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var story in filtered)
                FeedStories.Add(story);
        }
    }

    // ── Argument DTOs ────────────────────────────────────────────────────────────

    public class GenreFilter
    {
        public string Name { get; }
        public Genre? Value { get; }
        public GenreFilter(string name, Genre? value) { Name = name; Value = value; }
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
