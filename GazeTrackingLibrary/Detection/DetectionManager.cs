// <copyright file="DetectionManager.cs" company="ITU">
// ******************************************************
// GazeTrackingLibrary for ITU GazeTracker
// Copyright (C) 2010 Javier San Agustin  
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
// <author>Javier San Agustin</author>
// <email>javier@itu.dk</email>
// <modifiedby>Martin Tall</modifiedby>
// <modifiedby>Adrian Voßkühler</modifiedby>


using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using GazeTrackingLibrary.Camera;
using GazeTrackingLibrary.Detection.Eye;
using GazeTrackingLibrary.Detection.Eyes;
using GazeTrackingLibrary.Detection.Glint;
using GazeTrackingLibrary.Detection.Pupil;
using GazeTrackingLibrary.Log;
using GazeTrackingLibrary.Settings;
using GazeTrackingLibrary.Utils;

namespace GazeTrackingLibrary.Detection
{

    #region Includes

    #endregion

    public class DetectionManager
    {
        #region Variables

        private readonly GlintDetection glintDetectionLeft;
        private readonly GlintDetection glintDetectionRight;
        private readonly PupilDetection pupilDetectionLeft;
        private readonly PupilDetection pupilDetectionRight;

        private bool doEye = true;
        private bool doEyes = true;
        private EyesTracker eyestracker;
        private Eyetracker eyetracker;
        private bool featuresLeftFound;
        private bool featuresRightFound;

        private Image<Gray, byte> inputLeftEye;
        private Image<Gray, byte> inputRightEye;

        #endregion

        #region Constructor

        public DetectionManager()
        {
            eyestracker = new EyesTracker();
            eyetracker = new Eyetracker();

            pupilDetectionLeft = new PupilDetection(EyeEnum.Left);
            pupilDetectionRight = new PupilDetection(EyeEnum.Right);

            glintDetectionLeft = new GlintDetection(EyeEnum.Left);
            glintDetectionRight = new GlintDetection(EyeEnum.Right);
        }

        #endregion //CONSTRUCTION

        #region Get/Set properties

        public Eyetracker Eyetracker
        {
            set { eyetracker = value; }
            get { return eyetracker; }
        }

        public GlintDetection GlintDetectionLeft
        {
            get { return glintDetectionLeft; }
        }

        public GlintDetection GlintDetectionRight
        {
            get { return glintDetectionRight; }
        }

        public PupilDetection PupilDetectionLeft
        {
            get { return pupilDetectionLeft; }
        }

        public PupilDetection PupilDetectionRight
        {
            get { return pupilDetectionRight; }
        }

        #endregion //PROPERTIES

        #region Public methods - process image

        // This is the main image feature detection chain
        private int counter;

