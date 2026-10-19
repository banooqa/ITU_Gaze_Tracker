// <copyright file="Tracker.cs" company="ITU">
// ******************************************************
// GazeTrackingLibrary for ITU GazeTracker
// Copyright (C) 2010 Martin Tall. All rights reserved. 
// ------------------------------------------------------------------------
// We have a dual licence, open source (GPLv3) for individuals - licence for commercial ventures.
// You may not use or distribute any part of this software in a commercial product. Contact us to arrange a licence. 
// We accept no responsibility or liability.
// </copyright>
// <author>Martin Tall</author>
// <email>info@martintall.com</email>

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using GazeTracker.Tools;
using GazeTrackerClient;
using GazeTrackerUI.Calibration;
using GazeTrackerUI.Calibration.Events;
using GazeTrackerUI.Calibration.Events;
using GazeTrackerUI.Network;
using GazeTrackerUI.Settings;
using GazeTrackerUI.Tools;
using GazeTrackerUI.TrackerViewer;
using GazeTrackingLibrary;
using GazeTrackingLibrary.Camera;
using GazeTrackingLibrary.Logging;
using GazeTrackingLibrary.Settings;
using GTCommons;
using Application = System.Windows.Application;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Point = System.Windows.Point;
using GazeTrackingLibrary.Utils;

namespace GazeTrackerUI
{

    #region Includes 

    // System classes

    // GazeTracker classes

    //using GazeTrackingLibrary.Illumination;

    #endregion


    public partial class GazeTrackerUIMainWindow
    {

        #region Variables

        private readonly CrosshairDriver crosshairDriver = new CrosshairDriver();
        private readonly MouseDriver mouseDriver = new MouseDriver();
        private readonly TCPIPServer tcpipServer = new TCPIPServer();
        private Process clientUIProc;
        private bool isRunning;
        private MessageWindow msgWindow;
        private Tracker tracker;

        #endregion


        #region Constructor 

        public GazeTrackerUIMainWindow()
        {
            // Little fix for colorschema (must run before initializing)
            ComboBoxBackgroundColorFix.Initialize();

            // Register for special error messages
            ErrorLogger.TrackerError += tracker_OnTrackerError;

            InitializeComponent();
            Loaded += GazeTrackerUIMainWindow_Loaded;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (CameraControl.Instance.UsingUC480)
                CameraControl.Instance.SetupUC480(PresentationSource.FromVisual(this) as HwndSource);
        }

        private void GazeTrackerUIMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //if (!CameraControl.Instance.CameraExists()) 
            //{
            //    ShowMessageNoCamera();
            //    this.Close();
            //} 
            //else 
            //{

            // Load GTSettings
            GTSettings.Current.LoadLatestConfiguration();

            // Create Tracker
            tracker = new Tracker(GTCommands.Instance); // Hook up commands and events to tracker
            tracker.InitCamera();

            SettingsWindow.Instance.Title = "SettingsWindow"; // Just touch it..


            // Video preview window (tracker draws visualization)
            videoImageControl.Tracker = tracker;
            videoImageControl.Start();

            // Events
            RegisterEventListners();

            // Register event listners for incoming TCP/IP commands
            RegisterForIncomingServerRequests();

            // Start TCPIP command server (if enabled)
            tcpipServer.Port = GTSettings.Current.Network.TCPIPServerPort;
            tcpipServer.IPAddress = IPAddress.Parse(GTSettings.Current.Network.TCPIPServerIPAddress);
            tcpipServer.IsEnabled = GTSettings.Current.Network.TCPIPServerEnabled;

            // Start UDP data server (if enabled)
            tracker.Server.Port = GTSettings.Current.Network.UDPServerPort;
            tracker.Server.IPAddress = IPAddress.Parse(GTSettings.Current.Network.UDPServerIPAddress);
            tracker.Server.IsEnabled = GTSettings.Current.Network.UDPServerEnabled;

            menuBarIcons.IsSettingsVisible = false;

            // Show window
            Show();

            tracker.Run();
        }

        #endregion


