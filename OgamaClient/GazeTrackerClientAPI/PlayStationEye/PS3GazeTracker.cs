// <copyright file="PS3GazeTracker.cs" company="FU Berlin">
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
    using System.Threading;
    using System.Windows;
    using CLEyeMulticam;
    using GazeTrackingLibrary;
    using GazeTrackingLibrary.Logging;
    using GazeTrackingLibrary.Settings;
    using GazeTrackingLibrary.Camera;
    using GTCommons;

    /// <summary>
    /// This class derives from <see cref="Tracker"/> and implements
    /// the PlayStation3 camera as an input device without the need
    /// for using DirectShow.
    /// This boosts up performance for this camera about factor 3.
    /// </summary>
    public class PS3GazeTracker : Tracker
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
        /// The PlayStation3 camera device.
        /// </summary>
        private CLEyeCameraDevice playStationEyeCamera;

        #endregion //FIELDS

        ///////////////////////////////////////////////////////////////////////////////
        // Construction and Initializing methods                                     //
        ///////////////////////////////////////////////////////////////////////////////
        #region CONSTRUCTION

        /// <summary>
        /// Initializes a new instance of the PS3GazeTracker class.
        /// </summary>
        public PS3GazeTracker()
            : base(GTCommands.Instance)
        {
        }

        #endregion //CONSTRUCTION

        ///////////////////////////////////////////////////////////////////////////////
        // Defining events, enums, delegates                                         //
        ///////////////////////////////////////////////////////////////////////////////
        #region EVENTS
        #endregion EVENTS

        ///////////////////////////////////////////////////////////////////////////////
        // Defining Properties                                                       //
        ///////////////////////////////////////////////////////////////////////////////
        #region PROPERTIES

        /// <summary>
        /// Gets the <see cref="CLEyeCameraDevice"/> of the
        /// underlying PlayStation3 camera.
        /// </summary>
        public CLEyeCameraDevice PlayStationEyeCamera
        {
            get { return this.playStationEyeCamera; }
        }

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
        /// Overridden. Initializes the first found connected
        /// PlayStation3 camera as an input device for
        /// the ITU GazeTracker.
        /// </summary>
        public override void InitCamera()
        {
            // Query for number of connected cameras
            int numCameras = CLEyeCameraDevice.CameraCount;
            if (numCameras == 0)
            {
                MessageBox.Show("Could not find any PS3Eye cameras!");
                return;
            }

            // Create cameras, set some parameters and start capture
            if (numCameras >= 1)
            {
                this.playStationEyeCamera = new CLEyeCameraDevice();
                this.playStationEyeCamera.Framerate = 30;
                this.playStationEyeCamera.Resolution = CLEyeCameraResolution.CLEYE_VGA;
                this.playStationEyeCamera.ColorMode = CLEyeCameraColorMode.CLEYE_GRAYSCALE;
                this.playStationEyeCamera.Create(CLEyeCameraDevice.CameraUUID(0));
                this.playStationEyeCamera.FrameCaptureComplete +=
                  new CLEyeCameraDevice.FrameCapHandler(this.PlayStationEyeCamera_FrameCaptureComplete);

                GTSettings.Current.Camera.ZoomDefault = this.playStationEyeCamera.Zoom;
                GTSettings.Current.Camera.ZoomMinimum = -500;
                GTSettings.Current.Camera.ZoomMaximum = 500;
                GTSettings.Current.Camera.FocusAuto = this.playStationEyeCamera.AutoGain;
                GTSettings.Current.Camera.ExposureAuto = this.playStationEyeCamera.AutoExposure;
                GTSettings.Current.Camera.ExposureDefault = this.playStationEyeCamera.Exposure;
                GTSettings.Current.Camera.ExposureMinimum = -500;
                GTSettings.Current.Camera.ExposureMaximum = 500;
                GTSettings.Current.Camera.FocusDefault = this.playStationEyeCamera.Gain;
                GTSettings.Current.Camera.FocusMinimum = 0;
                GTSettings.Current.Camera.FocusMaximum = 75;
                GTSettings.Current.Camera.BrightnessDefault = this.playStationEyeCamera.LensBrightness;
                GTSettings.Current.Camera.BrightnessMinimum = -500;
                GTSettings.Current.Camera.BrightnessMaximum = 500;
            }
        }

        public void UpdateCamera()
        {
            int device = GTSettings.Current.Camera.DeviceNumber;
            int mode = GTSettings.Current.Camera.DeviceMode;
            CamSizeFPS camInfo = Devices.Current.Cameras[device].SupportedSizesAndFPS[mode];
            this.playStationEyeCamera.Resolution = 
                camInfo.Width == 640 ? CLEyeMulticam.CLEyeCameraResolution.CLEYE_VGA : CLEyeMulticam.CLEyeCameraResolution.CLEYE_QVGA;
            this.playStationEyeCamera.Framerate = camInfo.FPS;
            this.playStationEyeCamera.ResetDevice();
        }

        /// <summary>
        /// Starts the capture graph, that is sending captured frames for processing
        /// </summary>
        /// <returns>True if succesfull, otherwise false.</returns>
        public override bool Run()
        {
            this.playStationEyeCamera.Start();
            return true;
        }

        /// <summary>
        /// Overridden. Performs custom cleanup
        /// to destroy the camera.
        /// </summary>
        public override void Cleanup()
        {
            base.Cleanup();
            if (this.playStationEyeCamera != null)
            {
                this.playStationEyeCamera.Stop();
                this.playStationEyeCamera.Destroy();
            }
        }

        #endregion //OVERRIDES

        ///////////////////////////////////////////////////////////////////////////////
        // Eventhandler                                                              //
        ///////////////////////////////////////////////////////////////////////////////
        #region EVENTHANDLER

        /// <summary>
        /// The event handler for the FrameCaptureComplete event
        /// of the PlayStation camera device CLEyeCameraDevice.
        /// Sends the new frame to the processing tree of the tracker class.
        /// </summary>
        /// <param name="newFrame">An OpenCV image with the new frame.</param>
        private void PlayStationEyeCamera_FrameCaptureComplete(Emgu.CV.IImage newFrame)
        {
            this.ImgCounter++;

            switch (this.playStationEyeCamera.ColorMode)
            {
                case CLEyeCameraColorMode.CLEYE_GRAYSCALE:
                    this.ImageProcessing.NewImage(
                      (Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>)newFrame,
                      Emgu.CV.CvEnum.FLIP.NONE);
                    break;
                case CLEyeCameraColorMode.CLEYE_COLOR:
                    this.ImageProcessing.NewImage(
                      (Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>)newFrame,
                      Emgu.CV.CvEnum.FLIP.NONE);
                    break;
            }

            if (!this.ImageProcessing.ProcessImage())
            {
                if (this.Calibrating)
                {
                    this.SaveCalibInfo();
                }

                if (this.Calibration.calibrated)
                {
                    this.CalculateGazeCoordinates();
                }
            }

            // Raise OnFrameCaptureComplete event (UI listens for updating video stream)
            try
            {
                this.OnFrameCaptureComplete();
            }
            catch (ThreadInterruptedException ex)
            {
                ErrorLogger.RaiseGazeTrackerMessage("An error occured in Tracker.cs (ThreadInterruptedException). Message: " + ex.Message);
            }
            catch (Exception we)
            {
                ErrorLogger.ProcessException(we, false);
            }
        }

        #endregion //EVENTHANDLER

        ///////////////////////////////////////////////////////////////////////////////
        // Methods and Eventhandling for Background tasks                            //
        ///////////////////////////////////////////////////////////////////////////////
        #region THREAD
        #endregion //THREAD

        ///////////////////////////////////////////////////////////////////////////////
        // Methods for doing main class job                                          //
        ///////////////////////////////////////////////////////////////////////////////
        #region PRIVATEMETHODS
        #endregion //PRIVATEMETHODS

        ///////////////////////////////////////////////////////////////////////////////
        // Small helping Methods                                                     //
        ///////////////////////////////////////////////////////////////////////////////
        #region HELPER
        #endregion //HELPER
    }
}
