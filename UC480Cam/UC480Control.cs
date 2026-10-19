using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Interop;
using Emgu.CV;
using Emgu.CV.Structure;

namespace UC480Cam
{
    #region Using

    //using System.Diagnostics;

    #endregion

    public class UC480Control
    {

        #region Variables

        private UC480Camera m_uc480;
        private IntPtr hwnd = (IntPtr) 0;
        private string defaultParametersFile = "uc480.ini";

        private int counter;
        private Image<Gray, byte> grayImage;
        private int width = 1280;
        private int height = 1024;
        private int pitch;
        private Rectangle roi;
        private int stride;
        private int bits;

        private const int IMAGE_COUNT = 1;
        private readonly string parametersFileStr;
        private bool isImageFormatSet;
        private int m_RenderMode;
        private UC480IMAGE[] m_Uc480Images;
        private bool m_bDrawing;
        private bool m_bLive;
        private IntPtr m_pCurMem;

        private readonly BackgroundWorker workerInit = new BackgroundWorker();
        private BackgroundWorker workerGetImage = new BackgroundWorker();

        private struct UC480IMAGE
        {
            public int MemID;
            public int nSeqNum;
            public IntPtr pMemory;
        }

        #endregion


        #region Events

        #region Delegates

        public delegate void FrameCapHandler();

        #endregion

        public event FrameCapHandler FrameCaptureComplete;

        #endregion


        #region Constructor

        public UC480Control()
        {
            var ASCII = new ASCIIEncoding();

            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            var dirInfo = new DirectoryInfo(new Uri(dir).LocalPath);
            parametersFileStr = dirInfo.FullName + "\\" + defaultParametersFile;
        }

        public UC480Control(Rectangle roi)
        {
            this.roi = roi;
        }

        public UC480Control(string fullPathToConfigFile)
        {
            parametersFileStr = fullPathToConfigFile;
        }

        #endregion


        #region Get/Set

        public UC480Camera Camera
        {
            get { return m_uc480; }
        }

        public bool IsInitialized
        {
            get
            {
                if (m_uc480 == null)
                    return false;
                else
                    return true;
            }
        }

        public int Width
        {
            get { return width; }
        }

        public int Height
        {
            get { return height; }
        }

        public int FPS
        {
            get
            {
                double fps = 0;
                m_uc480.GetFramesPerSecond(ref fps);
                return Convert.ToInt32(fps);
            }
        }


        public Image<Gray, byte> VideoImage
        {
            get
            {
                Image<Gray, byte> returnImage = grayImage.Copy();
                return returnImage;
            }
        }

        private bool isRoiSet = false;

        public bool IsROISet
        {
            get { return isRoiSet;  }
        }

        public Rectangle ROI
        {
            get { return roi; }
        }


        #endregion


        #region Inititialize and start camera

        public HwndSource hwndSource;

        public void SetupWndProc(HwndSource hwndScr)
        {
            hwndSource = hwndScr;
            workerInit.DoWork += workerInit_DoWork;
            workerInit.RunWorkerAsync(hwndSource);
        }

        private void workerInit_DoWork(object sender, DoWorkEventArgs e)
        {
            var hwndSource = e.Argument as HwndSource;
            hwndSource.AddHook(WndProc);

            while (hwnd.ToInt32() == 0)
                Thread.Sleep(50);

            if (m_uc480 == null)
            {
                InitAndStartCamera(hwnd);
            }

            workerGetImage = new BackgroundWorker();
            workerGetImage.DoWork += workerGetImage_DoWork;
        }

