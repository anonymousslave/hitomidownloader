using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ToastNotifications;
using ToastNotifications.Core;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace hitomiDownloader.Core
{
    public class NotificationManager
    {
        private static readonly Lazy<NotificationManager> _instance = new Lazy<NotificationManager>(() => new NotificationManager());
        private Notifier Notifier { get; set; }
        private static MessageOptions option { get; set; } = new MessageOptions()
        {
         ShowCloseButton = true   
        };
        private NotificationManager()
        {
            Notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new WindowPositionProvider(
                    parentWindow: Application.Current.MainWindow,
                    corner: Corner.BottomRight,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));
                cfg.Dispatcher = Application.Current.Dispatcher;
            });
        }
        public static void NotifyInformation(string msg) => Application.Current.Dispatcher.Invoke(()=>Instance.Notifier.ShowInformation(msg,option));
        public static void NotifyError(string msg) => Application.Current.Dispatcher.Invoke(() => Instance.Notifier.ShowError(msg,option));
        public static void NotifySuccess(string msg) => Application.Current.Dispatcher.Invoke(() => Instance.Notifier.ShowSuccess(msg,option));
        public static void NotifyWarning(string msg) => Application.Current.Dispatcher.Invoke(() => Instance.Notifier.ShowWarning(msg,option));

        public static NotificationManager Instance => _instance.Value;
    }
}
