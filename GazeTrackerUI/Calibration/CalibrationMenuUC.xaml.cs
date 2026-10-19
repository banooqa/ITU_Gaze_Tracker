using System;
using System.Windows;
using GazeTrackingLibrary.Logging;
using UserControl=System.Windows.Controls.UserControl;

namespace GazeTracker
{
    #region Using

    

    #endregion

    public partial class CalibrationMenuUC : UserControl
    {

        #region Events

        public static readonly RoutedEvent RecalibrateEvent = EventManager.RegisterRoutedEvent("RecalibrateEvent",RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (CalibrationMenuUC));
        public static readonly RoutedEvent AcceptEvent = EventManager.RegisterRoutedEvent("AcceptEvent", RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (CalibrationMenuUC));
        public static readonly RoutedEvent ShareEvent = EventManager.RegisterRoutedEvent("ShareEvent", RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (CalibrationMenuUC));
        public static readonly RoutedEvent ToggleSmoothingEvent = EventManager.RegisterRoutedEvent("ToggleSmoothingEvent", RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (CalibrationMenuUC));
        public static readonly RoutedEvent ToggleCrosshairEvent = EventManager.RegisterRoutedEvent("ToggleCrosshairEvent", RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (CalibrationMenuUC));
        public static readonly RoutedEvent AccuracyParamChangeEvent = EventManager.RegisterRoutedEvent("AccuracyParamChangeEvent", RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (CalibrationMenuUC));

        #endregion


        #region Constructor

        public CalibrationMenuUC()
        {
            InitializeComponent();

            CheckBoxSmooth.Checked += ToggleSmoothing;
            CheckBoxSmooth.Unchecked += ToggleSmoothing;

            CheckBoxVisualFeedback.Checked += CrosshairToggle;
            CheckBoxVisualFeedback.Unchecked += CrosshairToggle;

            LabelAccuracy.MouseDown += new System.Windows.Input.MouseButtonEventHandler(LabelAccuracy_MouseDown);
            TextBoxDistanceFromScreen.KeyDown += new System.Windows.Input.KeyEventHandler(TextBoxDistanceFromScreen_KeyDown);
        }


        #endregion


        #region Eventhandlers

        public event RoutedEventHandler OnRecalibrate
        {
            add { base.AddHandler(RecalibrateEvent, value); }
            remove { base.RemoveHandler(RecalibrateEvent, value); }
        }

        public event RoutedEventHandler OnAccept
        {
            add { base.AddHandler(AcceptEvent, value); }
            remove { base.RemoveHandler(AcceptEvent, value); }
        }

        public event RoutedEventHandler OnShare
        {
            add { base.AddHandler(ShareEvent, value); }
            remove { base.RemoveHandler(ShareEvent, value); }
        }

        public event RoutedEventHandler OnToggleSmoothing
        {
            add { base.AddHandler(ToggleSmoothingEvent, value); }
            remove { base.RemoveHandler(ToggleSmoothingEvent, value); }
        }

        public event RoutedEventHandler OnToggleCrosshair
        {
            add { base.AddHandler(ToggleCrosshairEvent, value); }
            remove { base.RemoveHandler(ToggleCrosshairEvent, value); }
        }

        public event RoutedEventHandler OnAccuracyParamsChange
        {
            add { base.AddHandler(AccuracyParamChangeEvent, value); }
            remove { base.RemoveHandler(AccuracyParamChangeEvent, value); }
        }

        #endregion

        #region Get/Set

        public int DistanceFromScreen
        {
            get { return Convert.ToInt32(TextBoxDistanceFromScreen.Text);}
            set { TextBoxDistanceFromScreen.Text = value.ToString(); }
        }

        #endregion


        #region Public methods

        public void GenerateQualityIndicator(double avgStd, double avgDist)
        {
            // Determine quality by the average standard deviation of the points
            int StdRating = 0;

            if (avgStd > 0 && avgStd < 20)
                StdRating = 5;
            else if (avgStd > 20 && avgStd < 30)
                StdRating = 4;
            else if (avgStd > 30 && avgStd < 40)
                StdRating = 3;
            else if (avgStd > 40 && avgStd < 50)
                StdRating = 2;
            else
                StdRating = 1;

            // Determine quality by the avgerage distance from the points
            int DistRating = 0;

            if (avgDist > 0 && avgDist < 20)
                DistRating = 5;
            else if (avgDist > 20 && avgDist < 30)
                DistRating = 4;
            else if (avgDist > 30 && avgDist < 40)
                DistRating = 3;
            else if (avgDist > 40 && avgDist < 50)
                DistRating = 2;
            else
                DistRating = 1;

            // Combined value gives a quality indicator
            try
            {
                ratingCalibrationQuality.RatingValue = (int) Math.Round((double) (StdRating + DistRating)/2);
            }
            catch (Exception ex)
            {
                ErrorLogger.ProcessException(ex, false);
            }
        }

        public void SetAccuracy(double left, double right)
        {
            if(right != 0)
                LabelAccuracyValues.Content = "left " + left.ToString().Substring(0,3) + " right " + right.ToString().Substring(0,3);
            else
                LabelAccuracyValues.Content = left.ToString().Substring(0, 3);
        }


        #endregion


        #region Private methods (on menu clicks)

        private void AcceptCalibration(object sender, RoutedEventArgs e)
        {
            RoutedEventArgs args1 = new RoutedEventArgs();
            args1 = new RoutedEventArgs(AcceptEvent, new RoutedEventArgs());
            RaiseEvent(args1);
        }

        private void RedoCalibration(object sender, RoutedEventArgs e)
        {
            RoutedEventArgs args1 = new RoutedEventArgs();
            args1 = new RoutedEventArgs(RecalibrateEvent, new RoutedEventArgs());
            RaiseEvent(args1);
        }

        private void ShareData(object sender, RoutedEventArgs e)
        {
            RoutedEventArgs args1 = new RoutedEventArgs();
            args1 = new RoutedEventArgs(ShareEvent, new RoutedEventArgs());
            RaiseEvent(args1);
        }

        private void CrosshairToggle(object sender, RoutedEventArgs e)
        {
            RoutedEventArgs args1 = new RoutedEventArgs();
            args1 = new RoutedEventArgs(ToggleCrosshairEvent, new RoutedEventArgs());
            RaiseEvent(args1);
        }

        private void ToggleSmoothing(object sender, RoutedEventArgs e)
        {
            RoutedEventArgs args1 = new RoutedEventArgs();
            args1 = new RoutedEventArgs(ToggleSmoothingEvent, new RoutedEventArgs());
            RaiseEvent(args1);
        }

        private void LabelAccuracy_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) 
        {
            GridAccuracyParams.Visibility = Visibility.Visible;
        }

        private void CloseAccuracyParamGrid(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GridAccuracyParams.Visibility = Visibility.Collapsed;
        }

        private void TextBoxDistanceFromScreen_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Enter:
                    AccuracyParamSet(null, null);
                    break;
                case System.Windows.Input.Key.Escape:
                    GridAccuracyParams.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void AccuracyParamSet(object sender, RoutedEventArgs e)
        {
            RoutedEventArgs args1 = new RoutedEventArgs();
            args1 = new RoutedEventArgs(AccuracyParamChangeEvent, new RoutedEventArgs());
            RaiseEvent(args1);
            GridAccuracyParams.Visibility = Visibility.Collapsed;
        }

        #endregion

    }
}