// <copyright file="CameraControl.cs" company="ITU">
// ******************************************************
// GazeTrackingLibrary for ITU GazeTracker
// Copyright (C) 2010 Martin Tall  
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
// <author>Martin Tall</author>
// <email>tall@stanford.edu</email>
// <modifiedby>Adrian Voßkühler</modifiedby>

using System;
using System.Drawing;
using System.Windows.Interop;
using DirectShowLib;
using Emgu.CV;
using Emgu.CV.Structure;
using GazeTrackingLibrary.Logging;
using GazeTrackingLibrary.Settings;
using GazeTrackingLibrary.Utils;
using UC480Cam;

namespace GazeTrackingLibrary.Camera
{
    public class CameraControl
    {
        #region Variables

        #region AdjustROIDirectionEnum enum

        public enum AdjustROIDirectionEnum
        {
            Up,
            Down,
            Left,
            Right,
        }

        #endregion

        #region ROIModeEnum enum

        public enum ROIModeEnum
        {
            Software,
            Hardware,
        }

        #endregion

        private static CameraControl instance;

        private readonly UC480Control uc480; // Special camera class that needs to be initialized
        private Capture directShowCapture;
        private bool isROISet;
        private bool isUsingUC480;

        private ROIModeEnum roiMode = ROIModeEnum.Software;

        #endregion //FIELDS

        #region Events

        #region Delegates

        public delegate void FrameCapHandler(Image<Gray, byte> newFrame);

        #endregion

        public event FrameCapHandler FrameCaptureComplete;

        #endregion

        #region Constructor

        private CameraControl()
        {
            isUsingUC480 = DeterminUC480Connected();

            if (isUsingUC480)
            {
                uc480 = new UC480Control();
                uc480.FrameCaptureComplete += uc480_FrameCaptureComplete;
            }
        }

        /// <summary>
        /// Initializes a new instance of the CameraControl class using
        /// the specified device and mode.
        /// </summary>
        /// <param name="devNumber">Index of the capture device to be used.</param>
        /// <param name="devMode">Index of the capture mode to be used.</param>
        private CameraControl(int devNumber, int devMode)
        {
            if (CameraExists())
            {
                directShowCapture = new Capture();
                SetCamera(devNumber, devMode);
                directShowCapture.FrameCaptureComplete += directShow_FrameCaptureComplete;
            }
        }


        private void directShow_FrameCaptureComplete()
        {
            if (FrameCaptureComplete != null)
                FrameCaptureComplete(directShowCapture.VideoImage);
        }

        private void uc480_FrameCaptureComplete()
        {
            if (FrameCaptureComplete != null)
                FrameCaptureComplete(uc480.VideoImage);
        }

        #endregion //CONSTRUCTION

        #region Events

        #region Delegates

        public delegate void ROIChangeHandler(Rectangle newROI);

        #endregion

        public event ROIChangeHandler ROIChange;

        #endregion

        #region Get/Set properties

        public static CameraControl Instance
        {
            get
            {
                if (instance == null)
                    instance = new CameraControl();

                return instance;
            }
        }

        public Rectangle ROI
        {
            get { return GetROI(); }
            set { SetROI(value); }
        }

        public bool IsROISet
        {
            get {                 
                
                if(isUsingUC480 && uc480 != null)
                   return uc480.IsROISet;
                else
                    return false;
            }
        }

        public ROIModeEnum ROIMode
        {
            get { return roiMode; }
            set { roiMode = value; }
        }

        public Capture Capture
        {
            get { return directShowCapture; }
        }

        public bool UsingUC480
        {
            get { return isUsingUC480; }
            set { isUsingUC480 = value; }
        }

        public int Width
        {
            get
            {
                if (isUsingUC480)
                    if (uc480.ROI.Y != 0)
                        return uc480.ROI.Width;
                    else
                        return uc480.Width;
                return Capture.Width;
            }
        }

        public int Height
        {
            get
            {
                if (isUsingUC480)
                    if (uc480.ROI.Y != 0)
                        return uc480.ROI.Height;
                    else
                        return uc480.Height;
                return Capture.Height;
            }
        }

        public int FPS
        {
            get
            {
                if (isUsingUC480 == false)
                    return directShowCapture.FPS;
                return uc480.FPS;
            }
        }

        public string DeviceName
        {
            get
            {
                if (isUsingUC480)
                    return uc480.GetDeviceName();
                return directShowCapture.DeviceName;
            }
        }

        #endregion //PROPERTIES

        #region Public methods

        public bool CameraExists()
        {
            if (isUsingUC480)
                return true;
            else
            {
                DsDevice[] capDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
                if (capDevices != null) return capDevices.Length != 0;
            }
            return false;
        }

