using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace QQS_UI
{
    public struct DialogPath
    {
        public string MidiDirectory;
        public string VideoDirectory;
        public string ColorDirectory;
    }
    /// <summary>
    /// Indicates the Midi folder path and Video folder path.
    /// </summary>
    public class Config
    {
        private DialogPath ConfigPath;
        private readonly string ConfigName;
        /// <summary>
        /// Initialize a new <see cref="Config"/> example.
        /// </summary>
        /// <param name="configName">Configuration file name, need ".json" suffix.</param>
        public Config(string configName = "config.json")
        {
            ConfigName = configName;
            if (File.Exists(configName))
            {
                try
                {
                    string jsonData = File.ReadAllText(configName);
                    ConfigPath = JsonSerializer.Deserialize<DialogPath>(jsonData);
                }
                catch
                {
                    File.Create(ConfigName).Close();
                    ConfigPath = new DialogPath();
                }
            }
            else
            {
                File.Create(ConfigName).Close();
                ConfigPath = new DialogPath();
            }
        }
        public string CachedVideoDirectory
        {
            get => ConfigPath.VideoDirectory;
            set => ConfigPath.VideoDirectory = value;
        }

        public string CachedColorDirectory
        {
            get => ConfigPath.ColorDirectory;
            set => ConfigPath.ColorDirectory = value;
        }

        public string CachedMIDIDirectory
        {
            get => ConfigPath.MidiDirectory;
            set => ConfigPath.MidiDirectory = value;
        }

        public void SaveConfig()
        {
            File.WriteAllText(ConfigName, JsonSerializer.Serialize(ConfigPath));
        }
    }
}
