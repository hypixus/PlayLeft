/*
 * PlayLeft Small application to get the battery level of your Xbox Controller as a UWP app.
 * First UWP as such just learing how the platform works.
 * Released under GPL3, Developed by Spoonie_au.
 *
 * Fork changes:
 * Complete redesign of the class, to optimize it and eliminate redundant code.
 * Moved away from explicit variables naming.
 */

using System;
using Windows.ApplicationModel.Resources;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace PlayLeft
{
    public static class Toasts
    {
        public enum ToastType
        {
            ControllerConnected,
            ControllerDisconnected
        }

        public static void GenerateToast(ToastType type)
        {
            // Load localized string
            var resourceLoader = ResourceLoader.GetForCurrentView();
            // Contents of the toast
            string template;

            switch (type)
            {
                case ToastType.ControllerConnected:
                    template =
                        $"<toast launch=\"app-defined-string\"><visual><binding template =\"ToastGeneric\"><text>{resourceLoader.GetString("AppDisplayName")}</text><text>{resourceLoader.GetString("ControllerConnected")}</text></binding></visual></toast>";
                    break;
                case ToastType.ControllerDisconnected:
                    template =
                        $"<toast launch=\"app-defined-string\"><visual><binding template =\"ToastGeneric\"><text>{resourceLoader.GetString("AppDisplayName")}</text><text>{resourceLoader.GetString("ControllerDisconnected")}</text></binding></visual></toast>";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            var xmlToast = new XmlDocument();
            xmlToast.LoadXml(template);

            var notification = new ToastNotification(xmlToast);
            var notifier = ToastNotificationManager.CreateToastNotifier();

            notifier.Show(notification);
        }
    }
}