        public bool ProcessImage(Image<Gray, byte> input, TrackData trackData)
        {
            counter++;
            //Log.Performance.Now.IsEnabled = false;

            featuresLeftFound = false;
            featuresRightFound = false;

            #region Eyes region tracking (binocular)

            //// If binocular -> Track (head), (eye region), pupil, (glints)
            //if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
            //{
            //    if (GTSettings.Current.Processing.TrackingEyes && eyestracker.IsReady) 
            //    {
            //        if (doEyes && CameraControl.Instance.UsingUC480 == true && CameraControl.Instance.IsROISet == false) 
            //        {
            //            if (eyestracker.DetectEyes(input, trackData)) 
            //            {
            //               doEyes = false; // found both eyes
            //               CameraControl.Instance.ROI = trackData.EyesROI;
            //               TrackDB.Instance.Data.Clear();
            //               doEyes = false;
            //               doEye = true;
            //               return false;
            //            }
            //        }
            //    }
            //}

            #endregion

            #region Eye region tracking

            if (GTSettings.Current.Processing.TrackingEye && doEye)
            {
                // Eye feature detector ready when haar cascade xml file loaded
                if (eyetracker.IsReady)
                {
                    if (eyetracker.DetectEyes(input, trackData)) // will set left/right roi
                        doEye = false;
                    else
                    {
                        // No eye/eys found
                        doEye = true;
                        return false;
                    }
                }
            }

            #endregion

            #region Left eye

            // Set sub-roi, if eye feature detection was performed do nothing otherwise use values from previous frame 
            ApplyEstimatedEyeROI(EyeEnum.Left, trackData, input.Size);
            inputLeftEye = input.Copy(trackData.LeftROI);

            // Detect pupil
            if (pupilDetectionLeft.DetectPupil(inputLeftEye, trackData))
            {
                trackData.PupilDataLeft = pupilDetectionLeft.PupilData;

                // Detect glint(s)
                if (GTSettings.Current.Processing.TrackingGlints)
                {
                    if (glintDetectionLeft.DetectGlints(inputLeftEye, pupilDetectionLeft.PupilData.Center))
                    {
                        trackData.GlintDataLeft = ConvertGlintsToAbsolute(glintDetectionLeft.GlintData, trackData.LeftROI);
                        featuresLeftFound = true;
                    }
                }
                else
                    featuresLeftFound = true;

                // Convert values from subROI to whole absolute image space (ex. from 70x70 to 1280x1024)
                trackData.PupilDataLeft = ConvertPupilToAbsolute(EyeEnum.Left, pupilDetectionLeft.PupilData, trackData);
            }


            #endregion

            #region Right eye

            if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
            {
                ApplyEstimatedEyeROI(EyeEnum.Right, trackData, input.Size);
                inputRightEye = input.Copy(trackData.RightROI);

                // Detect pupil
                if (pupilDetectionRight.DetectPupil(inputRightEye, trackData))
                {
                    trackData.PupilDataRight = pupilDetectionRight.PupilData;

                    // Detect glint(s)
                    if (GTSettings.Current.Processing.TrackingGlints)
                    {
                        if (glintDetectionRight.DetectGlints(inputRightEye, pupilDetectionRight.PupilData.Center))
                        {
                            trackData.GlintDataRight = ConvertGlintsToAbsolute(glintDetectionRight.GlintData, trackData.RightROI);
                            featuresRightFound = true;
                        }
                    }
                    else
                        featuresRightFound = true;

                    trackData.PupilDataRight = ConvertPupilToAbsolute(EyeEnum.Right, pupilDetectionRight.PupilData, trackData);
                }
            }

            #endregion

            #region ROI mode / state / update

            if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
            {
                if (!featuresLeftFound || !featuresRightFound)
                {
                    if (GTSettings.Current.Processing.TrackingEye)
                    {
                        doEye = true;
                        //if(CameraControl.Instance.UsingUC480 && CameraControl.Instance.IsROISet == false)
                        //   CameraControl.Instance.ClearROI();
                    }
                    else
                        trackData.LeftROI = new Rectangle(new Point(0, 0), new Size(0, 0));
                }
                else
                {
                    // Disable eyes and eye feature detection
                    doEye = false;

                    // If using UC480 set roi and adjust EyeROIs
                    if(CameraControl.Instance.UsingUC480)
                    {
                        if (CameraControl.Instance.IsROISet)
                        {
                            // Set ROIs if we didn't detect them through features (only searching for eye features on first frame (or when lost)
                            CenterROIOnPupil(trackData, EyeEnum.Left, input.Size);
                            CenterROIOnPupil(trackData, EyeEnum.Right, input.Size);

                            // Re-center sub-ROIs, enuse that eyes stays within by margins
                            this.CenterEyesROI(trackData, input.Size);
                        }
                        else
                        {
                            SetROICamera(trackData);
                        }
                    }
                }
            }

            if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Monocular)
            {
                if (!featuresLeftFound)
                {
                    if (GTSettings.Current.Processing.TrackingEye)
                    {
                        doEye = true;
                        if(CameraControl.Instance.UsingUC480 && CameraControl.Instance.IsROISet == false)
                           CameraControl.Instance.ClearROI();
                    }
                    else
                        trackData.LeftROI = new Rectangle(new Point(0, 0), new Size(0, 0));
                }
                else
                {
                    trackData.LeftROI = SetROI(input.Size, trackData.PupilDataLeft.Center, Math.Sqrt(trackData.PupilDataLeft.Blob.Area)/2);
                    doEye = false;

                    // If using UC480 set roi and adjust EyeROIs
                    if(CameraControl.Instance.UsingUC480)
                        if(CameraControl.Instance.IsROISet == false)
                           SetROICamera(trackData);
                        else
                        {
                            CenterROIOnPupil(trackData, EyeEnum.Left, input.Size);
               
                            // Re-center sub-ROIs, enuse that eyes stays within by margins
                            if(CameraControl.Instance.UsingUC480 && CameraControl.Instance.IsROISet)
                               this.CenterEyesROI(trackData, input.Size);
                        }
                }
            }

