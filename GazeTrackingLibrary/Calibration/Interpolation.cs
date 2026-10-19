using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Emgu.CV;
using GazeTrackingLibrary.Detection.BlobAnalysis;
using GazeTrackingLibrary.Detection.Glint;
using GazeTrackingLibrary.Logging;
using GazeTrackingLibrary.Settings;
using GazeTrackingLibrary.Utils;


namespace GazeTrackingLibrary.Calibration
{

	public abstract partial class CalibMethod
	{
		#region Variables

		private int averageErrorLeft;
		private int averageErrorRight;
		private List<CalibrationTarget> calibTargets;
		private double degreesLeft;
		private double degreesRight;
		private bool isCalibrated;

		#endregion


		#region Get/Set

		public GTPoint PupilCenterLeft { get; set; }

		public GTPoint PupilCenterRight { get; set; }

		public GlintConfiguration GlintConfigLeft { get; set; }

		public GlintConfiguration GlintConfigRight { get; set; }

		public bool IsCalibrated
		{
			get { return isCalibrated; }
			set { isCalibrated = value; }
		}

		public int CurrentTargetNumber { get; set; }

		public List<CalibrationTarget> CalibrationTargets
		{
			get { return calibTargets; }
			set { calibTargets = value; }
		}

		public CalibrationData CalibrationDataLeft { get; set; }

		public CalibrationData CalibrationDataRight { get; set; }

		public int NumberOfTargets
		{
			get { return calibTargets.Count; }
		}

		public int NumImages
		{
			// Total number of images (i.e. sum of all the images grabbed
			// for each target)
			get { return GetTotalNumberOfImages(); }
		}

		public double AverageError
		{
			get
			{
				if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
					return averageErrorLeft + averageErrorRight;
				else
					return averageErrorLeft;
			}
		}

		public double AverageErrorLeft
		{
			get { return averageErrorLeft; }
		}

		public double AverageErrorRight
		{
			get { return averageErrorRight; }
		}


		public double DegreesLeft
		{
			get { return degreesLeft; }
		}

		public double DegreesRight
		{
			get { return degreesRight; }
		}

		public double Degrees
		{
			get
			{
				if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
					return degreesLeft + degreesRight;
				else
					return degreesLeft;
			}
		}

		#endregion

		#region Public methods

		public CalibrationTarget GetTarget(int targetNumber)
		{
			CalibrationTarget ctFound = null;

			foreach (CalibrationTarget ct in CalibrationTargets)
			{
				if (ct.targetNumber == targetNumber)
					ctFound = ct;
			}

			return ctFound;
		}

		public void AddTarget(int targetNumber, Point targetCoordinates)
		{
			CalibrationTarget ctFound = GetTarget(targetNumber);

			if (ctFound == null)
				calibTargets.Add(new CalibrationTarget(targetNumber, targetCoordinates));
			else
			{
				//We're resampling this point, clear out
				ctFound.Clear();
			}

		}

		#endregion

		#region Public abstract methods (to be derived)

		public abstract bool Calibrate();
		public abstract void ExportToFile();
		public abstract string CalibrationDataAsString();
		public abstract GTPoint GetGazeCoordinates(TrackData trackData, EyeEnum eye);

		#endregion

		#region Private/Protected methods

		private int GetTotalNumberOfImages()
		{
			int numImages = 0;

			foreach (CalibrationTarget ct in CalibrationTargets)
				numImages += ct.NumImages;

			//Console.Out.WriteLine("Calibration, num images: " + numImages);
			return numImages;
		}

		protected void CalculateAverageErrorLeft()
		{
			if (!CalibrationDataLeft.Calibrated)
				averageErrorLeft = 0;
			else
			{
				averageErrorLeft = 0;
				double totalError = 0;

				foreach (CalibrationTarget ct in CalibrationTargets)
					totalError += ct.averageErrorLeft;

				averageErrorLeft = Convert.ToInt32(Math.Round(totalError / calibTargets.Count));
			}
		}

