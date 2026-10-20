using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace KnufuLab
{
    public partial class MainWindow : Window
    {
        private const string ColorMuted = "#8B97B5";
        private const string ColorBusy = "#F59E0B";
        private const string ColorOk = "#22C55E";
        private const string ColorError = "#EF4444";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Log(string msg)
        {
            TxtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
            TxtLog.ScrollToEnd();
        }

        private void SetStatus(string text, string color)
        {
            var brush = (Brush)new BrushConverter().ConvertFromString(color);
            TxtStatus.Text = text;
            TxtStatus.Foreground = brush;
            StatusPill.BorderBrush = brush;
        }

        private void SetBusy(bool busy)
        {
            BtnEncrypt.IsEnabled = !busy;
            BtnDecrypt.IsEnabled = !busy;
            BtnVerify.IsEnabled = !busy;
            if (busy) SetStatus("Working...", ColorBusy);
        }

        private void BrowseInput_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Title = "Select a file" };
            if (dlg.ShowDialog() == true) TxtInput.Text = dlg.FileName;
        }

        private void BrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { Title = "Save as" };
            if (dlg.ShowDialog() == true) TxtOutput.Text = dlg.FileName;
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            TxtLog.Clear();
            SetStatus("Ready", ColorMuted);
        }

        private void Input_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Input_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                TxtInput.Text = files[0];
            e.Handled = true;
        }

        private bool Validate(bool needOutput = true)
        {
            if (!File.Exists(TxtInput.Text))
            {
                MessageBox.Show("Please specify an existing input file.");
                return false;
            }
            if (needOutput && string.IsNullOrWhiteSpace(TxtOutput.Text))
            {
                MessageBox.Show("Please specify an output file.");
                return false;
            }
            if (string.IsNullOrEmpty(TxtKey.Password))
            {
                MessageBox.Show("Please enter a key.");
                return false;
            }
            return true;
        }

        private async void Encrypt_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;
            string input = TxtInput.Text, output = TxtOutput.Text, key = TxtKey.Password;
            SetBusy(true);
            try
            {
                await Task.Run(() => new Knufu(key).EncryptFile(input, output));
                Log($"Encrypted: {input} -> {output}");
                SetStatus("Encrypted", ColorOk);
            }
            catch (Exception ex)
            {
                Log("Error: " + ex.Message);
                SetStatus("Error", ColorError);
            }
            finally { SetBusy(false); }
        }

        private async void Decrypt_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;
            string input = TxtInput.Text, output = TxtOutput.Text, key = TxtKey.Password;
            SetBusy(true);
            try
            {
                await Task.Run(() => new Knufu(key).DecryptFile(input, output));
                Log($"Decrypted: {input} -> {output}");
                SetStatus("Decrypted", ColorOk);
            }
            catch (Exception ex)
            {
                Log("Error: " + ex.Message);
                SetStatus("Error", ColorError);
            }
            finally { SetBusy(false); }
        }

        private async void Verify_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate(false)) return;
            string input = TxtInput.Text, key = TxtKey.Password;
            SetBusy(true);
            string enc = Path.GetTempFileName();
            string dec = Path.GetTempFileName();
            try
            {
                var result = await Task.Run(() =>
                {
                    var k = new Knufu(key);
                    k.EncryptFile(input, enc);
                    k.DecryptFile(enc, dec);
                    return (H1: Hash(input), H2: Hash(dec));
                });

                Log("SHA-256 of original:  " + BitConverter.ToString(result.H1).Replace("-", ""));
                Log("SHA-256 of decrypted: " + BitConverter.ToString(result.H2).Replace("-", ""));

                if (result.H1.SequenceEqual(result.H2))
                {
                    Log("RESULT: files are identical, encryption-decryption is correct.");
                    SetStatus("Verified: files match", ColorOk);
                }
                else
                {
                    Log("RESULT: files DO NOT match!");
                    SetStatus("Mismatch", ColorError);
                }
            }
            catch (Exception ex)
            {
                Log("Error: " + ex.Message);
                SetStatus("Error", ColorError);
            }
            finally
            {
                try { File.Delete(enc); File.Delete(dec); } catch { }
                SetBusy(false);
            }
        }

        private static byte[] Hash(string path)
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(path);
            return sha.ComputeHash(fs);
        }
    }
}