            #endregion

            Performance.Now.Stamp("Processing all done");

			if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
				return featuresRightFound;
			else
				return featuresLeftFound;
        }

        public void Clear()
        {
            doEyes = true;
            doEye = true;
            TrackDB.Instance.Data.Clear();
        }

        #endregion //PUBLICMETHODS

        #region Private methods

        #region ROI

        private static void ApplyEstimatedEyeROI(EyeEnum eye, TrackData trackData, Size imageSize)
        {
            // If the feature detection was used the trackdata.LeftROI/RightROI will already be set (roi != 0)
            // If detector hasen't been used (eg. roi=0) then values from previous frame should be used
            // Update: We set the values on the trackdata object instead of returning a rectangle.

            var ROI = new Rectangle(new Point(0, 0), new Size(imageSize.Width, imageSize.Height));

            switch (eye)
            {
                case EyeEnum.Left:

                    if (trackData.LeftROI.Width == 0)
                        trackData.LeftROI = TrackDB.Instance.GetLastEyeROI(EyeEnum.Left, imageSize);
                    break;

                case EyeEnum.Right:

                    if (trackData.RightROI.Width == 0)
                        trackData.RightROI = TrackDB.Instance.GetLastEyeROI(EyeEnum.Right, imageSize);
                    break;
            }

            Performance.Now.Stamp("ROI Estimated");
        }

        /// <summary>
        /// Set the ROI of an image around a central point given the radius, which would
        /// correspond to the radius of the inscribed circle (e.g a pupil). The method
        /// checks whether the ROI is actually within the limits of the image. If it's
        /// not, the ROI will not be set and the method return false
        /// </summary>
        /// <param name="image">Input image</param>
        /// <param name="center">Central point</param>
        /// <param name="radius">The radius of the roi.</param>
        /// <returns>True if succesfull, otherwise false.</returns>
        private bool SetRoi(Image<Gray, byte> image, Point center, double radius)
        {
            bool success = false;

            Size imageSize = image.Size;
            double aspectRatio = image.Width/image.Height;

            double r = 3*radius;

            var roiSize = new Size((int) (aspectRatio*r), (int) r);

            if (center.X - roiSize.Width > 0 &&
                center.Y - roiSize.Height > 0 &&
                center.X + roiSize.Width < imageSize.Width &&
                center.Y + roiSize.Height < imageSize.Height)
            {
                image.ROI = new Rectangle(
                    center.X - roiSize.Width,
                    center.Y - roiSize.Height,
                    roiSize.Width*2,
                    roiSize.Height*2);
                success = true;
            }
            else
            {
                success = false;
            }

            return success;
        }

        private static Rectangle SetROI(Size imageSize, GTPoint center, double radius)
        {
            var ROI = new Rectangle();
            bool success = false;

            double aspectRatio = imageSize.Width/imageSize.Height;
            double r = 3*radius;

            var roiSize = new Size((int) (aspectRatio*r), (int) r);

            if (center.X - roiSize.Width > 0 &&
                center.Y - roiSize.Height > 0 &&
                center.X + roiSize.Width < imageSize.Width &&
                center.Y + roiSize.Height < imageSize.Height)
            {
                ROI = new Rectangle(
                    (int) Math.Round(center.X) - roiSize.Width,
                    (int) Math.Round(center.Y) - roiSize.Height,
                    roiSize.Width*2,
                    roiSize.Height*2);
                success = true;
            }
            else
            {
                ROI = new Rectangle(new Point(0, 0), roiSize);
                success = false;
            }

            return ROI;
        }

