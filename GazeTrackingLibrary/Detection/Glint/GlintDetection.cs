using System;
using Emgu.CV;
using Emgu.CV.Structure;
using GazeTrackingLibrary.Detection.BlobAnalysis;
using GazeTrackingLibrary.Log;
using GazeTrackingLibrary.Settings;
using GazeTrackingLibrary.Utils;
using Combinatorics;
using System.Collections.Generic;
using System.Windows;

namespace GazeTrackingLibrary.Detection.Glint
{
    public partial class GlintDetection
    {

        #region Variables

        private readonly BlobDetector blobDetector;
        public Blobs blobResult;
        private Blobs blobs;
        public Blobs candidateGlints;
        private EyeEnum eye;
        private GlintData glintData;

        #endregion


        #region Constructor

        public GlintDetection(EyeEnum eye)
        {
            this.eye = eye;
            glintData = new GlintData();
            blobDetector = new BlobDetector();
        }

        #endregion


        #region Get/Set

        public GlintData GlintData
        {
            get { return glintData; }
            set { glintData = value; }
        }

        public EyeEnum Eye
        {
            get { return eye; }
            set { eye = value; }
        }

        public int GlintThreshold { get; set; }

        public int AngleValue { get; set; }

        public int MinGlintSize { get; set; }

        public int MaxGlintSize { get; set; }

        public GTPoint InitialLocation { get; set; }

        public int MinDistBetweenGlints { get; set; }

        public int MaxDistBetweenGlints { get; set; }

        #endregion

        /// <summary>
        /// Detect glint(s) main method (moved from ImageProcessing)
        /// </summary>
        /// <returns>True if glints detected, false otherwise</returns>
        public bool DetectGlints(Image<Gray, byte> gray, GTPoint pupilCenter)
        {
            bool glintsDetected = false;
            int threshold = GTSettings.Current.Processing.GlintThreshold; // default for both eyes

            // Treshold to apply, seperate for each eye.
            if (eye == EyeEnum.Left)
                threshold = GTSettings.Current.Processing.GlintThresholdLeft;
            else
                threshold = GTSettings.Current.Processing.GlintThresholdRight;

			MinDistBetweenGlints = (int) Math.Floor(0.1 * gray.Width);
			MaxDistBetweenGlints = (int) Math.Ceiling(0.6 * gray.Width);


            switch (GTSettings.Current.Processing.IRPlacement)
            {
                case IRPlacementEnum.Above:

                    if(GTSettings.Current.Processing.NumberOfGlints == 1)
                    {
                        glintsDetected = DetectGlintAbove(
                            gray,
                            threshold,
                            GTSettings.Current.Processing.GlintSizeMinimum,
                            GTSettings.Current.Processing.GlintSizeMaximum,
                            pupilCenter);
                    }
                    else
                    {
                        glintsDetected = DetectTwoGlintsAbove(
                            gray,
                            threshold,
                            GTSettings.Current.Processing.GlintSizeMinimum,
                            GTSettings.Current.Processing.GlintSizeMaximum,
                            pupilCenter);
                    }
                    break;


                case IRPlacementEnum.Below:

                    if(GTSettings.Current.Processing.NumberOfGlints == 1)
                    {
                        glintsDetected = DetectGlintBelow(
                            gray,
                            threshold,
                            GTSettings.Current.Processing.GlintSizeMinimum,
                            GTSettings.Current.Processing.GlintSizeMaximum,
                            pupilCenter);
                    }
                    else
                    {
                        glintsDetected = DetectTwoGlintsBelow(
                            gray,
                            threshold,
                            GTSettings.Current.Processing.GlintSizeMinimum,
                            GTSettings.Current.Processing.GlintSizeMaximum,
                            pupilCenter);

                    }
                    break;

                case IRPlacementEnum.None:

                    if(GTSettings.Current.Processing.NumberOfGlints == 1)
                    {
                        glintsDetected = DetectGlint(
                            gray,
                            threshold,
                            GTSettings.Current.Processing.GlintSizeMinimum,
                            GTSettings.Current.Processing.GlintSizeMaximum,
                            pupilCenter);
                    }
					else if (GTSettings.Current.Processing.NumberOfGlints == 2)
					{
						glintsDetected = DetectTwoGlints(
							gray,
							threshold,
							GTSettings.Current.Processing.GlintSizeMinimum,
							GTSettings.Current.Processing.GlintSizeMaximum,
							pupilCenter);
					}
                    break;

				case IRPlacementEnum.BelowAndAbove:

					if (GTSettings.Current.Processing.NumberOfGlints == 2)
					{
						glintsDetected = DetectTwoGlints(
							gray,
							threshold,
							GTSettings.Current.Processing.GlintSizeMinimum,
							GTSettings.Current.Processing.GlintSizeMaximum,
							pupilCenter);
					}
					break;
			}
            //Performance.Now.Stamp("Glint detected");

            return glintsDetected;
        }





	}
}