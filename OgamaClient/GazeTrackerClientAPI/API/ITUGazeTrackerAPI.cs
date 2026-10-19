// <copyright file="ITUGazeTrackerAPI.cs" company="FU Berlin">
// ******************************************************
// OgamaClient for ITU GazeTracker
// Copyright (C) 2010 Adrian Voßkühler  
// ------------------------------------------------------------------------
// This program is free software; you can redistribute it and/or modify it 
// under the terms of the GNU General Public License as published by the 
// Free Software Foundation; either version 3 of the License, 
// or (at your option) any later version.
// This program is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU 
// General Public License for more details.
// You should have received a copy of the GNU General Public License 
// along with this program; if not, see http://www.gnu.org/licenses/.
// **************************************************************
// </copyright>
// <author>Adrian Voßkühler</author>
// <email>adrian.vosskuehler@fu-berlin.de</email>

namespace OgamaClient
{
    using System;
    using System.Diagnostics;
    using System.Windows;

    // GazeTracker classes
    using GazeTrackerUI;
    using GazeTrackerUI.Calibration;
    using GazeTrackerUI.Tools;
    using GazeTrackingLibrary;
    using GazeTrackingLibrary.Logging;
    using GazeTrackingLibrary.Settings;
    using GazeTrackingLibrary.Camera;
    using GazeTracker.Tools;
    using GazeTrackingLibrary.Utils;
    using GazeTrackerUI.Settings;
    using GTCommons;

    /// <summary>
    /// This is the main API of the OGAMA client for the ITU GazeTracker.
    /// Its main purpose is to implement the functionality required for
    /// the ITracker interface of OGAMA.
    /// </summary>
    public class ITUGazeTrackerAPI
    {
        ///////////////////////////////////////////////////////////////////////////////
        // Defining Constants                                                        //
        ///////////////////////////////////////////////////////////////////////////////
        #region CONSTANTS
        #endregion //CONSTANTS

        ///////////////////////////////////////////////////////////////////////////////
        // Defining Variables, Enumerations, Events                                  //
        ///////////////////////////////////////////////////////////////////////////////
        #region FIELDS

        /// <summary>
        /// The <see cref="Tracker"/> that performs the processing.
        /// </summary>
        protected Tracker tracker;

        ///// <summary>
        ///// A modified version of the GazeTrackerUI SettingsWindow
        ///// class for reduced options in the OGAMA setup.
        ///// </summary>
        //protected OgamaClientSettingsWindow settingsWindow;

        /// <summary>
        /// The GazeTrackerUI SettingsWindow
        /// </summary>
        protected SettingsWindow settingsWindow;

        /// <summary>
        /// The GazeTrackerUI CameraSettingsWindow
        /// </summary>
        protected CameraSettingsWindow cameraSettingsWindow;

        /// <summary>
        /// A windows forms version of the GazeTrackerUI VideoImageControl
        /// that displays the processed eye video.
        /// </summary>
        protected EyeVideoControl eyeVideoControl;

        /// <summary>
        /// The object that displays and performs the calibration
        /// </summary>
        private CalibrationWindow calibrationWindow;

        /// <summary>
        /// A windows forms version of the GazeTrackerUI VideoViewer
        /// that displays the eye video in native size.
        /// </summary>
        private TrackStatusControl eyeTrackStatus;

        /// <summary>
        /// A windows forms control that receives the calibration
        /// result which can be separate from the calibration control.
        /// </summary>
        private CalibrationResultControl calibrationResultControl;

        /// <summary>
        /// A window for displaying convenient messages.
        /// </summary>
        private MessageWindow msgWindow;

        /// <summary>
        /// Flag, indicating whether this API client is currently
        /// tracking gaze data.
        /// </summary>
        private bool isRunning;

        /// <summary>
        /// A precise timer for getting local time stamps.
        /// </summary>
        private Stopwatch stopWatch;

        #endregion //FIELDS

        ///////////////////////////////////////////////////////////////////////////////
        // Construction and Initializing methods                                     //
        ///////////////////////////////////////////////////////////////////////////////
        #region CONSTRUCTION

        /// <summary>
        /// Initializes a new instance of the ITUGazeTrackerAPI class.
        /// </summary>
        public ITUGazeTrackerAPI()
        {
            this.msgWindow = new MessageWindow();
            this.stopWatch = new Stopwatch();
            this.isRunning = false;
        }

        #endregion //CONSTRUCTION

        ///////////////////////////////////////////////////////////////////////////////
        // Defining Enumerations                                                     //
        ///////////////////////////////////////////////////////////////////////////////
        #region ENUMS

