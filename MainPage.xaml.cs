/*
 * PlayLeft Small application to get the battery level of your Xbox Controller as a UWP app.
 * First UWP as such just learing how the platform works.
 * Released under GPL3, Developed by Spoonie_au.
 *
 * Fork changes:
 * Fixed formatting, spelling, redundant code, optimizations, separate voids for default, battery, and no-battery states.
 * Moved battery percentage calculation here, since creating an entire class for it previously was just pointless.
 * Renamed functions for cleaner code.
 * Moved away from explicit variables naming.
 */

using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Gaming.Input;
using Windows.System.Power;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using static PlayLeft.Toasts;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PlayLeft
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        private const int UpdateFrequency = 500;
        private string _batteryStatus = "";


        private Gamepad _controller;
        private string _fullChargeCapacity = "";
        private string _remainingCapacity = "";
        private bool _wirelessConnected;

        public MainPage()
        {
            InitializeComponent();

            // Set the window size.
            ApplicationView.PreferredLaunchViewSize = new Size(640, 480);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            LoadDefaultUi();

            // Attach event handlers.
            Gamepad.GamepadAdded += ControllerConnected;
            Gamepad.GamepadRemoved += ControllerDisconnected;

            ControllerLoop();
        }


        private async void ControllerLoop()
        {
            while (true)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    // If no game pad found, skip to next iteration.
                    if (_controller == null) return;

                    // Cannot handle more than one controller yet, will support 4 controllers once I can work out how to identify controllers.
                    if (Gamepad.Gamepads.Count > 1)
                        // TODO
                        return;

                    // Get the current state.
                    _wirelessConnected = _controller.IsWireless;

                    // Get battery status.
                    var controllerBattery = _controller.TryGetBatteryReport();

                    switch (controllerBattery.Status)
                    {
                        case BatteryStatus.Charging:
                            _batteryStatus = "charging";
                            break;
                        case BatteryStatus.Discharging:
                            _batteryStatus = "discharging";
                            break;
                        case BatteryStatus.Idle:
                            _batteryStatus = "idle";
                            break;
                        case BatteryStatus.NotPresent:
                            _batteryStatus = "not present";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (controllerBattery.Status == BatteryStatus.NotPresent)
                    {
                        _fullChargeCapacity = "0";
                        _remainingCapacity = "0";
                        NoBatteryUi();
                    }
                    else
                    {
                        if (controllerBattery.FullChargeCapacityInMilliwattHours != null &&
                            controllerBattery.RemainingCapacityInMilliwattHours != null)
                        {
                            _fullChargeCapacity = controllerBattery.FullChargeCapacityInMilliwattHours.ToString();
                            _remainingCapacity = controllerBattery.RemainingCapacityInMilliwattHours.ToString();
                        }
                        else
                        {
                            _fullChargeCapacity = "0";
                            _remainingCapacity = "0";
                        }

                        UpdateUiDetails();
                    }
                });
                //set time in ms to check.
                await Task.Delay(TimeSpan.FromMilliseconds(UpdateFrequency));
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private void UpdateUiDetails()
        {
            LblContSelected.Text = "Controller connected.";

            // Fetch resource for proper connection type.
            LblConnection.Text = _wirelessConnected
                ? "Device is connected via wireless."
                : "Device is connected via cable.";

            LblBatteryStatus.Text = "Battery is " + _batteryStatus + ".";
            LblFullChargeCap.Text = "Maximum capacity is " + _fullChargeCapacity + "mWh.";
            LblRemainingCap.Text = "Remaining capacity is " + _remainingCapacity + "mWh.";
            LblPercentage.Text = "Remaining percentage";

            // Calculate remaining percentage and display it.
            TxtPercentage.Text = CalculatePercentage(_remainingCapacity, _fullChargeCapacity);
        }

        private void NoBatteryUi()
        {
            LblContSelected.Text = "Controller without a battery connected.";

            // Fetch resource for proper connection type.
            LblConnection.Text = _wirelessConnected
                ? "Device is connected via wireless connection."
                : "Device is connected via cable connection.";

            LblBatteryStatus.Text = "Battery was not found.";
            LblFullChargeCap.Text = "Maximum capacity is None.";
            LblRemainingCap.Text = "Remaining capacity is None.";
            LblPercentage.Text = "Remaining percentage";

            TxtPercentage.Text = "None";
        }

        private void LoadDefaultUi()
        {
            LblContSelected.Text = "No controller is detected.";

            LblConnection.Text = "";
            LblBatteryStatus.Text = "";
            LblFullChargeCap.Text = "";
            LblRemainingCap.Text = "";
            LblPercentage.Text = "";
            TxtPercentage.Text = "";
        }

        private async void ControllerDisconnected(object sender, Gamepad e)
        {
            _controller = null;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // If controllers count is larger than one, do noting. Will support 4 controllers once I can work out how to identify controllers.
                if (Gamepad.Gamepads.Count > 1)
                    // TODO
                    return;
                if (Gamepad.Gamepads.Count != 0) return;

                LoadDefaultUi();

                GenerateToast(ToastType.ControllerDisconnected);
            });
        }

        private async void ControllerConnected(object sender, Gamepad e)
        {
            _controller = e;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // If controllers count is larger than one, do noting. Will support 4 controllers once I can work out how to identify controllers.
                if (Gamepad.Gamepads.Count > 1) return;
                // TODO
                GenerateToast(ToastType.ControllerConnected);
            });
        }

        private static string CalculatePercentage(string batteryCharge, string fullChargeCapacity)
        {
            return double.Parse(batteryCharge) / double.Parse(fullChargeCapacity) * 100 + "%";
        }
    }
}