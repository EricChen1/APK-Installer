using AdvancedSharpAdbClient;
using APKInstaller.Helpers;
using APKInstaller.Pages;
using Microsoft.UI.Xaml;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APKInstaller
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            UIHelper.GetAppWindowForCurrentWindow(this).SetIcon("favicon.ico");
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            SetWindowSize(hwnd, 652, 414);
            MicaHelper.EnableMica(this);
            UIHelper.MainWindow = this;
            MainPage MainPage = new();
            Content = MainPage;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            if (SettingsHelper.Get<bool>(SettingsHelper.IsCloseADB))
            {
                new AdvancedAdbClient().KillAdb();
            }
        }

        private void SetWindowSize(IntPtr hwnd, int width, int height)
        {
            int dpi = PInvoke.User32.GetDpiForWindow(hwnd);
            float scalingFactor = (float)dpi / 96;
            width = (int)(width * scalingFactor);
            height = (int)(height * scalingFactor);
            UIHelper.GetAppWindowForCurrentWindow(this).Resize(new Windows.Graphics.SizeInt32(width, height));
        }
    }
}