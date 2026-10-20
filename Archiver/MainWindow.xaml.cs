using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace Archiver
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BrowseSource_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() != true) return;
            SourceBox.Text = dialog.FileName;
            ArchiveBox.Text = dialog.FileName + ".arc";
        }

        private void BrowseArchiveOut_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { Filter = "Archive (*.arc)|*.arc|All files (*.*)|*.*" };
            if (dialog.ShowDialog() == true) ArchiveBox.Text = dialog.FileName;
        }

        private void BrowseArchiveIn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Archive (*.arc)|*.arc|All files (*.*)|*.*" };
            if (dialog.ShowDialog() != true) return;
            ArchiveInBox.Text = dialog.FileName;
            string name = Path.GetFileNameWithoutExtension(dialog.FileName);
            RestoredBox.Text = Path.Combine(Path.GetDirectoryName(dialog.FileName), "restored_" + name);
        }

        private void BrowseRestored_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            if (dialog.ShowDialog() == true) RestoredBox.Text = dialog.FileName;
        }

        private async void Compress_Click(object sender, RoutedEventArgs e)
        {
            string source = SourceBox.Text.Trim();
            string target = ArchiveBox.Text.Trim();

            if (!File.Exists(source))
            {
                MessageBox.Show("Source file not found.", "Archiver", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (target.Length == 0 || SamePath(source, target))
            {
                MessageBox.Show("Choose a different path for the archive.", "Archiver", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetBusy(true);
            try
            {
                var progress = new Progress<int>(v => Bar.Value = v);
                var r = await Task.Run(() => ArithmeticCodec.Compress(source, target, progress));

                var text = new StringBuilder();
                text.AppendLine("COMPRESSION");
                text.AppendLine($"Source file          : {Path.GetFileName(source)}");
                text.AppendLine($"Original size        : {r.InputSize:N0} bytes");
                text.AppendLine($"Archive size         : {r.OutputSize:N0} bytes");
                if (r.InputSize > 0 && r.OutputSize > 0)
                {
                    double ratio = (double)r.InputSize / r.OutputSize;
                    double share = (double)r.OutputSize / r.InputSize * 100;
                    text.AppendLine($"Compression ratio    : {ratio:F3} : 1");
                    text.AppendLine($"Archive / original   : {share:F2} %");
                    text.AppendLine($"Space saved          : {100 - share:F2} %");
                    text.AppendLine($"Source entropy       : {r.Entropy:F3} bits/byte");
                    text.AppendLine($"Average code length  : {r.OutputSize * 8.0 / r.InputSize:F3} bits/byte");
                }
                text.AppendLine($"Time                 : {r.Elapsed.TotalSeconds:F2} s");
                ResultBox.Text = text.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void Decompress_Click(object sender, RoutedEventArgs e)
        {
            string source = ArchiveInBox.Text.Trim();
            string target = RestoredBox.Text.Trim();

            if (!File.Exists(source))
            {
                MessageBox.Show("Archive not found.", "Archiver", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (target.Length == 0 || SamePath(source, target))
            {
                MessageBox.Show("Choose a different path for the restored file.", "Archiver", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetBusy(true);
            try
            {
                var progress = new Progress<int>(v => Bar.Value = v);
                var r = await Task.Run(() => ArithmeticCodec.Decompress(source, target, progress));

                var text = new StringBuilder();
                text.AppendLine("DECOMPRESSION");
                text.AppendLine($"Archive size         : {r.InputSize:N0} bytes");
                text.AppendLine($"Restored size        : {r.OutputSize:N0} bytes");
                text.AppendLine($"Integrity (SHA-256)  : {(r.IntegrityOk ? "PASSED - file restored without losses" : "FAILED - data does not match")}");
                text.AppendLine($"Time                 : {r.Elapsed.TotalSeconds:F2} s");
                ResultBox.Text = text.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy)
        {
            CompressButton.IsEnabled = !busy;
            DecompressButton.IsEnabled = !busy;
            if (busy) Bar.Value = 0;
        }

        private static bool SamePath(string a, string b)
        {
            return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
        }
    }
}