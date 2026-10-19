using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using GazeTrackingLibrary.Detection.Glint;
using GazeTrackingLibrary.Log;
using GazeTrackingLibrary.Settings;
using GazeTrackingLibrary.Utils;

namespace GazeTrackingLibrary
{
    public class Visualization
    {

        #region Variables

        private Image<Gray, byte> glintImage;
        private Image<Gray, byte> gray;
        private int height;
        private Image<Bgr, byte> processed;
        private Image<Gray, byte> pupilImage;
        private int width;

        #endregion


        #region Public methods (Visualize)

        public void Visualize(TrackData trackData)
        {
            if (gray == null)
                return;

            if (GTSettings.Current.Visualization.VideoMode == VideoModeEnum.RawNoTracking)
                return; // no further actions


            #region Paint processed

            if (GTSettings.Current.Visualization.VideoMode == VideoModeEnum.Processed)
            {
                processed = gray.Convert<Bgr, byte>();
                width = processed.Width;
                height = processed.Height;

                #region Draw threshold pupil

                if (GTSettings.Current.Visualization.DrawPupil)
                {
                    if (trackData.LeftROI.Y != 0) //roi
                        ThresholdColorizePupil(
                            trackData.LeftROI,
                            GTSettings.Current.Processing.PupilThresholdLeft,
                            GTSettings.Current.Visualization.PupilThresholdColor);

                    if (trackData.RightROI.Y != 0) //roi
                        ThresholdColorizePupil(
                            trackData.RightROI,
                            GTSettings.Current.Processing.PupilThresholdRight,
                            GTSettings.Current.Visualization.PupilThresholdColor);

                    if (trackData.LeftROI.Y == 0 && trackData.RightROI.Y == 0) //full image
                        ThresholdColorizePupilFullImage(GTSettings.Current.Visualization.PupilThresholdColor);
                }

                #endregion

                #region Draw glints glints

                if (GTSettings.Current.Processing.TrackingGlints)
                {
                    if (trackData.LeftROI.Y != 0) //roi
                        ThresholdColorizeGlints(
                            trackData.LeftROI,
                            GTSettings.Current.Processing.GlintThresholdLeft,
                            GTSettings.Current.Visualization.GlintThresholdColor);

                    if (trackData.RightROI.Y != 0) //roi
                        ThresholdColorizeGlints(trackData.RightROI,
                                                GTSettings.Current.Processing.GlintThresholdRight,
                                                GTSettings.Current.Visualization.GlintThresholdColor);

                    if (trackData.LeftROI.Y == 0 && trackData.RightROI.Y == 0) //full image
                        ThresholdColorizeGlintsFullImage(GTSettings.Current.Visualization.GlintThresholdColor);
                }

                #endregion
            }

            #endregion


            #region Draw roi, pupil, glint crosses etc.

            // Eye ROI
            if (GTSettings.Current.Visualization.DrawEyesROI && trackData.EyesROI.Width != 0)
                DrawEyesROI(trackData.EyesROI);

            if (GTSettings.Current.Visualization.DrawEyeROI && trackData.LeftROI.Width != 0)
                DrawEyeROI(trackData.LeftROI);

            if (GTSettings.Current.Visualization.DrawEyeROI && trackData.RightROI.Width != 0)
                DrawEyeROI(trackData.RightROI);

            // Pupil
            if (GTSettings.Current.Visualization.DrawPupil && trackData.PupilDataLeft.Center.Y != 0)
                DrawPupil(trackData.PupilDataLeft.Center.ToPoint(), GTSettings.Current.Processing.PupilSizeMaximum*2);

            if (GTSettings.Current.Visualization.DrawPupil && trackData.PupilDataRight.Center.Y != 0)
                DrawPupil(trackData.PupilDataRight.Center.ToPoint(), GTSettings.Current.Processing.PupilSizeMaximum*2);

            // Glint
            if (GTSettings.Current.Processing.TrackingGlints)
            {
                if (trackData.GlintDataLeft.Glints != null && trackData.GlintDataLeft.Glints.Count != 0)
                    DrawGlints(trackData.GlintDataLeft.Glints, GTSettings.Current.Processing.GlintSizeMaximum/2);

                if (trackData.GlintDataRight.Glints != null && trackData.GlintDataRight.Glints.Count != 0)
                    DrawGlints(trackData.GlintDataRight.Glints, GTSettings.Current.Processing.GlintSizeMaximum/2);
            }

            #endregion

            Performance.Now.Stamp("Visualized");
        }

        #endregion


        #region Private paint thresholds

        #region Threshold sub-roi

        private void ThresholdColorizePupil(Rectangle roi, int threshold, Color color)
        {
            // Create thresholded images
            gray.ROI = roi;
            pupilImage = gray.ThresholdBinaryInv(new Gray(threshold), new Gray(255));
            gray.ROI = Rectangle.Empty;

            // Replace pixles in the processed color image with values from the sub-roi pixels that are not 0
            int x, y;

            for (x = 0; x < pupilImage.Width; x++)
            {
                for (y = 0; y < pupilImage.Height; y++)
                {
                    if (pupilImage.Data[y, x, 0] != 0)
                    {
                        // This is the "fast" way of doin' it
                        processed.Data[y + roi.Y, x + roi.X, 0] = color.B;
                        processed.Data[y + roi.Y, x + roi.X, 1] = color.G;
                        processed.Data[y + roi.Y, x + roi.X, 2] = color.R;
                    }
                }
            }
        }

