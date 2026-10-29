/****************************************************
 * Team member name(s): Banooqa Banday
 * Date: 2026-10-28
 * Project: 2 (Eye Tracker)
 * Instructor: Komogortsev
 ****************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        // MODIFIED: state for the verification procedure (spatial accuracy / precision)
        private const int VerificationSettleMs = 1000;   // discard samples while the eye moves to the target
        private const int VerificationCollectMs = 1500;  // samples used for the metrics
        private readonly List<double[]> verificationSamples = new List<double[]>();
        private readonly object verificationLock = new object();
        private readonly List<Point> verificationTargets = new List<Point>();
        private readonly List<double> verificationAccuracy = new List<double>();
        private readonly List<double> verificationPrecision = new List<double>();
        private DispatcherTimer verificationTimer;
        private Stopwatch verificationWatch;
        private System.Windows.Shapes.Ellipse verificationDot;
        private TextBlock verificationText;
        private TextBlock verificationHint;
        private bool verificationRunning;
        private bool verificationCollecting;
        private bool verificationSmoothed;
        private int verificationIndex;

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
                ShowVerificationHint(true); // MODIFIED

                if (calibrationMenu.CheckBoxVisualFeedback.IsChecked.Value)
                    ToggleCrosshair(null, null);
            }
        }

        #endregion


        #region Events - StopOnEscape or ErrorMsgWin Close 

        private void Calibration_KeyDown(object sender, KeyEventArgs e)
        {
            // MODIFIED: V starts the verification once the calibration menu is shown, Escape aborts it
            if (verificationRunning)
            {
                if (e.Key.Equals(Key.Escape))
                    StopVerification(true);
                return;
            }

            if (e.Key.Equals(Key.V) && calibrationMenu.Visibility == Visibility.Visible)
            {
                StartVerification();
                return;
            }

            if (verificationText != null && verificationText.Visibility == Visibility.Visible)
            {
                // Results are on screen: any key goes back to the menu
                verificationText.Visibility = Visibility.Collapsed;
                calibrationMenu.Visibility = Visibility.Visible;
                ShowVerificationHint(true);
                return;
            }

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



        #region Verification - mean spatial accuracy and mean spatial precision (MODIFIED: new)

        private void ShowVerificationHint(bool show)
        {
            if (verificationHint == null)
            {
                verificationHint = new TextBlock();
                verificationHint.Text = "Press V to run the verification (spatial accuracy / precision)";
                verificationHint.Foreground = Brushes.White;
                verificationHint.FontSize = 16;
                Canvas.SetLeft(verificationHint, 250);
                Canvas.SetTop(verificationHint, 305);
                Panel.SetZIndex(verificationHint, 5);
                CanvasRoot.Children.Add(verificationHint);
            }
            verificationHint.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void StartVerification()
        {
            calibrationMenu.Visibility = Visibility.Collapsed;
            ShowVerificationHint(false);
            if (verificationText != null)
                verificationText.Visibility = Visibility.Collapsed;

            // 3x3 grid of targets, 10 %, 50 % and 90 % of the window
            verificationTargets.Clear();
            double[] fractions = new double[] { 0.1, 0.5, 0.9 };
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                    verificationTargets.Add(new Point(CanvasRoot.Width * fractions[col], CanvasRoot.Height * fractions[row]));

            verificationAccuracy.Clear();
            verificationPrecision.Clear();
            lock (verificationLock)
                verificationSamples.Clear();

            if (verificationDot == null)
            {
                verificationDot = new System.Windows.Shapes.Ellipse();
                verificationDot.Width = 24;
                verificationDot.Height = 24;
                verificationDot.Fill = Brushes.Red;
                verificationDot.Stroke = Brushes.White;
                verificationDot.StrokeThickness = 3;
                Panel.SetZIndex(verificationDot, 4);
                CanvasRoot.Children.Add(verificationDot);
            }
            verificationDot.Visibility = Visibility.Visible;

            // Same gaze signal as the on-screen crosshair
            verificationSmoothed = GTSettings.Current.Processing.EyeMouseSmooth;
            if (verificationSmoothed)
                tracker.GazeDataSmoothed.GazeDataChanged += Verification_OnNewGazeData;
            else
                tracker.GazeDataRaw.GazeDataChanged += Verification_OnNewGazeData;

            verificationRunning = true;
            verificationIndex = -1;
            verificationWatch = new Stopwatch();
            verificationTimer = new DispatcherTimer();
            verificationTimer.Interval = TimeSpan.FromMilliseconds(30);
            verificationTimer.Tick += Verification_OnTick;
            NextVerificationTarget();
            verificationTimer.Start();
        }

        private void NextVerificationTarget()
        {
            verificationIndex++;
            if (verificationIndex >= verificationTargets.Count)
            {
                StopVerification(false);
                return;
            }

            Point target = verificationTargets[verificationIndex];
            Canvas.SetLeft(verificationDot, target.X - verificationDot.Width / 2);
            Canvas.SetTop(verificationDot, target.Y - verificationDot.Height / 2);

            lock (verificationLock)
            {
                verificationSamples.Clear();
                verificationCollecting = false;
            }
            verificationWatch.Reset();
            verificationWatch.Start();
        }

        private void Verification_OnTick(object sender, EventArgs e)
        {
            long ms = verificationWatch.ElapsedMilliseconds;

            // Start collecting after the settle time
            if (ms >= VerificationSettleMs && !verificationCollecting)
            {
                lock (verificationLock)
                    verificationCollecting = true;
            }

            if (ms >= VerificationSettleMs + VerificationCollectMs)
            {
                FinishVerificationTarget();
                NextVerificationTarget();
            }
        }

        // May be called from the tracking thread
        private void Verification_OnNewGazeData(double x, double y)
        {
            if (x < 0 || y < 0) return;
            lock (verificationLock)
            {
                if (verificationCollecting)
                    verificationSamples.Add(new double[] { x, y });
            }
        }

        private void FinishVerificationTarget()
        {
            List<double[]> samples;
            lock (verificationLock)
            {
                verificationCollecting = false;
                samples = new List<double[]>(verificationSamples);
                verificationSamples.Clear();
            }

            Point target = verificationTargets[verificationIndex];
            double mmPerUnit = ScreenParameters.PrimarySize.Width / CanvasRoot.Width;
            double distanceMm = GTSettings.Current.Calibration.DistanceFromScreen;

            verificationAccuracy.Add(GazeQualityMetrics.SpatialAccuracy(samples, target.X, target.Y, mmPerUnit, distanceMm));
            verificationPrecision.Add(GazeQualityMetrics.SpatialPrecision(samples, mmPerUnit, distanceMm));
        }

        private void StopVerification(bool aborted)
        {
            if (verificationTimer != null)
            {
                verificationTimer.Stop();
                verificationTimer.Tick -= Verification_OnTick;
            }

            try
            {
                tracker.GazeDataSmoothed.GazeDataChanged -= Verification_OnNewGazeData;
                tracker.GazeDataRaw.GazeDataChanged -= Verification_OnNewGazeData;
            }
            catch (Exception ex)
            {
                ErrorLogger.ProcessException(ex, false);
            }

            verificationRunning = false;
            verificationDot.Visibility = Visibility.Collapsed;

            if (aborted)
            {
                calibrationMenu.Visibility = Visibility.Visible;
                ShowVerificationHint(true);
                return;
            }

            ShowVerificationResults();
        }

        private void ShowVerificationResults()
        {
            double meanAccuracy = GazeQualityMetrics.MeanIgnoringNaN(verificationAccuracy);
            double meanPrecision = GazeQualityMetrics.MeanIgnoringNaN(verificationPrecision);

            int valid = 0;
            string lines = "";
            for (int i = 0; i < verificationAccuracy.Count; i++)
            {
                if (!double.IsNaN(verificationAccuracy[i])) valid++;
                lines += "Target " + (i + 1) + ":  accuracy " + FormatDegrees(verificationAccuracy[i]) +
                         "   precision " + FormatDegrees(verificationPrecision[i]) + "\n";
            }

            string text = "VERIFICATION RESULTS\n\n" +
                          "Mean spatial accuracy:  " + FormatDegrees(meanAccuracy) + "\n" +
                          "Mean spatial precision (RMS-S2S):  " + FormatDegrees(meanPrecision) + "\n" +
                          "Targets with data:  " + valid + " / " + verificationAccuracy.Count + "\n" +
                          "Distance from screen:  " + GTSettings.Current.Calibration.DistanceFromScreen + " mm\n\n" +
                          lines + "\nPress any key to return";

            if (verificationText == null)
            {
                verificationText = new TextBlock();
                verificationText.Foreground = Brushes.White;
                verificationText.Background = Brushes.Black;
                verificationText.FontSize = 24;
                verificationText.FontFamily = new FontFamily("Consolas");
                Canvas.SetLeft(verificationText, 150);
                Canvas.SetTop(verificationText, 80);
                Panel.SetZIndex(verificationText, 6);
                CanvasRoot.Children.Add(verificationText);
            }
            verificationText.Text = text;
            verificationText.Visibility = Visibility.Visible;

            SaveVerificationResults(meanAccuracy, meanPrecision);
        }

        private static string FormatDegrees(double value)
        {
            return double.IsNaN(value) ? "n/a" : value.ToString("0.00") + " deg";
        }

        private void SaveVerificationResults(double meanAccuracy, double meanPrecision)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "verification_results.csv");
                bool exists = File.Exists(path);
                using (StreamWriter w = new StreamWriter(path, true))
                {
                    if (!exists)
                        w.WriteLine("time,distance_mm,mean_accuracy_deg,mean_precision_deg");
                    w.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," +
                                GTSettings.Current.Calibration.DistanceFromScreen + "," +
                                meanAccuracy.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," +
                                meanPrecision.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.ProcessException(ex, false);
            }
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