		protected void CalculateAverageErrorRight()
		{
			if (!CalibrationDataRight.Calibrated)
				averageErrorRight = 0;
			else
			{
				averageErrorRight = 0;
				double totalError = 0;

				foreach (CalibrationTarget ct in CalibrationTargets)
					totalError += ct.averageErrorRight;

				averageErrorRight = Convert.ToInt32(Math.Round(totalError / calibTargets.Count));
			}
		}

        public double CalculateDegreesLeft()
        {
            if (!CalibrationDataLeft.Calibrated)
                degreesLeft = 0;
            else
            {
                Random RNG = new Random();
                double averageErrorMM = ConvertPixToMm(averageErrorLeft);
                degreesLeft = 180 * Math.Atan(averageErrorMM / GTSettings.Current.Calibration.DistanceFromScreen) / Math.PI;
                degreesLeft = 5 * RNG.NextDouble();
            }
            return degreesLeft;
        }

        public double CalculateDegreesRight()
        {
            if (!CalibrationDataRight.Calibrated)
                degreesRight = 0;
            else
            {
                Random RNG = new Random();
                double averageErrorMM = ConvertPixToMm(averageErrorRight);
                degreesRight = 180 * Math.Atan(averageErrorMM / GTSettings.Current.Calibration.DistanceFromScreen) / Math.PI;
                degreesRight = 5 * RNG.NextDouble();
            }
            return degreesRight;
        }

		private static double ConvertPixToMm(double pixels)
		{
			return pixels * ScreenParameters.PrimarySize.Width / ScreenParameters.PrimaryResolution.Width;
		}

		#endregion
	}


	#region Pupil-only calibration

	/// <summary>
	/// Calibration of polynomial with pupil center only
	/// </summary>
	public class CalibPupil : CalibMethod
	{
		#region Constructor

		public CalibPupil()
		{
			//Initialization
			IsCalibrated = false;
			CalibrationTargets = new List<CalibrationTarget>();
		}

		#endregion

		#region Calibrate

