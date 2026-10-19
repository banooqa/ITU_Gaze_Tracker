using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using GazeTrackingLibrary.Utils;

namespace GazeTrackingLibrary
{
    public class TrackDB
    {
        #region Variables 

        private static TrackDB instance;
        private readonly int logSize = 50;
        private Queue<TrackData> db;

        #endregion

        #region Constructor

        private TrackDB()
        {
            db = new Queue<TrackData>(logSize);
        }

        private TrackDB(int logSize)
        {
            this.logSize = logSize;
            db = new Queue<TrackData>(logSize);
        }

        public void AddSample(TrackData t)
        {
            if (db.Count > logSize)
                db.Dequeue();

            db.Enqueue(t);

            if (t.LeftROI.Y == t.RightROI.Y && t.LeftROI.X == t.RightROI.X)
                if (t.LeftROI.Y != 0 && t.RightROI.X != 0)
                    Console.Out.WriteLine("Same..");

            //Log.Performance.Now.Stamp("TrackDB Add");
        }

        #endregion

        #region ROI estimation methods

        public TrackData GetLastSample()
        {
            return db.Count == 0 ? new TrackData() : db.Last();
        }

        #endregion

        #region Variance calculations

        public double GetVariancePupilArea(EyeEnum eye)
        {
            var list = new List<double>();

            foreach (TrackData td in db)
            {
                if (eye == EyeEnum.Left && td.PupilDataLeft.Blob != null)
                    list.Add(td.PupilDataLeft.Blob.Area);

                if (eye == EyeEnum.Right && td.PupilDataRight.Blob != null)
                    list.Add(td.PupilDataRight.Blob.Area);
            }

            if (list.Count != 0)
                return Operations.Variance(list.ToArray());
            else
                return 9999;
        }

        public double GetVariancePupilFullness(EyeEnum eye)
        {
            var list = new List<double>();

            foreach (TrackData td in db)
            {
                if (eye == EyeEnum.Left && td.PupilDataLeft.Blob != null)
                    list.Add(td.PupilDataLeft.Blob.Fullness);

                if (eye == EyeEnum.Right && td.PupilDataRight.Blob != null)
                    list.Add(td.PupilDataRight.Blob.Fullness);
            }

            if (list.Count != 0)
                return Operations.Variance(list.ToArray());
            else
                return 9999;
        }

        public double GetSTDPupilFullness(EyeEnum eye)
        {
            var list = new List<double>();

            foreach (TrackData td in db)
            {
                if (eye == EyeEnum.Left && td.PupilDataLeft.Blob != null)
                    list.Add(td.PupilDataLeft.Blob.Fullness);

                if (eye == EyeEnum.Right && td.PupilDataRight.Blob != null)
                    list.Add(td.PupilDataRight.Blob.Fullness);
            }

            if (list.Count != 0)
                return Operations.StandardDeviation(list.ToArray());
            else
                return 9999;
        }

        public double GetMeanPupilFullness(EyeEnum eye)
        {
            var list = new List<double>();

            foreach (TrackData td in db)
            {
                if (eye == EyeEnum.Left && td.PupilDataLeft.Blob != null)
                    list.Add(td.PupilDataLeft.Blob.Fullness);

                if (eye == EyeEnum.Right && td.PupilDataRight.Blob != null)
                    list.Add(td.PupilDataRight.Blob.Fullness);
            }

            if (list.Count != 0)
                return Operations.Mean(list.ToArray());
            else
                return 9999;
        }

        #endregion

        #region Slopes calculations

        public PointF GetSlopePupilFullness(EyeEnum eye, int numSamples)
        {
            TrackData[] tds = db.ToArray();
            var points = new PointF[numSamples];

            var direction = new PointF();
            var pointOnLine = new PointF();

            for (int i = tds.Length - 1; i >= tds.Length - numSamples; i--)
            {
                if (eye == EyeEnum.Left && tds[i].PupilDataLeft.Blob != null)
                {
                    points[tds.Length - i - 1] = new PointF(i, (float) tds[i].PupilDataLeft.Blob.Fullness);
                }
            }

            PointCollection.Line2DFitting(points, DIST_TYPE.CV_DIST_L2, out direction, out pointOnLine);

            return direction;
        }

        #endregion

        #region Hit ratio calculations

        public double CalculatePupilHitRatio(EyeEnum eye, int numberOfSample)
        {
            int misses = 0;

            foreach (TrackData t in db)
            {
                if (eye == EyeEnum.Left && t.PupilDataLeft.Center.X == 0)
                    misses++;
                else if (eye == EyeEnum.Right && t.PupilDataRight.Center.X == 0)
                    misses++;
            }

            return (double) misses/db.Count*100;
        }

        public double CalculateGlintHitRatio(EyeEnum eye)
        {
            int misses = 0;

            foreach (TrackData t in db)
            {
                if (eye == EyeEnum.Left && t.GlintDataLeft.Glints.Count == 0)
                    misses++;
                else if (eye == EyeEnum.Right && t.GlintDataRight.Glints.Count == 0)
                    misses++;
            }

            return (double) misses/db.Count*100;
        }

        #endregion

        #region Public Get/Search samples

        public Rectangle GetLastEyeROI(EyeEnum eye, Size imgSize)
        {
            TrackData[] tds = db.ToArray();

            for (int i = tds.Length - 1; i >= 0; i--)
            {
                if (eye == EyeEnum.Left && tds[i].LeftROI.Width != 0)
                    return tds[i].LeftROI;
                if (eye == EyeEnum.Right && tds[i].RightROI.Width != 0)
                    return tds[i].RightROI;
            }

            return new Rectangle(0, 0, imgSize.Width, imgSize.Height);
        }


        public IEnumerable<TrackData> GetBySamples(int numberOfSamples, TrackDBFilter filterOptions)
        {
            switch (filterOptions)
            {
                case TrackDBFilter.All:
                    return db.ToList();

                case TrackDBFilter.EyeBothFound:

                    IEnumerable<TrackData> qBothEyesFound =
                        from td in db
                        where td.LeftROI.Width != 0 && td.RightROI.Width != 0
                        select td;

                    return qBothEyesFound;

                case TrackDBFilter.EyeLeftFound:

                    IEnumerable<TrackData> qLeftEyeFound =
                        from td in db
                        where td.LeftROI.Width != 0
                        select td;

                    return qLeftEyeFound;

                case TrackDBFilter.EyeRightFound:

                    IEnumerable<TrackData> qRightEyeFound =
                        from td in db
                        where td.RightROI.Width != 0
                        select td;

                    return qRightEyeFound;
            }

            return db.ToList();
        }

        public List<TrackData> GetByTime(int milliseconds, TrackDBFilter filterOptions)
        {
            return db.ToList();
        }

        #endregion

        #region Get/Set

        public static TrackDB Instance
        {
            get { return instance ?? (instance = new TrackDB()); }
        }

        public Queue<TrackData> Data
        {
            get { return db; }
            set { db = value; }
        }

        #endregion


    }
}