using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GazeTrackingLibrary.Logging;
using GazeTrackingLibrary.Settings;
using GazeTrackingLibrary.Utils;

namespace GazeTrackingLibrary.Detection.Eye
{
    public class Eyetracker
    {
        #region Variables

        private bool foundLeft;
        private bool foundRight;
        private bool isReady;

        #endregion

        #region Constructor

        public Eyetracker()
        {
            try
            {
                GTSettings.Current.Eyetracker.OnHaarCascadeLoaded += EyetrackerSettings_OnHaarCascadeLoaded;

                if (GTSettings.Current.Eyetracker.HaarCascade == null)
                    GTSettings.Current.Eyetracker.LoadHaarCascade("haarcascade_eye.xml");
            }
            catch (Exception ex)
            {
                ErrorLogger.ProcessException(ex, true);
            }
        }

        #endregion

        #region Events

        #region Delegates

        public delegate void AutoTunedEventHandler(bool success);

        #endregion

        public event AutoTunedEventHandler OnAutoTuneCompleted;

        #endregion

        #region Public methods

        public bool DetectEyes(Image<Gray, byte> gray, TrackData trackData)
        {
            bool eyesFound = false;
            eyesFound = DoEyeRegionExtraction(gray, trackData);
            return eyesFound;
        }

        #endregion

        #region Private methods

        private void EyetrackerSettings_OnHaarCascadeLoaded(bool success)
        {
            isReady = success;
        }

        private bool DoEyeRegionExtraction(Image<Gray, byte> gray, TrackData trackData)
        {
            foundLeft = false;
            foundRight = false;

            MCvAvgComp[][] eyesDetected = gray.DetectHaarCascade(
                GTSettings.Current.Eyetracker.HaarCascade,
                GTSettings.Current.Eyetracker.ScaleFactor,
                2, //Min Neighbours (how many nodes should return overlapping hits, eg. reduce false detections)
                HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                GTSettings.Current.Eyetracker.SizeMin);


            // No eyes detected return false
            if (eyesDetected == null || eyesDetected[0] != null && eyesDetected[0].Length == 0)
                return false;

            if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Monocular)
            {
                if (eyesDetected[0].Length == 0)
                    return false;

                trackData.LeftROI = eyesDetected[0][0].rect;
                foundLeft = true;
                return true;
            }

            if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
            {
                // Binocular but didnt find both eyes
                if (eyesDetected[0].Length < 2)
                    return false;

                Rectangle r1 = new Rectangle();
                Rectangle r2 = new Rectangle();

                int minSize = GTSettings.Current.Eyetracker.SizeMin.Width;
                int maxSize = GTSettings.Current.Eyetracker.SizeMax.Width;

                // more than two, exclude false hit (nose?)
                if(eyesDetected[0].Length > 2)
                {
                    for(int i = 0 ; i < eyesDetected[0].Length-2 ; i++)
                    {
                        if(Operations.CalculateAngleDegrees(eyesDetected[0][i].rect.Location, eyesDetected[0][i+1].rect.Location)
                           <
                           Operations.CalculateAngleDegrees(eyesDetected[0][i].rect.Location, eyesDetected[0][i+2].rect.Location))

                        {
                            r1 = eyesDetected[0][i].rect;
                            r2 = eyesDetected[0][i + 1].rect;
                        }
                        else
                        {
                            r1 = eyesDetected[0][i+1].rect;
                            r2 = eyesDetected[0][i+2].rect;
                        }
                    }
                }
                else
                {
                    r1 = eyesDetected[0][0].rect;
                    r2 = eyesDetected[0][1].rect;
                }



                double angle    = Operations.CalculateAngleDegrees(r1.Location, r2.Location);
                double distance = Operations.Distance(r1.Location, r2.Location);
                double verticalLineOffset = Math.Abs(180 - angle);

                if(verticalLineOffset > 35 || distance < 150)
                   return false;

                if (r1.X < r2.X)
                {
                    trackData.LeftROI = r1;
                    trackData.RightROI = r2;
                }
                else
                {
                    trackData.LeftROI = r2;
                    trackData.RightROI = r1;
                }
                return true;
            }