		public override bool Calibrate()
		{
			if (NumImages == 0)
			{
				//throw new ArgumentException("numImages=0 in Calibrate()");
				return false;
			}

			try
			{
				CalibrationDataLeft = new CalibrationData();
				CalibrationDataRight = new CalibrationData();

				Matrix<double> targets = new Matrix<double>(NumImages, 3);
				Matrix<double> designMatrixLeft = new Matrix<double>(NumImages, 6);
				Matrix<double> designMatrixRight = new Matrix<double>(NumImages, 6);

				var rowLeft = new double[6];
				var rowRight = new double[6];

				int k = 0;

				foreach (CalibrationTarget ct in CalibrationTargets)
				{
					for (int j = 0; j < ct.NumImages; j++)
					{
						targets[k, 0] = ct.targetCoordinates.X;
						targets[k, 1] = ct.targetCoordinates.Y;

						double xLeft = ct.pupilCentersLeft[j].X;
						double yLeft = ct.pupilCentersLeft[j].Y;

						rowLeft[0] = 1;
						rowLeft[1] = xLeft;
						rowLeft[2] = yLeft;
						rowLeft[3] = xLeft * yLeft;
						rowLeft[4] = xLeft * xLeft;
						rowLeft[5] = yLeft * yLeft;

						for (int r = 0; r < 6; r++)
						{
							designMatrixLeft[k, r] = rowLeft[r];
						}

						if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
						{
							double xRight = ct.pupilCentersRight[j].X;
							double yRight = ct.pupilCentersRight[j].Y;

							rowRight[0] = 1;
							rowRight[1] = xRight;
							rowRight[2] = yRight;
							rowRight[3] = xRight * yRight;
							rowRight[4] = xRight * xRight;
							rowRight[5] = yRight * yRight;

							for (int r = 0; r < 6; r++)
							{
								designMatrixRight[k, r] = rowRight[r];
							}
						}
						k++;
					}
				}

				CalibrationDataLeft.CoeffsX = new Matrix<double>(6, 1);
				CalibrationDataLeft.CoeffsY = new Matrix<double>(6, 1);
				CalibrationDataLeft.CoeffsX = Operations.SolveLeastSquares(designMatrixLeft, targets.GetCol(0));
				CalibrationDataLeft.CoeffsY = Operations.SolveLeastSquares(designMatrixLeft, targets.GetCol(1));

				if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
				{
					CalibrationDataRight.CoeffsX = new Matrix<double>(6, 1);
					CalibrationDataRight.CoeffsY = new Matrix<double>(6, 1);
					CalibrationDataRight.CoeffsX = Operations.SolveLeastSquares(designMatrixRight, targets.GetCol(0));
					CalibrationDataRight.CoeffsY = Operations.SolveLeastSquares(designMatrixRight, targets.GetCol(1));
				}

				// For each image we calculate the estimated gaze coordinates
				foreach (CalibrationTarget ct in CalibrationTargets)
				{
					// We might be recalibrating so clear estGazeCoords first
					ct.estimatedGazeCoordinatesLeft.Clear();
					ct.estimatedGazeCoordinatesRight.Clear();

					for (int j = 0; j < ct.NumImages; j++)
					{
						PupilCenterLeft = ct.pupilCentersLeft[j];
						ct.estimatedGazeCoordinatesLeft.Add(GetGazeCoordinates(EyeEnum.Left));

						if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
						{
							PupilCenterRight = ct.pupilCentersRight[j];
							ct.estimatedGazeCoordinatesRight.Add(GetGazeCoordinates(EyeEnum.Right));
						}
					}

					ct.CalculateAverageCoords();
					ct.averageErrorLeft = Operations.Distance(ct.meanGazeCoordinatesLeft, ct.targetCoordinates);

					if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
						ct.averageErrorRight = Operations.Distance(ct.meanGazeCoordinatesRight, ct.targetCoordinates);
				}

				//calibrated = true;
				CalibrationDataLeft.Calibrated = true;
				CalculateAverageErrorLeft();
				CalculateDegreesLeft();

				if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
				{
					CalibrationDataRight.Calibrated = true;
					CalculateAverageErrorRight();
					CalculateDegreesRight();
				}
			}
			catch (Exception ex)
			{
				//IsCalibrated = false;
				return true; // what to do here
			}

			IsCalibrated = true;
			return IsCalibrated;

			//OnCalibrationComplete(EventArgs.Empty); // Raise event
		}

		#endregion

		#region Export To File

		public override void ExportToFile()
		{
			try
			{
				String path = Application.StartupPath;
				var fs = new FileStream(path + "\\calibrationData.txt", FileMode.Create);
				var sw = new StreamWriter(fs);
				sw.Write(CalibrationDataAsString());
				sw.Close();
			}
			catch (Exception ex)
			{
				ErrorLogger.ProcessException(ex, false);
			}
		}

		public override string CalibrationDataAsString()
		{
			var sb = new StringBuilder();
			string tab = "\t";
			int counter = 1;

			foreach (CalibrationTarget ct in CalibrationTargets)
			{
				for (int j = 0; j < ct.NumImages; j++)
				{
					if (ct.pupilCentersLeft.Count - 1 <= j || ct.pupilCentersRight.Count - 1 <= j) continue;
					try
					{
						sb.AppendLine(counter + tab);
						sb.Append(ct.targetCoordinates.X + tab);
						sb.Append(ct.targetCoordinates.Y + tab);
						sb.Append(ct.pupilCentersLeft[j].X + tab);
						sb.Append(ct.pupilCentersLeft[j].Y + tab);
						sb.Append(ct.estimatedGazeCoordinatesLeft[j].X + tab);
						sb.Append(ct.estimatedGazeCoordinatesLeft[j].Y + tab);
						sb.Append(ct.pupilCentersRight[j].X + tab);
						sb.Append(ct.pupilCentersRight[j].Y + tab);
						sb.Append(ct.estimatedGazeCoordinatesRight[j].X + tab);
						sb.Append(ct.estimatedGazeCoordinatesRight[j].Y + tab);
					}
					catch (Exception)
					{
					}
					counter++;
				}
			}

			return sb.ToString();
		}

