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

	// Main Calibration Class
    public class Calibration
    {


		public CalibMethod calibMethod;


		public Calibration()
		{
			if (GTSettings.Current.Processing.TrackingGlints)
			{
				calibMethod = new CalibPolynomial();
			}
			else
			{
				calibMethod = new CalibPupil();
			}
		}

		public bool Calibrate()
		{
			return calibMethod.Calibrate();
		}

		public void ExportToFile()
		{
			calibMethod.ExportToFile();
		}

		public GTPoint GetGazeCoordinates(TrackData trackData, EyeEnum eye)
		{
			return calibMethod.GetGazeCoordinates(trackData, eye);
		}

		public string CalibrationDataAsString()
		{
			return calibMethod.CalibrationDataAsString();
		}

		#region Get/Set

		public bool IsCalibrated
		{
			get { return calibMethod.IsCalibrated; }
			set { calibMethod.IsCalibrated = value; }
		}

		public GTPoint PupilCenterLeft
		{
			get { return calibMethod.PupilCenterLeft; }
			set { calibMethod.PupilCenterLeft = value; }
		}

		public GTPoint PupilCenterRight
		{
			get { return calibMethod.PupilCenterRight; }
			set { calibMethod.PupilCenterRight = value; }
		}

		public GlintConfiguration GlintConfigLeft
		{
			get { return calibMethod.GlintConfigLeft; }
			set { calibMethod.GlintConfigLeft = value; }
		}

		public GlintConfiguration GlintConfigRight
		{
			get { return calibMethod.GlintConfigRight; }
			set { calibMethod.GlintConfigRight = value; }
		}

		public int CurrentTargetNumber
		{
			get { return calibMethod.CurrentTargetNumber; }
			set { calibMethod.CurrentTargetNumber = value; }
		}



		#endregion
	}




}