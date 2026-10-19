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
		#region Detect two glints

		#region Detect the two closest glints to a specified coordinates

		/// <summary>
		/// Detect two glints in a grayscale image.
		/// This method will select the two glints closest to the initial location
		/// </summary>
		/// <param name="inputImage">Input image in grayscale</param>
		/// <param name="glintThreshold">Gray level to threshold the image</param>
		/// <param name="minGlintSize">Minimum glint size allowed (radius in pixels)</param>
		/// <param name="maxGlintSize">Maximum glint size allowed (radius in pixels)</param>
		/// <param name="initialLocation">Select the two glints closest this parameter</param>
		/// <returns>True if glint(s) detected, false if not</returns>
		public bool DetectTwoGlints(Image<Gray, byte> inputImage, int glintThreshold,
									int minGlintSize, int maxGlintSize, GTPoint initialLocation)
		{
			this.GlintThreshold = glintThreshold;
			this.MinGlintSize = minGlintSize;
			this.MaxGlintSize = 3 * maxGlintSize;

			bool foundGlints = false;

			// We get the blobs in the input image given the threshold (minWidth, maxWidth)
			blobs = blobDetector.DetectBlobs(inputImage, glintThreshold, MinGlintSize, MaxGlintSize, true);
			int unfilteredCount = blobs.Count;
			double unfilteredArea = blobs.TotalArea;

			// Filter out exterior blobs
			blobs.EliminateExteriorBlobs();

			if (blobDetector.IsFiltering == false) // Not using AForger filtering
				blobs.FilterByArea((int)MinGlintSize, (int)MaxGlintSize);

			if (blobs.Count > 1)
			{
				blobs.FilterByDistance(initialLocation, 2);
				glintData.Glints = new GlintConfiguration(blobs);
				// store blobcount for autotune
				glintData.Glints.UnfilteredCount = unfilteredCount;
				glintData.Glints.UnfilteredTotalArea = unfilteredArea;

				if (glintData.Glints.Count > 0)
					foundGlints = true;
			}

			return foundGlints;
		}

		#endregion

		#region Detect the two glints closest to and below a specified pair of coordinates

		/// <summary>
		/// Detect two glints in a grayscale image.
		/// This method will select the two glints closest to the initial location that are
		/// below the pupil (i.e., glints.y > pupil.y)
		/// </summary>
		/// <param name="inputImage">Input image in grayscale</param>
		/// <param name="glintThreshold">Gray level to threshold the image</param>
		/// <param name="minGlintSize">Minimum glint size allowed (radius in pixels)</param>
		/// <param name="maxGlintSize">Maximum glint size allowed (radius in pixels)</param>
		/// <param name="initialLocation">Select the two glints closest this parameter</param>
		/// <returns>True if glint(s) detected, false if not</returns>
		public bool DetectTwoGlintsBelow(Image<Gray, byte> inputImage, int glintThreshold,
									int minGlintSize, int maxGlintSize, GTPoint initialLocation)
		{
			this.GlintThreshold = glintThreshold;
			this.MinGlintSize = minGlintSize;
			this.MaxGlintSize = 3 * maxGlintSize;

			bool foundGlints = false;

			// We get the blobs in the input image given the threshold (minWidth, maxWidth)
			blobs = blobDetector.DetectBlobs(inputImage, glintThreshold, MinGlintSize, MaxGlintSize, true);
			int unfilteredCount = blobs.Count;
			double unfilteredArea = blobs.TotalArea;

			// Filter out exterior blobs
			blobs.EliminateExteriorBlobs();

			if (blobDetector.IsFiltering == false) // Not using AForger filtering
				blobs.FilterByArea((int)MinGlintSize, (int)MaxGlintSize);

			// Eliminate blobs above initialLocation (pupil center)
			blobs.FilterByLocation(initialLocation, 2, -1);

			if (blobs.Count > 1)
			{
				//blobs.FilterByDistance(initialLocation, 2);
				//glintData.Glints = new GlintConfiguration(blobs);

				glintData.Glints = GetValidConfiguration(blobs, initialLocation);


				// store blobcount for autotune
				glintData.Glints.UnfilteredCount = unfilteredCount;
				glintData.Glints.UnfilteredTotalArea = unfilteredArea;

				if (glintData.Glints.Count > 0)
					foundGlints = true;
			}

			return foundGlints;
		}

		#endregion

		#region Detect the two glints closest to and above a specified pair of coordinates

		/// <summary>
		/// Detect two glints in a grayscale image.
		/// This method will select the two glints closest to the initial location that are
		/// above the pupil (i.e., glints.y  pupil.y)
		/// </summary>
		/// <param name="inputImage">Input image in grayscale</param>
		/// <param name="glintThreshold">Gray level to threshold the image</param>
		/// <param name="minGlintSize">Minimum glint size allowed (radius in pixels)</param>
		/// <param name="maxGlintSize">Maximum glint size allowed (radius in pixels)</param>
		/// <param name="initialLocation">Select the two glints closest this parameter</param>
		/// <returns>True if glint(s) detected, false if not</returns>
		public bool DetectTwoGlintsAbove(Image<Gray, byte> inputImage, int glintThreshold,
									int minGlintSize, int maxGlintSize, GTPoint initialLocation)
		{
			this.GlintThreshold = glintThreshold;
			this.MinGlintSize = minGlintSize;
			this.MaxGlintSize = 3 * maxGlintSize;

			bool foundGlints = false;

			// We get the blobs in the input image given the threshold (minWidth, maxWidth)
			blobs = blobDetector.DetectBlobs(inputImage, glintThreshold, MinGlintSize, MaxGlintSize, true);
			int unfilteredCount = blobs.Count;
			double unfilteredArea = blobs.TotalArea;

			// Filter out exterior blobs
			blobs.EliminateExteriorBlobs();

			if (blobDetector.IsFiltering == false) // Not using AForger filtering
				blobs.FilterByArea((int)MinGlintSize, (int)MaxGlintSize);

			// Eliminate blobs below initialLocation (pupil center)
			blobs.FilterByLocation(initialLocation, 2, 1);

			if (blobs.Count > 1)
			{
				blobs.FilterByDistance(initialLocation, 2);
				glintData.Glints = new GlintConfiguration(blobs);
				// store blobcount for autotune
				glintData.Glints.UnfilteredCount = unfilteredCount;
				glintData.Glints.UnfilteredTotalArea = unfilteredArea;

				if (glintData.Glints.Count > 0)
					foundGlints = true;
			}

			return foundGlints;
		}

		#endregion



		#region Get valid configuration

		/// <summary>
		/// Calculates the optimal valid configuration and eliminates the rest of the blobs.
		/// It uses the number of light sources.
		/// </summary>
		public GlintConfiguration GetValidConfiguration(Blobs blobs, GTPoint initialLocation)
		{
			GlintConfiguration validConfig = new GlintConfiguration(4);
			Matrix<double> candidateGlintCenters = new Matrix<double>(blobs.Count, blobs.Count);
			Matrix<int> distMatrixThr;
			List<Point> combinations = new List<Point>();
			List<GlintConfiguration> validConfigurations = new List<GlintConfiguration>();
			List<int> indicesValidConfigs = new List<int>();

			distMatrixThr = GetDistanceMatrix(blobs, MinDistBetweenGlints, MaxDistBetweenGlints);

			for (int i = 0; i < distMatrixThr.Rows; i++)
			{
				combinations.AddRange(GetCombinations(distMatrixThr.GetRow(i).Clone(), i));
			}


			if (combinations.Count > 0)
				validConfig = FilterConfigsByDistance(blobs, combinations, initialLocation);
			else
				validConfig = new GlintConfiguration(1);

			return validConfig;

		}

		private GlintConfiguration FilterConfigsBySize(Blobs blobs, List<Point> combinations, GTPoint initialLocation)
		{
			int N = combinations.Count;

			double maxSize = 0;
			double combinationSize;

			int correctConfigIndex = 0;

			for (int i = 0; i < N; i++)
			{
				combinationSize = blobs.BlobList[(int)combinations[i].X].Area +
					blobs.BlobList[(int)combinations[i].Y].Area;
				if (combinationSize > maxSize)
				{
					correctConfigIndex = i;
					maxSize = combinationSize;
				}
			}

			return new GlintConfiguration(blobs.BlobList[(int)combinations[correctConfigIndex].X],
				blobs.BlobList[(int)combinations[correctConfigIndex].Y]);
		}


		private GlintConfiguration FilterConfigsByDistance(Blobs blobs, List<Point> combinations, GTPoint initialLocation)
		{
			int N = combinations.Count;

			double minDistance = 100000000;
			double dist1 = 0;
			double dist2 = 0;
			double avgDist = 0;

			int correctConfigIndex = 0;

			for (int i = 0; i < N; i++)
			{
				dist1 = Operations.Distance(blobs.BlobList[(int)combinations[i].X].CenterOfGravity, initialLocation);
				dist2 = Operations.Distance(blobs.BlobList[(int)combinations[i].Y].CenterOfGravity, initialLocation);
				avgDist = (dist1 + dist2) / 2;

				if (avgDist < minDistance)
				{
					correctConfigIndex = i;
					minDistance = avgDist;
				}
			}

			return new GlintConfiguration(blobs.BlobList[(int)combinations[correctConfigIndex].X],
				blobs.BlobList[(int)combinations[correctConfigIndex].Y]);
		}

		/// <summary>
		/// This method takes a row from a distanceMatrix
		/// </summary>
		/// <param name="row"></param>
		/// <returns></returns>
		private List<Point> GetCombinations(Matrix<int> row, int rowNumber)
		{
			int N = (int)row.Sum;
			Point pairOfGlints = new Point();

			List<Point> combinations = new List<Point>();

			//We need at least 4 blobs within the distance range
			if (N >= 2)
			{
				for (int i = rowNumber + 1; i < row.Cols; i++)
				{
					if (row[0, i] == 1)
					{
						pairOfGlints = new Point(rowNumber, i);
						combinations.Add(pairOfGlints);
					}
				}
			}

			return combinations;
		}


		private Matrix<int> GetDistanceMatrix(Blobs blobs, int minDistance, int maxDistance)
		{
			int N = blobs.Count;

			Matrix<double> distMatrix = new Matrix<double>(N, N);
			Matrix<int> distMatrixThr = new Matrix<int>(N, N);
			distMatrixThr.SetIdentity();
			double dist;

			for (int i = 0; i < N; i++)
			{
				for (int j = i; j < N; j++)
				{
					dist = Operations.Distance(blobs.BlobList[i].CenterOfGravity, blobs.BlobList[j].CenterOfGravity);

					if (dist >= minDistance && dist <= maxDistance)
					{
						distMatrixThr[i, j] = 1;
					}
				}
			}

			return distMatrixThr;
		}


		#endregion

		#endregion



	}
}