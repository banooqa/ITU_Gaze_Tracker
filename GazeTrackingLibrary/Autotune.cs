using System;
using GazeTrackingLibrary.Camera;
using GazeTrackingLibrary.Settings;
using GazeTrackingLibrary.Utils;

namespace GazeTrackingLibrary
{
    public class Autotune
    {
        #region Variables

        private static Autotune instance;
        private int missCountGlintLeft;
        private int missCountGlintRight;
        private int missCountPupilLeft;
        private int missCountPupilRight;

        private bool debug = false;

        #endregion

        #region Constructor

        private Autotune()
        {
        }

        #endregion

        #region Public methods

        public void Tune()
        {
            TrackData lastSample = TrackDB.Instance.GetLastSample();

            if (lastSample == null)
                return;

            if (GTSettings.Current.Processing.TrackingEye && GTSettings.Current.Processing.AutoEye)
                TuneEye(lastSample);

            if (GTSettings.Current.Processing.TrackingPupil && GTSettings.Current.Processing.AutoPupil)
                TunePupil(lastSample);

            if (GTSettings.Current.Processing.TrackingGlints && GTSettings.Current.Processing.AutoGlint)
                TuneGlint(lastSample);
        }

        #endregion

        #region Get/Set

        public static Autotune Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Autotune();
                }

