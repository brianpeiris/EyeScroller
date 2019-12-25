using System;
using System.Collections.Generic;
using Windows.Devices.Input.Preview;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace EyeScroller
{
    public sealed partial class MainPage : Page
    {
        InputInjector injector;
        DateTime lastSpace = DateTime.Now;
        DispatcherTimer timer = new DispatcherTimer();
        int counter = 0;

        public MainPage()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            injector = InputInjector.TryCreate();
            WatchGazeDevices();
            StartInputSource();
            timer.Tick += Timer_Tick;
        }

        private void WatchGazeDevices()
        {
            var watcher = GazeInputSourcePreview.CreateWatcher();
            watcher.Added += Watcher_Added;
            watcher.Removed += Watcher_Removed;
            watcher.Updated += Watcher_Updated;
            watcher.EnumerationCompleted += Watcher_EnumerationCompleted;
            watcher.Start();
        }

        private void Watcher_EnumerationCompleted(GazeDeviceWatcherPreview sender, object args)
        {
            Logger.Log("Enumeration Completed");
        }

        private void Watcher_Updated(GazeDeviceWatcherPreview sender, GazeDeviceWatcherUpdatedPreviewEventArgs args)
        {
            Logger.Log("Device Updated", args.Device.Id, args.Device.CanTrackEyes);
            StartInputSource();
        }

        private void Watcher_Removed(GazeDeviceWatcherPreview sender, GazeDeviceWatcherRemovedPreviewEventArgs args)
        {
            Logger.Log("Device Removed", args.Device.Id, args.Device.CanTrackEyes);
        }

        private void Watcher_Added(GazeDeviceWatcherPreview sender, GazeDeviceWatcherAddedPreviewEventArgs args)
        {
            Logger.Log("Device Added", args.Device.Id, args.Device.CanTrackEyes);
        }

        private void Timer_Tick(object sender, object e)
        {
            counter++;
            page.Background = new SolidColorBrush(Color.FromArgb(255, 50, (byte)(50 + counter * 5), 50));
            if (counter >= 10 && (DateTime.Now - lastSpace).TotalSeconds > 2)
            {
                Logger.Log("Pressing Space");
                injector.InjectKeyboardInput(new List<InjectedInputKeyboardInfo> { new InjectedInputKeyboardInfo { VirtualKey = (ushort)' ' } });
                lastSpace = DateTime.Now;
                timer.Stop();
                page.Background = new SolidColorBrush(Color.FromArgb(255, 50, 50, 50));
            }
        }

        private void StartInputSource()
        {
            var inputSource = GazeInputSourcePreview.GetForCurrentView();
            inputSource.GazeEntered += InputSource_GazeEntered;
            inputSource.GazeExited += InputSource_GazeExited;
            Logger.Log("Started Input Source");
        }

        private void InputSource_GazeExited(GazeInputSourcePreview sender, GazeExitedPreviewEventArgs args)
        {
            Logger.Log("GazeExited");
            page.Background = new SolidColorBrush(Color.FromArgb(255, 50, 50, 50));
            timer.Stop();
        }

        private void InputSource_GazeEntered(GazeInputSourcePreview sender, GazeEnteredPreviewEventArgs args)
        {
            Logger.Log("GazeEntered");
            counter = 0;
            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Start();

            args.Handled = true;
        }

    }
}
