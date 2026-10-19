using System.Windows;

namespace GTCommons.Commands
{
    public class IlluminationCommands : Window
    {
        public static readonly RoutedEvent IlluminationSettingsEvent =
            EventManager.RegisterRoutedEvent("IlluminationSettingsEvent", RoutingStrategy.Bubble,
                                             typeof (RoutedEventHandler), typeof (IlluminationCommands));

        public void IlluminationSettings()
        {
            var args1 = new RoutedEventArgs();
            args1 = new RoutedEventArgs(IlluminationSettingsEvent, this);
            RaiseEvent(args1);
        }

        public event RoutedEventHandler OnIlluminationSettings
        {
            add { base.AddHandler(IlluminationSettingsEvent, value); }
            remove { base.RemoveHandler(IlluminationSettingsEvent, value); }
        }
    }
}