        public void SetCamera(int deviceNumber, int deviceMode)
        {
            if (isUsingUC480)
                return; // use dedicated SDK

            try
            {
                if (directShowCapture == null)
                {
                    directShowCapture = new Capture();
                    directShowCapture.FrameCaptureComplete += directShow_FrameCaptureComplete;
                }

                // Specific deviceMode (e.g. 800x600 @ 30fps)
                if (deviceMode > -1)
                {
                    directShowCapture.NewCamera(
                        Devices.Current.Cameras[deviceNumber].DirectshowDevice,
                        25,
                        // Devices.Current.Cameras[deviceNumber].SupportedSizesAndFPS[deviceMode].FPS,
                        Devices.Current.Cameras[deviceNumber].SupportedSizesAndFPS[deviceMode].Width,
                        Devices.Current.Cameras[deviceNumber].SupportedSizesAndFPS[deviceMode].Height);
                }
                else
                {
                    directShowCapture.NewCamera(
                        Devices.Current.Cameras[deviceNumber].DirectshowDevice,
                        0,
                        0,
                        0); // No specific deviceMode, try default
                    deviceMode = 0;
                }

                isROISet = false;
            }
            catch (Exception ex)
            {
                ErrorLogger.ProcessException(ex, false);
            }

            // Update settings with new device
            GTSettings.Current.Camera.DeviceNumber = deviceNumber;
            GTSettings.Current.Camera.DeviceMode = deviceMode;

            if (directShowCapture != null)
                if (!directShowCapture.HasValidGraph)
                {
                    string message = "The " +
                                     Devices.Current.Cameras[GTSettings.Current.Camera.DeviceNumber].Name
                                     + " camera could not be initialized (Maybe it is already in use)."
                                     + Environment.NewLine + "If there is another device, we will try the next one.";
                    ErrorLogger.WriteLine(message);
                }
        }

        public void SetupUC480(HwndSource hwndSource)
        {
            if (isUsingUC480 && uc480 != null)
                uc480.SetupWndProc(hwndSource);
        }

        public bool Start()
        {
            try
            {
                if (isUsingUC480 == false)
                    directShowCapture.Start();

                return true;
            }
            catch (Exception ex)
            {
                ErrorLogger.ProcessException(ex, false);
                return false;
            }
        }

        public Rectangle GetROI()
        {
            if (isUsingUC480 == false && directShowCapture != null)
                return directShowCapture.ROI;
            else
                return uc480.ROI;
        }

        public void SetROI(Rectangle newROI)
        {
            if(isUsingUC480 && uc480 != null)
            {
                var fullCap = new Rectangle(new Point(0, 0), new Size(1280, 1024));
                if (Operations.IsWithinBounds(newROI, fullCap))
                {
                    switch(GTSettings.Current.Processing.TrackingMethod)
                    {
                        case TrackingMethodEnum.RemoteBinocular:
                            uc480.SetROI(newROI, 200);
                            uc480.ToggleGainBoost(true);
                            break;

                        case TrackingMethodEnum.RemoteMonocular:
                            uc480.SetROI(newROI, 500);
                            uc480.ToggleGainBoost(true);
                            break;
                    }
                }
            }

            //if (ROIChange != null)
            //    ROIChange(newROI);
        }

        public void AdjustROI(AdjustROIDirectionEnum adjustROIDirectionEnum, int value)
        {
            // If image is flipped, reverse flip the adjustment command
            if (GTSettings.Current.Camera.FlipImage)
            {
                if (adjustROIDirectionEnum == AdjustROIDirectionEnum.Down)
                    adjustROIDirectionEnum = AdjustROIDirectionEnum.Up;
                else if (adjustROIDirectionEnum == AdjustROIDirectionEnum.Up)
                    adjustROIDirectionEnum = AdjustROIDirectionEnum.Down;
                else if (adjustROIDirectionEnum == AdjustROIDirectionEnum.Left)
                    adjustROIDirectionEnum = AdjustROIDirectionEnum.Right;
                else if (adjustROIDirectionEnum == AdjustROIDirectionEnum.Right)
                    adjustROIDirectionEnum = AdjustROIDirectionEnum.Left;
            }


            Rectangle adjustedROI = Instance.ROI;

            switch (adjustROIDirectionEnum)
            {
                case AdjustROIDirectionEnum.Down:
                    adjustedROI.Y = ROI.Y - value;
                    break;

                case AdjustROIDirectionEnum.Up:
                    adjustedROI.Y = ROI.Y + value; //hmm. are we flipped here?
                    break;

                case AdjustROIDirectionEnum.Left:
                    adjustedROI.X = ROI.X - value;
                    break;

                case AdjustROIDirectionEnum.Right:
                    adjustedROI.X = ROI.X + value;
                    break;
            }

            Instance.ROI = adjustedROI;
        }

        public void ClearROI()
        {
            isROISet = false;

            //if (roiMode == ROIModeEnum.Software)
            //    CameraControl.Instance.ROI = new System.Drawing.Rectangle(new Point(0, 0), new Size(this.Width, this.Height));
            //else

            if(isUsingUC480 && uc480 != null)
               uc480.ClearROI();
        }

        public void Cleanup()
        {
            if (directShowCapture != null)
            {
                // Check for camera is null because an derived class can have its own camera
                if (Instance.Capture != null && Instance.CameraExists())
                {
                    directShowCapture.Pause();
                    directShowCapture.Dispose();
                }
            }

            if (isUsingUC480 && uc480 != null)
            {
                uc480.Stop();
                uc480.Cleanup();
            }
        }

        #endregion //PUBLICMETHODS

        #region Private methods

        private static bool DeterminUC480Connected()
        {
            try
            {
                DsDevice[] capDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

                foreach (DsDevice dev in capDevices)
                {
                    if (dev.Name.Length >= 5 && dev.Name.Substring(0, 5) == "uc480")
                        return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #endregion

    }
}