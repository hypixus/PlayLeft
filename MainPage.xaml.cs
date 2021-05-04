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
using Windows.Storage.Provider;
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

            UpdateUi(UiScenarios.Default);

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
                        UpdateUi(UiScenarios.WithoutBattery);
                    }
                    else
                    {
                        if (controllerBattery.FullChargeCapacityInMilliwattHours != null &&
                            controllerBattery.RemainingCapacityInMilliwattHours != null)
                        {
                            _fullChargeCapacity = controllerBattery.FullChargeCapacityInMilliwattHours.ToString();
                            _remainingCapacity = controllerBattery.RemainingCapacityInMilliwattHours.ToString();
                            UpdateUi(UiScenarios.WithBattery);
                        }
                        else
                        {
                            _fullChargeCapacity = "0";
                            _remainingCapacity = "0";
                            UpdateUi(UiScenarios.WithoutBattery);
                        }
                    }
                });
                //set time in ms to check.
                await Task.Delay(TimeSpan.FromMilliseconds(UpdateFrequency));
            }
            // ReSharper disable once FunctionNeverReturns
        }
        /// <summary>
        /// Update application UI according to given scenario.
        /// </summary>
        /// <param name="scenario">Selected scenario to be displayed.</param>
        private void UpdateUi(UiScenarios scenario)
        {
            switch (scenario)
            {
                case UiScenarios.Default:
                    LblContSelected.Text = "No controller is detected.";
                    LblConnection.Text = "";
                    LblBatteryStatus.Text = "";
                    LblFullChargeCap.Text = "";
                    LblRemainingCap.Text = "";
                    LblPercentage.Text = "";
                    TxtPercentage.Text = "";
                    break;
                case UiScenarios.WithoutBattery:
                    LblContSelected.Text = "Controller without a battery connected.";
                    LblConnection.Text = _wirelessConnected
                        ? "Device is connected via wireless connection."
                        : "Device is connected via cable connection.";

                    LblBatteryStatus.Text = "Battery was not found.";
                    LblFullChargeCap.Text = "Maximum capacity is None.";
                    LblRemainingCap.Text = "Remaining capacity is None.";
                    LblPercentage.Text = "Remaining percentage";
                    TxtPercentage.Text = "None";
                    break;
                case UiScenarios.WithBattery:
                    LblContSelected.Text = "Controller connected.";
                    LblConnection.Text = _wirelessConnected
                        ? "Device is connected via wireless."
                        : "Device is connected via cable.";
                    LblBatteryStatus.Text = "Battery is " + _batteryStatus + ".";
                    LblFullChargeCap.Text = "Maximum capacity is " + _fullChargeCapacity + "mWh.";
                    LblRemainingCap.Text = "Remaining capacity is " + _remainingCapacity + "mWh.";
                    LblPercentage.Text = "Remaining percentage";
                    TxtPercentage.Text = CalculatePercentage(_remainingCapacity, _fullChargeCapacity);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
            }
        }
        /// <summary>
        /// Application UI scenarios.
        /// </summary>
        private enum UiScenarios
        {
            Default,
            WithoutBattery,
            WithBattery
        }
        /// <summary>
        /// Event handler for disconnecting controllers.
        /// </summary>
        /// <param name="sender">Object that called this event handler.</param>
        /// <param name="e">Controller that was disconnected.</param>
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

                UpdateUi(UiScenarios.Default);

                GenerateToast(ToastType.ControllerDisconnected);
            });
        }
        /// <summary>
        /// Event handler for new controller connections.
        /// </summary>
        /// <param name="sender">Object that called this event handler.</param>
        /// <param name="e">Controller that was connected.</param>
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
            VibrateController(1, 0, 1, 0, 300);

            
        }
        /// <summary>
        /// Calculate remaining percentage based off the maximum and current capacity.
        /// </summary>
        /// <param name="batteryCharge">Current battery charge.</param>
        /// <param name="fullChargeCapacity">Maximum battery charge.</param>
        /// <returns></returns>
        private static string CalculatePercentage(string batteryCharge, string fullChargeCapacity)
        {
            return double.Parse(batteryCharge) / double.Parse(fullChargeCapacity) * 100 + "%";
        }
        /// <summary>
        /// Vibrate the controller according to data provided.
        /// </summary>
        /// <param name="leftMotor">The strength of vibration in values between 0 and 1.</param>
        /// <param name="leftTrigger">The strength of vibration in values between 0 and 1.</param>
        /// <param name="rightMotor">The strength of vibration in values between 0 and 1.</param>
        /// <param name="rightTrigger">The strength of vibration in values between 0 and 1.</param>
        /// <param name="time">Time in milliseconds to vibrate.</param>
        private async void VibrateController(double leftMotor, double leftTrigger, double rightMotor, double rightTrigger, int time)
        {
            var vibration = new GamepadVibration
            {
                LeftMotor = leftMotor,
                LeftTrigger = leftTrigger,
                RightMotor = rightMotor,
                RightTrigger = rightTrigger
            };
            _controller.Vibration = vibration;
            await Task.Delay(TimeSpan.FromMilliseconds(time));
            vibration.LeftMotor = 0;
            vibration.RightMotor = 0;
            _controller.Vibration = vibration;
        }
    }
}