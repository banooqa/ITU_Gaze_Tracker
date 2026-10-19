using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GazeTracker.Tools;
using GazeTrackerUI.Calibration.Events;
using GazeTrackerUI.Tools;
using GazeTrackingLibrary;
using GazeTrackingLibrary.Logging;
using GazeTrackingLibrary.Settings;
using GazeTrackingLibrary.Utils;
using GTCommons;

namespace GazeTrackerUI.Calibration
{


    public partial class CalibrationWindow : Window
    {

        #region Variables

        private static CalibrationWindow instance;
        private readonly VisualGazePoint visualPoint;
        private double margin = 100;
        private Tracker tracker;
        public bool ExportCalibrationResults { get; set; }

        #endregion


        #region Events

        public event CalibrationResultEventHandler CalibrationResultReadyEvent;

        #endregion


        #region Constructor

        public CalibrationWindow()
        {
            //this.DataContext = TrackingScreen.TrackingScreenBounds;
            InitializeComponent();
            visualPoint = new VisualGazePoint();

            // Get tracking-screen size
            Left = TrackingScreen.TrackingScreenLeft;
            Top = TrackingScreen.TrackingScreenTop;
            Width = TrackingScreen.TrackingScreenWidth;
            Height = TrackingScreen.TrackingScreenHeight;

            ExportCalibrationResults = false;

            // Hide menues and stuff
            calibrationMenu.Visibility = Visibility.Collapsed;
            sharingUC.Visibility = Visibility.Collapsed;

            calibrationMenu.OnAccept += AcceptCalibration;
            calibrationMenu.OnShare += ShareData;
            calibrationMenu.OnRecalibrate += RedoCalibration;
            calibrationMenu.OnToggleCrosshair += ToggleCrosshair;
            calibrationMenu.OnToggleSmoothing += ToggleSmoothing;
            calibrationMenu.OnAccuracyParamsChange += AccuracyParamsChange;

            sharingUC.OnDataSent += sharingUC_OnDataSent;

            KeyDown += Calibration_KeyDown;
        }

        #endregion


        #region Public Start/Stop/Reset/Recalibrate

        public void Start()
        {
            CanvasRoot.Width = Width; // Tracking screen width
            CanvasRoot.Height = Height;

            // Partial calibration if not zero or not full screen, set active calibration area by applying margin to the control. 
            if(GTSettings.Current.Calibration.AreaWidth != 0 && 
                GTSettings.Current.Calibration.AreaHeight != 0 && 
                GTSettings.Current.Calibration.AreaWidth != Width &&
                GTSettings.Current.Calibration.AreaHeight != Height)
            {
                calibrationControl.Width = GTSettings.Current.Calibration.AreaWidth;
                calibrationControl.Height = GTSettings.Current.Calibration.AreaHeight;
                Canvas.SetTop(calibrationControl, Height/2 - (calibrationControl.Height/2));
                Canvas.SetLeft(calibrationControl, Width/2 - (calibrationControl.Width/2));
            }
            else
            {
                calibrationControl.Width = Width;
                calibrationControl.Height = Height;
                Canvas.SetTop(calibrationControl, 0);
                Canvas.SetLeft(calibrationControl, 0);
            }

            // Initialize calibration control and settings
            calibrationControl.NumberOfPoints = GTSettings.Current.Calibration.NumberOfPoints;
            calibrationControl.RandomOrder = GTSettings.Current.Calibration.RandomizePointOrder;
            calibrationControl.ColorPoints = GTSettings.Current.Calibration.PointColor;
            calibrationControl.ColorBackground = GTSettings.Current.Calibration.BackgroundColor;
            calibrationControl.PointDuration = GTSettings.Current.Calibration.PointDuration;
            calibrationControl.PointTransitionDuration = GTSettings.Current.Calibration.PointTransitionDuration;
            calibrationControl.PointDiameter = GTSettings.Current.Calibration.PointDiameter;
            calibrationControl.Acceleration = GTSettings.Current.Calibration.Acceleration;
            calibrationControl.Deacceleration = GTSettings.Current.Calibration.Deacceleration;
            calibrationControl.UseInfantGraphics = GTSettings.Current.Calibration.UseInfantGraphics;

            // Register for events
            calibrationControl.OnCalibrationStart += calibrationControl_Start;
            calibrationControl.OnPointStart += calibrationControl_PointStart;
            calibrationControl.OnPointStop += calibrationControl_PointEnd;
            calibrationControl.OnCalibrationEnd += calibrationControl_End;

            // Start calibration procedure
            calibrationControl.Start();
        }

