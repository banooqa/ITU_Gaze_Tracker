/****************************************************
 * Team member name(s): Banooqa Banday
 * Date: 2026-10-28
 * Project: 2 (Eye Tracker)
 * Instructor: Komogortsev
 ****************************************************/
using System;
using System.Collections.Generic;

namespace GazeTrackingLibrary.Utils
{
    /// <summary>
    /// MODIFIED (new file): eye tracking signal quality metrics computed during the verification procedure.
    /// All results are in degrees of visual angle.
    /// Spatial accuracy  = mean angular distance between the gaze samples and the target (Lecture 4).
    /// Spatial precision = RMS of the angular distance between subsequent samples: sqrt( sum((x[i+1]-x[i])^2) / N ).
    /// </summary>
    public static class GazeQualityMetrics
    {
        /// <summary>
        /// Converts a distance on the screen to degrees of visual angle.
        /// </summary>
        /// <param name="distancePx">Distance in screen units (the same units as the gaze samples).</param>
        /// <param name="mmPerUnit">Physical size of one screen unit in millimeters.</param>
        /// <param name="eyeToScreenMm">Distance from the eye to the screen in millimeters.</param>
        public static double UnitsToDegrees(double distancePx, double mmPerUnit, double eyeToScreenMm)
        {
            double mm = distancePx * mmPerUnit;
            return 180 * Math.Atan(mm / eyeToScreenMm) / Math.PI;
        }

        /// <summary>
        /// Spatial accuracy for one target: mean angular offset of all samples from the target.
        /// </summary>
        public static double SpatialAccuracy(IList<double[]> samples, double targetX, double targetY, double mmPerUnit, double eyeToScreenMm)
        {
            if (samples == null || samples.Count == 0)
                return double.NaN;

            double sum = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                double dx = samples[i][0] - targetX;
                double dy = samples[i][1] - targetY;
                sum += UnitsToDegrees(Math.Sqrt(dx * dx + dy * dy), mmPerUnit, eyeToScreenMm);
            }
            return sum / samples.Count;
        }

        /// <summary>
        /// Spatial precision for one fixation: RMS of the angular distances between subsequent samples.
        /// </summary>
        public static double SpatialPrecision(IList<double[]> samples, double mmPerUnit, double eyeToScreenMm)
        {
            if (samples == null || samples.Count < 2)
                return double.NaN;

            double sumSquares = 0;
            for (int i = 0; i < samples.Count - 1; i++)
            {
                double dx = samples[i + 1][0] - samples[i][0];
                double dy = samples[i + 1][1] - samples[i][1];
                double deg = UnitsToDegrees(Math.Sqrt(dx * dx + dy * dy), mmPerUnit, eyeToScreenMm);
                sumSquares += deg * deg;
            }
            return Math.Sqrt(sumSquares / (samples.Count - 1));
        }

        /// <summary>
        /// Mean of the values that are not NaN. Returns NaN if there are none.
        /// </summary>
        public static double MeanIgnoringNaN(IList<double> values)
        {
            double sum = 0;
            int n = 0;
            for (int i = 0; i < values.Count; i++)
            {
                if (double.IsNaN(values[i])) continue;
                sum += values[i];
                n++;
            }
            return n == 0 ? double.NaN : sum / n;
        }
    }
}
