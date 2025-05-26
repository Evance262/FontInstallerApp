using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Xunit;

namespace FontInstallerApp.Tests
{
    public class MainWindowTests
    {
        [Fact]
        public void Extracts_Zip_And_Finds_Fonts()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string zipPath = Path.Combine(tempDir, "fonts.zip");
            string fontFile = Path.Combine(tempDir, "TestFont.ttf");
            File.WriteAllText(fontFile, "dummy"); // Simulate a font file

            // Create zip
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(fontFile, "TestFont.ttf");
            }

            // Act
            string extractPath = Path.Combine(tempDir, "extracted");
            ZipFile.ExtractToDirectory(zipPath, extractPath, true);
            var fonts = Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Assert
            Assert.Single(fonts);
            Assert.EndsWith("TestFont.ttf", fonts[0]);
        }

        [Fact]
        public void GetFontRegistryName_Returns_Correct_Format()
        {
            // Arrange
            var window = new FontInstallerApp.MainWindow();
            string ttfPath = @"C:\fonts\MyFont.ttf";
            string otfPath = @"C:\fonts\MyFont.otf";

            // Act
            var ttfName = window.GetType().GetMethod("GetFontRegistryName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(window, new object[] { ttfPath }) as string;
            var otfName = window.GetType().GetMethod("GetFontRegistryName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(window, new object[] { otfPath }) as string;

            // Assert
            Assert.Equal("MyFont (TrueType)", ttfName);
            Assert.Equal("MyFont (OpenType)", otfName);
        }
    }
}