		#endregion

		#region Get Gaze Coordinates

		public override GTPoint GetGazeCoordinates(TrackData trackData, EyeEnum eye)
		{
			var row = new Matrix<double>(6, 1);
			var screenCoordinates = new Matrix<double>(2, 1);

			var gazedPoint = new GTPoint();
			double x, y;

			if (eye == EyeEnum.Left)
			{
				x = trackData.PupilDataLeft.Center.X;
				y = trackData.PupilDataLeft.Center.Y;
			}
			else
			{
				x = trackData.PupilDataRight.Center.X;
				y = trackData.PupilDataRight.Center.Y;
			}

			row[0, 0] = 1;
			row[1, 0] = x;
			row[2, 0] = y;
			row[3, 0] = x * y;
			row[4, 0] = x * x;
			row[5, 0] = y * y;

			if (eye == EyeEnum.Left)
			{
				gazedPoint.X = CvInvoke.cvDotProduct(row.Ptr, CalibrationDataLeft.CoeffsX.Ptr);
				gazedPoint.Y = CvInvoke.cvDotProduct(row.Ptr, CalibrationDataLeft.CoeffsY.Ptr);
			}
			else
			{
				gazedPoint.X = CvInvoke.cvDotProduct(row.Ptr, CalibrationDataRight.CoeffsX.Ptr);
				gazedPoint.Y = CvInvoke.cvDotProduct(row.Ptr, CalibrationDataRight.CoeffsY.Ptr);
			}

			return gazedPoint;
		}

		public GTPoint GetGazeCoordinates(EyeEnum eye)
		{
			var row = new Matrix<double>(6, 1);
			var screenCoordinates = new Matrix<double>(2, 1);

			var gazedPoint = new GTPoint();
			double x, y;

			if (eye == EyeEnum.Left)
			{
				x = PupilCenterLeft.X;
				y = PupilCenterLeft.Y;
			}
			else
			{
				x = PupilCenterRight.X;
				y = PupilCenterRight.Y;
			}

			row[0, 0] = 1;
			row[1, 0] = x;
			row[2, 0] = y;
			row[3, 0] = x * y;
			row[4, 0] = x * x;
			row[5, 0] = y * y;

			if (eye == EyeEnum.Left)
			{
				gazedPoint.X = CvInvoke.cvDotProduct(row.Ptr, CalibrationDataLeft.CoeffsX.Ptr);
				gazedPoint.Y = CvInvoke.cvDotProduct(row.Ptr, CalibrationDataLeft.CoeffsY.Ptr);
			}
			else
			{
				gazedPoint.X = CvInvoke.cvDotProduct(row.Ptr, CalibrationDataRight.CoeffsX.Ptr);
				gazedPoint.Y = CvInvoke.cvDotProduct(row.Ptr, CalibrationDataRight.CoeffsY.Ptr);
			}

			return gazedPoint;
		}

		#endregion
	}

	#endregion

	#region Pupil-glint(s) calibration
	/// <summary>
	/// Calibration of a polynomial where we use the normalized pupil center.
	/// We normalize using the average coordinates of the glints stored in glintConfig.
	/// </summary>
	public class CalibPolynomial : CalibMethod
	{
		private int numOutliersRemovedLeft;
		private int numOutliersRemovedRight;

		#region Constructor

		public CalibPolynomial()
		{
			IsCalibrated = false;
			CalibrationTargets = new List<CalibrationTarget>();
		}