        /// <summary>
        /// Implementation of the ITracker interface.
        /// Event. Raised, when new gaze data is available.
        /// </summary>
        public event OgamaGazeDataChangedEventHandler GazeDataChanged;

        /// <summary>
        /// Event that is raised whenever a calibration process
        /// has been succesfully finished.
        /// </summary>
        public event EventHandler CalibrationFinishedEvent;

        #endregion ENUMS

        ///////////////////////////////////////////////////////////////////////////////
        // Defining Properties                                                       //
        ///////////////////////////////////////////////////////////////////////////////
        #region PROPERTIES

        /// <summary>
        /// Gets or sets a value indicating whether the the tracker is connected 
        /// to a device.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Gets the <see cref="Tracker"/>, that is the underlying control
        /// for the ogama client.
        /// </summary>
        public Tracker Tracker
        {
            get { return this.tracker; }
        }

        #endregion //PROPERTIES

        ///////////////////////////////////////////////////////////////////////////////
        // Public methods                                                            //
        ///////////////////////////////////////////////////////////////////////////////
        #region PUBLICMETHODS

        /// <summary>
        /// Method to return the current timing of the tracking system.
        /// </summary>
        /// <returns>A <see cref="Int64"/> with the current time in milliseconds
        /// if the stopwatch is running, otherwise -1.</returns>
        public long GetCurrentTime()
        {
            if (this.stopWatch == null)
            {
                return -1;
            }
            else
            {
                return this.stopWatch.ElapsedMilliseconds;
            }
        }

        /// <summary>
        /// The implementation of this method connects the client, so that the
        /// system is ready for calibration.
        /// </summary>
        /// <returns><strong>True</strong> if succesful connected to tracker,
        /// otherwise <strong>false</strong>.</returns>
        public virtual bool Connect()
        {
            // Little fix for colorschema (must run before initializing)
            GazeTrackerUI.Tools.ComboBoxBackgroundColorFix.Initialize();

            // Register for special error messages
            ErrorLogger.TrackerError += new ErrorLogger.TrackerErrorMessageHandler(tracker_OnTrackerError);

            if (!CameraControl.Instance.CameraExists())
            {
                ShowMessageNoCamera();
                return false;
            }

            // Load GTSettings
            GTSettings.Current.LoadLatestConfiguration();

            // Create Tracker
            tracker = new Tracker(GTCommons.GTCommands.Instance);
            tracker.InitCamera();

            if (!this.tracker.HasValidCamera)
            {
                string message = "Please choose a camera and resolution from the following dialog." + Environment.NewLine +
                  "Verified configurations can be found in the forum located at http://forum.gazegroup.org";
                MessageBox.Show(message, "Please select camera ...", MessageBoxButton.OK, MessageBoxImage.Information);
                this.ShowSetupWindow();
                return false;
            }

            // Load settings and apply 
            this.InitSettings();

            // Video preview window (tracker visualizes image processing) 
            this.eyeVideoControl.Tracker = this.tracker;
            this.eyeVideoControl.Start();

            this.tracker.Run();
            this.IsConnected = true;

            return true;
        }

        /// <summary>
        /// This method initializes custom components of the 
        /// implemented tracking device.
        /// </summary>
        /// <param name="clientEyeVideoConrol">The <see cref="EyeVideoControl"/>
        /// of the client where to display the eye video.</param>
        /// <param name="clientCalibrationResultControl">The <see cref="CalibrationResultControl"/>
        /// of the client where to display the calibration results</param>
        public void Initialize(
          EyeVideoControl clientEyeVideoConrol,
          CalibrationResultControl clientCalibrationResultControl)
        {
            this.eyeVideoControl = clientEyeVideoConrol;
            this.calibrationResultControl = clientCalibrationResultControl;

            Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte> initialImage =
              new Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>(Properties.Resources.StartUp);
            this.eyeVideoControl.CVImageBox.Image = initialImage.Resize(
              this.eyeVideoControl.CVImageBox.Width,
              this.eyeVideoControl.CVImageBox.Height,
              Emgu.CV.CvEnum.INTER.CV_INTER_AREA);
        }