        private void ThresholdColorizeGlints(Rectangle roi, int threshold, Color color)
        {
            // Create thresholded images
            gray.ROI = roi;
            glintImage = gray.ThresholdBinaryInv(new Gray(threshold), new Gray(255));
            gray.ROI = Rectangle.Empty;

            // Replace pixles in the processed color image with values from the sub-roi pixels that are not 0
            int x, y;

            for (x = 0; x < glintImage.Width; x++)
            {
                for (y = 0; y < glintImage.Height; y++)
                {
                    if (glintImage.Data[y, x, 0] == 0)
                    {
                        processed.Data[y + roi.Y, x + roi.X, 0] = color.B;
                        processed.Data[y + roi.Y, x + roi.X, 1] = color.G;
                        processed.Data[y + roi.Y, x + roi.X, 2] = color.R;
                    }
                }
            }
        }

        #endregion

        #region Threshold whole image

        private void ThresholdColorizePupilFullImage(Color color)
        {
            // Threshold (whole image)
            pupilImage = gray.ThresholdBinaryInv(new Gray(GTSettings.Current.Processing.PupilThreshold), new Gray(255));

            // Convert thresholded to color and add
            var pupilThresholdImage = new Image<Bgr, byte>(width, height,
                                                           new Bgr(GTSettings.Current.Visualization.PupilThresholdColor));
            pupilThresholdImage = pupilThresholdImage.And(pupilImage.Convert<Bgr, byte>());
            processed = processed.Add(pupilThresholdImage);
        }

        private void ThresholdColorizeGlintsFullImage(Color color)
        {
            // Threshold (whole image)
            glintImage = gray.ThresholdBinary(new Gray(GTSettings.Current.Processing.GlintThreshold), new Gray(255));

            // Convert thresholded to color and add
            // Negate the selected color (unary minus) otherwise yellow becomes blue, green = red etc. 
            Color c = GTSettings.Current.Visualization.GlintThresholdColor;
            Color glintNegated = Color.FromArgb(c.A, 255 - c.R, 255 - c.G, 255 - c.B);
            var glintThresholdImage = new Image<Bgr, byte>(width, height, new Bgr(glintNegated));
            glintThresholdImage = glintThresholdImage.And(glintImage.Convert<Bgr, byte>());
            processed = processed.Sub(glintThresholdImage);
        }

        #endregion

        #endregion


        #region Private draw methods (eye, pupil, glint)

        private void DrawEyesROI(Rectangle eyesROI)
        {
            if (eyesROI != null)
            {
                switch (GTSettings.Current.Visualization.VideoMode)
                {
                    case VideoModeEnum.Normal:
                        if (eyesROI != null)
                            DrawRectangle(gray, eyesROI, 255, 1);
                        break;

                    case VideoModeEnum.Processed:
                        if (eyesROI != null)
                            DrawRectangle(Processed, eyesROI, Color.White, 1);
                        break;
                }
            }
        }

        private void DrawEyeROI(Rectangle rectangle)
        {
            switch (GTSettings.Current.Visualization.VideoMode)
            {
                case VideoModeEnum.Normal:
                    DrawRectangle(gray, rectangle, GTSettings.Current.Visualization.EyeROIGray, 1);
                    break;
                case VideoModeEnum.Processed:
                    DrawRectangle(processed, rectangle, GTSettings.Current.Visualization.EyeROIColor, 1);
                    break;
            }
        }

        private void DrawPupil(Point pupilCenter, int lineLenght)
        {
            var pCircleMin = new CircleF(pupilCenter, GTSettings.Current.Processing.PupilSizeMinimum);
            var pCircleMax = new CircleF(pupilCenter, GTSettings.Current.Processing.PupilSizeMaximum);

            switch (GTSettings.Current.Visualization.VideoMode)
            {
                case VideoModeEnum.Normal:
                    DrawCross(gray, pupilCenter, lineLenght, GTSettings.Current.Visualization.PupilCrossGray, 1);
                    DrawCircle(gray, pCircleMin, GTSettings.Current.Visualization.PupilMinGray, 1);
                    DrawCircle(gray, pCircleMax, GTSettings.Current.Visualization.PupilMaxGray, 1);
                    break;

                case VideoModeEnum.Processed:
                    DrawCross(processed, pupilCenter, lineLenght, GTSettings.Current.Visualization.PupilCrossColor, 1);
                    DrawCircle(processed, pCircleMin, GTSettings.Current.Visualization.PupilMinColor, 1);
                    DrawCircle(processed, pCircleMax, GTSettings.Current.Visualization.PupilMaxColor, 1);
                    break;
            }
        }