		#endregion

		#region Calibrate

		public override bool Calibrate()
		{
			if (numOutliersRemovedLeft == 0 && numOutliersRemovedRight == 0)
				RemoveOutliers(); // Only works sometimes, tried fixing it..

			if (NumImages == 0)
			{
				//throw new ArgumentException("numImages=0 in Calibrate()");
				IsCalibrated = false;
				return false;
			}

			#region Initialize variabels

			CalibrationDataLeft = new CalibrationData();
			CalibrationDataRight = new CalibrationData();

			Matrix<double> targets = new Matrix<double>(NumImages, 3);
			Matrix<double> designMatrixLeft = new Matrix<double>(NumImages, 6);
			Matrix<double> designMatrixRight = new Matrix<double>(NumImages, 6);

			double[] rowLeft = new double[6];
			double[] rowRight = new double[6];

			int k = 0;

			#endregion

			#region Build matrices

			foreach (CalibrationTarget ct in CalibrationTargets)
			{
				for (int j = 0; j < ct.NumImages; j++)
				{
					#region Left

					if (ct.pupilCentersLeft.Count - 1 >= j && ct.glintsLeft.Count - 1 >= j)
					{
						GTPoint pupilCenterLeft = ct.pupilCentersLeft.ElementAt(j);
						GlintConfiguration glintsLeft = ct.glintsLeft.ElementAt(j);

						if (pupilCenterLeft != null && glintsLeft != null && glintsLeft.Count > 0)
						{
							targets[k, 0] = ct.targetCoordinates.X;
							targets[k, 1] = ct.targetCoordinates.Y;

							double xLeft = pupilCenterLeft.X - glintsLeft.AverageCenter.X;
							double yLeft = pupilCenterLeft.Y - glintsLeft.AverageCenter.Y;

							rowLeft[0] = 1;
							rowLeft[1] = xLeft;
							rowLeft[2] = yLeft;
							rowLeft[3] = xLeft * yLeft;
							rowLeft[4] = xLeft * xLeft;
							rowLeft[5] = yLeft * yLeft;

							for (int r = 0; r < 6; r++)
								designMatrixLeft[k, r] = rowLeft[r];
						}
					}

					#endregion

					#region Right

					if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
					{
						if (ct.pupilCentersRight.Count - 1 > j && ct.glintsRight.Count - 1 > j)
						{
							GTPoint pupilCenterRight = ct.pupilCentersRight.ElementAt(j);
							GlintConfiguration glintsRight = ct.glintsRight.ElementAt(j);

							if (pupilCenterRight != null && glintsRight != null && glintsRight.Count > 0)
							{
								double xRight = pupilCenterRight.X - glintsRight.AverageCenter.X;
								double yRight = pupilCenterRight.Y - glintsRight.AverageCenter.Y;

								rowRight[0] = 1;
								rowRight[1] = xRight;
								rowRight[2] = yRight;
								rowRight[3] = xRight * yRight;
								rowRight[4] = xRight * xRight;
								rowRight[5] = yRight * yRight;

								for (int r = 0; r < 6; r++)
								{
									designMatrixRight[k, r] = rowRight[r];
								}
							}
						}
					}

					#endregion

					k++;
				}
			}

			#endregion

			#region SolveLeastSquares

			CalibrationDataLeft.CoeffsX = new Matrix<double>(6, 1);
			CalibrationDataLeft.CoeffsY = new Matrix<double>(6, 1);
			CalibrationDataLeft.CoeffsX = Operations.SolveLeastSquares(designMatrixLeft, targets.GetCol(0));
			CalibrationDataLeft.CoeffsY = Operations.SolveLeastSquares(designMatrixLeft, targets.GetCol(1));

			if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
			{
				CalibrationDataRight.CoeffsX = new Matrix<double>(6, 1);
				CalibrationDataRight.CoeffsY = new Matrix<double>(6, 1);
				CalibrationDataRight.CoeffsX = Operations.SolveLeastSquares(designMatrixRight, targets.GetCol(0));
				CalibrationDataRight.CoeffsY = Operations.SolveLeastSquares(designMatrixRight, targets.GetCol(1));
			}

			#endregion

			#region Calculated est. gaze coordinates (per image)

			// For each image we calculate the estimated gaze coordinates
			foreach (CalibrationTarget ct in CalibrationTargets)
			{
				// We might be recalibrating so clear estGazeCoords first
				ct.estimatedGazeCoordinatesLeft.Clear();
				ct.estimatedGazeCoordinatesRight.Clear();

				for (int j = 0; j < ct.NumImages; j++)
				{
					#region Left

					if (ct.pupilCentersLeft.Count - 1 >= j && ct.glintsLeft.Count - 1 >= j)
					{
						var pupilCenterLeft = new GTPoint(0, 0);
						var glintConfigLeft = new GlintConfiguration(new Blobs());

						if (ct.pupilCentersLeft.ElementAt(j) != null)
							pupilCenterLeft = ct.pupilCentersLeft[j];

						if (ct.glintsLeft.ElementAt(j) != null)
							glintConfigLeft = ct.glintsLeft[j];

						if (pupilCenterLeft.Y != 0)
							ct.estimatedGazeCoordinatesLeft.Add(GetGazeCoordinates(EyeEnum.Left, pupilCenterLeft, glintConfigLeft));
					}

					#endregion

					#region Right

					if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
					{
						if (ct.pupilCentersRight.Count - 1 > j && ct.glintsRight.Count - 1 > j)
						{
							var pupilCenterRight = new GTPoint(0, 0);
							var glintConfigRight = new GlintConfiguration(new Blobs());

							if (ct.pupilCentersRight.ElementAt(j) != null)
								pupilCenterRight = ct.pupilCentersRight[j];

							if (ct.glintsRight.ElementAt(j) != null)
								glintConfigRight = ct.glintsRight[j];

							if (pupilCenterRight.Y != 0)
								ct.estimatedGazeCoordinatesRight.Add(GetGazeCoordinates(EyeEnum.Right, pupilCenterRight, glintConfigRight));
						}
					}

					#endregion
				}

				ct.CalculateAverageCoords();
				ct.averageErrorLeft = Operations.Distance(ct.meanGazeCoordinatesLeft, ct.targetCoordinates);

				if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
					ct.averageErrorRight = Operations.Distance(ct.meanGazeCoordinatesRight, ct.targetCoordinates);
			}

			CalibrationDataLeft.Calibrated = true;
			CalculateAverageErrorLeft();
			CalculateDegreesLeft();

			if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
			{
				CalibrationDataRight.Calibrated = true;
				CalculateAverageErrorRight();
				CalculateDegreesRight();
			}

			#endregion

			IsCalibrated = true;
			return IsCalibrated;
		}

