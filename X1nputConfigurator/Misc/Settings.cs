using System;
using System.IO;
using CompactJson;

namespace X1nputConfigurator.Misc
{
    public static class Settings
    {
        static string saveLocation = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\araghon007\\X1nput\\";

        public static SettingsData Data = new SettingsData();

        static Settings()
        {
            if (!Directory.Exists(saveLocation)) Directory.CreateDirectory(saveLocation);
            Load();
        }

        public static void Load()
        {
            if (File.Exists($"{saveLocation}settings.json"))
            {
                try
                {
                    Data = Serializer.Parse<SettingsData>(File.ReadAllText($"{saveLocation}settings.json"));
                }
                catch
                {
                    File.Move($"{saveLocation}settings.json", $"{saveLocation}settings_corrupt.json");
                }
            }
        }

        public static void Save()
        {
            File.WriteAllText($"{saveLocation}settings.json", Serializer.ToString(Data, true));
        }
    }

    public class SettingsData
    {
        public bool OverrideConfig { get; internal set; }

        public SettingsData()
        {
            OverrideConfig = true;
        }
    }
}
