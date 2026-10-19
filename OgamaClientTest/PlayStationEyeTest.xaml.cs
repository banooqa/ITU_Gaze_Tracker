using System.Windows;
using OgamaClient;

namespace OgamaClientTest
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class PlayStationEyeTest : Window
    {
        private PS3GazeTrackerAPI ituPS3Client;

        public PlayStationEyeTest()
        {
            InitializeComponent();
            this.windowsFormsHostCalibration.Visibility = Visibility.Collapsed;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.ituPS3Client = new PS3GazeTrackerAPI();
            this.ituPS3Client.Initialize(this.videoImageControl, this.calibrationResultControl);
        }


        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            this.ituPS3Client.Connect();
        }

        private void Setup_Click(object sender, RoutedEventArgs e)
        {
            this.ituPS3Client.ChangeSettings();
        }

        private void Calibrate_Click(object sender, RoutedEventArgs e)
        {
            this.ituPS3Client.Calibrate(false);
            this.ituPS3Client.CalibrationFinishedEvent += new System.EventHandler(ituPS3Client_CalibrationFinishedEvent);
        }

        private void ituPS3Client_CalibrationFinishedEvent(object sender, System.EventArgs e)
        {
            this.windowsFormsHostCalibration.Visibility = Visibility.Visible;
            this.windowsFormsHost.Visibility = Visibility.Collapsed;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.ituPS3Client.Dispose();
        }

        private void ShowOnCalibrationScreen_Click(object sender, RoutedEventArgs e)
        {
            this.ituPS3Client.ShowOrHideTrackStatusOnPresentationScreen(true);
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            this.ituPS3Client.AcceptCalibration();
            this.windowsFormsHostCalibration.Visibility = Visibility.Collapsed;
            this.windowsFormsHost.Visibility = Visibility.Visible;
        }

        private void Recalibrate_Click(object sender, RoutedEventArgs e)
        {
            this.ituPS3Client.Calibrate(true);
        }

        private void CameraSettings_Click(object sender, RoutedEventArgs e)
        {
            this.ituPS3Client.ChangeCameraSettings();
        }

    }
}
