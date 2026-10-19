// <copyright file="EyeVideoControl.cs" company="FU Berlin">
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
    using System.Windows.Forms;
    using GazeTrackingLibrary;
    using GazeTrackingLibrary.Logging;
    using GazeTrackingLibrary.Settings;
    using System.ComponentModel;

    /// <summary>
    /// This WinForms <see cref="UserControl"/> is created to be integrated
    /// into OGAMAs recording module displaying the current track status,
    /// resp. the processed eye video of the ITU GazeTracker
    /// </summary>
    public partial class EyeVideoControl : UserControl
    {
        ///////////////////////////////////////////////////////////////////////////////
        // Defining Constants                                                        //
        ///////////////////////////////////////////////////////////////////////////////
        #region CONSTANTS

        private const int defaultVideoImageWidth = 240;
        private const int defaultVideoImageHeight = 180;

        #endregion //CONSTANTS

        ///////////////////////////////////////////////////////////////////////////////
        // Defining Variables, Enumerations, Events                                  //
        ///////////////////////////////////////////////////////////////////////////////
        #region FIELDS

        /// <summary>
        /// This hosts the <see cref="Tracker"/> thats processing
        /// image should be displayed
        /// </summary>
        private Tracker tracker;

        /// <summary>
        /// The OpenCV image control that displays the image.
        /// </summary>
        private Emgu.CV.IImage image;

        /// <summary>
        /// Flag, indicating whether this control should display
        /// the video in native resolution (don´t resize to 
        /// fit image control)
        /// </summary>
        private bool isNativeResolution;

        /// <summary>
        /// Flag. Indicating successfull finish of new
        /// image frame rendering.
        /// </summary>
        private bool isReadyToRender;

        private bool isRendering;

        #endregion //FIELDS

        ///////////////////////////////////////////////////////////////////////////////
        // Construction and Initializing methods                                     //
        ///////////////////////////////////////////////////////////////////////////////
        #region CONSTRUCTION

        /// <summary>
        /// Initializes a new instance of the EyeVideoControl class.
        /// </summary>
        public EyeVideoControl()
        {
            this.InitializeComponent();
            this.VideoImageHeight = defaultVideoImageHeight;
            this.VideoImageWidth = defaultVideoImageWidth;
            this.isReadyToRender = true;
            this.isNativeResolution = false;
            this.isRendering = false;
        }

        /// <summary>
        /// Finalizes an instance of the EyeVideoControl class.
        /// </summary>
        ~EyeVideoControl()
        {
            this.Stop();
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
        /// Gets or sets the <see cref="Tracker"/> thats processing
        /// image should be displayed
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Tracker Tracker
        {
            get { return this.tracker; }
            set { this.tracker = value; }
        }

        /// <summary>
        /// Gets the OpenCV image control that displays the image.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Emgu.CV.UI.ImageBox CVImageBox
        {
            get { return this.pictureBox; }
        }

        /// <summary>
        /// Sets the width of the video image.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int VideoImageWidth
        {
            set { this.pictureBox.Width = value; }
        }

        /// <summary>
        /// Sets the height of the video image.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int VideoImageHeight
        {
            set { this.pictureBox.Height = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this control should display
        /// the video in native resolution (don´t resize to 
        /// fit image control)
        /// </summary>
        public bool IsNativeResolution
        {
            get { return this.isNativeResolution; }
            set { this.isNativeResolution = value; }
        }

        #endregion //PROPERTIES

        ///////////////////////////////////////////////////////////////////////////////
        // Public methods                                                            //
        ///////////////////////////////////////////////////////////////////////////////
        #region PUBLICMETHODS

        /// <summary>
        /// Starts updating the image by subscribing to
        /// the trackers FrameCaptureComplete event.
        /// </summary>
        public void Start()
        {
            if (!this.isRendering)
            {
                if (this.tracker != null)
                {
                    tracker.FrameCaptureComplete += new EventHandler(this.Tracker_FrameCaptureComplete);
                    this.isRendering = true;
                }
            }
        }

        /// <summary>
        /// Stops updating the image by unsubscribing 
        /// from the trackers FrameCaptureComplete event.
        /// </summary>
        public void Stop()
        {
            if (this.isRendering)
            {
                if (this.tracker != null)
                {
                    tracker.FrameCaptureComplete -= new EventHandler(this.Tracker_FrameCaptureComplete);
                    this.isRendering = false;
                }
            }
        }

        #endregion //PUBLICMETHODS

        ///////////////////////////////////////////////////////////////////////////////
        // Inherited methods                                                         //
        ///////////////////////////////////////////////////////////////////////////////
        #region OVERRIDES
        #endregion //OVERRIDES

        ///////////////////////////////////////////////////////////////////////////////
        // Eventhandler                                                              //
        ///////////////////////////////////////////////////////////////////////////////
        #region EVENTHANDLER

        /// <summary>
        /// The event handler for the <see cref="Tracker.FrameCaptureComplete"/>
        /// event which updates the OpenCV image control with the new frame
        /// according to the current display settings.
        /// </summary>
        private void Tracker_FrameCaptureComplete(object sender, EventArgs args)
        {
            try
            {
                if (this.isReadyToRender)
                {
                    this.isReadyToRender = false;

                    if (this.isNativeResolution)
                    {
                        if (GTSettings.Current.Visualization.IsVideoModeProcessed)
                        {
                            this.image = this.tracker.GetProcessedImage();
                        }
                        else
                        {
                            this.image = this.tracker.GetOriginalImage();
                        }
                    }
                    else
                    {
                        if (GTSettings.Current.Visualization.IsVideoModeProcessed)
                        {
                            this.image = this.tracker.GetProcessedImage(this.Width, this.Height);
                        }
                        else
                        {
                            this.image = this.tracker.GetOriginalImage(this.Width, this.Height);
                        }
                    }

                    if (this.pictureBox.InvokeRequired)
                    {
                        this.pictureBox.BeginInvoke(new MethodInvoker(delegate
                        {
                            this.pictureBox.Image = this.image;
                        }));
                    }
                    else
                    {
                        // Emgu.CV.IImage
                        this.pictureBox.Image = this.image;
                    }

                    this.isReadyToRender = true;
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.ProcessException(ex, false);
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
