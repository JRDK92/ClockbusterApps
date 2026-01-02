using Newtonsoft.Json;
using System;
using System.IO;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace ClockbusterApps.Services
{
    public class AppSettings
    {
        public bool TrackExistingApplications { get; set; } = false;

        private static readonly string _settingsFilePath;

        static AppSettings()
        {
            _settingsFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClockbusterApps",
                "settings.json"
            );
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
                // If loading fails, return default settings
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(_settingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch
            {
                // Silently handle save errors
            }
        }
    }
}