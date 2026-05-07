using System;
using System.IO;
using CreativeWrites.Models;
using Newtonsoft.Json;

namespace CreativeWrites.Services
{
    /// <summary>
    /// Handles saving and loading all application data to/from a local JSON file.
    /// This satisfies the persistent storage requirement from the project feedback.
    /// </summary>
    public class DataService
    {
        private static readonly string DataFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CreativeWrites");

        private static readonly string DataFilePath = Path.Combine(DataFolder, "data.json");

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// Saves the full AppData snapshot to disk.
        /// Called any time users or stories are added/edited/deleted.
        /// </summary>
        public void Save(AppData data)
        {
            try
            {
                Directory.CreateDirectory(DataFolder);
                string json = JsonConvert.SerializeObject(data, JsonSettings);
                File.WriteAllText(DataFilePath, json);
            }
            catch (Exception ex)
            {
                // TODO: surface this error to the user via a dialog
                Console.Error.WriteLine($"[DataService] Save failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads AppData from disk, or returns a fresh empty AppData if no file exists.
        /// </summary>
        public AppData Load()
        {
            try
            {
                if (!File.Exists(DataFilePath))
                    return new AppData();

                string json = File.ReadAllText(DataFilePath);
                return JsonConvert.DeserializeObject<AppData>(json, JsonSettings) ?? new AppData();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DataService] Load failed: {ex.Message}");
                return new AppData();
            }
        }

        /// <summary>Returns the full path where data is stored (useful for debugging).</summary>
        public string GetDataFilePath() => DataFilePath;
    }
}
