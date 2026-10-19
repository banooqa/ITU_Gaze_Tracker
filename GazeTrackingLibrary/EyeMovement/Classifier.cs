using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using GazeTrackingLibrary.Utils;
using GazeTrackingLibrary.Settings;

namespace GazeTrackingLibrary.EyeMovement
{
    /// <summary>
    /// This class implements the methods to detect the type of 
    /// eye movement (fixation or saccade)
    /// </summary>
    public class Classifier
    {

        #region EyeMovementStateEnum enum

        public enum EyeMovementStateEnum
        {
            NoFixation = 0,
            Fixation = 1,
        }

        #endregion

        #region Calculate Velocity

        private double angularVelocity; //angular velocity between 2 consecutive samples
        private double distDegrees; //distance converted to degrees of visual angle
        private double distMm; //distance converted to mm
        private double distPixels; //distance in pixels (i.e. error on screen)
        private double timeElapsed; //time elapsed between 2 consecutive images
		#endregion

		private readonly List<GTPoint> recentPoints = new List<GTPoint>();
        private readonly List<double> velocities = new List<double>();

        private EyeMovementStateEnum eyeMovementState = EyeMovementStateEnum.NoFixation;

        private long previousTime;

        /// <summary>
        /// Constructor. Initializes the eye movement detector with default parameters
        /// </summary>
        public Classifier()
        {

            // This should be in an XML file (or in the UI)
			maxWindowSize = 10;
			windowSize = 2;
            distUserToScreen = 60; // In cm
            maxAngularSpeed = 50; // In degrees per second
			maxDispersion = 150;
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="windowSize">Window size in samples (recommended value: 5 to 10)</param>
        /// <param name="maxAngularSpeed">Maximum angular speed allowed to consider
        /// a fixation. Angular speeds above this value will be considered a saccade</param>
        public Classifier(int maxWindowSize, int maxAngularSpeed)
        {
			this.maxWindowSize = maxWindowSize;
			windowSize = 2;
            distUserToScreen = 60;
            this.maxAngularSpeed = maxAngularSpeed;
			maxDispersion = 150;
        }

        /// <summary>
        /// This method calculates the type of eye movement given a new point
        /// </summary>
        /// <param name="newPoint">New point</param>
        public void CalculateEyeMovement(GTPoint newPoint)
        {
            long time = DateTime.UtcNow.Ticks/TimeSpan.TicksPerMillisecond;
            AddNewPoint(newPoint, time);

            if (recentPoints.Count > 1)
            {
				if (Operations.GetMaxDistanceOnWindow(recentPoints) < maxDispersion)
				{
					CalculateVelocity();
					if (velocities[velocities.Count - 1] > maxAngularSpeed)
						eyeMovementState = EyeMovementStateEnum.NoFixation;
					else
						eyeMovementState = EyeMovementStateEnum.Fixation;
				}
				else
				    eyeMovementState = EyeMovementStateEnum.NoFixation;

            }
            else
                eyeMovementState = EyeMovementStateEnum.NoFixation;

			if (eyeMovementState == EyeMovementStateEnum.NoFixation)
			{
				if (TrackDB.Instance.GetLastSample().EyeMovement == EyeMovementStateEnum.Fixation)
				{
					recentPoints.Clear();
					windowSize = 2;
					AddNewPoint(newPoint, time);
				}
			}
			else
				windowSize = Math.Min(windowSize, maxWindowSize);

        }


        private void AddNewVelocity(double velocity)
        {
            if (velocities.Count == windowSize)
                velocities.RemoveAt(0);

            velocities.Add(velocity);
        }


        private void AddNewPoint(GTPoint newPoint, long time)
        {
            if (recentPoints.Count >= windowSize)
                recentPoints.RemoveAt(0);

            recentPoints.Add(newPoint);

            timeElapsed = time - previousTime;
            timeElapsed = timeElapsed/1000;
            previousTime = time;
        }

        /// <summary>
        /// Calculate angular velocity
        /// </summary>
        private void CalculateVelocity()
        {
            var newPoint = new GTPoint(recentPoints[recentPoints.Count - 1]);
            var oldPoint = new GTPoint(recentPoints[recentPoints.Count - 2]);
            distPixels = Operations.Distance(newPoint, oldPoint);

            distMm = ConvertPixToMm(distPixels);

            distDegrees = Math.Atan(distMm/10/distUserToScreen);
            distDegrees = distDegrees*180/Math.PI;

            angularVelocity = distDegrees/timeElapsed;

            AddNewVelocity(angularVelocity);
        }


		private static double ConvertPixToMm(double pixels)
		{
			return pixels * ScreenParameters.PrimarySize.Width / ScreenParameters.PrimaryResolution.Width;
		}


        #region Getters / Setters

        public EyeMovementStateEnum EyeMovementState
        {
            get { return eyeMovementState; }
        }

        public int distUserToScreen { get; set; }

        public int maxWindowSize { get; set; }

		public int windowSize { get; set; }

        public int maxAngularSpeed { get; set; }

		public int maxDispersion { get; set; }

        #endregion
    }
}