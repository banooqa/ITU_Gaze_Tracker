using System.Collections.Generic;
using System.Drawing;
using GazeTrackingLibrary.Utils;
using GazeTrackingLibrary.Settings;

namespace GazeTrackingLibrary.Calibration
{
    public class Recalibration
    {

        #region Delegates

        public delegate void RecalibrationAvailableHandler();

        #endregion

		#region Variables
		private readonly int numTargetsForRecalib;
		private int numCorners = 4;
		private Point[] corners = new Point[4];


        public Calibration calibration;

        public List<GTPoint> gazeCoordinates;

        private int numRecalibTargets;
        public List<Point> targetCoordinates;

		public bool recalibrating = false;
		#endregion

		public Recalibration()
        {
			numTargetsForRecalib = 30;
			targetCoordinates = new List<Point>(numTargetsForRecalib);
			gazeCoordinates = new List<GTPoint>(numTargetsForRecalib);
            numRecalibTargets = 0;

			calibration = new Calibration();
			//if (GTSettings.Current.Processing.TrackingGlints)
			//    calibration = new CalibPolynomial();
			//else
			//    calibration = new CalibPupil();
        }

		public void StartRecalibration(Calibration calib)
		{
			CopyCorners(calib);
			recalibrating = true;
		}


        public int NumRecalibTargets
        {
            get { return numRecalibTargets; }
            set
            {
                numRecalibTargets = value;
                if (numRecalibTargets >= numTargetsForRecalib)
                {
					switch (GTSettings.Current.Calibration.RecalibrationType)
					{
						case RecalibrationTypeEnum.Full:
							FullRecalibration();
							break;
						case RecalibrationTypeEnum.Continuous:
							ContinuousRecalibration();
							break;
						default:
							break;
					}
                }
            }
        }

        public int NumTargetsForRecalib
        {
            get { return numTargetsForRecalib; }
        }

        public event RecalibrationAvailableHandler RecalibrationAvailable;

		public void OnRecalibrationAvailable()
		{
			RecalibrationAvailable();
		}


        public void FullRecalibration()
        {
			//double previousError = ErrorPreviousCalib();

            calibration.Calibrate();

            OnRecalibrationAvailable();

			#region Clear calibration data
			//if (GTSettings.Current.Processing.TrackingGlints)
			//    calibration = new CalibPolynomial();
			//else
			//    calibration = new CalibPupil();
			//gazeCoordinates.Clear();
			//targetCoordinates.Clear();
			//numRecalibTargets = 0;
			#endregion
		}

		public void ContinuousRecalibration()
		{
			calibration.Calibrate();
			OnRecalibrationAvailable();

			//gazeCoordinates.RemoveAt(0);
			//targetCoordinates.RemoveAt(0);
			//calibration.CalibrationTargets.RemoveAt(0);

			for (int i = 0; i < numCorners; i++)
			{
				if(Operations.Distance(calibration.calibMethod.CalibrationTargets[NumRecalibTargets-1].targetCoordinates, corners[i]) < 150)
				{
					calibration.calibMethod.CalibrationTargets.RemoveAt(i);
					calibration.calibMethod.CalibrationTargets.Insert(i, calibration.calibMethod.CalibrationTargets[NumRecalibTargets - 2]);
				}
			}

			if (calibration.calibMethod.CalibrationTargets.Count == numTargetsForRecalib)
			{
				gazeCoordinates.RemoveAt(4);
				calibration.calibMethod.CalibrationTargets.RemoveAt(4);
			}

			numRecalibTargets--;
		}

        public void RecalibrateOffset(GTPoint gazeCoords, Point targetCoords)
        {
			double distanceX;
			double distanceY;
			distanceX = gazeCoords.X - targetCoords.X;
			distanceY = gazeCoords.Y - targetCoords.Y;

			calibration.calibMethod.CalibrationDataLeft.CoeffsX[0, 0] -= distanceX / 2;
			calibration.calibMethod.CalibrationDataLeft.CoeffsY[0, 0] -= distanceY / 2;
			if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
			{
				calibration.calibMethod.CalibrationDataRight.CoeffsX[0, 0] += distanceX / 2;
				calibration.calibMethod.CalibrationDataRight.CoeffsY[0, 0] += distanceY / 2;
			}

			OnRecalibrationAvailable();
		}

        private double ErrorPreviousCalib()
        {
            double totalError = 0;

            for (int i = 0; i < targetCoordinates.Count; i++)
            {
                totalError = totalError + Operations.Distance(targetCoordinates[i], gazeCoordinates[i]);
            }

            return totalError/targetCoordinates.Count;
        }

		public void CopyCorners(Calibration calib)
		{
			for (int i = 0; i < calib.calibMethod.CalibrationTargets.Count; i++)
			{
				if (calib.calibMethod.CalibrationTargets[i].IsCorner)
				{
					corners[numRecalibTargets] = calib.calibMethod.CalibrationTargets[i].targetCoordinates;
					calibration.calibMethod.CalibrationTargets.Add(new CalibrationTarget(numRecalibTargets, calib.calibMethod.CalibrationTargets[i].targetCoordinates));
					for (int j = 0; j < calib.calibMethod.CalibrationTargets[i].NumImages; j++)
					{
						calibration.calibMethod.CalibrationTargets[numRecalibTargets].pupilCentersLeft.Add(calib.calibMethod.CalibrationTargets[i].pupilCentersLeft[j]);
						calibration.calibMethod.CalibrationTargets[numRecalibTargets].pupilCentersRight.Add(calib.calibMethod.CalibrationTargets[i].pupilCentersRight[j]);
						if (GTSettings.Current.Processing.TrackingGlints)
						{
							calibration.calibMethod.CalibrationTargets[numRecalibTargets].glintsLeft.Add(calib.calibMethod.CalibrationTargets[i].glintsLeft[j]);
							calibration.calibMethod.CalibrationTargets[numRecalibTargets].glintsRight.Add(calib.calibMethod.CalibrationTargets[i].glintsRight[j]);
						}
					}
					numRecalibTargets++;
				}
			}
		}
    }
}