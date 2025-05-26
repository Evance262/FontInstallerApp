using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace FontInstallerApp
{
    /// <summary>
    /// Main window for the FontInstallerApp.
    /// Handles UI events, font extraction, installation, and system integration.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The folder selected by the user containing ZIP files with fonts.
        /// </summary>
        string selectedFolder = "";

        /// <summary>
        /// Initializes the main window and sets up dark mode for the title bar.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }


        /// <summary>
        /// Enables immersive dark mode for the window's title bar on load.
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int attribute = 20; // DWMWA_USE_IMMERSIVE_DARK_MODE
            int useDark = 1;
            DwmSetWindowAttribute(hwnd, attribute, ref useDark, Marshal.SizeOf(typeof(int)));
        }

        /// <summary>
        /// Handles the Browse button click event.
        /// Opens a folder browser dialog for the user to select a folder containing ZIP files.
        /// </summary>
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                selectedFolder = dialog.SelectedPath;
                FolderPathText.Text = selectedFolder;
                Log("Selected: " + selectedFolder);
            }
        }

        /// <summary>
        /// Handles the Install Fonts button click event.
        /// Extracts ZIP files, finds font files, and installs them to the system.
        /// </summary>
        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFolder)) return;

            string tempFolder = Path.Combine(Path.GetTempPath(), "FontInstallTemp");
            Directory.CreateDirectory(tempFolder);

            var zipFiles = Directory.GetFiles(selectedFolder, "*.zip");
            bool anyInstalled = false;

            foreach (var zip in zipFiles)
            {
                try
                {
                    string extractPath = Path.Combine(tempFolder, Path.GetFileNameWithoutExtension(zip));
                    ZipFile.ExtractToDirectory(zip, extractPath, true);
                    Log("Extracted: " + Path.GetFileName(zip));

                    var fonts = Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => f.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".otf", StringComparison.OrdinalIgnoreCase));

                    foreach (var fontPath in fonts)
                    {
                        if (InstallFont(fontPath))
                        {
                            anyInstalled = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("Error: " + ex.Message);
                }
            }

            if (anyInstalled)
            {
                System.Windows.MessageBox.Show("Font installation complete!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("No fonts were installed. Check the log for details.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Installs a single font file to the system Fonts folder and updates the registry.
        /// </summary>
        /// <param name="fontPath">The full path to the font file to install.</param>
        /// <returns>True if the font was installed successfully, otherwise false.</returns>
        private bool InstallFont(string fontPath)
        {
            string fontName = Path.GetFileName(fontPath);
            string fontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            string destPath = Path.Combine(fontsFolder, fontName);

            // Check if font is already installed
            if (File.Exists(destPath))
            {
                Log($"Skipped: {fontName} is already installed.");
                return false;
            }

            try
            {
                File.Copy(fontPath, destPath, true);
                Log($"Copied: {fontName} to Fonts folder.");
            }
            catch (Exception ex)
            {
                Log($"Copy failed for {fontName}: {ex.Message}");
                return false;
            }

            int result = AddFontResourceEx(destPath, 0, IntPtr.Zero);
            if (result > 0)
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts", writable: true);
                    if (key != null)
                    {
                        string registryName = GetFontRegistryName(fontPath);
                        key.SetValue(registryName, fontName);
                        Log($"Registry updated: {registryName} → {fontName}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Registry update failed for {fontName}: {ex.Message}");
                }

                SendMessage(HWND_BROADCAST, WM_FONTCHANGE, IntPtr.Zero, IntPtr.Zero);
                Log($"Installed: {fontName}");
                return true;
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                Log($"Failed to install {fontName}: AddFontResourceEx returned 0 (Win32 Error {error})");
                return false;
            }
        }

        /// <summary>
        /// Generates the registry display name for a font based on its file extension.
        /// </summary>
        /// <param name="fontPath">The full path to the font file.</param>
        /// <returns>The registry display name for the font.</returns>
        private string GetFontRegistryName(string fontPath)
        {
            string ext = Path.GetExtension(fontPath).ToLower();
            string name = Path.GetFileNameWithoutExtension(fontPath);
            return ext == ".ttf" ? name + " (TrueType)" : name + " (OpenType)";
        }

        /// <summary>
        /// Adds a message to the log ListBox in the UI.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void Log(string message)
        {
            LogBox.Items.Add(message);
        }

        /// <summary>
        /// Adds a font resource to the system (Win32 API).
        /// </summary>
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern int AddFontResourceEx(string lpszFilename, uint fl, IntPtr pdv);

        /// <summary>
        /// Sends a message to all top-level windows (used to notify system of font changes).
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_FONTCHANGE = 0x001D;
        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);

        /// <summary>
        /// Sets a window attribute (used for immersive dark mode title bar).
        /// </summary>
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        /// <summary>
        /// Handles selection changes in the log ListBox (currently unused).
        /// </summary>
        private void LogBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }
    }
}