            return false;

            //Log.Performance.Now.Stamp("Eye: Left X:" + trackData.LeftROI.X + " Y:" + trackData.LeftROI.Y + " W:" + trackData.LeftROI.Width + " H:" + trackData.LeftROI.Height + " Right X:" + trackData.RightROI.X + " Y:" + trackData.RightROI.Y + " W:" + trackData.RightROI.Width + " H:" + trackData.RightROI.Height);
        }







        //private EyeEnum DetermineEye(Rectangle newROI, TrackData trackData) 
        //{
        //if (newROI.Width == 0)
        //    return EyeEnum.False;

        //// Monocular (headmounted) always left eye
        //if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Monocular)
        //    return EyeEnum.Left;


        //// Default full video size
        //Rectangle eyesROI = new Rectangle(0,0, CameraControl.Instance.Width, CameraControl.Instance.Height);

        //// if sub-roi is set, use it
        //if (trackData.EyesROI.Width != 0)
        //    eyesROI = trackData.EyesROI;

        //// Determin by split, once tracking is should be determined by tracking history (prev. frame)
        //if (newROI.X <= eyesROI.Width / 2)
        //    return EyeEnum.Left;

        //if (newROI.X >= eyesROI.Width / 2)
        //    return EyeEnum.Right;

        //// old stuff
        ////else 
        ////{
        ////    // Determin by full frame split
        ////    if (newROI.X >= eyesROI.X && newROI.X <= eyesROI.X + eyesROI.Width / 2 &&
        ////        newROI.Y >= eyesROI.Y && newROI.Y <= eyesROI.Y + eyesROI.Height - newROI.Height) 
        ////    {
        ////        return EyeEnum.Left;
        ////    } 
        ////    else if (newROI.X <= eyesROI.X + eyesROI.Width && newROI.X >= eyesROI.X - eyesROI.Width / 2 &&
        ////             newROI.Y >= eyesROI.Y && newROI.Y <= eyesROI.Y + eyesROI.Height - newROI.Height) 
        ////    {
        ////        return EyeEnum.Right;
        ////    } 
        ////    else 
        ////    {
        ////        return EyeEnum.False;
        ////    }
        ////}


        //// If nothing else 
        //return EyeEnum.False;


        // first time, assign by middle split
        //if (avgLeftX == 0 && newROI.X < videoWidth / 2)
        //{
        //    return EyeEnum.Left;
        //}
        ////else 
        //if (avgLeftX != 0 && newROI.X >= avgLeftX - 50 && newROI.X <= avgLeftX + 50
        //         && avgLeftY != 0 && newROI.Y >= avgLeftY - 50 && newROI.Y <= avgLeftY + 50)
        //{
        //    // Check if its not too far below from the opposite eye (avoid nose)
        //    //if (Math.Abs(newROI.Y - GetAvgROIRight().Y) < maxYDistance)
        //    //    return Eyes.False;
        //    //else
        //        return EyeEnum.Left;
        //}

        ////if (AvgRightX == 0 && newROI.X > videoWidth / 2)
        ////{
        ////    return  EyeEnum.Right;
        ////}
        ////else 
        //if (AvgRightX != 0 && newROI.X >= AvgRightX - 50 && newROI.X <= AvgRightX + 50
        //         && avgRightY != 0 && newROI.Y >= avgRightY - 50 && newROI.Y <= avgRightY + 50)
        //{
        //    // Check if its not too far below from the opposite eye (avoid nose)
        //    //if (Math.Abs(newROI.Y - GetAvgROILeft().Y) < maxYDistance)
        //    //    return Eyes.False;
        //    //else
        //        return EyeEnum.Right;
        //}

        //return Eyes.False;
        //}

        #endregion

        #region Get/Set

        public bool IsReady
        {
            get { return isReady; }
        }

        public bool FoundLeft
        {
            get { return foundLeft; }
        }

        public bool FoundRight
        {
            get { return foundRight; }
        }

        #endregion
    }
}