        private bool InitAndStartCamera(IntPtr hwnd)
        {
            this.hwnd = hwnd; // long way to get that...phew..

            m_uc480 = new UC480Camera();
            m_Uc480Images = new UC480IMAGE[IMAGE_COUNT];

            if (m_uc480.InitCamera(1, hwnd.ToInt32()) != UC480Camera.IS_SUCCESS)
            {
                Console.Out.WriteLine("UC480 Camera init failed");
                return false;
            }

            // get full image size
            roi.Width = m_uc480.SetImageSize(UC480Camera.IS_GET_IMAGE_SIZE_X, 0);
            roi.Height = m_uc480.SetImageSize(UC480Camera.IS_GET_IMAGE_SIZE_Y, 0);

            // Load camera configuration file
            if (m_uc480.LoadParameters(parametersFileStr) != UC480Camera.IS_SUCCESS)
                Console.Out.WriteLine("UC480 Unable to load camera configuration file.");
            else
            {
                // Loaded successfully, set initial roi
                roi.Width = m_uc480.SetImageSize(UC480Camera.IS_GET_IMAGE_SIZE_X, 0);
                roi.Height = m_uc480.SetImageSize(UC480Camera.IS_GET_IMAGE_SIZE_Y, 0);
                roi.X = m_uc480.SetImagePos(UC480Camera.IS_GET_IMAGE_POS_X, 0);
                roi.Y = m_uc480.SetImagePos(UC480Camera.IS_GET_IMAGE_POS_Y, 0);
            }

            // Apply ROI
            this.SetROI(roi, 23);

            //this.ToggleGainBoost(false);

            // Allocate memory for images
            AllocateImageMemory(roi.Width, roi.Height);

            // Set monochrome
            m_uc480.SetColorMode(UC480Camera.IS_SET_CM_BAYER);

            // enables on new frame event
            m_uc480.EnableMessage(UC480Camera.IS_FRAME, hwnd.ToInt32()); 

            // Start live capture
            if (m_uc480.CaptureVideo(UC480Camera.IS_WAIT) == UC480Camera.IS_SUCCESS)
                m_bLive = true;
            else
                m_bLive = false;


            return m_bLive;
        }

        private void AllocateImageMemory(int w, int h) 
        {
            m_uc480.ClearSequence();

            for (int i = 0; i < IMAGE_COUNT; i++)
            {
                // alloc memory
                m_uc480.AllocImageMem(w, h, 32, ref m_Uc480Images[i].pMemory, ref m_Uc480Images[i].MemID);
                // add our memory to the sequence
                m_uc480.AddToSequence(m_Uc480Images[i].pMemory, m_Uc480Images[i].MemID);
                // set sequence number
                m_Uc480Images[i].nSeqNum = i + 1;
            }
        }

        #endregion


        #region Stop and cleanup

        public void Stop()
        {
            m_uc480.StopLiveVideo(0);
            m_uc480.ExitCamera();
        }

        public void Cleanup()
        {
            m_uc480.StopLiveVideo(0);

            //for (int i = 0; i < IMAGE_COUNT; i++)
            //    Marshal.FreeCoTaskMem(m_Uc480Images[i].pMemory);

            m_uc480.ExitCamera();
        }

        #endregion


        #region New image (by WndProc msg)

        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            this.hwnd = hwnd;

            // Listen for operating system messages
            switch (msg)
            {
                    // uc480 Message
                case UC480Camera.IS_UC480_MESSAGE:

                    switch (wParam.ToInt32())
                    {
                        case UC480Camera.IS_FRAME:
                            {
                                // losing some frames due to busy worker, tried pooling but gets filled on heavy processing.. solution?
                                //if (workerGetImage.IsBusy)
                                //    Thread.Sleep(1);

                                if (!workerGetImage.IsBusy)
                                    workerGetImage.RunWorkerAsync(wParam.ToInt32().ToString());
                            }
                            break;
                            //case UC480Camera.IS_DEVICE_REMOVAL:
                            //case UC480Camera.IS_NEW_DEVICE:
                    }

                    handled = true;
                    break;
            }
            return hwnd;
        }

        private void workerGetImage_DoWork(object sender, DoWorkEventArgs e)
        {
            counter++;
            m_bDrawing = true;

            // draw current memory if a camera is opened
            if (m_uc480.IsOpen() && m_bLive == true)
            {
                int num = 0;
                var pMem = new IntPtr();
                var pLast = new IntPtr();
                m_uc480.GetActSeqBuf(ref num, ref pMem, ref pLast);

                if (pLast.ToInt32() == 0)
                {
                    m_bDrawing = false;
                    return;
                }

                int nLastID = GetImageID(pLast);
                int nLastNum = GetImageNum(pLast);

                m_uc480.LockSeqBuf(nLastNum, pLast);

                if (isImageFormatSet == false)
                {
                    m_uc480.InquireImageMem(pLast, nLastID, ref width, ref height, ref bits, ref pitch);
                    stride = width*4;
                    if (stride%4 != 0)
                    {
                        stride += (4 - (stride%4));
                    }
                    isImageFormatSet = true;
                }

                //if(isRoiSet == false)
                //    grayImage = new Image<Gray, byte>(width, height, stride, pLast);
                //else
                    grayImage = new Image<Gray, byte>(roi.Width, roi.Height, stride, pLast);

                // When AOI/ROI has been set the active area will have been repositioned so (Y:300,X:20) will be 0,0 
                // Only need to allocate the active area
               //grayImage = new Image<Gray, byte>(roi.Width, roi.Height, stride, pLast);


                m_uc480.UnlockSeqBuf(nLastNum, pLast);

                if (FrameCaptureComplete != null)
                    FrameCaptureComplete();
            }
        }