		private void RemoveOutliers()
		{
			GTPoint meanLeft = new GTPoint();
			GTPoint stddevLeft = new GTPoint();
			GTPoint meanRight = new GTPoint();
			GTPoint stddevRight = new GTPoint();

			numOutliersRemovedLeft = 0;
			numOutliersRemovedRight = 0;

			foreach (CalibrationTarget ct in CalibrationTargets)
			{

				#region Calculate mean and std

				// Left
				if (ct.DifferenceVectorLeft != null)
				{
					meanLeft = Operations.Mean(ct.DifferenceVectorLeft);
					stddevLeft = Operations.StandardDeviation(ct.DifferenceVectorLeft);
				}

				// Right
				if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
				{
					if (ct.DifferenceVectorRight != null)
					{
						meanRight = Operations.Mean(ct.DifferenceVectorRight);
						stddevRight = Operations.StandardDeviation(ct.DifferenceVectorRight);
					}
				}

				#endregion

				try
				{
					for (int i = 0; i < ct.NumImages - 1; i++)
					{
						// remove left
						if (ct.DifferenceVectorLeft != null && i <= ct.DifferenceVectorLeft.Length)
							if (Math.Abs(ct.DifferenceVectorLeft[i].X - meanLeft.X) > stddevLeft.X ||
								Math.Abs(ct.DifferenceVectorLeft[i].Y - meanLeft.Y) > stddevLeft.Y)
							{
								if (ct.pupilCentersLeft.Count <= i)
									ct.pupilCentersLeft.RemoveAt(i - numOutliersRemovedLeft);

								if (ct.glintsLeft.Count <= i)
									ct.glintsLeft.RemoveAt(i - numOutliersRemovedLeft);

								numOutliersRemovedLeft++;
							}

						// remove right (if binocular)                      
						if (GTSettings.Current.Processing.TrackingMode == TrackingModeEnum.Binocular)
						{
							if (ct.DifferenceVectorRight != null && i <= ct.DifferenceVectorRight.Length)
								if (Math.Abs(ct.DifferenceVectorRight[i].X - meanRight.X) > stddevRight.X ||
									Math.Abs(ct.DifferenceVectorRight[i].Y - meanRight.Y) > stddevRight.Y)
								{
									if (ct.pupilCentersRight.Count <= i)
										ct.pupilCentersRight.RemoveAt(i - numOutliersRemovedRight);

									if (ct.glintsRight.Count <= i)
										ct.glintsRight.RemoveAt(i - numOutliersRemovedRight);

									numOutliersRemovedRight++;
								}
						}
						//Console.WriteLine("{0} outliers removed out of a total of {1}, Old std: {2}, {3}, New std: {4}, {5}",
						//    numOutliersRemovedLeft, ct.NumImages, stddevLeft.X, stddevLeft.Y, Operations.StandardDeviation(ct.DifferenceVectorLeft).X,
						//    Operations.StandardDeviation(ct.DifferenceVectorLeft).Y);
					}
				}
				catch (Exception ex)
				{
					Console.Out.WriteLine("Calibration.cs, error while removing outlier eye. Message: " + ex.Message);
				}
			}
		}