        /// <summary>
        /// This method shows or hides a copy of the eye video on
        /// the current presentation screen in native resolution.
        /// </summary>
        /// <param name="show"><strong>True</strong> if control should be shown,
        /// otherwise<strong>false</strong></param>
        public void ShowOrHideTrackStatusOnPresentationScreen(bool show)
        {
            // Ignore if no tracker selected
            if (this.tracker == null)
            {
                return;
            }

            if (show)
            {
                // Create a new window to host a larger VideoViewer
                this.eyeTrackStatus = new TrackStatusControl();

                this.eyeTrackStatus.EyeVideoControl.Tracker = this.tracker;
                this.eyeTrackStatus.EyeVideoControl.IsNativeResolution = true;
                if (this.tracker is PS3GazeTracker)
                {
                    CLEyeMulticam.CLEyeCameraResolution resolution =
                      ((PS3GazeTracker)this.tracker).PlayStationEyeCamera.Resolution;
                    int width = resolution == CLEyeMulticam.CLEyeCameraResolution.CLEYE_QVGA ? 320 : 640;
                    int height = resolution == CLEyeMulticam.CLEyeCameraResolution.CLEYE_QVGA ? 240 : 480;
                    this.eyeTrackStatus.SetSizeAndLabels(
                      width,
                      height,
                      ((PS3GazeTracker)this.tracker).PlayStationEyeCamera.Framerate);
                }
                else
                {
                    this.eyeTrackStatus.SetSizeAndLabels(
                      this.tracker.VideoWidth,
                      this.tracker.VideoHeight,
                      this.tracker.VideoFPS);
                }

                this.eyeTrackStatus.Left = (int)(
                  TrackingScreen.TrackingScreenLeft +
                  TrackingScreen.TrackingScreenCenter.X - (this.eyeTrackStatus.Width / 2));
                this.eyeTrackStatus.Top = (int)(
                  TrackingScreen.TrackingScreenTop +
                  TrackingScreen.TrackingScreenCenter.Y - (this.eyeTrackStatus.Height / 2));

                this.eyeTrackStatus.Show();
            }
            else
            {
                if (this.eyeTrackStatus != null)
                {
                    this.eyeTrackStatus.Close();
                }
            }
        }

        /// <summary>
        /// This method performs the calibration
        /// for the client, so that the
        /// system is ready for recording.
        /// </summary>
        /// <param name="isRecalibrating"><strong>True</strong> if calibration
        /// is in recalibration mode, indicating to renew only a few points,
        /// otherwise <strong>false</strong>.</param>
        /// <returns><strong>True</strong> if succesful calibrated,
        /// otherwise <strong>false</strong>.</returns>
        public bool Calibrate(bool isRecalibrating)
        {
            if (isRecalibrating)
            {
                this.calibrationWindow.Visibility = Visibility.Visible;
                this.calibrationWindow.Recalibrate();
            }
            else
            {
                // Save settings before starting calibration
                this.settingsWindow.SaveSettings();

                this.calibrationWindow = new CalibrationWindow();
                this.calibrationWindow.Tracker = this.tracker;
                this.calibrationWindow.ExportCalibrationResults = true;
                this.calibrationWindow.Show();
                this.calibrationWindow.Start();
                this.calibrationWindow.CalibrationResultReadyEvent +=
                  new GazeTrackerUI.Calibration.Events.CalibrationResultEventHandler(this.CalibrationWindow_CalibrationResultReadyEvent);
            }

            return true;
        }

        /// <summary>
        /// This method should be called, when the shown calibration result
        /// is good enough for recording.
        /// </summary>
        public void AcceptCalibration()
        {
            if (this.calibrationWindow != null)
            {
                this.calibrationWindow.AcceptCalibration();
                this.calibrationWindow.Close();
            }
        }

        /// <summary>
        /// Implementors should use this method to customize the
        /// preparation of the recording.
        /// </summary>
        public void PrepareRecording()
        {
        }

        /// <summary>
        /// This method starts the recording
        /// for the specific hardware, so the
        /// system sends <see cref="GazeDataChanged"/> events.
        /// </summary>
        public void Record()
        {
            if (!this.isRunning)
            {
                this.stopWatch.Start();

                // Register listner for gazedata events
                this.tracker.GazeDataRaw.GazeDataChanged += new GTGazeData.GazeDataChangedEventHandler(this.GazeDataRaw_OnNewGazeData);

                this.isRunning = true;
            }
        }

        /// <summary>
        /// This method stops the recording
        /// for the specific hardware.
        /// </summary>
        public void Stop()
        {
            if (this.isRunning)
            {
                this.stopWatch.Reset();

                // Unregister events
                this.tracker.GazeDataRaw.GazeDataChanged -= new GTGazeData.GazeDataChangedEventHandler(this.GazeDataRaw_OnNewGazeData);

                this.isRunning = false;
            }
        }