        private void StartStop(object sender, RoutedEventArgs e)
        {
			if (tracker.Calibration.IsCalibrated == false)
            {
                msgWindow = new MessageWindow("You need to calibrate before starting");
                msgWindow.Show();
                return;
            }

            // Starting
            if (!isRunning)
            {
                #region EyeMouse

                // Start eye mouse (register listner for gazedata events)
                if (GTSettings.Current.Processing.EyeMouseEnabled)
                {
                    if (GTSettings.Current.Processing.EyeMouseSmooth)
                        tracker.GazeDataSmoothed.GazeDataChanged += mouseDriver.Move;
                    else
                        tracker.GazeDataRaw.GazeDataChanged += mouseDriver.Move;
                }

                #endregion

                #region Crosshair

                if (GTSettings.Current.Processing.EyeCrosshairEnabled)
                {
                    crosshairDriver.Show();

                    if (GTSettings.Current.Processing.EyeMouseSmooth)
                        tracker.GazeDataSmoothed.GazeDataChanged += crosshairDriver.Move;
                    else
                        tracker.GazeDataRaw.GazeDataChanged += crosshairDriver.Move;
                }

                #endregion

                #region TCPIP Server

                // Start TCPIP command server (if enabled)
                tcpipServer.IsEnabled = GTSettings.Current.Network.TCPIPServerEnabled;

                // Start UDP data server (if enabled)
                tracker.Server.IsEnabled = GTSettings.Current.Network.UDPServerEnabled;

                // Start logging (if enabled)
                tracker.LogData.IsEnabled = GTSettings.Current.FileSettings.LoggingEnabled;

                #endregion

                BtnStartStop.Label = "Stop";

                //if(GTSettings.Current.Processing.EyeMouseEnabled)
                //    BtnStartStop.ActivationMethod = "Dwell";


                isRunning = true;
            }

                // Stopping
            else
            {
                #region EyeMouse

                // Stop eye mouse (unregister events)
                if (GTSettings.Current.Processing.EyeMouseEnabled)
                {
                    if (GTSettings.Current.Processing.EyeMouseSmooth)
                        tracker.GazeDataSmoothed.GazeDataChanged -= mouseDriver.Move;
                    else
                        tracker.GazeDataRaw.GazeDataChanged -= mouseDriver.Move;
                }

                #endregion

                #region Crosshair

                if (GTSettings.Current.Processing.EyeCrosshairEnabled)
                {
                    if (GTSettings.Current.Processing.EyeMouseSmooth)
                        tracker.GazeDataSmoothed.GazeDataChanged -= crosshairDriver.Move;
                    else
                        tracker.GazeDataRaw.GazeDataChanged -= crosshairDriver.Move;

                    crosshairDriver.Hide();
                }

                #endregion

                #region TCPIP Server

                // Stop TCPIP command server (if enabled)
                if (tcpipServer.IsEnabled)
                    tcpipServer.IsEnabled = false;

                // Stop UDP data server (if enabled)
                if (tracker.Server.IsEnabled)
                    tracker.Server.IsEnabled = false;

                if (tracker.LogData.IsEnabled)
                    tracker.LogData.IsEnabled = false; // Will stop and close filestream

                #endregion

                BtnStartStop.Label = "Start";

                //if(GTSettings.Current.Processing.EyeMouseEnabled)
                //    BtnStartStop.ActivationMethod = "Mouse";

                isRunning = false;
            }
        }


        #region Events