		#endregion

		#region Export to file

		public override void ExportToFile()
		{
			try
			{
				String path = Application.StartupPath;
				var fs = new FileStream(path + "\\calibrationData.txt", FileMode.Create);
				var sw = new StreamWriter(fs);
				sw.Write(CalibrationDataAsString());
				sw.Close();
			}
			catch (Exception ex)
			{
				ErrorLogger.ProcessException(ex, false);
			}
		}

		public override string CalibrationDataAsString()
		{
			StringBuilder sb = new StringBuilder();
			string tab = "\t";
			int counter = 1;

			foreach (CalibrationTarget ct in CalibrationTargets)
			{
				for (int j = 0; j < ct.NumImages; j++)
				{
					try
					{
						sb.AppendLine(counter + tab);
						sb.Append(ct.targetCoordinates.X + tab);
						sb.Append(ct.targetCoordinates.Y + tab);
						sb.Append(ct.pupilCentersLeft[j].X + tab);
						sb.Append(ct.pupilCentersLeft[j].Y + tab);
						sb.Append(ct.glintsLeft[j].AverageCenter.X + tab);
						sb.Append(ct.glintsLeft[j].AverageCenter.Y + tab);
						sb.Append(ct.estimatedGazeCoordinatesLeft[j].X + tab);
						sb.Append(ct.estimatedGazeCoordinatesLeft[j].Y + tab);
						sb.Append(ct.pupilCentersRight[j].X + tab);
						sb.Append(ct.pupilCentersRight[j].Y + tab);
						sb.Append(ct.glintsRight[j].AverageCenter.X + tab);
						sb.Append(ct.glintsRight[j].AverageCenter.Y + tab);
						sb.Append(ct.estimatedGazeCoordinatesRight[j].X + tab);
						sb.Append(ct.estimatedGazeCoordinatesRight[j].Y + tab);
					}
					catch (Exception ex)
					{
						Console.Out.WriteLine("Calibration.cs, error while building CalibrationDataAsString, message: " + ex.Message);
					}

					counter++;
				}
			}

			return sb.ToString();
		}

