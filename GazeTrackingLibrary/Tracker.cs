// <copyright file="Tracker.cs" company="ITU">
// ******************************************************
// GazeTrackingLibrary for ITU GazeTracker
// Copyright (C) 2010 Javier San Agustin  
// ------------------------------------------------------------------------
// This software is distributed under a dual-licence structure.
//
// For profit usage:
// If you use the software in a way that generates any form of compensation or profit 
// you must obtain a licence to do so. This will allow you to use the codebase without 
// sharing your own source code. Any redistribution the software or parts of it 
// requires a mutual agreement. Contact Javier San Agustin (javier@itu.dk)
// or Martin Tall (m@martintall.com) for more information. 
//
// For educational and non-profit usage:
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
//
// **************************************************************
// </copyright>
// <author>Javier San Agustin</author>
// <email>javier@itu.dk</email>
// <modifiedby>Martin Tall</modifiedby>
// <modifiedby>Adrian Voßkühler</modifiedby>

using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GazeTrackingLibrary.Calibration;
using GazeTrackingLibrary.Camera;
using GazeTrackingLibrary.Detection;
using GazeTrackingLibrary.EyeMovement;
using GazeTrackingLibrary.EyeMovementDetection;
using GazeTrackingLibrary.Log;
using GazeTrackingLibrary.Logging;
using GazeTrackingLibrary.Network;
using GazeTrackingLibrary.Settings;
using GazeTrackingLibrary.Utils;
using GTCommons;
using Size = System.Drawing.Size;

namespace GazeTrackingLibrary
{
    public class Tracker
    {
        #region Variables & events

        private static Tracker instance;
        private readonly DetectionManager detectionManager;
        private readonly ExponentialSmoother exponentialSmoother;
        private readonly GTGazeData gazeDataRaw;
        private readonly GTGazeData gazeDataSmoothed;

        private readonly Logger logGaze;
        private readonly UDPServer server;

        /// <summary>
        /// This timerCalibration is used to wait 300 milliseconds before starting 
        /// grabbing images when a new calibration target starts
        /// // Changed to 100ms. are we doing this because of overshooting? /martin
        /// </summary>
        private readonly DispatcherTimer timerCalibrationDelay;

        private readonly Visualization visualization;
        private Calibration.Calibration calibration;
        private int calibrationDelayMilliseconds = 100;
        private GTCommands commands;

        private Classifier eyeMovement;
        private double fpsTracking;
        private int imagesReceivedSinceCounterStart;
        private long imgCounter;
        private bool isCalibrating;
        private bool processingDone;
        private bool recalibrate;
        private Recalibration recalibration;

        private DateTime timerStartTime;
        private TrackData trackData;

        #endregion //FIELDS

        #region Events

        /// <summary>
        /// This event is raised, whenever the tracker has received
        /// and processed a new video capture frame.
        /// </summary>
        public event EventHandler OnProcessedFrame;

        /// <summary>
        /// This event is raised, whenever the tracker has calculated the calibration
        /// </summary>
        public event EventHandler OnCalibrationComplete;

        #endregion

        #region Constructor

        public Tracker(GTCommands commands)
        {
            this.commands = commands;
            detectionManager = new DetectionManager();
            calibration = new Calibration.Calibration();
            recalibration = new Recalibration();
            eyeMovement = new Classifier();
			exponentialSmoother = new ExponentialSmoother(20, 0, (int)Math.Ceiling(GTSettings.Current.Processing.SmoothLevel / 5.0));
            visualization = new Visualization();
            server = new UDPServer();
            gazeDataRaw = new GTGazeData();
            gazeDataSmoothed = new GTGazeData();
            processingDone = true;
            timerCalibrationDelay = new DispatcherTimer();
            timerCalibrationDelay.Interval = TimeSpan.FromMilliseconds(calibrationDelayMilliseconds);
            timerCalibrationDelay.Tick += TimerCalibrationTick;
            logGaze = new Logger();
            logGaze.Server = server; // Used to send messages back to client (log start/stop etc.)
			logGaze.IsEnabled = GTSettings.Current.FileSettings.LoggingEnabled;

			recalibration.RecalibrationAvailable += new Recalibration.RecalibrationAvailableHandler(recalibration_RecalibrationAvailable);
			//recalibration.OnRecalibrationAvailable +=new Recalibration.RecalibrationAvailable(recalibration_OnRecalibrationAvailable);
            GTSettings.Current.Processing.PropertyChanged += ProcessingSettingsPropertyChanged;
            timerCalibrationDelay.Tick += TimerCalibrationTick;
            CameraControl.Instance.FrameCaptureComplete += Camera_FrameCaptureComplete;
        }

