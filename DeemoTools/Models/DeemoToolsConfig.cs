using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput.Native;

namespace DeemoTools.Models
{
    public class DeemoToolsConfig
    {
        public int Version = 1;
        public int LastResolutionDropdownIndex = -1;
        public int LastSelectedLanguageIndex = 0;
        public bool? UseCustomBinds = false;
        public double BindPollTimer = 1;
        public int SKeyIndex = -1;
        public int DKeyIndex = -1;
        public int FKeyIndex = -1;
        public int JKeyIndex = -1;
        public int KKeyIndex = -1;
        public int LKeyIndex = -1;
        public double MusicVolume = 50;
        public double NoteVolume = 50;

        public DeemoToolsConfig(int resolutionIndex, int languageIndex, bool? useBinds, double bindTimer, int sKey, int dKey, int fKey, int jKey, int kKey, int lKey, double musicVol, double noteVol)
        {
            LastResolutionDropdownIndex = resolutionIndex;
            LastSelectedLanguageIndex = languageIndex;
            UseCustomBinds = useBinds;
            BindPollTimer = bindTimer;
            SKeyIndex = sKey;
            DKeyIndex = dKey;
            FKeyIndex = fKey;
            JKeyIndex = jKey;
            KKeyIndex = kKey;
            LKeyIndex = lKey;
            MusicVolume = musicVol;
            NoteVolume = noteVol;
        }
    }
}