        private void RegisterEventListners()
        {
            #region Settings 

            GTCommands.Instance.Settings.OnSettings += OnSettings;

            #endregion

            #region Camera 

            GTCommands.Instance.Camera.OnCameraChange += OnCameraChanged;
            CameraControl.Instance.ROIChange += OnROIChange;

            #endregion

            #region TrackerViewer

            GTCommands.Instance.TrackerViewer.OnVideoDetach += OnVideoDetach;
            GTCommands.Instance.TrackerViewer.OnTrackBoxShow += OnTrackBoxShow;
            GTCommands.Instance.TrackerViewer.OnTrackBoxHide += OnTrackBoxHide;

            #endregion

            #region Calibration

            GTCommands.Instance.Calibration.OnAccepted += OnCalibrationAccepted;
            GTCommands.Instance.Calibration.OnStart += OnCalibrationStart;
            GTCommands.Instance.Calibration.OnRunning += OnCalibrationRunning;

            GTCommands.Instance.Calibration.OnPointStart += OnPointStart;
            //GTCommands.Instance.Calibration.OnPointStart += new GTCommons.Events.CalibrationPointEventArgs.CalibrationPointEventHandler(OnPointStart);
            //GTCommands.Instance.Calibration.OnPointStart += new GazeTrackerUI.Calibration.Events.CalibrationPointEventArgs.CalibrationPointEventHandler(OnPointStart);

            GTCommands.Instance.Calibration.OnAbort += OnCalibrationAbort;
            GTCommands.Instance.Calibration.OnEnd += OnCalibrationEnd;

            #endregion

            #region Misc

            GTCommands.Instance.OnNetworkClient += OnNetworkClient;

            #endregion

            #region This window

            ExpanderVisualization.Expanded += new RoutedEventHandler(ExpanderVisualization_Expanded);
            ExpanderVisualization.Collapsed += new RoutedEventHandler(ExpanderVisualization_Collapsed);
            Activated += Window1_Activated;
            Deactivated += Window1_Deactivated;


            KeyDown += KeyDownAction;

            #endregion
        }

        private void ExpanderVisualization_Collapsed(object sender, RoutedEventArgs e) 
        {
            videoImageControl.Overlay.GridVisualization.Visibility = Visibility.Collapsed;
        }

        private void ExpanderVisualization_Expanded(object sender, RoutedEventArgs e) 
        {
            videoImageControl.Overlay.GridVisualization.Visibility = Visibility.Visible;
        }


        private void tracker_OnTrackerError(string message)
        {
            msgWindow = new MessageWindow {Text = message};
            msgWindow.Show();
        }

