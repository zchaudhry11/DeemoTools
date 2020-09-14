using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WindowsInput;
using WindowsInput.Native;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using DeemoTools.Models;
using Newtonsoft.Json;

namespace DeemoTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string DeemoToolsConfigPath = @"\DeemoToolsConfig.json";
        private const string DeemoProfilePath = @"..\LocalLow\Rayark\DEEMO -Reborn-\profiles";

        private bool initializationComplete = false;
        private int KEYBIND_UPDATE_TIME = 1;
        private System.Timers.Timer keybindTimer;
        private InputSimulator keySimulator = new InputSimulator();

        private enum LanguageSelection { English, 日本語 };

        // Key references need to be persisted since timer runs on different thread
        private VirtualKeyCode SKeySelection;
        private VirtualKeyCode DKeySelection;
        private VirtualKeyCode FKeySelection;
        private VirtualKeyCode JKeySelection;
        private VirtualKeyCode KKeySelection;
        private VirtualKeyCode LKeySelection;

        private bool SKeyActive = false;
        private bool DKeyActive = false;
        private bool FKeyActive = false;
        private bool JKeyActive = false;
        private bool KKeyActive = false;
        private bool LKeyActive = false;

        public MainWindow()
        {
            InitializeComponent();
            GetLocalizedLanguages();
            GetSupportedResolutions();
            SetKeyComboBoxes();

            // Create timer to watch for keybind presses
            keybindTimer = new System.Timers.Timer(KEYBIND_UPDATE_TIME);
            keybindTimer.Elapsed += new ElapsedEventHandler(HandleKeybinds);

            LoadToolConfig();
            SetLocalizedText();
        }

        private void LoadToolConfig()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configSavePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(appDataPath, DeemoProfilePath));
            var pathInfo = new DirectoryInfo(configSavePath);

            if (File.Exists(pathInfo.FullName + DeemoToolsConfigPath))
            {
                using (FileStream fileStream = new FileStream(pathInfo.FullName + DeemoToolsConfigPath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    StreamReader reader = new StreamReader(fileStream);
                    var json = reader.ReadToEnd();
                    var config = JsonConvert.DeserializeObject<DeemoToolsConfig>(json);

                    if (config.LastResolutionDropdownIndex >= 0 && ResolutionDropdown.Items.Count > config.LastResolutionDropdownIndex)
                    {
                        ResolutionDropdown.SelectedIndex = config.LastResolutionDropdownIndex;
                    }

                    if (config.LastSelectedLanguageIndex >= 0 && LanguageDropdown.Items.Count > config.LastSelectedLanguageIndex)
                    {
                        LanguageDropdown.SelectedIndex = config.LastSelectedLanguageIndex;
                    }

                    EnableBindsCheckbox.IsChecked = config.UseCustomBinds;
                    KEYBIND_UPDATE_TIME = (int)config.BindPollTimer;
                    BindPollTimer.Value = config.BindPollTimer;

                    if (config.SKeyIndex >= 0 && SKeyComboBox.Items.Count > config.SKeyIndex)
                    {
                        SKeyComboBox.SelectedIndex = config.SKeyIndex;
                    }

                    if (config.DKeyIndex >= 0 && DKeyComboBox.Items.Count > config.DKeyIndex)
                    {
                        DKeyComboBox.SelectedIndex = config.DKeyIndex;
                    }

                    if (config.FKeyIndex >= 0 && FKeyComboBox.Items.Count > config.FKeyIndex)
                    {
                        FKeyComboBox.SelectedIndex = config.FKeyIndex;
                    }

                    if (config.JKeyIndex >= 0 && JKeyComboBox.Items.Count > config.JKeyIndex)
                    {
                        JKeyComboBox.SelectedIndex = config.JKeyIndex;
                    }

                    if (config.KKeyIndex >= 0 && KKeyComboBox.Items.Count > config.KKeyIndex)
                    {
                        KKeyComboBox.SelectedIndex = config.KKeyIndex;
                    }

                    if (config.LKeyIndex >= 0 && LKeyComboBox.Items.Count > config.LKeyIndex)
                    {
                        LKeyComboBox.SelectedIndex = config.LKeyIndex;
                    }

                    MusicVolumeSlider.Value = config.MusicVolume;
                    NoteVolumeSlider.Value = config.NoteVolume;
                }
            }

            initializationComplete = true;
        }

        private void UpdateToolConfig()
        {
            if (initializationComplete)
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var configSavePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(appDataPath, DeemoProfilePath));
                var pathInfo = new DirectoryInfo(configSavePath);

                using (FileStream fileStream = new FileStream(pathInfo.FullName + DeemoToolsConfigPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    using (StreamWriter writer = new StreamWriter(fileStream))
                    {
                        DeemoToolsConfig config = new DeemoToolsConfig(ResolutionDropdown.SelectedIndex, LanguageDropdown.SelectedIndex, EnableBindsCheckbox.IsChecked, BindPollTimer.Value,
                            SKeyComboBox.SelectedIndex, DKeyComboBox.SelectedIndex, FKeyComboBox.SelectedIndex, JKeyComboBox.SelectedIndex, KKeyComboBox.SelectedIndex, LKeyComboBox.SelectedIndex,
                            MusicVolumeSlider.Value, NoteVolumeSlider.Value);

                        var configJson = JsonConvert.SerializeObject(config);

                        fileStream.SetLength(0);
                        writer.Write(configJson);
                    }
                }
            }
        }

        private void ApplyResolutionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ResolutionDropdown.SelectedItem != null)
            {
                string[] resolution = ResolutionDropdown.SelectedItem.ToString().Split('x');

                if (resolution.Length == 2)
                {
                    int targetWidth = Int32.Parse(resolution[0]);
                    int targetHeight = Int32.Parse(resolution[1]);

                    InjectResolution(targetWidth, targetHeight);
                }
            }
        }

        private void EnableBindsCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            keybindTimer.Enabled = true;
            UpdateToolConfig();
        }

        private void EnableBindsCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            keybindTimer.Enabled = false;
            UpdateToolConfig();
        }

        private void BackupSaveBtn_Click(object sender, RoutedEventArgs e)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var saveGamePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(appDataPath, DeemoProfilePath));

            // Backup all save game slots
            var backupPath = BackupGameSaves(saveGamePath).Replace("\\", "/");

            SaveLocationLabel.Visibility = Visibility.Visible;
            SaveLocationDirectory.Content = backupPath;
        }

        private void ApplyVolumeBtn_Click(object sender, RoutedEventArgs e)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var saveGamePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(appDataPath, DeemoProfilePath));

            // Backup all save game slots
            BackupGameSaves(saveGamePath);

            // Get all save game content files
            string[] subdirectoryEntries = Directory.GetDirectories(saveGamePath);
            foreach (string subdirectory in subdirectoryEntries)
            {
                DirectoryInfo target = new DirectoryInfo(subdirectory);

                if (target.Name == "autosave" || target.Name == "slot-0" || target.Name == "slot-1" || target.Name == "slot-2")
                {
                    foreach (string filePath in Directory.GetFiles(target.FullName, "*.*", SearchOption.AllDirectories))
                    {
                        DirectoryInfo fileName = new DirectoryInfo(filePath);

                        // Edit sound settings
                        if (fileName.Name == "content")
                        {
                            using (FileStream fileStream = new FileStream(fileName.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                            {
                                StreamReader reader = new StreamReader(fileStream);
                                using (StreamWriter writer = new StreamWriter(fileStream))
                                {
                                    string json = reader.ReadToEnd();
                                    JObject rootJson = JObject.Parse(json);

                                    var Songs = rootJson["UserRhythmModelA"]["_Songs"]["_Storage"].ToList();

                                    foreach (var song in Songs)
                                    {
                                        song["Value"]["MusicVolume"] = Math.Round(Math.Min(Math.Max(0.01f, (MusicVolumeSlider.Value / 10)), 9.5f), 2);
                                        song["Value"]["NoteVolume"] = Math.Round(Math.Min(Math.Max(0.01f, (NoteVolumeSlider.Value / 10)), 9.5f), 2);
                                    }

                                    fileStream.SetLength(0);

                                    writer.Write(rootJson.ToString());
                                }
                            }
                        }
                    }
                }
            }

            UpdateToolConfig();
        }

        private void LanguageDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetLocalizedText();
            UpdateToolConfig();
        }

        private void SetLocalizedText()
        {
            int languageIndex = LanguageDropdown.SelectedIndex;

            using (StreamReader reader = new StreamReader("Assets/localization/localization.json"))
            {
                var json = reader.ReadToEnd();
                var localizationJson = JsonConvert.DeserializeObject<Languages>(json);

                if (localizationJson.LocalizedLanguages.Count > languageIndex)
                {
                    var language = localizationJson.LocalizedLanguages[languageIndex];

                    DeemoToolsWindow.Title = language.DeemoToolsWindowTitle;
                    ResolutionLabel.Content = language.GameResolutionLabel;
                    ApplyResolutionBtn.Content = language.SetResolutionBtn;
                    BackupSaveBtn.Content = language.BackupSavesBtn;
                    KeyBindLabel.Content = language.KeyBindLabel;
                    PollDelayLabel.Content = language.PollDelayLabel;
                    EnableBindsCheckbox.Content = language.BindCheckbox;
                    GlobalVolumeLabel.Content = language.VolumeControlsLabel;
                    MusicVolumeLabel.Content = language.MusicVolumeLabel;
                    NoteVolumeLabel.Content = language.NoteVolumeLabel;
                    ApplyVolumeBtn.Content = language.SetVolumeBtn;
                    SaveLocationLabel.Content = language.SaveLocationLabel;
                }
            }
        }

        private void GetLocalizedLanguages()
        {
            LanguageDropdown.ItemsSource = Enum.GetValues(typeof(LanguageSelection)).Cast<LanguageSelection>();
        }

        private void GetSupportedResolutions()
        {
            var scope = new ManagementScope();
            var query = new ObjectQuery("SELECT * FROM CIM_VideoControllerResolution");
            HashSet<string> supportedResolutions = new HashSet<string>();

            using (var searcher = new ManagementObjectSearcher(scope, query))
            {

                var results = searcher.Get();

                foreach (var result in results)
                {
                    if (!supportedResolutions.Contains($"{result["HorizontalResolution"]}x{result["VerticalResolution"]}"))
                    {
                        supportedResolutions.Add($"{result["HorizontalResolution"]}x{result["VerticalResolution"]}");
                    }
                }
            }

            ResolutionDropdown.ItemsSource = supportedResolutions;
        }

        private void SetKeyComboBoxes()
        {
            var keys = Enum.GetValues(typeof(VirtualKeyCode)).Cast<VirtualKeyCode>();

            SKeyComboBox.ItemsSource = keys;
            DKeyComboBox.ItemsSource = keys;
            FKeyComboBox.ItemsSource = keys;
            JKeyComboBox.ItemsSource = keys;
            KKeyComboBox.ItemsSource = keys;
            LKeyComboBox.ItemsSource = keys;
        }

        private string BackupGameSaves(string saveGamePath)
        {
            var backupSavesPath = $"{saveGamePath}\\DeemoTools_SaveBackup_{DateTime.Now.ToString("yyyy-MM-ddThh_mm_ss")}";
            DirectoryInfo backupBaseDirectory = new DirectoryInfo(backupSavesPath);
            Directory.CreateDirectory(backupBaseDirectory.FullName);

            string[] subdirectoryEntries = Directory.GetDirectories(saveGamePath);
            foreach (string subdirectory in subdirectoryEntries)
            {
                DirectoryInfo target = new DirectoryInfo(subdirectory);

                if (target.Name == "autosave" || target.Name == "slot-0" || target.Name == "slot-1" || target.Name == "slot-2")
                {
                    var backupTarget = Directory.CreateDirectory($"{backupBaseDirectory.FullName}\\{target.Name}");

                    foreach (string filePath in Directory.GetFiles(target.FullName, "*.*", SearchOption.AllDirectories))
                    {
                        File.Copy(filePath, filePath.Replace(target.FullName, backupTarget.FullName), true);
                    }
                }
            }

            return backupBaseDirectory.FullName;
        }

        private void InjectResolution(int targetWidth, int targetHeight)
        {
            Process[] processes = Process.GetProcessesByName("DEEMO -Reborn-");

            if (processes.Length > 0)
            {
                IntPtr BaseAddress = GetModuleBaseAddress(processes[0], "UnityPlayer.dll");

                if (BaseAddress != IntPtr.Zero)
                {
                    var ResWidthOffsets = new int[] { 0xD8, 0x28, 0x0, 0x194 };
                    var ResHeightOffsets = new int[] { 0xE0, 0x18, 0x10, 0xC8 };

                    VAMemory vam = new VAMemory("DEEMO -Reborn-");

                    var ResolutionWidthBaseAddr = vam.ReadInt64((IntPtr)BaseAddress + 0x01604030);
                    var ResolutionHeightBaseAddr = vam.ReadInt64((IntPtr)BaseAddress + 0x01604030);

                    var widthBuffer = vam.ReadInt64((IntPtr)(ResolutionWidthBaseAddr + ResWidthOffsets[0]));
                    var heightBuffer = vam.ReadInt64((IntPtr)(ResolutionHeightBaseAddr + ResHeightOffsets[0]));

                    for (int offset = 1; offset < ResWidthOffsets.Length - 1; offset++)
                    {
                        widthBuffer = vam.ReadInt64((IntPtr)(widthBuffer + ResWidthOffsets[offset]));
                        heightBuffer = vam.ReadInt64((IntPtr)(heightBuffer + ResHeightOffsets[offset]));
                    }

                    IntPtr ResolutionWidth = (IntPtr)(widthBuffer + ResWidthOffsets[ResWidthOffsets.Length - 1]);
                    IntPtr ResolutionHeight = (IntPtr)(heightBuffer + ResHeightOffsets[ResHeightOffsets.Length - 1]);

                    vam.WriteInt32(ResolutionWidth, targetWidth);
                    vam.WriteInt32(ResolutionHeight, targetHeight);
                }
            }
        }

        private IntPtr GetModuleBaseAddress(Process proc, string moduleName)
        {
            IntPtr addr = IntPtr.Zero;

            foreach (ProcessModule m in proc.Modules)
            {
                if (m.ModuleName == moduleName)
                {
                    addr = m.BaseAddress;
                    break;
                }
            }
            return addr;
        }

        private void HandleKeybinds(object source, ElapsedEventArgs e)
        {
            // Update keys' active state
            if (keySimulator.InputDeviceState.IsHardwareKeyUp(SKeySelection))
            {
                SKeyActive = false;
            }

            if (keySimulator.InputDeviceState.IsHardwareKeyUp(DKeySelection))
            {
                DKeyActive = false;
            }

            if (keySimulator.InputDeviceState.IsHardwareKeyUp(FKeySelection))
            {
                FKeyActive = false;
            }

            if (keySimulator.InputDeviceState.IsHardwareKeyUp(JKeySelection))
            {
                JKeyActive = false;
            }

            if (keySimulator.InputDeviceState.IsHardwareKeyUp(KKeySelection))
            {
                KKeyActive = false;
            }

            if (keySimulator.InputDeviceState.IsHardwareKeyUp(LKeySelection))
            {
                LKeyActive = false;
            }

            // Check if keys were released before simulating again
            if (!SKeyActive && keySimulator.InputDeviceState.IsHardwareKeyDown(SKeySelection))
            {
                keySimulator.Keyboard.KeyPress(VirtualKeyCode.VK_S);
                SKeyActive = true;
            }

            if (!DKeyActive && keySimulator.InputDeviceState.IsKeyDown(DKeySelection))
            {
                keySimulator.Keyboard.KeyPress(VirtualKeyCode.VK_D);
                DKeyActive = true;
            }

            if (!FKeyActive && keySimulator.InputDeviceState.IsKeyDown(FKeySelection))
            {
                keySimulator.Keyboard.KeyPress(VirtualKeyCode.VK_F);
                FKeyActive = true;
            }

            if (!JKeyActive && keySimulator.InputDeviceState.IsKeyDown(JKeySelection))
            {
                keySimulator.Keyboard.KeyPress(VirtualKeyCode.VK_J);
                JKeyActive = true;
            }

            if (!KKeyActive && keySimulator.InputDeviceState.IsKeyDown(KKeySelection))
            {
                keySimulator.Keyboard.KeyPress(VirtualKeyCode.VK_K);
                KKeyActive = true;
            }

            if (!LKeyActive && keySimulator.InputDeviceState.IsKeyDown(LKeySelection))
            {
                keySimulator.Keyboard.KeyPress(VirtualKeyCode.VK_L);
                LKeyActive = true;
            }
        }

        private void SKeyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SKeySelection = (VirtualKeyCode)e.AddedItems[0];
            UpdateToolConfig();
        }

        private void DKeyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DKeySelection = (VirtualKeyCode)e.AddedItems[0];
            UpdateToolConfig();
        }

        private void FKeyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FKeySelection = (VirtualKeyCode)e.AddedItems[0];
            UpdateToolConfig();
        }

        private void JKeyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            JKeySelection = (VirtualKeyCode)e.AddedItems[0];
            UpdateToolConfig();
        }

        private void KKeyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            KKeySelection = (VirtualKeyCode)e.AddedItems[0];
            UpdateToolConfig();
        }

        private void LKeyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LKeySelection = (VirtualKeyCode)e.AddedItems[0];
            UpdateToolConfig();
        }

        private void BindPollTimer_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            KEYBIND_UPDATE_TIME = (int)BindPollTimer.Value;

            if (keybindTimer != null)
            {
                keybindTimer.Interval = KEYBIND_UPDATE_TIME;
            }

            UpdateToolConfig();
        }

        private void ResolutionDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateToolConfig();
        }
    }
}
