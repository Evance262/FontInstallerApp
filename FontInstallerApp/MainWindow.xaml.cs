using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Forms = System.Windows.Forms;  // Alias to avoid confusion

namespace FontInstallerApp
{
    public partial class MainWindow : Window
    {
        string selectedFolder = "";

        public MainWindow()
        {
            InitializeComponent();
        }

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
                        string fontName = Path.GetFileName(fontPath);
                        string destPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fontName);

                        try
                        {
                            File.Copy(fontPath, destPath, true);
                        }
                        catch (Exception ex)
                        {
                            Log($"Failed to copy {fontName}: {ex.Message}");
                            continue;
                        }

                        int result = AddFontResourceEx(destPath, 0x10, IntPtr.Zero); // private install
                        if (result > 0)
                        {
                            Log("Installed: " + fontName);
                            anyInstalled = true;
                        }
                        else
                        {
                            int error = Marshal.GetLastWin32Error();
                            Log($"Failed to install {fontName}: AddFontResourceEx returned 0 (Win32 Error {error})");
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
                System.Windows.MessageBox.Show("No fonts were installed. Check the log for errors.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Log(string message)
        {
            LogBox.Items.Add(message);
        }

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern int AddFontResourceEx(string lpszFilename, uint fl, IntPtr pdv);
    }
}
