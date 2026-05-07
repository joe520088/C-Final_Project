using System.Collections.Generic;

namespace CreativeWrites.Models
{
    /// <summary>
    /// Root container that gets serialized/deserialized to data.json.
    /// Holds all users and stories so the whole app state is stored in one file.
    /// </summary>
    public class AppData
    {
        public List<User> Users { get; set; } = new();
        public List<Story> Stories { get; set; } = new();
        public string? LastActiveUserId { get; set; }
    }
}
