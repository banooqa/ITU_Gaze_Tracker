// <copyright file="VideoViewer.xaml.cs" company="ITU">
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
using System.Windows;
using System.Windows.Input;
using GazeTrackerUI.Settings;
using GazeTrackerUI.Tools;
using GazeTrackingLibrary.Settings;
using GazeTrackingLibrary.Utils;
using GTCommons;

namespace GazeTrackerUI.TrackerViewer
{
    public partial class VideoViewer : Window
    {
        #region Variables

        private static VideoViewer instance;

        #endregion

        #region Constructor

        private VideoViewer()
        {
            ComboBoxBackgroundColorFix.Initialize();
            InitializeComponent();
            menuBarIcons.IsDetachVideoVisible = false;
            RegisterForEvents();
        }


        private void RegisterForEvents()
        {
            Activated += WindowActivated;
            Deactivated += WindowDeactivated;
            SizeChanged += WindowSizeChanged;
        }


        private void OnSettings(object sender, RoutedEventArgs e)
        {
            if (SettingsWindow.Instance.Visibility == Visibility.Visible)
                SettingsWindow.Instance.Visibility = Visibility.Collapsed;
            else
                SettingsWindow.Instance.Visibility = Visibility.Visible;

            UpdateSettingsWindowPosition();
        }


        private void OnAutoTune(object sender, RoutedEventArgs e)
        {
        }


        private void OnNetworkClient(object sender, RoutedEventArgs e)
        {
        }

        #endregion //CONSTRUCTION

        #region Get/Set

        public static VideoViewer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new VideoViewer();
                }

                return instance;
            }
        }

        public VideoImageControl VideoImageControl
        {
            get { return this.VideoImageControl; }
        }

        public VideoModeEnum VideoMode
        {
            get { return GTSettings.Current.Visualization.VideoMode; }
            set { GTSettings.Current.Visualization.VideoMode = value; }
        }

        public bool HasBeenResized { get; set; }

        #endregion //PROPERTIES

        #region Public methods

        /// <summary>
        /// Sets the size and labels.
        /// </summary>
        /// <param name="imgWidth">Width of the img.</param>
        /// <param name="imgHeight">Height of the img.</param>
        /// <param name="fps">The Frames Per Second.</param>
        public void SetSizeAndLabels(int imgWidth, int imgHeight, int fps)
        {
            // Size
            videoImageControl.VideoImageWidth = imgWidth;
            videoImageControl.VideoImageHeight = imgHeight;

            Width = imgWidth + videoImageControl.Margin.Left + videoImageControl.Margin.Right;
            Height = imgHeight + videoImageControl.Margin.Top + videoImageControl.Margin.Bottom;

            // Label
            if (fps == 0)
            {
                LabelResolution.Content = "Native resolution: " + imgWidth + " x " + imgHeight;
            }
            else
            {
                LabelResolution.Content = "Native resolution: " + imgWidth + " x " + imgHeight + " @ " + fps +" frames per second";
            }
        }


        public void ShowWindow(int width, int height)
        {
            try
            {
                Width = width + videoImageControl.Margin.Left + videoImageControl.Margin.Right;
                Height = height + videoImageControl.Margin.Top + videoImageControl.Margin.Bottom;
                Show();
                videoImageControl.Overlay.Visibility = Visibility.Visible;
            }
            catch (Exception)
            {

            }
        }

        #endregion //PUBLICMETHODS

        #region Private methods


        #region UpdateWindowPosition

        private static void UpdateSettingsWindowPosition()
        {
            //if (SettingsWindow.Instance.HasBeenMoved)
            //    return;

            //SettingsWindow.Instance.Left = this.Left + this.Width - SettingsWindow.Instance.Width - 18;
            //SettingsWindow.Instance.Top = this.Top + this.videoImageControl.VideoImageHeight - SettingsWindow.Instance.Height - 2;
            //SettingsWindow.Instance.Topmost = true;
        }

        private void UpdateEyetrackerSettingsWindowPosition()
        {
            if (EyetrackerSettingsWindow.Instance.HasBeenMoved) return;
            EyetrackerSettingsWindow.Instance.Left = Left + 18;
            EyetrackerSettingsWindow.Instance.Top = Top + this.videoImageControl.VideoImageHeight -
                                                    EyetrackerSettingsWindow.Instance.Height - 2;
            EyetrackerSettingsWindow.Instance.Topmost = true;
        }

        #endregion

        #region Window-events

        private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Size prevSize = e.PreviousSize;
            Size newSize = e.NewSize;

            if (prevSize != newSize)
            {
                videoImageControl.VideoImageWidth = Convert.ToInt32(newSize.Width - videoImageControl.Margin.Left - videoImageControl.Margin.Right);
                videoImageControl.VideoImageHeight = Convert.ToInt32(newSize.Height - videoImageControl.Margin.Top - videoImageControl.Margin.Bottom);
                videoImageControl.UpdateLayout();
                
                UpdateSettingsWindowPosition();
                UpdateEyetrackerSettingsWindowPosition();
            }
        }

        private void WindowActivated(object sender, EventArgs e)
        {
            if (WindowState.Equals(WindowState.Normal))
            {
                videoImageControl.Start();
            }
        }

        private void WindowDeactivated(object sender, EventArgs e)
        {
            if (WindowState.Equals(WindowState.Minimized))
            {
                videoImageControl.Stop();
            }
        }

        private void WindowHide(object sender, MouseButtonEventArgs e)
        {
            videoImageControl.Stop();
            videoImageControl.Overlay.Visibility = Visibility.Collapsed;
            Visibility = Visibility.Collapsed;
        }

        #endregion

        #region DragWindow

        /// <summary>
        /// Enters the move window.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void EnterMoveWindow(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Exits the move window.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ExitMoveWindow(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// Drags the window.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void DragWindow(object sender, MouseButtonEventArgs args)
        {
            DragMove();
        }

        #endregion

        #endregion //PRIVATEMETHODS
    }
}