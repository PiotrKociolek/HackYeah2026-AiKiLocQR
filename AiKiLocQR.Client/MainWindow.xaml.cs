using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;
using AiKiLocQR.Core.Services;
using AiKiLocQR.Crypto;

namespace AiKiLocQR.Client
{
    public partial class MainWindow : Window
    {
        private readonly IPackageService _packageService;
        private readonly List<string> _selectedFiles = new();

        public MainWindow()
        {
            InitializeComponent();
            
            // Client nie potrzebuje klucza USB!
            var cryptoProvider = new AesGcmCryptoProvider(null);
            _packageService = new PackageService(cryptoProvider);
        }

        private void BtnSelectFiles_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Multiselect = true };
            if (dlg.ShowDialog() == true)
            {
                _selectedFiles.Clear();
                _selectedFiles.AddRange(dlg.FileNames);
                ListSelectedFiles.ItemsSource = null;
                ListSelectedFiles.ItemsSource = _selectedFiles;
            }
        }

        private void BtnEncrypt_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFiles.Count == 0)
            {
                MessageBox.Show("Najpierw wybierz pliki!");
                return;
            }

            var dlg = new SaveFileDialog { Filter = "AiKiLoc Package|*.aikilocpkg" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    _packageService.CreatePackage(_selectedFiles, dlg.FileName);
                    MessageBox.Show("Paczka została pomyślnie utworzona i zabezpieczona. Możesz ją teraz bezpiecznie dostarczyć firmie.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    _selectedFiles.Clear();
                    ListSelectedFiles.ItemsSource = null; 
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd tworzenia paczki, proszęskontaktuj się z nami: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}