        private void DrawGlints(GlintConfiguration glints, int lineLenght)
        {
            if (glints != null)
            {
                for (int i = 0; i < glints.Count; i++)
                {
                    double gcX = glints.Centers[i].X;
                    double gcY = glints.Centers[i].Y;

                    var gcPoint = new Point(Convert.ToInt32(gcX), Convert.ToInt32(gcY));
                    var gCircleMin = new CircleF(gcPoint, GTSettings.Current.Processing.GlintSizeMinimum);
                    var gCircleMax = new CircleF(gcPoint, GTSettings.Current.Processing.GlintSizeMaximum);

                    switch (GTSettings.Current.Visualization.VideoMode)
                    {
                        case VideoModeEnum.Normal:
                            DrawCross(gray, gcPoint, lineLenght, GTSettings.Current.Visualization.GlintCrossGray, 1);
                            //DrawCircle(gray, gCircleMin, GTSettings.Current.Visualization.GlintMinGray, 1);
                            //DrawCircle(gray, gCircleMax, GTSettings.Current.Visualization.GlintMaxGray, 1);
                            break;

                        case VideoModeEnum.Processed:
                            DrawCross(processed, gcPoint, lineLenght, GTSettings.Current.Visualization.GlintCrossColor, 1);
                            //DrawCircle(processed, gCircleMin, GTSettings.Current.Visualization.GlintMinColor, 1);
                            //DrawCircle(processed, gCircleMax, GTSettings.Current.Visualization.GlintMaxColor, 1);
                            break;
                    }
                }
            }
        }

        #endregion


        #region Private Draw-shapes methods (rectangle, cross)

        /// <summary>
        /// This method draws a cross at the given position.
        /// </summary>
        /// <param name="image">The input image.</param>
        /// <param name="point">The position of the cross.</param>
        /// <param name="size">The size of the cross.</param>
        /// <param name="color">The color of the cross.</param>
        /// <param name="thickness">The thickness of the cross.</param>
        private static void DrawCross(Image<Bgr, byte> image, Point point, int size, Color color, int thickness)
        {
            // Convert to System.Drawing.Point until EMGU updates its library
            var p1 = new Point(point.X - (size/2), point.Y);
            var p2 = new Point(point.X + (size/2), point.Y);

            image.Draw(new LineSegment2D(p1, p2), new Bgr(color), thickness);

            var p3 = new Point(point.X, point.Y - (size/2));
            var p4 = new Point(point.X, point.Y + (size/2));

            image.Draw(new LineSegment2D(p3, p4), new Bgr(color), thickness);
        }

        /// <summary>
        /// This method draws a cross at the given position.
        /// </summary>
        /// <param name="image">The input image.</param>
        /// <param name="point">The position of the cross.</param>
        /// <param name="size">The size of the cross.</param>
        /// <param name="color">The color of the cross.</param>
        /// <param name="thickness">The thickness of the cross.</param>
        private static void DrawCross(Image<Gray, byte> image, Point point, int size, int grayScale, int thickness)
        {
            // Convert to System.Drawing.Point until EMGU updates its library
            var p1 = new Point(point.X - (size/2), point.Y);
            var p2 = new Point(point.X + (size/2), point.Y);

            image.Draw(new LineSegment2D(p1, p2), new Gray(grayScale), thickness);

            var p3 = new Point(point.X, point.Y - (size/2));
            var p4 = new Point(point.X, point.Y + (size/2));

            image.Draw(new LineSegment2D(p3, p4), new Gray(grayScale), thickness);
        }

        /// <summary>
        /// This method draws a circle at the given position.
        /// </summary>
        /// <param name="image">The input image.</param>
        /// <param name="circle">The position of the circle.</param>
        /// <param name="color">The color of the cross.</param>
        /// <param name="thickness">The thickness of the cross.</param>
        private static void DrawCircle(Image<Gray, byte> image, CircleF circle, int grayScale, int thickness)
        {
            //image.Draw(circle, new Gray(grayScale), thickness);
        }

        private static void DrawCircle(Image<Bgr, byte> image, CircleF circle, Color color, int thickness)
        {
            //image.Draw(circle, new Bgr(color), thickness);
        }

        private static void DrawRectangle(Image<Bgr, byte> image, Rectangle rectangle, Color color, int thickness)
        {
            if (rectangle.Width > 0 && rectangle.Height > 0)
                image.Draw(rectangle, new Bgr(color), thickness);
        }

        private static void DrawRectangle(Image<Gray, byte> image, Rectangle rectangle, int grayScale, int thickness)
        {
            if (rectangle.Width > 0 && rectangle.Height > 0)
                image.Draw(rectangle, new Gray(grayScale), thickness);
        }

        #endregion


        #region Get/Set

        public Image<Gray, byte> Gray
        {
            get { return gray; }
            set { gray = value; }
        }

        public Image<Bgr, byte> Processed
        {
            get { return processed; }
            set { processed = value; }
        }

        public Image<Gray, byte> PupilImage
        {
            get { return pupilImage; }
            set { pupilImage = value; }
        }

        public Image<Gray, byte> GlintImage
        {
            get { return glintImage; }
            set { glintImage = value; }
        }

        #endregion
    }
}