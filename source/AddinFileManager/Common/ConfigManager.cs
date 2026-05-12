using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace AddinFileManager.Common
{
    public class AppConfig
    {
        public List<string> RevitVersions { get; set; }
    }

    public static class ConfigManager
    {
        private static readonly string ConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "addinFileManager");
        private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "settings.json");

        public static AppConfig LoadConfig()
        {
            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    var config = JsonConvert.DeserializeObject<AppConfig>(json);
                    if (config != null && config.RevitVersions != null && config.RevitVersions.Count > 0)
                    {
                        // 去重以防之前保存了重复的数据
                        var distinctVersions = new List<string>();
                        foreach (var v in config.RevitVersions)
                        {
                            if (!distinctVersions.Contains(v))
                            {
                                distinctVersions.Add(v);
                            }
                        }
                        config.RevitVersions = distinctVersions;
                        return config;
                    }
                }
                catch (Exception)
                {
                    // Ignore errors, return default
                }
            }
            return new AppConfig()
            {
                RevitVersions = new List<string>
                {
                    "Autodesk Revit 2016",
                    "Autodesk Revit 2017",
                    "Autodesk Revit 2018",
                    "Autodesk Revit 2019",
                    "Autodesk Revit 2020",
                    "Autodesk Revit 2021",
                    "Autodesk Revit 2022",
                    "Autodesk Revit 2023",
                    "Autodesk Revit 2024",
                    "Autodesk Revit 2025",
                    "Autodesk Revit 2026",
                    "Autodesk Revit 2027"
                }
            };
        }

        public static void SaveConfig(AppConfig config)
        {
            try
            {
                if (!Directory.Exists(ConfigDirectory))
                {
                    Directory.CreateDirectory(ConfigDirectory);
                }

                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception)
            {
                // Ignore errors
            }
        }
    }
}
