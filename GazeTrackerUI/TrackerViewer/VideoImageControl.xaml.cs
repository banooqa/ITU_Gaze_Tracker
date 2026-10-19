// <copyright file="VideoImageControl.xaml.cs" company="ITU">
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
// <email>info@martintall.com</email>

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.UI;
using GazeTrackingLibrary;
using GazeTrackingLibrary.Logging;
using GazeTrackingLibrary.Settings;
using GazeTrackingLibrary.Utils;
using System.Diagnostics;
using System.Windows.Media;
using System.Management;
using System.Management.Instrumentation;

namespace GazeTrackerUI.TrackerViewer
{
    public partial class VideoImageControl
    {
        #region CONSTANTS

        private const int DefaultVideoImageWidth = 380;
        private const int DefaultVideoImageHeight = 200;

        #endregion //CONSTANTS

        #region Variables

        //private Emgu.CV.IImage image;

        private bool isReadyToRender;
        private bool isRendering;
        private Tracker tracker;
        private VideoImageOverlay overlay;

        #endregion //FIELDS

        #region CONSTRUCTION


        public VideoImageControl()
        {
            InitializeComponent();

            VideoImageHeight = DefaultVideoImageHeight;
            VideoImageWidth = DefaultVideoImageWidth;
            isReadyToRender = true;
            isRendering = false;

            pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;

            this.LayoutUpdated += new EventHandler(VideoImageControl_LayoutUpdated);
            this.pictureBox.MouseEnter += new EventHandler(pictureBox_MouseEnter);
            this.pictureBox.MouseLeave += new EventHandler(pictureBox_MouseLeave);
        }

        void pictureBox_MouseLeave(object sender, EventArgs e) 
        {
            if(overlay != null)
               overlay.GridPerformanceCounters.Visibility = System.Windows.Visibility.Collapsed;
        }

        void pictureBox_MouseEnter(object sender, EventArgs e) 
        {
            if(overlay != null)
               overlay.GridPerformanceCounters.Visibility = System.Windows.Visibility.Visible;
        }



        void VideoImageControl_LayoutUpdated(object sender, EventArgs e) 
        {
            if(overlay != null)
            {
                overlay.Width = this.Width;
                overlay.Height = this.Height;
                overlay.Top = PointToScreen(new Point(0, 0)).Y;
                overlay.Left = PointToScreen(new Point(0, 0)).X;
            }
        }



        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="VideoImageControl"/> is reclaimed by garbage collection.
        /// </summary>
        ~VideoImageControl()
        {
            Stop();
        }

        #endregion //CONSTRUCTION

        #region Properties

        /// <summary>
        /// Gets the CV image box.
        /// </summary>
        /// <value>The CV image box.</value>
        public ImageBox CVImageBox
        {
            get { return pictureBox; }
        }

        /// <summary>
        /// Gets or sets the tracker.
        /// </summary>
        /// <value>The tracker.</value>
        public Tracker Tracker
        {
            get { return tracker; }
            set { tracker = value; }
        }

        /// <summary>
        /// Sets the width of the video image.
        /// </summary>
        /// <value>The width of the video image.</value>
        public int VideoImageWidth
        {
            set { pictureBox.Width = value; }
            get { return pictureBox.Width; }
        }

        /// <summary>
        /// Sets the height of the video image.
        /// </summary>
        /// <value>The height of the video image.</value>
        public int VideoImageHeight
        {
            set { pictureBox.Height = value; }
            get { return pictureBox.Height; }
        }

        public VideoImageOverlay Overlay
        {
            get { return this.overlay; }
        }

        public bool VideoOverlayTopMost
        {
            set
            {
                if(overlay != null)
                    overlay.Topmost = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is native resolution.
        /// </summary>
        /// <value>
        /// <c>true</c> if running at native resolution; otherwise, <c>false</c>.
        /// </value>
        //public bool IsNativeResolution
        //{
        //    get { return this.isNativeResolution; }
        //    set { this.isNativeResolution = value; }
        //}

        #endregion //PROPERTIES

        #region Public methods

        public void Start()
        {
            if (isRendering) return;

            if (tracker != null)
            {
                tracker.OnProcessedFrame += Tracker_FrameCaptureComplete;
                isRendering = true;

                if(overlay == null)
                {
                    overlay = new VideoImageOverlay();
                    overlay.Width = this.VideoImageWidth;
                    overlay.Height = this.VideoImageHeight;
                    overlay.Top = PointToScreen(new Point(0, 0)).Y;
                    overlay.Left = PointToScreen(new Point(0, 0)).X;
                    overlay.Show();
                }

                overlay.Topmost = true;
            }
        }

        public void Stop()
        {
            if (isRendering)
            {
                if (tracker != null)
                {
                    tracker.OnProcessedFrame -= Tracker_FrameCaptureComplete;
                    isRendering = false;

                    this.Dispatcher.BeginInvoke(
                        new MethodInvoker(
                            delegate
                            {
                                overlay.Topmost = false;                           
                            }));
                
                }
            }
        }


        #endregion

        #region Eventhandler

        private void Tracker_FrameCaptureComplete(object sender, EventArgs args)
        {
            // Don't draw while calibrating to obtain maximum images
            if (tracker.IsCalibrating)
                return;

            try
            {
                if (isReadyToRender)
                {
                    isReadyToRender = false;

                    if (pictureBox.InvokeRequired)
                    {
                        pictureBox.BeginInvoke(
                            new MethodInvoker(
                                delegate
                                {
                                    UpdateImage();
                                    if(overlay != null)
                                       overlay.performanceCountersUC.Update(tracker.FPSVideo, tracker.FPSTracking);
                                 }));
                    }
                    else
                    {
                       UpdateImage();
                        if(overlay != null)
                           overlay.performanceCountersUC.Update(tracker.FPSVideo, tracker.FPSTracking);
                    }

                    isReadyToRender = true;
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.ProcessException(ex, false);
            }
        }
        #endregion

        #region Private methods

        private void UpdateImage() 
        {
             if (GTSettings.Current.Visualization.VideoMode == VideoModeEnum.Processed)
                 pictureBox.Image = tracker.GetProcessedImage();
             else
                 pictureBox.Image = tracker.GetOriginalImage();
        }

        #endregion

    }
}