                return instance;
            }
        }

        #endregion

        #region Private methods

        private static void TuneEye(TrackData lastSample)
        {
            // To be implemented
        }

        private void TunePupil(TrackData lastSample)
        {
            int cLeft = lastSample.UnfilteredBlobCountLeft;
            int cRight = lastSample.UnfilteredBlobCountRight;

            #region Left eye

            if (lastSample.PupilLeftDetected)
            {
                missCountPupilLeft = 0;
                double meanPupilGray = lastSample.PupilDataLeft.Blob.ColorMean.G; //Only the Green channel is needed

                double minOuterGray =
                    Math.Min(Math.Min(lastSample.PupilDataLeft.GrayCorners[0], lastSample.PupilDataLeft.GrayCorners[1]),
                             Math.Min(lastSample.PupilDataLeft.GrayCorners[2], lastSample.PupilDataLeft.GrayCorners[3]));

                GTSettings.Current.Processing.PupilThresholdLeft =
                    Math.Max((int) (meanPupilGray + minOuterGray)/2,
                             (int) (meanPupilGray + lastSample.PupilDataLeft.Blob.ColorStdDev.G));

                //Max size
                GTSettings.Current.Processing.PupilSizeMaximum =
                    Convert.ToInt32(lastSample.PupilDataLeft.Blob.Rectangle.Width*1.3);


                if (GTSettings.Current.Processing.PupilThresholdLeft > 100)
                    GTSettings.Current.Processing.PupilThresholdLeft = 30;
            }
            else
            {
                // No blobs detected, increase threshold
                if (lastSample.LeftROI.Y != 0 && cLeft == 0)
                    GTSettings.Current.Processing.PupilThresholdLeft += 2;

                else if (cLeft > 0 && cLeft < 15 &&
                         lastSample.UnfilteredTotalBlobAreaLeft <
                         Math.PI*Math.Pow(GTSettings.Current.Processing.PupilSizeMaximum, 2))
                    GTSettings.Current.Processing.PupilThresholdLeft += 5;

                else if (cLeft > 15 &&
                         lastSample.UnfilteredTotalBlobAreaLeft <
                         Math.PI*Math.Pow(GTSettings.Current.Processing.PupilSizeMaximum, 2))
                    GTSettings.Current.Processing.PupilThresholdLeft += 10;

                else if (lastSample.UnfilteredTotalBlobAreaLeft >
                         Math.PI*Math.Pow(GTSettings.Current.Processing.PupilSizeMinimum, 2))
                    if (GTSettings.Current.Processing.PupilThresholdLeft > 10)
                        GTSettings.Current.Processing.PupilThresholdLeft -= 10;
            }

            if (debug)
                Console.WriteLine("Pupil threshold: {0}", GTSettings.Current.Processing.PupilThresholdLeft);

            #endregion

            #region Right eye

            if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
            {
                if (lastSample.PupilRightDetected)
                {
                    missCountPupilRight = 0;
                    double meanPupilGray = lastSample.PupilDataRight.Blob.ColorMean.G;
                        //Only the Green channel is needed

                    double minOuterGray =
                        Math.Min(
                            Math.Min(lastSample.PupilDataRight.GrayCorners[0], lastSample.PupilDataRight.GrayCorners[1]),
                            Math.Min(lastSample.PupilDataRight.GrayCorners[2], lastSample.PupilDataRight.GrayCorners[3]));

                    GTSettings.Current.Processing.PupilThresholdRight =
                        Math.Max((int) (meanPupilGray + minOuterGray)/2,
                                 (int) (meanPupilGray + lastSample.PupilDataRight.Blob.ColorStdDev.G));

                    if (GTSettings.Current.Processing.PupilThresholdRight > 100)
                        GTSettings.Current.Processing.PupilThresholdRight = 40;
                }
                else
                {
                    // No blobs detected, increase threshold
                    if (lastSample.RightROI.Y != 0 && cRight == 0)
                        GTSettings.Current.Processing.PupilThresholdRight += 2;

                    else if (cRight > 0 && cRight < 15 &&
                             lastSample.UnfilteredTotalBlobAreaRight <
                             Math.PI*Math.Pow(GTSettings.Current.Processing.PupilSizeMaximum, 2))
                        GTSettings.Current.Processing.PupilThresholdRight += 5;

                    else if (cRight > 15 &&
                             lastSample.UnfilteredTotalBlobAreaRight <
                             Math.PI*Math.Pow(GTSettings.Current.Processing.PupilSizeMaximum, 2))
                        GTSettings.Current.Processing.PupilThresholdRight += 10;

                    else if (lastSample.UnfilteredTotalBlobAreaRight >
                             Math.PI*Math.Pow(GTSettings.Current.Processing.PupilSizeMinimum, 2))
                        GTSettings.Current.Processing.PupilThresholdRight -= 10;
                }
            }
            //if (cRight < 1 && GTSettings.Current.Processing.PupilThresholdLeft < 70)
            //   GTSettings.Current.Processing.PupilThresholdRight += 1;

            //if (cRight == 1 && lastSample.PupilDataRight.Blob.Fullness < 0.65)
            //   GTSettings.Current.Processing.PupilThresholdRight += 2;


            //else if(cRight >= 15 && GTSettings.Current.Processing.PupilThresholdRight >= 11) // don't go too low
            //    GTSettings.Current.Processing.PupilThresholdRight -= 8;

            //else if(cRight >= 8 && GTSettings.Current.Processing.PupilThresholdRight >= 6) // don't go too low
            //    GTSettings.Current.Processing.PupilThresholdRight -= 4;

            // // check area
            // if(cRight >=2 && lastSample.UnfilteredTotalBlobAreaRight <  Math.PI * Math.Pow(GTSettings.Current.Processing.PupilSizeMinimum, 2))
            //     GTSettings.Current.Processing.PupilThresholdRight += 5;

            #endregion
        }

        private void TuneGlint(TrackData lastSample)
        {
            if (lastSample.GlintDataLeft == null && lastSample.GlintDataRight == null)
                return;

            if (lastSample.PupilLeftDetected == false || lastSample.PupilRightDetected == false)
                return;

            if (lastSample.GlintDataLeft != null)
                if (lastSample.GlintDataLeft.Glints == null && lastSample.GlintDataRight == null)
                    return;

            #region Reset on too many misses

            //if (lastSample.GlintsLeftDetected == false)
            //{
            //    missCountGlintLeft++;
            //    GTSettings.Current.Processing.GlintThresholdLeft--;

            //    if (missCountGlintLeft > CameraControl.Instance.FPS/5)
            //    {
            //        GTSettings.Current.Processing.GlintThreshold = GTSettings.Current.Processing.GlintThreshold;
            //            //reset to default (for both)
            //        missCountGlintLeft = 0;
            //    }
            //}
            //else
            //{
            //    missCountGlintLeft = 0;
            //}


            //if (lastSample.GlintsRightDetected == false)
            //{
            //    missCountGlintRight++;
            //    GTSettings.Current.Processing.GlintThresholdRight--;

            //    if (missCountGlintRight > CameraControl.Instance.FPS/5)
            //    {
            //        GTSettings.Current.Processing.GlintThreshold = GTSettings.Current.Processing.GlintThreshold;
            //            //default
            //        missCountGlintRight = 0;
            //    }
            //}
            //else
            //{
            //    missCountGlintRight = 0;
            //}

            #endregion

            //int lCount = lastSample.GlintDataLeft.Glints.UnfilteredCount;
            if (lastSample.GlintDataLeft != null)
            {
                if (lastSample.GlintDataLeft.Glints != null)
                {
                    double lArea = lastSample.GlintDataLeft.Glints.UnfilteredTotalArea;
                    double rArea = lastSample.GlintDataRight.Glints.UnfilteredTotalArea;

                    // No glints, drop threshold
                    if (lastSample.GlintDataLeft.Glints.Count == 0)
                        GTSettings.Current.Processing.GlintThresholdLeft -= 3;

                    if (lastSample.GlintDataRight.Glints.Count == 0)
                        GTSettings.Current.Processing.GlintThresholdRight -= 3;

                    // Area too big (compare with pupil)

                    // Left
                    if(lastSample.PupilLeftDetected && lastSample.PupilDataLeft.Blob.Area > 10)
                    {
                        if(GTSettings.Current.Processing.GlintThresholdLeft < 230) // max
                        {
                            if (lArea > lastSample.PupilDataLeft.Blob.Area*4)
                                GTSettings.Current.Processing.GlintThresholdLeft += 3;
                            else if (lArea > lastSample.PupilDataLeft.Blob.Area*3)
                                GTSettings.Current.Processing.GlintThresholdLeft += 1;
                        }
                    }

                    //Right
                    if(lastSample.PupilRightDetected && lastSample.PupilDataRight.Blob.Area > 10)
                    {
                        if(GTSettings.Current.Processing.GlintThresholdRight < 230)
                        {
                            if (rArea > lastSample.PupilDataRight.Blob.Area*4)
                                GTSettings.Current.Processing.GlintThresholdRight += 3;
                            else if (rArea > lastSample.PupilDataRight.Blob.Area*3)
                                GTSettings.Current.Processing.GlintThresholdRight += 1;
                        }
                    }
                }
            }

            //Console.Out.WriteLine("L#: " + lCount + " lA: " + lArea + "  R#:" + rCount + " rA: " + rArea);
        }

        #endregion
    }
}