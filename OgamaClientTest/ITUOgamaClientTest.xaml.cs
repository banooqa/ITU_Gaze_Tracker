using System.Windows;
using OgamaClient;

namespace OgamaClientTest
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ITUOgamaClientTest : Window
    {
        private ITUGazeTrackerAPI ituClient;

        public ITUOgamaClientTest()
        {
            InitializeComponent();
            this.windowsFormsHostCalibration.Visibility = Visibility.Collapsed;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.ituClient = new ITUGazeTrackerAPI();
            this.ituClient.Initialize(this.videoImageControl, this.calibrationResultControl);
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            this.ituClient.Connect();
        }

        private void Setup_Click(object sender, RoutedEventArgs e)
        {
            this.ituClient.ChangeSettings();
        }

        private void CameraSettings_Click(object sender, RoutedEventArgs e)
        {
            this.ituClient.ChangeCameraSettings();
        }
 
        private void Calibrate_Click(object sender, RoutedEventArgs e)
        {
            this.ituClient.Calibrate(false);
            this.ituClient.CalibrationFinishedEvent += new System.EventHandler(ituClient_CalibrationFinishedEvent);
        }

        void ituClient_CalibrationFinishedEvent(object sender, System.EventArgs e)
        {
            this.windowsFormsHostCalibration.Visibility = Visibility.Visible;
            this.windowsFormsHost.Visibility = Visibility.Collapsed;
        }

        private void Record_Click(object sender, RoutedEventArgs e)
        {
            this.ituClient.Record();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.ituClient.Dispose();
        }

        private void ShowOnPresentationScreen_Click(object sender, RoutedEventArgs e)
        {
            this.ituClient.ShowOrHideTrackStatusOnPresentationScreen(true);
        }

        private void HideFromPresentationScreen_Click(object sender, RoutedEventArgs e)
        {
            this.ituClient.ShowOrHideTrackStatusOnPresentationScreen(false);
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            this.ituClient.AcceptCalibration();
            this.windowsFormsHostCalibration.Visibility = Visibility.Collapsed;
            this.windowsFormsHost.Visibility = Visibility.Visible;
        }

        private void Recalibrate_Click(object sender, RoutedEventArgs e)
        {
            this.ituClient.Calibrate(true);
        }
   }
}
