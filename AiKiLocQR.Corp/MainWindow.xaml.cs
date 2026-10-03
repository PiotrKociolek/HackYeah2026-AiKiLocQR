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
        private readonly IPackageService _packageService;

        public MainWindow()
        {
            InitializeComponent();
            var masterKeyProvider = new UsbMasterKeyProvider();
            var cryptoProvider = new AesGcmCryptoProvider(masterKeyProvider);
            _packageService = new PackageService(cryptoProvider);
        }

        private void BtnSelectPackage_Click(object sender, RoutedEventArgs e)
        {
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
                    MessageBox.Show($"Błąd odszyfrowywania: {ex.Message}\n\nUpewnij się, że masz włożony poprawny klucz USB (MasterKey)!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
