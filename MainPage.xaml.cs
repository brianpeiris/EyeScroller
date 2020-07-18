using System;
using System.Collections.Generic;
using Windows.Devices.Input.Preview;
using Windows.Media.Import;
using Windows.System;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace EyeScroller
{
    enum Side
    {
        Left,
        Right
    }

    public sealed partial class MainPage : Page
    {
        InputInjector injector;
        DateTime lastInteraction = DateTime.Now;
        readonly DispatcherTimer timer = new DispatcherTimer();
        int counter = 0;
        Side? side;

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

        private Grid GetCurrentArea()
        {
            return side == Side.Left ? leftArea : rightArea;
        }

        private void Timer_Tick(object sender, object e)
        {
            counter++;
            var area = GetCurrentArea();
            area.Background = new SolidColorBrush(Color.FromArgb(255, 50, (byte)(50 + counter), 50));
            if (counter >= 10 && (DateTime.Now - lastInteraction).TotalSeconds > 2)
            {
                Logger.Log("Triggered");
                var selectedMode = ((ComboBoxItem)comboBox.SelectedItem).Content.ToString();
                switch (selectedMode)
                {
                    case "Space":
                        var keys = new List<InjectedInputKeyboardInfo> { new InjectedInputKeyboardInfo { VirtualKey = ' ', } };
                        if (side == Side.Left)
                        {
                            keys.Add(new InjectedInputKeyboardInfo { VirtualKey = (ushort)VirtualKey.Shift });
                        }
                        injector.InjectKeyboardInput(keys);

                        injector.InjectKeyboardInput(new List<InjectedInputKeyboardInfo> { new InjectedInputKeyboardInfo {
                            VirtualKey = (ushort)VirtualKey.Shift,
                            KeyOptions = InjectedInputKeyOptions.KeyUp
                        }});
                        break;
                    case "Left/Right":
                        var key = (ushort)(side == Side.Left ? VirtualKey.Left : VirtualKey.Right);
                        injector.InjectKeyboardInput(new List<InjectedInputKeyboardInfo> { new InjectedInputKeyboardInfo { VirtualKey = key } });
                        break;
                    case "Scroll":
                        var mouseInfo = new InjectedInputMouseInfo { MouseOptions = InjectedInputMouseOptions.Wheel };
                        int scrollAmount = (int)Math.Floor((side == Side.Left ? 1 : -1) * 120 * scrollMultipllier.Value);
                        unchecked
                        {
                            mouseInfo.MouseData = (uint)scrollAmount;
                        }
                        injector.InjectMouseInput(new List<InjectedInputMouseInfo> { mouseInfo });
                        break;
                }
                lastInteraction = DateTime.Now;
                timer.Stop();
                side = null;
                area.Background = new SolidColorBrush(Color.FromArgb(255, 50, 50, 50));
            }
        }

        private void StartInputSource()
        {
            var inputSource = GazeInputSourcePreview.GetForCurrentView();
            inputSource.GazeEntered += InputSource_GazeEntered;
            inputSource.GazeExited += InputSource_GazeExited;
            inputSource.GazeMoved += InputSource_GazeMoved;
            Logger.Log("Started Input Source");
        }

        private void InputSource_GazeMoved(GazeInputSourcePreview sender, GazeMovedPreviewEventArgs args)
        {
            if (args.CurrentPoint.EyeGazePosition.Value.Y < 60) return;
            var newSide = args.CurrentPoint.EyeGazePosition.Value.X / page.ActualWidth < 0.5 ? Side.Left : Side.Right;
            if (side != newSide)
            {
                Logger.Log(newSide);
                side = newSide;
                leftArea.Background = new SolidColorBrush(Color.FromArgb(255, 50, 50, 50));
                rightArea.Background = new SolidColorBrush(Color.FromArgb(255, 50, 50, 50));
                counter = 0;
                timer.Stop();
                timer.Interval = TimeSpan.FromMilliseconds(10);
                timer.Start();
            }
        }

        private void InputSource_GazeExited(GazeInputSourcePreview sender, GazeExitedPreviewEventArgs args)
        {
            Logger.Log("GazeExited");
            leftArea.Background = new SolidColorBrush(Color.FromArgb(255, 50, 50, 50));
            rightArea.Background = new SolidColorBrush(Color.FromArgb(255, 50, 50, 50));
            counter = 0;
            side = null;
            timer.Stop();
        }

        private void InputSource_GazeEntered(GazeInputSourcePreview sender, GazeEnteredPreviewEventArgs args)
        {
            Logger.Log("GazeEntered");
            args.Handled = true;
        }
    }
}
