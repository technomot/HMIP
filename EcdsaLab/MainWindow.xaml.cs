using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace EcdsaLab
{
    public partial class MainWindow : Window
    {
        private const string ColorMuted = "#64748B";
        private const string ColorBusy = "#F59E0B";
        private const string ColorOk = "#22C55E";
        private const string ColorError = "#EF4444";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Log(string message)
        {
            TxtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
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
            BtnGenerate.IsEnabled = !busy;
            BtnSign.IsEnabled = !busy;
            BtnVerify.IsEnabled = !busy;
            if (busy) SetStatus("Working...", ColorBusy);
        }

        private static void Pick(TextBox target)
        {
            var dialog = new OpenFileDialog { CheckFileExists = false };
            if (dialog.ShowDialog() == true) target.Text = dialog.FileName;
        }

        private void BrowseFile_Click(object sender, RoutedEventArgs e) => Pick(TxtFile);
        private void BrowseSignature_Click(object sender, RoutedEventArgs e) => Pick(TxtSignature);
        private void BrowsePrivate_Click(object sender, RoutedEventArgs e) => Pick(TxtPrivate);
        private void BrowsePublic_Click(object sender, RoutedEventArgs e) => Pick(TxtPublic);

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            TxtLog.Clear();
            SetStatus("Ready", ColorMuted);
        }

        private static bool Filled(params TextBox[] boxes)
        {
            foreach (var box in boxes)
            {
                if (string.IsNullOrWhiteSpace(box.Text))
                {
                    MessageBox.Show("Please fill in all required paths.");
                    return false;
                }
            }
            return true;
        }

        private static bool Exists(params TextBox[] boxes)
        {
            foreach (var box in boxes)
            {
                if (!File.Exists(box.Text))
                {
                    MessageBox.Show($"File not found: {box.Text}");
                    return false;
                }
            }
            return true;
        }

        private async Task Run(Func<(string Message, bool Ok)> work)
        {
            SetBusy(true);
            try
            {
                var result = await Task.Run(work);
                Log(result.Message);
                SetStatus(result.Ok ? "Success" : "Failed", result.Ok ? ColorOk : ColorError);
            }
            catch (Exception ex)
            {
                Log("Error: " + ex.Message);
                SetStatus("Error", ColorError);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (!Filled(TxtPrivate, TxtPublic)) return;
            string privatePath = TxtPrivate.Text, publicPath = TxtPublic.Text;

            await Run(() =>
            {
                SignatureService.GenerateKeys(privatePath, publicPath);
                return ($"Key pair created:{Environment.NewLine}  {privatePath}{Environment.NewLine}  {publicPath}", true);
            });
        }

        private async void Sign_Click(object sender, RoutedEventArgs e)
        {
            if (!Filled(TxtSignature) || !Exists(TxtFile, TxtPrivate)) return;
            string file = TxtFile.Text, privatePath = TxtPrivate.Text, signaturePath = TxtSignature.Text;

            await Run(() =>
            {
                string hash = SignatureService.Sign(file, privatePath, signaturePath);
                return ($"RIPEMD-160: {hash}{Environment.NewLine}Signature saved: {signaturePath}", true);
            });
        }

        private async void Verify_Click(object sender, RoutedEventArgs e)
        {
            if (!Exists(TxtFile, TxtSignature, TxtPublic)) return;
            string file = TxtFile.Text, signaturePath = TxtSignature.Text, publicPath = TxtPublic.Text;

            await Run(() =>
            {
                bool valid = SignatureService.Verify(file, signaturePath, publicPath);
                return (valid ? "Signature is valid, the file is unchanged." : "Signature is NOT valid.", valid);
            });
        }
    }
}