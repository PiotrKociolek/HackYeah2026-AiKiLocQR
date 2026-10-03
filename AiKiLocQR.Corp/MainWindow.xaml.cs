using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using AiKiLocQR.Core.Security;
using AiKiLocQR.Core.Services;
using AiKiLocQR.Crypto;

namespace AiKiLocQR.Corp
{
    public partial class MainWindow : Window
    {
        private IPackageService? _packageService;
        private UsbMasterKeyProvider _masterKeyProvider;

        public MainWindow()
        {
            InitializeComponent();
            _masterKeyProvider = new UsbMasterKeyProvider();
            
            // Spróbuj automatycznie wykryć klucz z pendrive'a przy uruchomieniu
            if (_masterKeyProvider.IsKeyPresent())
            {
                UnlockDecryptionPanel();
            }
        }

        private void BtnLoadKey_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "MasterKey (*.key)|*.key|Wszystkie pliki (*.*)|*.*" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    _masterKeyProvider.LoadFromFile(dlg.FileName);
                    MessageBox.Show("Klucz MasterKey został pomyślnie załadowany!", "Autoryzacja", MessageBoxButton.OK, MessageBoxImage.Information);
                    UnlockDecryptionPanel();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas ładowania klucza: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnGenerateKey_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { Filter = "MasterKey (*.key)|*.key", FileName = "aikiloc_master.key" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    UsbMasterKeyProvider.GenerateKeyFile(dlg.FileName);
                    MessageBox.Show($"Wygenerowano nowy sprzętowy MasterKey i zapisano w: {dlg.FileName}\n\nUżywaj tego pliku do odszyfrowywania paczek.", "Wygenerowano", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Automatycznie załaduj po wygenerowaniu
                    _masterKeyProvider.LoadFromFile(dlg.FileName);
                    UnlockDecryptionPanel();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd generowania klucza: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UnlockDecryptionPanel()
        {
            var cryptoProvider = new AesGcmCryptoProvider(_masterKeyProvider);
            _packageService = new PackageService(cryptoProvider);
            
            PanelAuth.Visibility = Visibility.Collapsed;
            PanelDecrypt.Visibility = Visibility.Visible;
            TxtKeyStatus.Text = $"Zalogowano. Aktywny klucz: {_masterKeyProvider.GetLoadedPath() ?? "Pamięć zewnętrzna (USB)"}";
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            _masterKeyProvider = new UsbMasterKeyProvider();
            _packageService = null;
            PanelDecrypt.Visibility = Visibility.Collapsed;
            PanelAuth.Visibility = Visibility.Visible;
        }

        private void BtnSelectPackage_Click(object sender, RoutedEventArgs e)
        {
            if (_packageService == null) return;

            var dlg = new OpenFileDialog { Filter = "AiKiLoc Package|*.aikilocpkg" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string outDir = Path.Combine(Path.GetDirectoryName(dlg.FileName) ?? "", "Odszyfrowane_" + Path.GetFileNameWithoutExtension(dlg.FileName));
                    _packageService.ExtractPackage(dlg.FileName, outDir);
                    
                    MessageBox.Show($"Paczka pomyślnie zdeszyfrowana do:\n{outDir}", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd odszyfrowywania: {ex.Message}\n\nUpewnij się, że załączony MasterKey jest prawidłowy dla tej paczki!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
