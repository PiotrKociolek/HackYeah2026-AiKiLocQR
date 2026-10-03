using System;
using System.IO;
using System.Text.Json;
using Microsoft.Maui.Controls;
using ZXing.Net.Maui;
using QRCoder;
using AiKiLocQR.Core.Models;
using AiKiLocQR.Core.Security;
using System.Linq;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.ApplicationModel;

#if ANDROID
using Android.Graphics;
#endif

namespace AiKiLocQR.Mobile
{
    public partial class MainPage : ContentPage
    {
        private QrChallenge? _currentChallenge;
        private readonly QrAuthenticator _authenticator;

        public MainPage()
        {
            InitializeComponent();
            _authenticator = new QrAuthenticator();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status == PermissionStatus.Granted)
            {
                BarcodeReader.Options = new BarcodeReaderOptions
                {
                    Formats = BarcodeFormats.All,
                    AutoRotate = true,
                    Multiple = false
                };
                BarcodeReader.IsDetecting = true;
            }
            else
            {
                await DisplayAlert("Brak uprawnień", "Aplikacja wymaga dostępu do kamery, aby skanować kody QR.", "OK");
            }
        }

        private void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
        {
            var first = e.Results?.FirstOrDefault();
            if (first == null) return;

            string json = first.Value;

            try
            {
                var challenge = JsonSerializer.Deserialize<QrChallenge>(json);
                if (challenge != null && !string.IsNullOrEmpty(challenge.Action))
                {
                    Dispatcher.Dispatch(() =>
                    {
                        BarcodeReader.IsDetecting = false;
                        _currentChallenge = challenge;
                        LblOperationDetails.Text = $"Action: {challenge.Action}\nPackage: {challenge.PackageId}";
                        ApprovalOverlay.IsVisible = true;
                    });
                }
            }
            catch
            {
                // Ignore parsing errors (maybe not a valid QR)
            }
        }

        private void BtnApprove_Clicked(object sender, EventArgs e)
        {
            GenerateResponse(true);
        }

        private void BtnReject_Clicked(object sender, EventArgs e)
        {
            GenerateResponse(false);
        }

        private void GenerateResponse(bool approved)
        {
            if (_currentChallenge == null) return;

            var response = _authenticator.GenerateResponse(_currentChallenge, approved);
            string jsonResponse = JsonSerializer.Serialize(response);

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(jsonResponse, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            byte[] qrBytes = qrCode.GetGraphic(20);
            
            ImgResponseQr.Source = ImageSource.FromStream(() => new MemoryStream(qrBytes));
            
            ApprovalOverlay.IsVisible = false;
            ResponseOverlay.IsVisible = true;
        }

        private void BtnDone_Clicked(object sender, EventArgs e)
        {
            ResponseOverlay.IsVisible = false;
            _currentChallenge = null;
            BarcodeReader.IsDetecting = true;
        }
    }
}
