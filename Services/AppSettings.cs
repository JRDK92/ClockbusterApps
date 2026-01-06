using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace ClockbusterApps
{
    public class AppSettings
    {
        public bool TrackExistingApplications { get; set; }

        // Requirement 3: Persist ignored list
        public List<string> IgnoredProcesses { get; set; } = new List<string>();

        public static AppSettings Load()
        {
            var path = GetSettingsPath();
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    if (settings != null)
                    {
                        if (settings.IgnoredProcesses == null)
                            settings.IgnoredProcesses = new List<string>();
                        return settings;
                    }
                }
                catch { }
            }
            return new AppSettings();
        }

        public void Save()
        {
            var path = GetSettingsPath();
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        private static string GetSettingsPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClockbusterApps",
                "settings.json");
        }
    }
}