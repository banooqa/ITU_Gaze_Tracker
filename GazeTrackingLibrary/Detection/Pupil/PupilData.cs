using GazeTrackingLibrary.Detection.BlobAnalysis;
using GazeTrackingLibrary.Utils;

namespace GazeTrackingLibrary.Detection.Pupil
{
    public class PupilData
    {
        public PupilData()
        {
            Eye = EyeEnum.Left;
            Center = new GTPoint();
            GrayCorners = new int[4];
        }

        public EyeEnum Eye { get; set; }

        public Blob Blob { get; set; }

        public GTPoint Center { get; set; }

        public int[] GrayCorners { get; set; }
    }
}