        public void Stop()
        {
            calibrationControl.Stop();
        }

        public void Reset()
        {
            instance = new CalibrationWindow();
        }

        public void Recalibrate()
        {
            calibrationControl.CanvasCalibration.Background = calibrationControl.ColorBackground;
            calibrationMenu.Visibility = Visibility.Collapsed;
            calibrationControl.Reset();
            calibrationControl.Start();
        }

        #endregion


        #region OnEvents

        #region OnEvents from CalibrationControl / Tracker

        private void calibrationControl_Start(object sender, RoutedEventArgs e)
        {
            // Notify tracker that calibration starts
            tracker.CalibrationStart();
        }

        private void calibrationControl_PointStart(object sender, RoutedEventArgs e)
        {
            var control = sender as CalibrationControl;
            // Notify tracker that a point is displayed (start sampling)

            if (control != null) 
                tracker.CalibrationPointStart(control.CurrentPoint.Number, calibrationControl.AdjustPointFromPartialCalibration(calibrationControl.CurrentPoint.Point)); // Convert point to absolute screen pos.
        }

        private void calibrationControl_PointEnd(object sender, RoutedEventArgs e)
        {
            var control = sender as CalibrationControl;

            //// Notify tracker that a point has been displayed (stop sampling)
            if (control == null) return;

            tracker.CalibrationPointEnd();

            if (!control.IsRecalibratingPoint) return;

            calibrationControl_End(null, null);
            control.IsRecalibratingPoint = false;
        }

        private void calibrationControl_End(object sender, RoutedEventArgs e)
        {
            try
            {
                // Notify tracker that calibration has ended, it will raise an event when calculations are done
                tracker.OnCalibrationComplete += Tracker_OnCalibrationCompleted;
                tracker.CalibrationEnd();
            }
            catch (Exception ex)
            {
                ErrorLogger.ProcessException(ex, false);
            }
        }

        private void Tracker_OnCalibrationCompleted(object sender, EventArgs e)
        {
            //if (!tracker.Calibration.IsCalibrated)
            //{
            //    MessageWindow msgWin = new MessageWindow();
            //    msgWin.Text = "Calibration unsuccessful. Not enough images could be captured during the calibration. Try to calibrate again.";
            //    msgWin.Show();
            //    msgWin.Closed += new EventHandler(errorMsgWin_Closed);
            //    return;
            //}

            // Unregister event
            tracker.OnCalibrationComplete -= Tracker_OnCalibrationCompleted;

            // Draw feedback on calibration points and the tracker.calibrationTargets overlaid
			BitmapSource calibrationResult = calibrationControl.VisualizeCalibrationResults(tracker.Calibration.calibMethod.CalibrationTargets);

            // Generate indicator of calibration quality (1-5 star)
            calibrationMenu.GenerateQualityIndicator(calibrationControl.AvgSumStdDev, calibrationControl.AvgDistFromTargets);
			calibrationMenu.SetAccuracy(tracker.Calibration.calibMethod.DegreesLeft, tracker.Calibration.calibMethod.DegreesRight);

            if (ExportCalibrationResults)
            {
                calibrationControl.CanvasCalibration.Background = Brushes.Transparent;
                GTCommands.Instance.Calibration.ExportResults(calibrationResult, calibrationMenu.ratingCalibrationQuality.RatingValue);
                CalibrationResultEventArgs args = new CalibrationResultEventArgs(calibrationResult, calibrationMenu.ratingCalibrationQuality.RatingValue);

                if (CalibrationResultReadyEvent != null)
                    CalibrationResultReadyEvent(this, args); // Raise event, calibration process is done.
            }
            else
            {
                // Show menu to accept or recalibrate
                calibrationMenu.Visibility = Visibility.Visible;

                if (calibrationMenu.CheckBoxVisualFeedback.IsChecked.Value)
                    ToggleCrosshair(null, null);
            }
        }

        #endregion


        #region Events - StopOnEscape or ErrorMsgWin Close 

        private void Calibration_KeyDown(object sender, KeyEventArgs e)
        {
            // Exit on Escape-key
            if (e.Key.Equals(Key.Escape))
            {
                calibrationControl.Stop();
                tracker.CalibrationAbort();
                Close();
                
            }
        }

