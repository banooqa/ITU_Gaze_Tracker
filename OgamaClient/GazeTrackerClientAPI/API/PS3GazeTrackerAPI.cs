// <copyright file="PS3GazeTrackerAPI.cs" company="FU Berlin">
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
    using System.Windows;

    // GazeTracker classes
    using GazeTrackerUI;
    using GazeTrackingLibrary;
    using GazeTrackingLibrary.Settings;
    using GazeTrackingLibrary.Logging;
    using GazeTrackerUI.Settings;

    /// <summary>
    /// This is the main API of the OGAMA client for the ITU GazeTracker
    /// which uses a infared capable PlayStation 3 camera as video input device.
    /// Its main purpose is to implement the functionality required for
    /// the ITracker interface of OGAMA along with some hacks to
    /// avoid the webcam search of the ITU Tracker.
    /// </summary>
    public class PS3GazeTrackerAPI : ITUGazeTrackerAPI
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
        #endregion //FIELDS

        ///////////////////////////////////////////////////////////////////////////////
        // Construction and Initializing methods                                     //
        ///////////////////////////////////////////////////////////////////////////////
        #region CONSTRUCTION

        /// <summary>
        /// Initializes a new instance of the PS3GazeTrackerAPI class.
        /// </summary>
        public PS3GazeTrackerAPI()
            : base()
        {
        }

        #endregion //CONSTRUCTION

        ///////////////////////////////////////////////////////////////////////////////
        // Defining Enumerations                                                     //
        ///////////////////////////////////////////////////////////////////////////////
        #region ENUMS
        #endregion ENUMS

        ///////////////////////////////////////////////////////////////////////////////
        // Defining Properties                                                       //
        ///////////////////////////////////////////////////////////////////////////////
        #region PROPERTIES
        #endregion //PROPERTIES

        ///////////////////////////////////////////////////////////////////////////////
        // Public methods                                                            //
        ///////////////////////////////////////////////////////////////////////////////
        #region PUBLICMETHODS
        #endregion //PUBLICMETHODS

        ///////////////////////////////////////////////////////////////////////////////
        // Inherited methods                                                         //
        ///////////////////////////////////////////////////////////////////////////////
        #region OVERRIDES

        /// <summary>
        /// The implementation of this method connects the client, so that the
        /// system is ready for calibration.
        /// </summary>
        /// <returns><strong>True</strong> if succesful connected to tracker,
        /// otherwise <strong>false</strong>.</returns>
        public override bool Connect()
        {
            // Little fix for colorschema (must run before initializing)
            GazeTrackerUI.Tools.ComboBoxBackgroundColorFix.Initialize();

            // Register for special error messages
            ErrorLogger.TrackerError += new ErrorLogger.TrackerErrorMessageHandler(tracker_OnTrackerError);

            if (CLEyeMulticam.CLEyeCameraDevice.CameraCount == 0)
            {
                this.ShowMessageNoCamera();
                return false;
            }

            // Load GTSettings
            GTSettings.Current.LoadLatestConfiguration();

            // Create Tracker
            this.tracker = new PS3GazeTracker();
            this.tracker.InitCamera();

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
        /// This virtual method creates system and camera settings and
        /// a corresponding <see cref="OgamaClientSettingsWindow"/>
        /// to modify them.
        /// </summary>
        protected override void InitSettings()
        {
            SettingsWindow.Instance.ShowOrHideTabs(SettingsWindow.SettingsTabs.Tracking | SettingsWindow.SettingsTabs.Calibration);
            SettingsWindow.Instance.Visibility = Visibility.Collapsed;

            CameraSettingsWindow.Instance.ShowOrHideDeviceCombo(false);
            CameraSettingsWindow.Instance.Visibility = Visibility.Collapsed;

            // Listen for changes in settings and pass on to the tracker
            GTSettings.Current.Camera.OnCameraControlPropertyChanged +=
              new CameraSettings.CameraControlPropertyChangeHandler(this.CameraSettings_OnCameraControlPropertyChanged);
            GTSettings.Current.Camera.OnVideoProcAmpPropertyChanged +=
              new CameraSettings.VideoProcAmpPropertyChangeHandler(this.CameraSettings_OnVideoProcAmpPropertyChanged);
            GTSettings.Current.Camera.OnVideoControlFlagsChanged +=
                new CameraSettings.VideoControlFlagsChangeHandler(this.CameraSettings_OnVideoControlFlagsChanged);
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
        /// This method updates the underlying ps3 camera driver with the new settings.
        /// </summary>
        /// <param name="sender">Source of the event</param>
        /// <param name="e">Empty event param.</param>
        protected void cameraSettingsWindow_OnCameraChanged(object sender, RoutedEventArgs e)
        {
            ((PS3GazeTracker)this.tracker).UpdateCamera();
        }

        /// <summary>
        /// The event handler for the OnVideoProcAmpPropertyChanged event
        /// of the camera settings class.
        /// Updates the PlayStation Camera with the new properties.
        /// </summary>
        /// <param name="property">The <see cref="DirectShowLib.VideoProcAmpProperty"/>
        /// to be updated.</param>
        /// <param name="value">An <see cref="Int32"/> with the new value.</param>
        private void CameraSettings_OnVideoProcAmpPropertyChanged(DirectShowLib.VideoProcAmpProperty property, int value)
        {
            switch (property)
            {
                case DirectShowLib.VideoProcAmpProperty.BacklightCompensation:
                    break;
                case DirectShowLib.VideoProcAmpProperty.Brightness:
                    ((PS3GazeTracker)this.tracker).PlayStationEyeCamera.LensBrightness = value;
                    break;
                case DirectShowLib.VideoProcAmpProperty.ColorEnable:
                    break;
                case DirectShowLib.VideoProcAmpProperty.Contrast:
                    break;
                case DirectShowLib.VideoProcAmpProperty.Gain:
                    ((PS3GazeTracker)this.tracker).PlayStationEyeCamera.Gain = value;
                    break;
                case DirectShowLib.VideoProcAmpProperty.Gamma:
                    break;
                case DirectShowLib.VideoProcAmpProperty.Hue:
                    break;
                case DirectShowLib.VideoProcAmpProperty.Saturation:
                    break;
                case DirectShowLib.VideoProcAmpProperty.Sharpness:
                    break;
                case DirectShowLib.VideoProcAmpProperty.WhiteBalance:
                    ((PS3GazeTracker)this.tracker).PlayStationEyeCamera.WhiteBalanceBlue = value;
                    ((PS3GazeTracker)this.tracker).PlayStationEyeCamera.WhiteBalanceGreen = value;
                    ((PS3GazeTracker)this.tracker).PlayStationEyeCamera.WhiteBalanceRed = value;
                    break;
            }
        }

        /// <summary>
        /// The event handler for the OnCameraControlPropertyChanged event
        /// of the camera settings class.
        /// Updates the PlayStation Camera with the new properties.
        /// </summary>
        /// <param name="property">The <see cref="DirectShowLib.CameraControlProperty"/>
        /// to be updated.</param>
        /// <param name="value">An <see cref="Int32"/> with the new value.</param>
        private void CameraSettings_OnCameraControlPropertyChanged(DirectShowLib.CameraControlProperty property, int value)
        {
            switch (property)
            {
                case DirectShowLib.CameraControlProperty.Exposure:
                    ((PS3GazeTracker)this.tracker).PlayStationEyeCamera.Exposure = value;
                    break;
                case DirectShowLib.CameraControlProperty.Focus:
                    break;
                case DirectShowLib.CameraControlProperty.Iris:
                    break;
                case DirectShowLib.CameraControlProperty.Pan:
                    break;
                case DirectShowLib.CameraControlProperty.Roll:
                    break;
                case DirectShowLib.CameraControlProperty.Tilt:
                    break;
                case DirectShowLib.CameraControlProperty.Zoom:
                    ((PS3GazeTracker)this.tracker).PlayStationEyeCamera.Zoom = value;
                    break;
            }
        }

        /// <summary>
        /// The event handler for the OnVideoControlFlagsChanged event
        /// of the camera settings class.
        /// Updates the PlayStation Camera with the new properties.
        /// </summary>
        /// <param name="property">The <see cref="DirectShowLib.VideoControlFlags"/>
        /// to be updated.</param>
        /// <param name="value">An <see cref="Int32"/> with the new value.</param>
        private void CameraSettings_OnVideoControlFlagsChanged(DirectShowLib.VideoControlFlags property, int value)
        {
            switch (property)
            {
                case DirectShowLib.VideoControlFlags.ExternalTriggerEnable:
                    break;
                case DirectShowLib.VideoControlFlags.FlipHorizontal:
                    ((PS3GazeTracker)this.tracker).PlayStationEyeCamera.HorizontalFlip = value == 1 ? true : false;
                    break;
                case DirectShowLib.VideoControlFlags.FlipVertical:
                    ((PS3GazeTracker)this.tracker).PlayStationEyeCamera.VerticalFlip = value == 1 ? true : false;
                    break;
                case DirectShowLib.VideoControlFlags.Trigger:
                    break;
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
        /// This method shows a MessageBox that 
        /// describes that there is no PS3 tracking camera found.
        /// </summary>
        private void ShowMessageNoCamera()
        {
            MessageBox.Show("No PlayStation3 camera devices seem to be connected to the system.");
        }

        #endregion //METHODS

        ///////////////////////////////////////////////////////////////////////////////
        // Small helping Methods                                                     //
        ///////////////////////////////////////////////////////////////////////////////
        #region HELPER
        #endregion //HELPER
    }
}
