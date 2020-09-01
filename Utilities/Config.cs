using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace StalkBot.Utilities
{
    public class Config
    {
        public string Token { get; set; }
        public string Prefix { get; set; }
        public int CamTimer { get; set; }
        public double BlurAmount { get; set; }
        public bool TtsEnabled { get; set; }
        public bool CamEnabled { get; set; }
        public bool SsEnabled { get; set; }
        public bool PlayEnabled { get; set; }
        public bool ProcessesEnabled { get; set; }
        public double Timeout { get; set; }
        public string FolderPath { get; set; }

        public void Save()
        {
            File.WriteAllText("config.json", JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public bool IsEnabled(string command)
        {
            switch (command)
            {
                case "webcam":
                    return CamEnabled;
                case "play":
                    return PlayEnabled;
                case "screenshot":
                    return SsEnabled;
                case "tts":
                    return TtsEnabled;
                case "folder":
                    return string.IsNullOrEmpty(FolderPath);
                case "proc":
                    return ProcessesEnabled;
                default:
                    return false;
            }
        }
        public override string ToString()
        {
            return
                $"```Prefix: {Prefix}\n" +
                $"Cam Timer: {CamTimer.ToString()}\n" +
                $"Blur Amount: {BlurAmount.ToString(CultureInfo.InvariantCulture)}\n" +
                $"TTS: {TtsEnabled.ToString()}\n" +
                $"Webcam: {CamEnabled.ToString()}\n" +
                $"Screenshots: {SsEnabled.ToString()}\n" +
                $"PlaySounds: {PlayEnabled.ToString()}\n" +
                $"Processes: {ProcessesEnabled}\n" +
                $"Timeout: {Timeout.ToString(CultureInfo.InvariantCulture)}\n" +
                $"Folder: {FolderPath}```";
        }
    }
}