        /// <summary>
        /// This method performs a clean up
        /// for the specific hardware, so that the
        /// system is ready for shut down.
        /// </summary>
        public void CleanUp()
        {
            this.stopWatch.Reset();

            // If video is detached (seperate window), stop updating images)
            if (this.eyeTrackStatus != null)
            {
                this.eyeTrackStatus.EyeVideoControl.Stop();
            }

            // Save settings 
            if (this.settingsWindow != null)
            {
                this.settingsWindow.SaveSettings();
            }

            // Cleaup tracker & release camera
            if (this.tracker != null)
            {
                this.tracker.Cleanup();
            }

            // Null tracker..
            this.tracker = null;
            this.IsConnected = false;
        }

        /// <summary>
        /// This method shows a hardware 
        /// system specific dialog to change its settings like
        /// sampling rate or connection properties. It should also
        /// provide a xml serialization possibility of the settings,
        /// so that the user can store and backup system settings in
        /// a separate file.
        /// </summary>
        public void ChangeSettings()
        {
            if (this.IsConnected)
            {
                this.ShowSetupWindow();
            }
        }

        /// <summary>
        /// This method shows the ITU dialog for the Camera settings.
        /// </summary>
        public void ChangeCameraSettings()
        {
            this.ShowCameraSetupWindow();
        }

        /// <summary>
        /// Disposes the <see cref="Tracker"/> if applicable
        /// by a call to <see cref="ITracker.CleanUp()"/> and 
        /// removing the connected event handlers.
        /// </summary>
        public void Dispose()
        {
            if (this.tracker != null)
            {
                this.tracker.GazeDataRaw.GazeDataChanged -= new GTGazeData.GazeDataChangedEventHandler(this.GazeDataRaw_OnNewGazeData);
            }

            this.CleanUp();

            // Close all windows (including Visibility.Collapsed & Hidden)
            if (this.eyeTrackStatus != null)
            {
                this.eyeTrackStatus.Close();
            }

            if (this.settingsWindow != null)
            {
                this.settingsWindow.Close();
            }

            if (this.cameraSettingsWindow != null)
            {
                this.cameraSettingsWindow.Close();
            }

            if (this.calibrationWindow != null)
            {
                this.calibrationWindow.Close();
            }

            if (this.msgWindow != null)
            {
                this.msgWindow.Close();
            }
        }

        #endregion //PUBLICMETHODS

        ///////////////////////////////////////////////////////////////////////////////
        // Inherited methods                                                         //
        ///////////////////////////////////////////////////////////////////////////////
        #region OVERRIDES

        /// <summary>
        /// This virtual method creates system and camera settings and
        /// a corresponding <see cref="OgamaClientSettingsWindow"/>
        /// to modify them.
        /// </summary>
        protected virtual void InitSettings()
        {
            //this.settingsWindow = new SettingsWindow();
            //this.cameraSettingsWindow = new CameraSettingsWindow();
            ////this.settingsWindow = new OgamaClientSettingsWindow();

            //this.settingsWindow.Visibility = Visibility.Collapsed;
            //this.cameraSettingsWindow.Visibility = Visibility.Collapsed;
            GTCommons.GTCommands.Instance.Camera.OnCameraChange += new RoutedEventHandler(OnCameraChange);
        }

        #endregion //OVERRIDES

        ///////////////////////////////////////////////////////////////////////////////
        // Eventhandler                                                              //
        ///////////////////////////////////////////////////////////////////////////////
        #region EVENTS

        ///////////////////////////////////////////////////////////////////////////////
        // Eventhandler for UI, Menu, Buttons, Toolbars etc.                         //
        ///////////////////////////////////////////////////////////////////////////////
        #region WINDOWSEVENTHANDLER
        #endregion //WINDOWSEVENTHANDLER

        ///////////////////////////////////////////////////////////////////////////////
        // Eventhandler for Custom Defined Events                                    //
        ///////////////////////////////////////////////////////////////////////////////
        #region CUSTOMEVENTHANDLER

        /// <summary>
        /// Is called whenever new raw data is available,
        /// adds the timestamp to the data and sends the <see cref="GazeDataChanged"/>
        /// event.
        /// </summary>
        /// <param name="x">A <see cref="Double"/> with the x coordinate of
        /// the new gaze position.</param>
        /// <param name="y">A <see cref="Double"/> with the y coordinate of
        /// the new gaze position.</param>
        protected void GazeDataRaw_OnNewGazeData(double x, double y)
        {
            OgamaGazeData newGazeData = new OgamaGazeData();

            newGazeData.Time = this.stopWatch.ElapsedMilliseconds;
            newGazeData.GazePosX = (float)x;
            newGazeData.GazePosY = (float)y;

            if (this.GazeDataChanged != null)
            {
                this.GazeDataChanged(this, new OgamaGazeDataChangedEventArgs(newGazeData));
            }
        }

