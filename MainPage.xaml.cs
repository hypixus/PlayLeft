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
using Windows.ApplicationModel.Resources;
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

        private string _fullChargeCapacity = "";
        private Gamepad _gamepad;
        private string _remainingCapacity = "";
        private bool _wirelessConnected;

        public MainPage()
        {
            InitializeComponent();

            //Set the window size.
            ApplicationView.PreferredLaunchViewSize = new Size(600, 300);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            LoadDefaultUi();

            ControllerLoop();
        }

        private void LoadDefaultUi()
        {
            //Load localized string
            var resourceLoader = ResourceLoader.GetForCurrentView();
            lblContSelected.Text = resourceLoader.GetString("NoController");

            lblConnection.Text = "";
            lblBatteryStatus.Text = "";
            lblFullChargeCap.Text = "";
            lblRemainingCap.Text = "";
            txtPercentage.Text = "";
        }

        private async void ControllerLoop()
        {
            //Set Add/Remove actions.
            Gamepad.GamepadAdded += ControllerConnected;
            Gamepad.GamepadRemoved += ControllerDisconnected;

            while (true)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    //If no game pad found.
                    if (_gamepad == null) return;

                    //Can not handle more than one controller, will support 4 controllers once I can work out how to identify controllers.
                    if (Gamepad.Gamepads.Count > 1)
                        // TODO
                        return;

                    // Get the current state.
                    _wirelessConnected = _gamepad.IsWireless;

                    // Get battery status.
                    var controllerBattery = _gamepad.TryGetBatteryReport();

                    // Get battery state and retrieve the correct localization string
                    var resourceLoader = ResourceLoader.GetForCurrentView();
                    switch (controllerBattery.Status)
                    {
                        case BatteryStatus.Charging:
                            _batteryStatus = resourceLoader.GetString("BatteryStatusCharging");
                            break;
                        case BatteryStatus.Discharging:
                            _batteryStatus = resourceLoader.GetString("BatteryStatusDischarging");
                            break;
                        case BatteryStatus.Idle:
                            _batteryStatus = resourceLoader.GetString("BatteryStatusIdle");
                            break;
                        case BatteryStatus.NotPresent:
                            _batteryStatus = resourceLoader.GetString("BatteryStatusNotPresent");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (controllerBattery.Status == BatteryStatus.NotPresent)
                    {
                        _fullChargeCapacity = "0";
                        _remainingCapacity = "0";
                        NoBatteryUi(ref resourceLoader);
                    }
                    else
                    {
                        _fullChargeCapacity = controllerBattery.FullChargeCapacityInMilliwattHours?.ToString();
                        _remainingCapacity = controllerBattery.RemainingCapacityInMilliwattHours?.ToString();
                        UpdateUiDetails(ref resourceLoader);
                    }
                });
                //set time in ms to check.
                await Task.Delay(TimeSpan.FromMilliseconds(UpdateFrequency));
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private void UpdateUiDetails(ref ResourceLoader rsLoader)
        {
            lblContSelected.Text = rsLoader.GetString("ControllerConnected");

            // Fetch resource for proper connection type.
            lblConnection.Text = _wirelessConnected
                ? rsLoader.GetString("ConnectedViaWireless")
                : rsLoader.GetString("ConnectedViaCable");

            lblBatteryStatus.Text = rsLoader.GetString("BatteryState") + " " + _batteryStatus;
            lblFullChargeCap.Text = rsLoader.GetString("FullCapacity") + " " + _fullChargeCapacity + "mWh";
            lblRemainingCap.Text = rsLoader.GetString("RemainingCapacity") + " " + _remainingCapacity + "mWh";

            // Calculate remaining percentage and display it.
            txtPercentage.Text = CalculatePercentage(_remainingCapacity, _fullChargeCapacity);
        }

        private void NoBatteryUi(ref ResourceLoader rsLoader)
        {
            lblContSelected.Text = rsLoader.GetString("ControllerConnected");

            // Fetch resource for proper connection type.
            lblConnection.Text = _wirelessConnected
                ? rsLoader.GetString("ConnectedViaWireless")
                : rsLoader.GetString("ConnectedViaCable");

            lblBatteryStatus.Text = rsLoader.GetString("BatteryState") + " None";
            lblFullChargeCap.Text = rsLoader.GetString("FullCapacity") + " None";
            lblRemainingCap.Text = rsLoader.GetString("RemainingCapacity") + " None";

            // Calculate remaining percentage and display it.
            txtPercentage.Text = "None";
        }

        private async void ControllerDisconnected(object sender, Gamepad e)
        {
            _gamepad = null;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // If controllers count is larger than one, do noting. Will support 4 controllers once I can work out how to identify controllers.
                if (Gamepad.Gamepads.Count > 1)
                    // TODO
                    return;
                if (Gamepad.Gamepads.Count == 0)
                {
                    var resourceLoader = ResourceLoader.GetForCurrentView();
                    lblContSelected.Text = resourceLoader.GetString("NoController");
                    lblConnection.Text = "";
                    lblBatteryStatus.Text = "";
                    lblFullChargeCap.Text = "";
                    lblRemainingCap.Text = "";
                    txtPercentage.Text = "";

                    GenerateToast(ToastType.ControllerDisconnected);
                }
            });
        }

        private async void ControllerConnected(object sender, Gamepad e)
        {
            _gamepad = e;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                //If controllers count is larger than one, do noting. Will support 4 controllers once I can work out how to identify controllers.
                if (Gamepad.Gamepads.Count > 1) return;

                GenerateToast(ToastType.ControllerConnected);
            });
        }

        private static string CalculatePercentage(string batteryCharge, string fullChargeCapacity)
        {
            return double.Parse(batteryCharge) / double.Parse(fullChargeCapacity) * 100 + "%";
        }
    }
}