        private static void CenterROIOnPupil(TrackData trackData, EyeEnum eye, Size imageSize)
        {
            var roi = new Rectangle();

            switch (eye)
            {
                case EyeEnum.Left:

                    roi = trackData.LeftROI; // for size

                    if (roi.Width != 0)
                    {
                        roi.X = (int) trackData.PupilDataLeft.Center.X - roi.Width/2; // center it 
                        roi.Y = (int) trackData.PupilDataLeft.Center.Y - roi.Height/2;

                        if (roi.X > 0 && roi.Right < imageSize.Width &&
                            roi.Y > 0 && roi.Bottom < imageSize.Height)
                            trackData.LeftROI = roi; // ok, within image
                    }
                    break;

                case EyeEnum.Right:

                    roi = trackData.RightROI;

                    if (roi.Width != 0)
                    {
                        roi.X = (int) trackData.PupilDataRight.Center.X - roi.Width/2;
                        roi.Y = (int) trackData.PupilDataRight.Center.Y - roi.Height/2;

                        if (roi.X > 0 && roi.Right < imageSize.Width &&
                            roi.Y > 0 && roi.Bottom < imageSize.Height)
                            trackData.RightROI = roi;
                    }
                    break;
            }
        }


        #region Camera ROI methods

        private void SetROICamera(TrackData td)
        {
            // Only apply when we got both eyes
            if (CameraControl.Instance.IsROISet == true)
                return;

            #region Binocular 

            if (GTSettings.Current.Processing.TrackingMethod == TrackingMethodEnum.RemoteBinocular)
            {
                if (td.LeftROI.Height == 0 || td.RightROI.Height == 0 || td.LeftROI.Y == 0 || td.RightROI.Y == 0)
                    return;

                if (Operations.Distance(new GTPoint(td.LeftROI.Location), new GTPoint(td.RightROI.Location)) < 200)
                    return;

                Rectangle roi = new Rectangle();
                roi.Width =  Convert.ToInt32(Operations.Distance(new GTPoint(td.LeftROI.Location), new GTPoint(td.RightROI.Location))*2.2);
                roi.Height = Convert.ToInt32(td.LeftROI.Height*2.3);
                roi.X = td.LeftROI.X - td.LeftROI.Width*2;
                roi.Y = Convert.ToInt32((td.LeftROI.Y + td.RightROI.Y)/2 - 50);

                if (Operations.IsWithinBounds(roi, new Rectangle(new Point(0, 0),
                                                                 new Size(CameraControl.Instance.Width,
                                                                          CameraControl.Instance.Height))))
                {
                    CameraControl.Instance.SetROI(roi);

                    Rectangle newLeft = new Rectangle(new Point(td.LeftROI.X - roi.X, td.LeftROI.Y - roi.Y),
                                                      new Size(td.LeftROI.Width, td.LeftROI.Height));

                    Rectangle newRight = new Rectangle(new Point(td.RightROI.X - roi.X, td.RightROI.Y - roi.Y),
                                                       new Size(td.RightROI.Width, td.RightROI.Height));

                    td.LeftROI = newLeft;
                    td.RightROI = newRight;
                }
            }
                #endregion

            #region monocular

            else if (GTSettings.Current.Processing.TrackingMethod == TrackingMethodEnum.RemoteMonocular)
            {
                if (td.LeftROI.Height == 0 || td.LeftROI.Y == 0)
                    return;

                Rectangle roi = new Rectangle();
                roi.Width = td.LeftROI.Width*5;
                roi.Height = td.LeftROI.Height*4;
                roi.X = Convert.ToInt32((td.LeftROI.X-td.LeftROI.Width/2));
                roi.Y = Convert.ToInt32((td.LeftROI.Y-td.LeftROI.Height/2));

                if (Operations.IsWithinBounds(roi, new Rectangle(new Point(0, 0),
                                                                 new Size(CameraControl.Instance.Width,
                                                                          CameraControl.Instance.Height))))
                {
                    CameraControl.Instance.SetROI(roi);

                    td.LeftROI = new Rectangle(new Point(td.LeftROI.X - roi.X, td.LeftROI.Y - roi.Y),
                                               new Size (td.LeftROI.Width, td.LeftROI.Height));
                }

                #endregion
            }
        }