        protected void tracker_OnTrackerError(string message)
        {
            msgWindow = new MessageWindow();
            msgWindow.Text = message;
            msgWindow.Show();
        }

        /// <summary>
        /// The event handler for the CalibrationResultReadyEvent which updates
        /// the calibration result control with the feedback image and rating.
        /// </summary>
        /// <param name="sender">Source of the event</param>
        /// <param name="e">A <see cref="GazeTracker.Calibration.CalibrationResultEventArgs"/>
        /// with the event data.</param>
        private void CalibrationWindow_CalibrationResultReadyEvent(object sender, GazeTrackerUI.Calibration.Events.CalibrationResultEventArgs e)
        {
            this.calibrationResultControl.CalibrationResult = e.ResultBitmap;
            this.calibrationResultControl.CalibrationResultRating = e.RatingValue;
            this.calibrationWindow.Visibility = Visibility.Collapsed;

            // Raise event to notify listeners
            this.OnCalibrationFinished();
        }

        /// <summary>
        /// The event handler for the OnCameraChanged which creates
        /// a new camera for the tracker.
        /// </summary>
        /// <param name="sender">Source of the event</param>
        /// <param name="e">An empty <see cref="System.Windows.RoutedEventArgs"/></param>
        private void OnCameraChange(object sender, RoutedEventArgs e)
        {
            if (this.tracker != null)
            {
                this.tracker.SetCamera(GTSettings.Current.Camera.DeviceNumber, GTSettings.Current.Camera.DeviceMode);
            }
        }

        /// <summary>
        /// Synchronously raises the <see cref="CalibrationFinishedEvent"/> event.
        /// </summary>
        private void OnCalibrationFinished()
        {
            if (this.CalibrationFinishedEvent != null)
            {
                this.CalibrationFinishedEvent(this, EventArgs.Empty);
            }
        }


        #endregion //CUSTOMEVENTHANDLER

        #endregion //EVENTS

        ///////////////////////////////////////////////////////////////////////////////
        // Methods and Eventhandling for Background tasks                            //
        ///////////////////////////////////////////////////////////////////////////////
        #region BACKGROUNDWORKER
        #endregion //BACKGROUNDWORKER

        ///////////////////////////////////////////////////////////////////////////////
        // Methods for doing main class job                                          //
        ///////////////////////////////////////////////////////////////////////////////
        #region METHODS

        /// <summary>
        /// This method shows a <see cref="MessageWindow"/> that 
        /// describes that there is no tracking camera found.
        /// </summary>
        private void ShowMessageNoCamera()
        {
            this.msgWindow = new MessageWindow();
            this.msgWindow.Text = "The GazeTracker was unable to connect a camera. \n" +
                             "Make sure that the device is connected and that the device drivers are installed. " +
                             "Verified configurations can be found in our forum located at http://forum.gazegroup.org";
            this.msgWindow.Show();
            ErrorLogger.WriteLine("Fatal error on startup, could not connect to a camera.");
        }

        /// <summary>
        /// This method shows/hides an already existing settings window.
        /// </summary>
        private void ShowSetupWindow()
        {
            if (this.settingsWindow != null)
            {
                if (this.settingsWindow.Visibility.Equals(Visibility.Collapsed))
                {
                    this.settingsWindow.Visibility = Visibility.Visible;
                }
                else
                {
                    this.settingsWindow.SaveSettings();
                    this.settingsWindow.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// This method shows/hides an already existing settings window.
        /// </summary>
        private void ShowCameraSetupWindow()
        {
            if (this.cameraSettingsWindow != null)
            {
                if (this.cameraSettingsWindow.Visibility.Equals(Visibility.Collapsed))
                {
                    this.cameraSettingsWindow.Visibility = Visibility.Visible;
                }
                else
                {
                    this.cameraSettingsWindow.Visibility = Visibility.Collapsed;
                }
            }
        }


        #endregion //METHODS

        ///////////////////////////////////////////////////////////////////////////////
        // Small helping Methods                                                     //
        ///////////////////////////////////////////////////////////////////////////////
        #region HELPER
        #endregion //HELPER
    }
}
