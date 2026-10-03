using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using AiKiLocQR.Core.Models;
using AiKiLocQR.Core.Security;
using AiKiLocQR.Core.Services;
using AiKiLocQR.Crypto;
using QRCoder;

namespace AiKiLocQR.WPF
{
    public partial class MainWindow : Window
    {
        private readonly IPackageService _packageService;
        private readonly QrAuthenticator _qrAuthenticator;
        private readonly List<string> _selectedFilesForEncryption = new();
        private string _selectedPackageForDecryption = string.Empty;
        private QrChallenge? _currentChallenge;

        public MainWindow()
        {
            InitializeComponent();
            var masterKeyProvider = new UsbMasterKeyProvider();
            var cryptoProvider = new AesGcmCryptoProvider(masterKeyProvider);
            _packageService = new PackageService(cryptoProvider);
            _qrAuthenticator = new QrAuthenticator();
        }

        private void BtnSelectFiles_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Multiselect = true };
            if (dlg.ShowDialog() == true)
            {
                _selectedFilesForEncryption.Clear();
                _selectedFilesForEncryption.AddRange(dlg.FileNames);
                ListSelectedFiles.ItemsSource = null;
                ListSelectedFiles.ItemsSource = _selectedFilesForEncryption;
            }
        }

        private void BtnEncrypt_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFilesForEncryption.Count == 0) return;

            var dlg = new SaveFileDialog { Filter = "AiKiLoc Package|*.aikilocpkg" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    _packageService.CreatePackage(_selectedFilesForEncryption, dlg.FileName, "default_password");
                    MessageBox.Show("Package encrypted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    _selectedFilesForEncryption.Clear();
                    ListSelectedFiles.ItemsSource = null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Encryption Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSelectPackage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "AiKiLoc Package|*.aikilocpkg" };
            if (dlg.ShowDialog() == true)
            {
                _selectedPackageForDecryption = dlg.FileName;
                TxtSelectedPackage.Text = Path.GetFileName(_selectedPackageForDecryption);

                // Generate QR Challenge
                _currentChallenge = _qrAuthenticator.GenerateChallenge(Path.GetFileName(_selectedPackageForDecryption), "DECRYPT", "WPF_DEVICE_1");
                string jsonChallenge = JsonSerializer.Serialize(_currentChallenge);

                // Render QR
                using var qrGenerator = new QRCodeGenerator();
                using var qrData = qrGenerator.CreateQrCode(jsonChallenge, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrData);
                byte[] qrBytes = qrCode.GetGraphic(20);
                
                var bmp = new BitmapImage();
                using (var ms = new MemoryStream(qrBytes))
                {
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                }
                ImgQrChallenge.Source = bmp;
            }
        }

        private void BtnVerifyAndDecrypt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedPackageForDecryption) || _currentChallenge == null)
            {
                MessageBox.Show("Please select a package first.");
                return;
            }

            try
            {
                string jsonResponse = TxtQrResponse.Text;
                var response = JsonSerializer.Deserialize<QrResponse>(jsonResponse);

                if (response == null) throw new Exception("Invalid response format");

                bool isApproved = _qrAuthenticator.VerifyResponse(_currentChallenge, response);
                if (!isApproved)
                {
                    MessageBox.Show("Operation was REJECTED by Android device.", "Rejected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Approved! Decrypt.
                string outDir = Path.Combine(Path.GetDirectoryName(_selectedPackageForDecryption) ?? "", "Extracted");
                _packageService.ExtractPackage(_selectedPackageForDecryption, outDir, "default_password");
                
                MessageBox.Show($"Package decrypted successfully to {outDir}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                _currentChallenge = null;
                ImgQrChallenge.Source = null;
                TxtQrResponse.Text = "";
                _selectedPackageForDecryption = "";
                TxtSelectedPackage.Text = "None selected";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Verification Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