        #endregion //CONSTRUCTION

        #region Get/set properties

        public Tracker Instance
        {
            get { return instance ?? (instance = new Tracker(GTCommands.Instance)); }
        }

        public GTCommands Commands
        {
            set { commands = value; }
        }

        public bool HasValidCamera
        {
            get { return CameraControl.Instance.Capture.HasValidGraph; }
        }

        public int VideoWidth
        {
            get { return CameraControl.Instance.Width; }
        }

        public int VideoHeight
        {
            get { return CameraControl.Instance.Height; }
        }

        public int FPSVideo
        {
            get { return CameraControl.Instance.FPS; }
        }

        public double FPSTracking
        {
            get { return fpsTracking; }
        }

        public DetectionManager ImageProcessing
        {
            get { return detectionManager; }
        }

        public UDPServer Server
        {
            get { return server; }
        }

        public Logger LogData
        {
            get { return logGaze; }
        }

        public Calibration.Calibration Calibration
        {
            get { return calibration; }
        }

        public bool IsCalibrating
        {
            get { return isCalibrating; }
        }

        public GTGazeData GazeDataRaw
        {
            get { return gazeDataRaw; }
        }

        public GTGazeData GazeDataSmoothed
        {
            get { return gazeDataSmoothed; }
        }

        protected long ImgCounter
        {
            get { return imgCounter; }
            set { imgCounter = value; }
        }

        protected bool ProcessingDone
        {
            get { return processingDone; }
        }

        #region Get images

        public IImage GetOriginalImage()
        {
            if (visualization.Gray != null)
                return visualization.Gray;
            else
                return new Image<Gray, byte>(1, 1);
        }

        public IImage GetOriginalImage(int width, int height)
        {
            if (visualization.Gray != null)
                return visualization.Gray.Resize(width, height, INTER.CV_INTER_LINEAR);
            else
                return new Image<Gray, byte>(1, 1);
        }

        public IImage GetProcessedImage()
        {
            return visualization.Processed;
        }

        public IImage GetProcessedImage(int width, int height)
        {
            return visualization.Processed.Resize(width, height, INTER.CV_INTER_LINEAR);
        }

        #endregion

        #endregion //PROPERTIES

        #region Public methods

        public virtual bool Run()
        {
            CameraControl.Instance.Start();
            return true;
        }

        public void SetCamera(int deviceNumber, int deviceMode)
        {
            CameraControl.Instance.SetCamera(deviceNumber, deviceMode);
            Run();
        }

        #region Calibration

        public void CalibrationStart()
        {
			//if (GTSettings.Current.Processing.TrackingGlints)
			//{
			//    if (GTSettings.Current.Processing.NumberOfGlints == 4)
			//        calibration = new CalibHomography();
			//    else
			//        calibration = new CalibPolynomial();
			//}
			//else
			//{
			//    calibration = new CalibPupil();
			//}

			calibration = new Calibration.Calibration();

            //server.SendMessage(GTCommands.Instance.Calibration, this.calibration.numTargets.ToString());
        }

        public void CalibrationEnd()
        {
            try
            {
                bool success = calibration.Calibrate();
                trackData.CalibrationDataLeft = calibration.calibMethod.CalibrationDataLeft;
				trackData.CalibrationDataRight = calibration.calibMethod.CalibrationDataRight;
                server.SendMessage(GazeTrackerClient.Commands.CalibrationEnd, 5); // should be quality
            }
            catch (Exception ex)
            {
                ErrorLogger.ProcessException(ex, false);
            }

            //this.calibration.ExportToFile();

            // Raise event to UI
            if (OnCalibrationComplete != null)
            {
                OnCalibrationComplete(this, new EventArgs());
            }
        }

        public void CalibrationAbort()
        {
            timerCalibrationDelay.Stop();
            isCalibrating = false;
            server.SendMessage(GazeTrackerClient.Commands.CalibrationAbort);
        }

