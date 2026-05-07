using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CreativeWrites.Models;
using CreativeWrites.ViewModels;

namespace CreativeWrites.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel VM => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();

            // Auto-save when window closes
            Closing += (_, _) => VM.SaveData();
        }

        // ── Panel toggle ─────────────────────────────────────────────────────────

        private void OnShowWritePanel(object sender, RoutedEventArgs e)
        {
            WritePanelScroll.Visibility  = Visibility.Visible;
            DetailPanelScroll.Visibility = Visibility.Collapsed;
        }

        private void OnShowDetailPanel(object sender, RoutedEventArgs e)
        {
            WritePanelScroll.Visibility  = Visibility.Collapsed;
            DetailPanelScroll.Visibility = Visibility.Visible;
        }

        // ── Story feed click ─────────────────────────────────────────────────────

        private void OnStoryCardClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is Story story)
            {
                VM.SelectedStory = story;
                OnShowDetailPanel(sender, e); // switch to detail panel
            }
        }

        // ── Publish ──────────────────────────────────────────────────────────────

        private void OnPublishStory(object sender, RoutedEventArgs e)
        {
            string title = StoryTitleBox.Text.Trim();
            string body  = StoryBodyBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
            {
                MessageBox.Show("Please fill in both a title and body.", "Missing Fields",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var genre = StoryGenreBox.SelectedItem is Genre g ? g : Genre.Fantasy;

            VM.PublishStoryCommand.Execute(new StoryDraft
            {
                Title = title,
                Body  = body,
                Genre = genre
            });

            // Clear form
            StoryTitleBox.Text = string.Empty;
            StoryBodyBox.Text  = string.Empty;
        }

        // ── Like ─────────────────────────────────────────────────────────────────

        private void OnLikeStory(object sender, RoutedEventArgs e)
        {
            if (VM.SelectedStory != null)
                VM.LikeStoryCommand.Execute(VM.SelectedStory);
        }

        // ── Delete story ─────────────────────────────────────────────────────────

        private void OnDeleteStory(object sender, RoutedEventArgs e)
        {
            if (VM.SelectedStory == null) return;

            var result = MessageBox.Show($"Delete \"{VM.SelectedStory.Title}\"?", "Confirm Delete",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                VM.DeleteStoryCommand.Execute(VM.SelectedStory);
        }

        // ── Comments ─────────────────────────────────────────────────────────────

        private void OnPostComment(object sender, RoutedEventArgs e)
        {
            string body = CommentBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(body) || VM.SelectedStory == null) return;

            VM.AddCommentCommand.Execute(new CommentArgs
            {
                Story = VM.SelectedStory,
                Body  = body
            });

            CommentBox.Text = string.Empty;
        }

        private void OnDeleteComment(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && VM.SelectedStory != null)
            {
                string commentId = btn.Tag?.ToString() ?? string.Empty;
                VM.DeleteCommentCommand.Execute(new CommentDeleteArgs
                {
                    Story     = VM.SelectedStory,
                    CommentId = commentId
                });
            }
        }

        // ── New user profile dialog ──────────────────────────────────────────────

        private void OnNewProfileClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new NewUserDialog { Owner = this };
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.Username))
            {
                VM.CreateUser(dialog.Username, dialog.Bio);
            }
        }
    }
}