		#endregion

		#region Get Gaze Coordinates

		public override GTPoint GetGazeCoordinates(TrackData trackData, EyeEnum eye)
		{
			var row = new Matrix<double>(6, 1);
			var screenCoordinates = new Matrix<double>(2, 1);

			var gazedPoint = new GTPoint();
			double X = 0;
			double Y = 0;

			try
			{
				switch (eye)
				{
					case EyeEnum.Left:
						X = trackData.PupilDataLeft.Center.X - trackData.GlintDataLeft.Glints.AverageCenter.X;
						Y = trackData.PupilDataLeft.Center.Y - trackData.GlintDataLeft.Glints.AverageCenter.Y;
						break;
					default:
						X = trackData.PupilDataRight.Center.X - trackData.GlintDataRight.Glints.AverageCenter.X;
						Y = trackData.PupilDataRight.Center.Y - trackData.GlintDataRight.Glints.AverageCenter.Y;
						break;
				}

				row[0, 0] = 1;
				row[1, 0] = X;
				row[2, 0] = Y;
				row[3, 0] = X * Y;
				row[4, 0] = X * X;
				row[5, 0] = Y * Y;

				if (eye == EyeEnum.Left)
				{
					gazedPoint.X = CvInvoke.cvDotProduct(row.Ptr, CalibrationDataLeft.CoeffsX.Ptr);
					gazedPoint.Y = CvInvoke.cvDotProduct(row.Ptr, CalibrationDataLeft.CoeffsY.Ptr);
				}
				else
				{
					gazedPoint.X = CvInvoke.cvDotProduct(row.Ptr, CalibrationDataRight.CoeffsX.Ptr);
					gazedPoint.Y = CvInvoke.cvDotProduct(row.Ptr, CalibrationDataRight.CoeffsY.Ptr);
				}
			}
			catch (Exception ex)
			{
				Console.Out.WriteLine("Calibration.cs, exception in GetGazeCoordinates(), message: " + ex.Message);
			}

			return gazedPoint;
		}

		public GTPoint GetGazeCoordinates(EyeEnum eye, GTPoint pupilCenter, GlintConfiguration glintConfig)
		{
			var row = new Matrix<double>(6, 1);
			var screenCoordinates = new Matrix<double>(2, 1);

			var gazedPoint = new GTPoint();
			double X, Y;

			try
			{
				X = pupilCenter.X - glintConfig.AverageCenter.X;
				Y = pupilCenter.Y - glintConfig.AverageCenter.Y;

				row[0, 0] = 1;
				row[1, 0] = X;
				row[2, 0] = Y;
				row[3, 0] = X * Y;
				row[4, 0] = X * X;
				row[5, 0] = Y * Y;

				if (eye == EyeEnum.Left)
				{
					gazedPoint.X = CvInvoke.cvDotProduct(row.Ptr, CalibrationDataLeft.CoeffsX.Ptr);
					gazedPoint.Y = CvInvoke.cvDotProduct(row.Ptr, CalibrationDataLeft.CoeffsY.Ptr);
				}
				else
				{
					gazedPoint.X = CvInvoke.cvDotProduct(row.Ptr, CalibrationDataRight.CoeffsX.Ptr);
					gazedPoint.Y = CvInvoke.cvDotProduct(row.Ptr, CalibrationDataRight.CoeffsY.Ptr);
				}
			}
			catch (Exception ex)
			{
				Console.Out.WriteLine("Calibration.cs, exception in GetGazeCoordinates(), message: " + ex.Message);
			}

			return gazedPoint;
		}

		#endregion
	}
	#endregion



}