        public void CalibrationPointStart(int id, Point coords)
        {
            var targetCoordinates = new System.Drawing.Point((int) coords.X, (int) coords.Y);

			calibration.calibMethod.AddTarget(id, targetCoordinates);
			calibration.calibMethod.CurrentTargetNumber = id;

            // We wait a little before starting grabbing images
            timerCalibrationDelay.Start();
            server.SendMessage(GazeTrackerClient.Commands.CalibrationPointChange, "x:" + coords.X + "y:" + coords.Y);
        }

        public void CalibrationPointEnd()
        {
            timerCalibrationDelay.Stop();
            isCalibrating = false;
        }

        public string CalibrationDataAsString()
        {
            return calibration.CalibrationDataAsString();
        }

        #region Recalibration methods

		//public void SaveRecalibInfo(long rawTimeOnClient, int packagenumber, System.Drawing.Point targetCoords,
		//                            GTPoint gazeCoords)
		//{
		//    if (!calibration.calibration.IsCalibrated)
		//        return;

		//    int package = packagenumber; //the package number 

		//    switch (GTSettings.Current.Calibration.RecalibrationType)
		//    {
		//        case RecalibrationTypeEnum.Offset:
		//            recalibration.calibration = Calibration;
		//            recalibration.RecalibrateOffset(gazeCoords, targetCoords);
		//            break;

		//        case RecalibrationTypeEnum.None:
		//            break;

		//        default:
		//            if (!recalibration.recalibrating)
		//            {
		//                recalibration.StartRecalibration(calibration);
		//            }

		//            int currentTarget = recalibration.NumRecalibTargets;
		//            recalibration.calibration.CalibrationTargets.Add(new CalibrationTarget(currentTarget, targetCoords));

		//            recalibration.gazeCoordinates.Add(gazeCoords);

		//            recalibration.calibration.CalibrationTargets[currentTarget].pupilCentersLeft.Add(trackData.PupilDataLeft.Center);
		//            recalibration.calibration.CalibrationTargets[currentTarget].pupilCentersRight.Add(trackData.PupilDataRight.Center);

		//            if (GTSettings.Current.Processing.TrackingGlints)
		//            {
		//                recalibration.calibration.CalibrationTargets[currentTarget].glintsLeft.Add(trackData.GlintDataLeft.Glints);
		//                recalibration.calibration.CalibrationTargets[currentTarget].glintsRight.Add(trackData.GlintDataRight.Glints);
		//            }

		//            recalibration.NumRecalibTargets++;
		//            break;
		//    }
		//}

        #endregion

        #endregion // Calibration

        #region Autotune

        public void AutoTune()
        {
        }

        #endregion

        #endregion //Public methods

        #region Overrides