        private void CenterEyesROI(TrackData trackData, Size imgSize)
        {
            // Crude method for recentering the camera roi based on margins
            int heightMargin = CameraControl.Instance.ROI.Height/3;
            int widthMargin = CameraControl.Instance.ROI.Width/5;
            int adjustPixels = trackData.LeftROI.Width/2;

            #region Monocular (left)

            if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Monocular)
            {
                if (trackData.PupilDataLeft.Center.Y < heightMargin)
                    CameraControl.Instance.AdjustROI(CameraControl.AdjustROIDirectionEnum.Down, adjustPixels);

                else if (trackData.PupilDataLeft.Center.Y > imgSize.Height - heightMargin)
                    CameraControl.Instance.AdjustROI(CameraControl.AdjustROIDirectionEnum.Up, adjustPixels);

                if (trackData.PupilDataLeft.Center.X < widthMargin)
                    CameraControl.Instance.AdjustROI(CameraControl.AdjustROIDirectionEnum.Left, adjustPixels*2);

                else if (trackData.PupilDataLeft.Center.X > imgSize.Width - widthMargin)
                    CameraControl.Instance.AdjustROI(CameraControl.AdjustROIDirectionEnum.Right, adjustPixels*2);
            }

            #endregion

            #region Binocular (right)

            if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
            {
                if (trackData.PupilDataRight.Center.Y < heightMargin || trackData.PupilDataLeft.Center.Y < heightMargin)
                    CameraControl.Instance.AdjustROI(CameraControl.AdjustROIDirectionEnum.Down, adjustPixels);

                else if (trackData.PupilDataRight.Center.Y > imgSize.Height - heightMargin ||
                         trackData.PupilDataLeft.Center.Y > imgSize.Height - heightMargin)
                    CameraControl.Instance.AdjustROI(CameraControl.AdjustROIDirectionEnum.Up, adjustPixels);

                if (trackData.PupilDataRight.Center.X < widthMargin || trackData.PupilDataLeft.Center.X < widthMargin)
                    CameraControl.Instance.AdjustROI(CameraControl.AdjustROIDirectionEnum.Left, adjustPixels);

                else if (trackData.PupilDataRight.Center.X > imgSize.Width - widthMargin ||
                         trackData.PupilDataLeft.Center.X > imgSize.Width - widthMargin)
                    CameraControl.Instance.AdjustROI(CameraControl.AdjustROIDirectionEnum.Right, adjustPixels);
            }

            #endregion

        }

        #endregion

        #endregion

        #region Convert local to absolute position

        private static PupilData ConvertPupilToAbsolute(EyeEnum eye, PupilData pupilData, TrackData trackData)
        {
            var eyeROI = new Rectangle();
            if (eye == EyeEnum.Left)
                eyeROI = trackData.LeftROI;
            else
                eyeROI = trackData.RightROI;

            pupilData.Center.X += eyeROI.X;
            pupilData.Center.Y += eyeROI.Y;
            pupilData.Blob.CenterOfGravity = new GTPoint(pupilData.Blob.CenterOfGravity.X + eyeROI.X,
                                                         pupilData.Blob.CenterOfGravity.Y + eyeROI.Y);
            return pupilData;
        }

        private static GlintData ConvertGlintsToAbsolute(GlintData input, Rectangle ROI)
        {
            foreach (GTPoint t in input.Glints.Centers)
            {
                t.X += ROI.X;
                t.Y += ROI.Y;
                //input.Glints.blobs[i].CentroidX = input.Glints.blobs[i].CentroidX + ROI.X;
                //input.Glints.blobs[i].CentroidY = input.Glints.blobs[i].CentroidY + ROI.Y;
            }

            return input;
        }

        #endregion

        #endregion
    }
}