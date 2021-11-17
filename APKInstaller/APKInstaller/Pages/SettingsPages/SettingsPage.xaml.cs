﻿using AdvancedSharpAdbClient;
using APKInstaller.Helpers;
using APKInstaller.Models;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace APKInstaller.Pages.SettingsPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page, INotifyPropertyChanged
    {
        private IEnumerable<DeviceData> _deviceList;
        internal IEnumerable<DeviceData> DeviceList
        {
            get => _deviceList;
            set
            {
                _deviceList = value;
                RaisePropertyChangedEvent();
                if (!IsOnlyWSA) { ChooseDevice(); }
            }
        }

        private bool _isOnlyWSA = SettingsHelper.Get<bool>(SettingsHelper.IsOnlyWSA);
        internal bool IsOnlyWSA
        {
            get => _isOnlyWSA;
            set
            {
                SettingsHelper.Set(SettingsHelper.IsOnlyWSA, value);
                _isOnlyWSA = SettingsHelper.Get<bool>(SettingsHelper.IsOnlyWSA);
                SelectDeviceBox.SelectionMode = value ? ListViewSelectionMode.None : ListViewSelectionMode.Single;
                if (!value) { ChooseDevice(); }
                RaisePropertyChangedEvent();
            }
        }

        private bool isCloseADB = SettingsHelper.Get<bool>(SettingsHelper.IsCloseADB);
        internal bool IsCloseADB
        {
            get => isCloseADB;
            set
            {
                SettingsHelper.Set(SettingsHelper.IsCloseADB, value);
                isCloseADB = SettingsHelper.Get<bool>(SettingsHelper.IsCloseADB);
            }
        }
        
        private DateTime _updateDate = SettingsHelper.Get<DateTime>(SettingsHelper.UpdateDate);
        internal DateTime UpdateDate
        {
            get => _updateDate;
            set
            {
                SettingsHelper.Set(SettingsHelper.UpdateDate, value);
                _updateDate = SettingsHelper.Get<DateTime>(SettingsHelper.UpdateDate);
                RaisePropertyChangedEvent();
            }
        }

        private bool _checkingUpdate;
        internal bool CheckingUpdate
        {
            get => _checkingUpdate;
            set
            {
                _checkingUpdate = value;
                RaisePropertyChangedEvent();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChangedEvent([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (name != null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
        }

        private const string IssuePath = "https://github.com/Paving-Base/APK-Installer/issues";

        internal string VersionTextBlockText
        {
            get
            {
                string ver = $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}";
                ResourceLoader loader = ResourceLoader.GetForViewIndependentUse();
                string name = loader?.GetString("AppName") ?? "APK Installer";
                return $"{name} v{ver}";
            }
        }

        public SettingsPage() => InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            //#if DEBUG
            GoToTestPage.Visibility = Visibility.Visible;
            //#endif
            SelectDeviceBox.SelectionMode = IsOnlyWSA ? ListViewSelectionMode.None : ListViewSelectionMode.Single;
            if (UpdateDate == DateTime.MinValue) { CheckUpdate(); }
            ADBHelper.Monitor.DeviceChanged += OnDeviceChanged;
            DeviceList = new AdvancedAdbClient().GetDevices();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ADBHelper.Monitor.DeviceChanged -= OnDeviceChanged;
        }

        private void OnDeviceChanged(object sender, DeviceDataEventArgs e)
        {
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                DeviceList = new AdvancedAdbClient().GetDevices();
            });
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement).Tag as string)
            {
                case "Reset":
                    ApplicationData.Current.LocalSettings.Values.Clear();
                    SettingsHelper.SetDefaultSettings();
                    if (Reset.Flyout is Flyout flyout_reset)
                    {
                        flyout_reset.Hide();
                    }
                    _ = Frame.Navigate(typeof(SettingsPage));
                    Frame.GoBack();
                    break;
                case "TestPage":
                    _ = Frame.Navigate(typeof(TestPage));
                    break;
                case "FeedBack":
                    _ = Launcher.LaunchUriAsync(new Uri(IssuePath));
                    break;
                case "LogFolder":
                    _ = await Launcher.LaunchFolderAsync(await ApplicationData.Current.LocalFolder.CreateFolderAsync("MetroLogs", CreationCollisionOption.OpenIfExists));
                    break;
                case "CheckUpdate":
                    CheckUpdate();
                    break;
                default:
                    break;
            }
        }

        private async void CheckUpdate()
        {
            CheckingUpdate = true;
            UpdateInfo info = null;
            try
            {
                info = await UpdateHelper.CheckUpdateAsync("Paving-Base", "APK-Installer");
            }
            catch (Exception ex)
            {
                UpdateState.Message = ex.Message;
                UpdateState.Title = "Check Failed";
                UpdateState.Visibility = Visibility.Visible;
                UpdateState.Severity = InfoBarSeverity.Error;
            }
            if (info != null)
            {
                if (info.IsExistNewVersion)
                {

                }
                else
                {
                    UpdateState.Title = "Up To Date";
                    UpdateState.Visibility = Visibility.Visible;
                    UpdateState.Severity = InfoBarSeverity.Success;
                }
            }
            UpdateDate = DateTime.Now;
            CheckingUpdate = false;
        }

        private void MarkdownTextBlock_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            _ = Launcher.LaunchUriAsync(new Uri(e.Link));
        }

        private void TitleBar_BackRequested(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void SelectDeviceBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object vs = (sender as ListView).SelectedItem;
            if (vs != null && vs is DeviceData device)
            {
                SettingsHelper.Set(SettingsHelper.DefaultDevice, device);
            }
        }

        private void ChooseDevice()
        {
            DeviceData device = SettingsHelper.Get<DeviceData>(SettingsHelper.DefaultDevice);
            if (device == null) { return; }
            foreach (DeviceData data in DeviceList)
            {
                if (data.Name == device.Name && data.Model == device.Model && data.Product == device.Product)
                {
                    SelectDeviceBox.SelectedItem = data;
                    break;
                }
            }
        }
    }
}