        private void KeyDownAction(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    if (isRunning)
                        StartStop(null, null);
                    break;

                case Key.C:
                    GTCommands.Instance.Calibration.Start();
                    break;

                case Key.S:
                    if (!isRunning)
                        StartStop(null, null);
                    break;
            }
        }

        #endregion


        #region On GTCommands -> actions 

        #region Settings

        private void ShowSetupWindow(object sender, RoutedEventArgs e)
        {
            if (SettingsWindow.Instance.Visibility != Visibility.Collapsed) return;
            SettingsWindow.Instance.Visibility = Visibility.Visible;

            if (SettingsWindow.Instance.HasBeenMoved != false) return;
            SettingsWindow.Instance.Left = Left + Width + 5;
            SettingsWindow.Instance.Top = Top;
        }

        private static void OnSettings(object sender, RoutedEventArgs e)
        {
            SettingsWindow.Instance.Visibility = Visibility.Visible;
        }

        #endregion

        #region TrackerViewer

        private void OnVideoDetach(object sender, RoutedEventArgs e)
        {
            if (!CameraControl.Instance.CameraExists())
            {
                ShowMessageNoCamera();
                return;
            }

            int width = 0;
            int height = 0;

            VideoViewer.Instance.videoImageControl.Tracker = tracker;
            VideoViewer.Instance.SetSizeAndLabels(tracker.VideoWidth, tracker.VideoHeight, tracker.FPSVideo);

            // If ROI has been set display at twice the image size
            if (CameraControl.Instance.IsROISet)
            {
                width = CameraControl.Instance.ROI.Width*2;
                height = CameraControl.Instance.ROI.Height*2;
            }
            else
            {
                width = tracker.VideoWidth;
                height = tracker.VideoHeight;
            }

            int posX = Convert.ToInt32(Left - width - 5);
            int posY = Convert.ToInt32(Top);

            this.videoImageControl.VideoOverlayTopMost = false;

            VideoViewer.Instance.ShowWindow(width, height);
        }

        private void OnTrackBoxShow(object sender, RoutedEventArgs e)
        {
            //if (trackBoxUC.Visibility == Visibility.Collapsed)
            //    trackBoxUC.Visibility = Visibility.Visible;
        }

        private void OnTrackBoxHide(object sender, RoutedEventArgs e)
        {
            //if (trackBoxUC.Visibility == Visibility.Visible)
            //    trackBoxUC.Visibility = Visibility.Collapsed;
        }

        private void OnROIChange(Rectangle newROI)
        {
            //Dispatcher.Invoke
            //    (
            //        DispatcherPriority.ApplicationIdle,
            //        new Action
            //            (
            //            delegate { trackBoxUC.UpdateROI(newROI); }
            //            )
            //    );
        }

        #endregion

        #region Calibration

        private void Calibrate(object sender, RoutedEventArgs e)
        {
            GTCommands.Instance.Calibration.Start();
            videoImageControl.VideoOverlayTopMost = false;
        }

        private void OnCalibrationStart(object sender, RoutedEventArgs e)
        {
            CalibrationWindow.Instance.Reset();
            CalibrationWindow.Instance.Tracker = tracker;
            CalibrationWindow.Instance.Show();
            CalibrationWindow.Instance.Start();
        }

        private void OnCalibrationRunning(object sender, RoutedEventArgs e)
        {
            tracker.CalibrationStart();
        }

        private void OnCalibrationAccepted(object sender, RoutedEventArgs e)
        {
            CalibrationWindow.Instance.Close();
            WindowState = WindowState.Normal;
            BtnStartStop.IsEnabled = true;
            BtnCalibrate.Label = "Recalibrate";
            this.videoImageControl.VideoOverlayTopMost = true;
        }

        private void OnPointStart(object sender, RoutedEventArgs e)
        {
            var control = sender as CalibrationControl;
            if (control != null) tracker.CalibrationPointStart(control.CurrentPoint.Number, control.CurrentPoint.Point);
            e.Handled = true;
        }

        //private void OnPointStart(object sender, GazeTrackerUI.Calibration.Events.CalibrationPointEventArgs e)
        //{
        //    tracker.CalibrationPointStart(e.Number, e.Point);
        //    e.Handled = true;
        //}

        //private void OnPointEnd(object sender, CalibrationPointEventArgs e)
        //{
        //    tracker.CalibrationPointEnd(e.Number, e.Point);
        //    e.Handled = true;
        //}

        private void OnCalibrationAbort(object sender, RoutedEventArgs e)
        {
            CalibrationWindow.Instance.Stop();
            tracker.CalibrationAbort();
        }

        private void OnCalibrationEnd(object sender, RoutedEventArgs e)
        {
            tracker.CalibrationEnd();
        }


        #endregion

        #endregion


        #region Camera/Video viewing

        private void OnCameraChanged(object sender, RoutedEventArgs e)
        {
            tracker.SetCamera(GTSettings.Current.Camera.DeviceNumber, GTSettings.Current.Camera.DeviceMode);

            Point oldWinPos = new Point(VideoViewer.Instance.Top, VideoViewer.Instance.Left);
            VideoViewer.Instance.Width = tracker.VideoWidth + videoImageControl.Margin.Left + videoImageControl.Margin.Right;
            VideoViewer.Instance.Height = tracker.VideoHeight + videoImageControl.Margin.Top + videoImageControl.Margin.Bottom;
        }


        private void ShowMessageNoCamera()
        {
            msgWindow = new MessageWindow();
            msgWindow.Text = "The GazeTracker was unable to connect a camera. \n" +
                             "Make sure that the device is connected and that the device drivers are installed. " +
                             "Verified configurations can be found in our forum located at http://forum.gazegroup.org";
            msgWindow.Show();
            ErrorLogger.WriteLine("Fatal error on startup, could not connect to a camera.");
        }

        #endregion


        #region Server

        private void RegisterForIncomingServerRequests()
        {
            //tcpipServer.OnCalibrationStart += new TCPIPServer.CalibrationStartHandler(tcpipServer_OnCalibrationStart);
            //tcpipServer.OnCalibrationAbort += new TCPIPServer.CalibrationAbortHandler(tcpipServer_OnCalibrationAbort);
            tcpipServer.OnCalibrationParameters += tcpipServer_OnCalibrationParameters;

            /*Henriks testing area - updating the gaze tracker calibration point-by-point*/
            tcpipServer.OnCalibrationFeedbackPoint +=new TCPIPServer.CalibrationFeedbackPointHandler(tcpipServer_OnCalibrationFeedbackPoint);
            tcpipServer.onCalibrationUpdateMethod += new TCPIPServer.CalibrationUpdateMethod(tcpipServer_onCalibrationUpdateMethod);
            /* end testing area */

            tcpipServer.OnDataStreamStart += tcpipServer_OnDataStreamStart;
            tcpipServer.OnDataStreamStop += tcpipServer_OnDataStreamStop;

            tcpipServer.OnLogStart += tcpipServer_OnLogStart;
            tcpipServer.OnLogStop += tcpipServer_OnLogStop;
            tcpipServer.OnLogWriteLine += tcpipServer_OnLogWriteLine;
            tcpipServer.OnLogPathSet += tcpipServer_OnLogPathSet;
            tcpipServer.OnLogPathGet += tcpipServer_OnLogPathGet;

            //tcpipServer.OnCameraSettings += new TCPIPServer.CameraSettingsHandler(tcpipServer_OnCameraSettings);

            tcpipServer.OnUIMinimize += tcpipServer_OnUIMinimize;
            tcpipServer.OnUIRestore += tcpipServer_OnUIRestore;
            tcpipServer.OnUISettings += tcpipServer_OnUISettings;
        }


        #region Stream

        private void tcpipServer_OnDataStreamStart()
        {
            tracker.Server.IsStreamingGazeData = true;
        }

        private void tcpipServer_OnDataStreamStop()
        {
            tracker.Server.IsStreamingGazeData = false;
        }

        #endregion

        #region Calibration

        private void tcpipServer_OnCalibrationParameters(CalibrationParameters calParams)
        {
            // Todo: Should be sent from the tracker after applying settings everywhere..
            tracker.Server.SendMessage(Commands.CalibrationParameters, calParams.ParametersAsString);
        }

        private void tcpipServer_OnCalibrationStart()
        {
            CalibrationWindow.Instance.Dispatcher.Invoke
                (
                    DispatcherPriority.ApplicationIdle,
                    new Action
                        (
                        delegate { Calibrate(null, null); }
                        )
                );
        }

        private void tcpipServer_OnCalibrationAbort()
        {
            CalibrationWindow.Instance.Dispatcher.Invoke
                (
                    DispatcherPriority.ApplicationIdle,
                    new Action
                        (
                        delegate
                            {
                                try
                                {
                                    CalibrationWindow.Instance.Stop();
                                    CalibrationWindow.Instance.Close();
                                    tracker.CalibrationAbort();
                                }
                                catch (Exception ex)
                                {
                                    ErrorLogger.WriteLine(
                                        "GazeTrackerUIMainWindow.cs, error in tcpipServer_OnCalibrationAbort. Message: " +
                                        ex.Message);
                                }
                            }
                        )
                );
        }


        private void tcpipServer_OnCalibrationFeedbackPoint(long time, int packagenumber, int targetX, int targetY,
                                                            int gazeX, int gazeY, float distance, int acquisitionTime)
        {
            CalibrationWindow.Instance.Dispatcher.Invoke
                (
                    DispatcherPriority.ApplicationIdle,
                    new Action
                        (
                        delegate
                            {
                                //pass info from the dedicated interface to the tracker class
                                var target = new System.Drawing.Point(targetX, targetY);
                                var gaze = new GTPoint(gazeX, gazeY);
								//tracker.SaveRecalibInfo(time, packagenumber, target, gaze);

                                /* outputting the data in a local class */
                                string del = " ";
                                string msg = DateTime.Now.Ticks + del
                                             + time + del
                                             + packagenumber + del
                                             + targetX + del
                                             + targetX + del
                                             + gazeX + del
                                             + gazeY + del
                                             + distance + del
                                             + acquisitionTime;
                                Output.Instance.appendToFile(msg);
                            }
                        )
                );
        }

        void tcpipServer_onCalibrationUpdateMethod(int method)
        {
            CalibrationWindow.Instance.Dispatcher.Invoke
                            (
                                DispatcherPriority.ApplicationIdle,
                                new Action
                                    (
                                    delegate
                                    {
                                        GTSettings.Current.Calibration.RecalibrationType = (RecalibrationTypeEnum)method;
                                    }
                                    )
                            );
        }


        #endregion

        #region Log

        private void tcpipServer_OnLogStart()
        {
            Dispatcher.Invoke
                (
                    DispatcherPriority.ApplicationIdle, new Action(delegate
                                                                       {
                                                                           try
                                                                           {
                                                                               tracker.LogData.IsEnabled = true;
                                                                           }
                                                                           catch (Exception ex)
                                                                           {
                                                                               ErrorLogger.ProcessException(ex, false);
                                                                           }
                                                                       }));
        }


        private void tcpipServer_OnLogStop()
        {
            Dispatcher.Invoke
                (
                    DispatcherPriority.ApplicationIdle, new Action(delegate
                                                                       {
                                                                           try
                                                                           {
                                                                               tracker.LogData.IsEnabled = false;
                                                                           }
                                                                           catch (Exception ex)
                                                                           {
                                                                               ErrorLogger.ProcessException(ex, false);
                                                                           }
                                                                       }));
        }


        private void tcpipServer_OnLogPathGet()
        {
            Dispatcher.Invoke
                (
                    DispatcherPriority.ApplicationIdle, new Action(delegate
                                                                       {
                                                                           try
                                                                           {
                                                                               tracker.Server.SendMessage(
                                                                                   Commands.LogPathGet + " " +
                                                                                   tracker.LogData.LogFilePath);
                                                                           }
                                                                           catch (Exception ex)
                                                                           {
                                                                               ErrorLogger.ProcessException(ex, false);
                                                                           }
                                                                       }));
        }

        private void tcpipServer_OnLogPathSet(string path)
        {
            Dispatcher.Invoke
                (
                    DispatcherPriority.ApplicationIdle, new Action(delegate
                                                                       {
                                                                           try
                                                                           {
                                                                               tracker.LogData.LogFilePath = path;
                                                                           }
                                                                           catch (Exception ex)
                                                                           {
                                                                               ErrorLogger.ProcessException(ex, false);
                                                                           }
                                                                       }));
        }

        private void tcpipServer_OnLogWriteLine(string line)
        {
            Dispatcher.Invoke
                (
                    DispatcherPriority.ApplicationIdle, new Action(delegate
                                                                       {
                                                                           try
                                                                           {
                                                                               tracker.LogData.WriteLine(line);
                                                                               tracker.Server.SendMessage(
                                                                                   Commands.LogWriteLine + " " + line);
                                                                           }
                                                                           catch (Exception ex)
                                                                           {
                                                                               ErrorLogger.ProcessException(ex, false);
                                                                           }
                                                                       }));
        }

        #endregion

        #region Camera

        //void tcpipServer_OnCameraSettings(CameraSettings camSettings)
        //{
        //    this.Dispatcher.Invoke
        //    (
        //    DispatcherPriority.ApplicationIdle, new Action(delegate()
        //    {
        //        try
        //        {
        //            tracker.Camera.CameraSettings = camSettings;
        //            MessageBox.Show("CamSettings!");
        //        }
        //        catch (Exception ex)
        //        { Console.Out.WriteLine("Could not apply CameraSettings via Network call" + ex.Message); }
        //    }));

        //}

        #endregion

        #region U.I

        private void tcpipServer_OnUISettings()
        {
            Dispatcher.Invoke
                (
                    DispatcherPriority.ApplicationIdle, new Action(
                       delegate
                       {
                           try
                           {
                               SettingsWindow.Instance.Visibility = Visibility.Visible;
                               SettingsWindow.Instance.WindowState = WindowState.Normal;
                           }
                           catch (Exception ex)
                           {
                               ErrorLogger.ProcessException(ex, false);
                           }
                       }));
        }

        private void tcpipServer_OnUIRestore()
        {
            Dispatcher.Invoke
                (
                    DispatcherPriority.ApplicationIdle, new Action(
                        delegate
                       {
                           try
                           {
                               WindowState = WindowState.Normal;
                               SettingsWindow.Instance.WindowState = WindowState.Normal;
                           }
                           catch (Exception ex)
                           {
                               ErrorLogger.ProcessException(ex, false);
                           }
                       }));
        }

        private void tcpipServer_OnUIMinimize()
        {
            Dispatcher.Invoke
                (
                    DispatcherPriority.ApplicationIdle, new Action(
                        delegate
                       {
                           try
                           {
                               WindowState = WindowState.Minimized;
                               SettingsWindow.Instance.WindowState =
                                   WindowState.Minimized;
                           }
                           catch (Exception ex)
                           {
                               ErrorLogger.ProcessException(ex, false);
                           }
                       }));
        }


        #endregion

        #endregion


        #region NetworkClient

        private void OnNetworkClient(object sender, RoutedEventArgs e)
        {
            NetworkClientLaunch();
        }

        private void NetworkClientLaunch()
        {
            if (GTSettings.Current.Network.ClientUIPath == "" ||
                !File.Exists(GTSettings.Current.Network.ClientUIPath))
            {
                var ofd = new OpenFileDialog();
                ofd.Title = "Select the GazeTrackerClientUI executable file";
                ofd.Multiselect = false;
                ofd.Filter = "Executables (*.exe)|*.exe*;*.xml|All Files|*.*";

                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string[] filePath = ofd.FileNames;
                    GTSettings.Current.Network.ClientUIPath = filePath[0];
                }
                else
                {
                }
            }

            if (!File.Exists(GTSettings.Current.Network.ClientUIPath)) return;
            var psi = new ProcessStartInfo(GTSettings.Current.Network.ClientUIPath);
            clientUIProc = new Process {StartInfo = psi};
            clientUIProc.Start();
        }

        #endregion


        #region Minimize/Activate/Close main app window

        private void AppMinimize(object sender, MouseButtonEventArgs e)
        {
            // If video is detached (seperate window), stop updating images and close the window)
            if (VideoViewer.Instance.WindowState.Equals(WindowState.Normal))
            {
                VideoViewer.Instance.videoImageControl.Stop();
                VideoViewer.Instance.Close();
            }

            // Stop updating images in small preview box
            this.videoImageControl.Stop();

            // Mimimize the application window
            WindowState = WindowState.Minimized;
        }


        private void AppClose(object sender, MouseButtonEventArgs e)
        {
            // Save settings 
            SettingsWindow.Instance.SaveSettings();
            //CameraSettingsWindow.Instance.Close(); //is already closed - will force the class to reinitialize only to be closed again

            // Close server
            if (tcpipServer.IsEnabled)
                tcpipServer.IsEnabled = false;

            // Kill the ClientUI process (if initiated)
            if (clientUIProc != null && clientUIProc.HasExited == false)
                clientUIProc.Kill();

            // Cleaup tracker & release camera
            if (tracker != null)
                tracker.Cleanup();

            // Close all windows (including Visibility.Collapsed & Hidden)
            for (int i = 0; i < Application.Current.Windows.Count; i++)
                Application.Current.Windows[i].Close();

            // Null tracker..
            tracker = null;

            //if (Illuminator.Instance != null)
            //    Illuminator.Instance.Disconnect(true);

            // Force exit, now dammit!
            Environment.Exit(Environment.ExitCode);
        }


        private void Window1_Activated(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Normal)
                this.videoImageControl.Start();

            videoImageControl.VideoOverlayTopMost = true;
        }

        private void Window1_Deactivated(object sender, EventArgs e)
        {
            if (WindowState.Equals(WindowState.Minimized))
                videoImageControl.Stop();

            videoImageControl.VideoOverlayTopMost = false;
        }

        #endregion


        #region DragWindow

        private void DragWindow(object sender, MouseButtonEventArgs args)
        {
            DragMove();
        }

        #endregion
    }
}