using System.Windows;
using GTCommons.Commands;

namespace GTCommons
{
    public class GTCommands : Window
    {
        #region Variables 

        private static GTCommands instance;

        private readonly AutotuneCommands autotuneCommands;
        private readonly CalibrationCommands calibrationCommands;
        private readonly CameraCommands cameraCommands;
        private readonly IlluminationCommands illuminationCommands;
        private readonly SettingsCommands settingsCommands;
        private readonly TrackerViewerCommands videoViewerCommands;

        #endregion

        #region Events

        public static readonly RoutedEvent NetworkClientEvent = EventManager.RegisterRoutedEvent("NetworkClientEvent",
                                                                                                 RoutingStrategy.Bubble,
                                                                                                 typeof (RoutedEventHandler),
                                                                                                 typeof (GTCommands));

        public static readonly RoutedEvent TrackQualityEvent = EventManager.RegisterRoutedEvent("TrackStatsEvent",
                                                                                                RoutingStrategy.Bubble,
                                                                                                typeof (RoutedEventHandler),
                                                                                                typeof (GTCommands));

        #endregion

        #region Constructor

        private GTCommands()
        {
            settingsCommands = new SettingsCommands();
            calibrationCommands = new CalibrationCommands();
            autotuneCommands = new AutotuneCommands();
            videoViewerCommands = new TrackerViewerCommands();
            cameraCommands = new CameraCommands();
            illuminationCommands = new IlluminationCommands();
        }

        #endregion

        #region EventHandlers

        public event RoutedEventHandler OnNetworkClient
        {
            add { base.AddHandler(NetworkClientEvent, value); }
            remove { base.RemoveHandler(NetworkClientEvent, value); }
        }

        public event RoutedEventHandler OnTrackingQuality
        {
            add { base.AddHandler(TrackQualityEvent, value); }
            remove { base.RemoveHandler(TrackQualityEvent, value); }
        }

        #endregion

        #region Raise events

        public void NetworkClient()
        {
            var args1 = new RoutedEventArgs();
            args1 = new RoutedEventArgs(NetworkClientEvent, this);
            RaiseEvent(args1);
        }

        public void TrackQuality()
        {
            var args1 = new RoutedEventArgs();
            args1 = new RoutedEventArgs(TrackQualityEvent, this);
            RaiseEvent(args1);
        }

        #endregion

        #region Get/Set

        public SettingsCommands Settings
        {
            get { return settingsCommands; }
        }

        public CalibrationCommands Calibration
        {
            get { return calibrationCommands; }
        }

        public AutotuneCommands Autotune
        {
            get { return autotuneCommands; }
        }

        public TrackerViewerCommands TrackerViewer
        {
            get { return videoViewerCommands; }
        }

        public CameraCommands Camera
        {
            get { return cameraCommands; }
        }

        public IlluminationCommands Illumination
        {
            get { return illuminationCommands; }
        }

        public static GTCommands Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GTCommands();
                }

                return instance;
            }
        }

        #endregion
    }
}