        private void errorMsgWin_Closed(object sender, EventArgs e)
        {
            Close();
        }

        #endregion


        #region Calibration menu events (accept/redo/share)

        private void RedoCalibration(object sender, RoutedEventArgs e)
        {
            Recalibrate();
        }

        private void AcceptCalibration(object sender, RoutedEventArgs e)
        {
            GTCommands.Instance.Calibration.Accept();
        }

        private void ShareData(object sender, RoutedEventArgs e)
        {
            calibrationMenu.Visibility = Visibility.Collapsed;
            sharingUC.SendData(tracker, CanvasRoot.GetScreenShot(1));
            sharingUC.Visibility = Visibility.Visible;
        }

        private void sharingUC_OnDataSent(object sender, RoutedEventArgs e)
        {
            sharingUC.Visibility = Visibility.Collapsed;
            calibrationMenu.Visibility = Visibility.Visible;
        }

        #endregion


        #endregion


        #region Crosshair - Visual feedback on gaze position

        private void ToggleCrosshair(object sender, RoutedEventArgs e)
        {
            if (calibrationMenu.CheckBoxVisualFeedback.IsChecked.Value)
            {
                RegisterForGazeDataEvent();

                // Add crosshair (visual feedback indicator) to canvas
                CanvasRoot.Children.Add(visualPoint);

                Canvas.SetTop(visualPoint, 0);
                Canvas.SetLeft(visualPoint, 0);
                Panel.SetZIndex(visualPoint, 3);
            }
            else
            {
                UnregisterForGazeDataEvent();
                CanvasRoot.Children.Remove(visualPoint);
            }
        }

        private void ToggleSmoothing(object sender, RoutedEventArgs e)
        {
            if (calibrationMenu.CheckBoxSmooth.IsChecked.Value)
                GTSettings.Current.Processing.EyeMouseSmooth = true;
            else
                GTSettings.Current.Processing.EyeMouseSmooth = false;

            UnregisterForGazeDataEvent();
            RegisterForGazeDataEvent();
        }

        private void AccuracyParamsChange(object sender, RoutedEventArgs e)
        {
            if(calibrationMenu.DistanceFromScreen != GTSettings.Current.Calibration.DistanceFromScreen)
            {
                GTSettings.Current.Calibration.DistanceFromScreen = calibrationMenu.DistanceFromScreen;
				double left = tracker.Calibration.calibMethod.CalculateDegreesLeft();  // recalculate
				double right = tracker.Calibration.calibMethod.CalculateDegreesRight(); // recalculate
                calibrationMenu.SetAccuracy(left, right);
            }
        }


        private void RegisterForGazeDataEvent()
        {
            if (GTSettings.Current.Processing.EyeMouseSmooth)
                tracker.GazeDataSmoothed.GazeDataChanged += GazeDataRaw_OnNewGazeData;
            else
                tracker.GazeDataRaw.GazeDataChanged += GazeDataRaw_OnNewGazeData;
        }

        private void UnregisterForGazeDataEvent()
        {
            try
            {
                tracker.GazeDataSmoothed.GazeDataChanged -= GazeDataRaw_OnNewGazeData;
                tracker.GazeDataRaw.GazeDataChanged -= GazeDataRaw_OnNewGazeData;
            }
            catch (Exception ex)
            {
            }
        }

        private void GazeDataRaw_OnNewGazeData(double x, double y)
        {
            // On new data move the cross-hair
            CanvasRoot.Dispatcher.BeginInvoke
                (
                    DispatcherPriority.Normal,
                    new Action
                        (
                        delegate
                            {
                                if (x < 0 || y < 0) return;
                                Canvas.SetTop(visualPoint, y - visualPoint.Height/2);
                                Canvas.SetLeft(visualPoint, x - visualPoint.Width/2);
                            }
                        )
                );
        }

        #endregion


        #region Set/Get

        public static CalibrationWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CalibrationWindow();
                }

                return instance;
            }
        }

        public Tracker Tracker
        {
            get { return tracker; }
            set { tracker = value; }
        }

        public CalibrationPoint CurrentPoint
        {
            get { return calibrationControl.CurrentPoint; }
        }

        public GTGazeData GazeDataRaw { get; set; }

        #endregion

    }
}