        #endregion


        #region GetImageID/Number

        private int GetImageID(IntPtr pBuffer)
        {
            // get image id for a given memory
            if (!m_uc480.IsOpen())
                return 0;

            int i = 0;
            for (i = 0; i < IMAGE_COUNT; i++)
                if (m_Uc480Images[i].pMemory == pBuffer)
                    return m_Uc480Images[i].MemID;
            return 0;
        }

        private int GetImageNum(IntPtr pBuffer)
        {
            // get number of sequence for a given memory
            if (!m_uc480.IsOpen())
                return 0;

            int i = 0;
            for (i = 0; i < IMAGE_COUNT; i++)
                if (m_Uc480Images[i].pMemory == pBuffer)
                    return m_Uc480Images[i].nSeqNum;

            return 0;
        }

        #endregion


        private int lastfps = 0;
        private bool isSettingRoi = false;
        private bool hasTriedTwice = false;

        public void SetROI(Rectangle newRoi, int fps)
        {
            // Out of bounds
            if (newRoi.X > 1280 || newRoi.X < 0 || newRoi.Y > 1024 || newRoi.Y < 0 || newRoi.Width > 1280 || newRoi.Width < 0 ||
                newRoi.Height > 1024 || newRoi.Height < 0)
                return;

            // Same size
            if(roi.X == newRoi.X && roi.Y == newRoi.Y && roi.Width == newRoi.Width && roi.Height == newRoi.Height)
                return;

            // Not done with previous set-roi
            if(isSettingRoi)
               return;

            isSettingRoi = true;
            bool success = true;
            m_bLive = false;

            if(m_uc480.SetAOI(newRoi.X, newRoi.Y, newRoi.Width, newRoi.Height) != UC480Camera.IS_SUCCESS)
                success = false;

            //if (success == false)
            //{
            //    m_uc480.SetImageSize(newRoi.Width, newRoi.Height) != UC480Camera.IS_SUCCESS)
            //    m_uc480.SetImagePos(newRoi.X, newRoi.Y) != UC480Camera.IS_SUCCESS)
            //}

            m_bLive = true;

            if(success)
            {
                // Set ROI
                this.roi = newRoi;
                //Console.Out.WriteLine("ROI x:" + roi.X + " y:" + roi.Y + " w:" + roi.Width + " h:" + roi.Height);

                if (newRoi.Width < 1280 || newRoi.Height < 1024)
                    isRoiSet = true;
            }


            if(fps != lastfps)
            {
                SetFPS(fps); // Setting fps to high number will force highest possible framerate, eg. 500 will give 120 at 1000x250
                lastfps = fps;
            }

            isSettingRoi = false;
        }

        private void SetFPS(int fps)
        {
            double actualFPS = 0;

            if (m_uc480.SetFrameRate(fps, ref actualFPS) != UC480Camera.IS_SUCCESS)
                Console.Out.WriteLine("UC480 failed to set fps.");
            //else
            //    Console.Out.WriteLine("Tried to set fps, set " + fps + " and got " + actualFPS);
        }

        public void ClearROI()
        {
            ToggleGainBoost(false);
            SetROI(new Rectangle(new Point(0, 0), new Size(1280, 1024)), 20);
            isRoiSet = false;
        }

        public string GetDeviceName()
        {
            var camInfo = new UC480Camera.CAMINFO();

            if (m_uc480.GetCameraInfo(ref camInfo) == UC480Camera.IS_SUCCESS)
                return camInfo.id + "\t" + camInfo.Version + "\t" + camInfo.SerNo + "\t" + camInfo.Date;
            else
                return "UC480";
        }

        public void ToggleGainBoost(bool on) 
        {
            if(on)
               m_uc480.SetGainBoost(UC480Camera.IS_SET_GAINBOOST_ON);
            else
               m_uc480.SetGainBoost(UC480Camera.IS_SET_GAINBOOST_OFF);
        }
    }
}