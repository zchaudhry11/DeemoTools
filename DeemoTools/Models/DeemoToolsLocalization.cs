using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeemoTools.Models
{
    public class Languages
    {
        public List<DeemoToolsLocalization> LocalizedLanguages { get; set; }
    }

    public class DeemoToolsLocalization
    {
        public string Language { get; set; }
        public string DeemoToolsWindowTitle { get; set; }
        public string GameResolutionLabel { get; set; }
        public string SetResolutionBtn { get; set; }
        public string BackupSavesBtn { get; set; }
        public string KeyBindLabel { get; set; }
        public string PollDelayLabel { get; set; }
        public string BindCheckbox { get; set; }
        public string VolumeControlsLabel { get; set; }
        public string MusicVolumeLabel { get; set; }
        public string NoteVolumeLabel { get; set; }
        public string SetVolumeBtn { get; set; }
        public string SaveLocationLabel { get; set; }
    }
}