        public virtual void InitCamera()
        {
            try
            {
                //if(CameraControl.Instance.Width != 0)
                //   CameraControl.Instance.Cleanup();

                if (CameraControl.Instance.CameraExists() && CameraControl.Instance.UsingUC480 == false)
                {
                    // Load camera and mode specified in settings
                    CameraControl.Instance.SetCamera(GTSettings.Current.Camera.DeviceNumber,
                                                     GTSettings.Current.Camera.DeviceMode);

                    //  If loading of last set camera failed, try default mode
                    if (!CameraControl.Instance.Capture.HasValidGraph)
                        CameraControl.Instance.SetCamera(0, 0);

                    // If this is already occupied by another application and there
                    // are more than one camera connected, try the next one.
                    if (!CameraControl.Instance.Capture.HasValidGraph && Devices.Current.Cameras.Count > 1)
                        CameraControl.Instance.SetCamera(1, 0);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public virtual void Cleanup()
        {
            if (server.IsEnabled)
                server.IsEnabled = false;

            if (logGaze.IsEnabled)
                logGaze.IsEnabled = false;

            CameraControl.Instance.Cleanup();
        }

        #endregion //OVERRIDES

        #region Eventhandlers

        /// <summary>
        /// Resets the wait before calibration begins timer and sets the calibrating flag
        /// to now pop up the images down to the calibration routines.
        /// </summary>
        /// <param name="sender">Source of the event</param>
        /// <param name="e">An empty <see cref="EventArgs"/></param>
        private void TimerCalibrationTick(object sender, EventArgs e)
        {
            timerCalibrationDelay.Stop();
            isCalibrating = true;
        }

        private void ProcessingSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TrackingMethod")
            {
                CameraControl.Instance.ClearROI();
                detectionManager.Clear();
            }
        }

        #endregion //EVENTHANDLER

        #region Private methods

        #region FrameCaptureCompleted & Processing completed

        /// <summary>
        /// The main event handler that is called whenever the camera capture class
        /// has an new eye video image available.
        /// Starts to process the frame trying to find pupil and glints.
        /// </summary>
        /// <param name="newVideoFrame">The <see cref="Emgu.CV.Image{Emgu.CV.Structure.Bgr, byte}"/>
        /// image with the new frame.</param>
        private void Camera_FrameCaptureComplete(Image<Gray, byte> newVideoFrame)
        {
            imgCounter++;
            processingDone = false;
            bool processingOk;

            Performance.Now.IsEnabled = false;
            Performance.Now.Start(); // Stop output by setting IsEnabled = false or Stop()

            // TrackData object stores all information on pupil centers, glints etc.
            trackData = new TrackData();
            trackData.TimeStamp = DateTime.UtcNow.Ticks/TimeSpan.TicksPerMillisecond;
			trackData.FrameNumber = imgCounter;

            // Flip image here, directshow flipping is not supported by every device
            if (GTSettings.Current.Camera.FlipImage)
                newVideoFrame = newVideoFrame.Flip(FLIP.VERTICAL);

            // Set the original gray frame for visualization
            if (isCalibrating == false)
                visualization.Gray = newVideoFrame;

            // Calculate the frames per second we're tracking at
            CalculateFPS();

            if (GTSettings.Current.Visualization.VideoMode == VideoModeEnum.RawNoTracking)
            {
                Performance.Now.Stop();
                RaiseFrameProcessingCompletedEvent(true);
                return;
            }

            try
            {
                // Process image, find features, main entry point to processing chain
                processingOk = detectionManager.ProcessImage(newVideoFrame, trackData);

				if (processingOk)
				{
					if (calibration.calibMethod.IsCalibrated)
					{
						CalculateGazeCoordinates(trackData);

						if (GTSettings.Current.FileSettings.LoggingEnabled)
							logGaze.LogData(trackData);
					}
					else
					{
						if (isCalibrating)
							SaveCalibInfo(trackData);

						if (GTSettings.Current.FileSettings.LoggingEnabled)
							logGaze.LogData(trackData);
					}
				}
				else
				{
					if (GTSettings.Current.FileSettings.LoggingEnabled)
						logGaze.LogData(trackData);
				}
            }
            catch (Exception)
            {
                processingOk = false;
            }


            // Add sample to database
            TrackDB.Instance.AddSample(trackData.Copy());

            Autotune.Instance.Tune();

            // Update visualization when features have been detected
            if (isCalibrating == false)
                visualization.Visualize(trackData);

            // Stop performance timer and calculate FPS
            Performance.Now.Stop();

            // Raise FrameCaptureComplete event (UI listens for updating video stream)
            RaiseFrameProcessingCompletedEvent(processingOk);
        }


        private void RaiseFrameProcessingCompletedEvent(bool processingOk)
        {
            try
            {
                if (OnProcessedFrame != null)
                    OnProcessedFrame(this, EventArgs.Empty);
            }
            catch (ThreadInterruptedException e)
            {
                string message = "An error occured in Tracker.cs (ThreadInterruptedException). Message: " + e.Message;
                ErrorLogger.RaiseGazeTrackerMessage(message);
            }
            catch (Exception we)
            {
                ErrorLogger.ProcessException(we, false);
            }

            processingDone = true;
        }

        #endregion

        #region Calibration and gaze coordinate calculateion

        private void CalculateGazeCoordinates(TrackData td)
        {
            GTPoint gazedCoordinatesLeft;
            GTPoint gazedCoordinatesRight = new GTPoint();
            GTPoint smoothedCoordinates;

            #region Monocular/Left eye

			calibration.PupilCenterLeft = trackData.PupilDataLeft.Center;

            if (GTSettings.Current.Processing.TrackingGlints)
				calibration.GlintConfigLeft = td.GlintDataLeft.Glints;

			gazedCoordinatesLeft = calibration.GetGazeCoordinates(td, EyeEnum.Left);

            #endregion

            #region Binocular/Right eye

            if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
            {
				calibration.PupilCenterRight = td.PupilDataRight.Center;

                if (GTSettings.Current.Processing.TrackingGlints)
					calibration.GlintConfigRight = td.GlintDataRight.Glints;

                gazedCoordinatesRight = calibration.GetGazeCoordinates(td, EyeEnum.Right);
            }

            #endregion

            #region Smoothing/Eye movement state

            if (GTSettings.Current.Processing.EyeMouseSmooth)
            {
                var p = new GTPoint(gazedCoordinatesLeft.X, gazedCoordinatesLeft.Y);

                if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
                {
                    if (gazedCoordinatesRight.Y != 0 && gazedCoordinatesRight.X != 0)
                    {
                        p.X += gazedCoordinatesRight.X;
                        p.Y += gazedCoordinatesRight.Y;
                        p.X = p.X/2;
                        p.Y = p.Y/2;
                    }
                }

				this.eyeMovement.CalculateEyeMovement(p);

				if (this.eyeMovement.EyeMovementState == Classifier.EyeMovementStateEnum.Fixation)
					smoothedCoordinates = exponentialSmoother.Smooth(p);
				else
				{
					smoothedCoordinates = p;
					this.exponentialSmoother.Stop();
				}
				trackData.EyeMovement = this.eyeMovement.EyeMovementState;
                gazeDataSmoothed.Set(smoothedCoordinates.X, smoothedCoordinates.Y, smoothedCoordinates.X, smoothedCoordinates.Y);
            }

            #endregion

            #region Set values, raise events

            // trigger OnGazeData events
            gazeDataRaw.Set(gazedCoordinatesLeft.X, gazedCoordinatesLeft.Y, gazedCoordinatesRight.X, gazedCoordinatesRight.Y);

            // Sends values via the UDP server directly
            if (server.IsStreamingGazeData)
            {
                if (server.SendSmoothedData)
                    server.SendGazeData(gazeDataSmoothed.GazePositionX, gazeDataSmoothed.GazePositionY);
                else
                    // Send avg. value
                    server.SendGazeData(gazeDataRaw.GazePositionX, gazeDataRaw.GazePositionY);
            }

			trackData.GazeDataRaw = gazeDataRaw;
			trackData.GazeDataSmoothed = gazeDataSmoothed;

            #endregion
        }

        private void SaveCalibInfo(TrackData td)
        {
			CalibrationTarget ct = calibration.calibMethod.GetTarget(calibration.CurrentTargetNumber);

            ct.pupilCentersLeft.Add(td.PupilDataLeft.Center);
            ct.pupilCentersRight.Add(td.PupilDataRight.Center);

            if (GTSettings.Current.Processing.TrackingGlints)
            {
                ct.glintsLeft.Add(td.GlintDataLeft.Glints);
                ct.glintsRight.Add(td.GlintDataRight.Glints);
            }

            // Important: Only print if really needed for debugging, you'll only receive 1/10 of the samples..

            //foreach (CalibrationTarget ctOutput in this.calibration.CalibrationTargets) 
            //{
            //    foreach (GTPoint pLeft in ctOutput.pupilCentersLeft)
            //        Console.Out.WriteLine("Target " + calibration.CurrentTargetNumber + " PupilCenterLeft:" + pLeft.X + " " + pLeft.Y);
            //    foreach (GTPoint pRight in ctOutput.pupilCentersRight)
            //        Console.Out.WriteLine("Target " + calibration.CurrentTargetNumber + " PupilCenterRight:" + pRight.X + " " + pRight.Y);
            //}

            Performance.Now.Stamp("SaveCalibInfo");
        }

        private void recalibration_RecalibrationAvailable()
        {
			calibration.calibMethod.CalibrationDataLeft.CoeffsX = new Matrix<double>(recalibration.calibration.calibMethod.CalibrationDataLeft.CoeffsX.Data);
			calibration.calibMethod.CalibrationDataLeft.CoeffsY = new Matrix<double>(recalibration.calibration.calibMethod.CalibrationDataLeft.CoeffsY.Data);
        }

        #endregion

        private void CalculateFPS()
        {
            TimeSpan ts = DateTime.Now.Subtract(timerStartTime);

            if (ts.TotalMilliseconds < 1000)
            {
                imagesReceivedSinceCounterStart++;
            }
            else
            {
                fpsTracking = imagesReceivedSinceCounterStart;
                timerStartTime = DateTime.Now;
                imagesReceivedSinceCounterStart = 0;
            }
        }

        #endregion
    }
}