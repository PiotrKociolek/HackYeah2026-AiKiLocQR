using System;
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

namespace AiKiLocQR.Corp
{
    public partial class MainWindow : Window
    {
        private readonly IPackageService _packageService;
        private readonly QrAuthenticator _qrAuthenticator;
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

        private void BtnSelectPackage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "AiKiLoc Package|*.aikilocpkg" };
            if (dlg.ShowDialog() == true)
            {
                _selectedPackageForDecryption = dlg.FileName;
                TxtSelectedPackage.Text = Path.GetFileName(_selectedPackageForDecryption);

                _currentChallenge = _qrAuthenticator.GenerateChallenge(Path.GetFileName(_selectedPackageForDecryption), "DECRYPT", "CORP_DEVICE_1");
                string jsonChallenge = JsonSerializer.Serialize(_currentChallenge);

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
                MessageBox.Show("Proszę najpierw wybrać paczkę.");
                return;
            }

            try
            {
                string jsonResponse = TxtQrResponse.Text;
                var response = JsonSerializer.Deserialize<QrResponse>(jsonResponse);

                if (response == null) throw new Exception("Nieprawidłowy format odpowiedzi");

                bool isApproved = _qrAuthenticator.VerifyResponse(_currentChallenge, response);
                if (!isApproved)
                {
                    MessageBox.Show("Operacja została ODRZUCONA przez urządzenie z Androidem.", "Odrzucono", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string outDir = Path.Combine(Path.GetDirectoryName(_selectedPackageForDecryption) ?? "", "Odszyfrowane_" + Path.GetFileNameWithoutExtension(_selectedPackageForDecryption));
                _packageService.ExtractPackage(_selectedPackageForDecryption, outDir);
                
                MessageBox.Show($"Paczka pomyślnie zdeszyfrowana do: {outDir}", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                _currentChallenge = null;
                ImgQrChallenge.Source = null;
                TxtQrResponse.Text = "";
                _selectedPackageForDecryption = "";
                TxtSelectedPackage.Text = "Brak wybranej paczki";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd odszyfrowywania: {ex.Message}\nUpewnij się, że masz włożony klucz USB!", "Błąd weryfikacji", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
