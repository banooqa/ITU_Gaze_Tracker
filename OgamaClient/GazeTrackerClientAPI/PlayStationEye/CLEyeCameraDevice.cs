// <copyright file="CLEyeCameraDevice.cs" company="FU Berlin">
// ******************************************************
// OgamaClient for ITU GazeTracker
// Copyright (C) 2010 Adrian Voßkühler  
// ------------------------------------------------------------------------
// This program is free software; you can redistribute it and/or modify it 
// under the terms of the GNU General Public License as published by the 
// Free Software Foundation; either version 3 of the License, 
// or (at your option) any later version.
// This program is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU 
// General Public License for more details.
// You should have received a copy of the GNU General Public License 
// along with this program; if not, see http://www.gnu.org/licenses/.
// **************************************************************
// </copyright>
// <author>Adrian Voßkühler</author>
// <email>adrian.vosskuehler@fu-berlin.de</email>

////////////////////////////////////////////////////////////////////////
// This file is part of CL-EyeMulticam SDK
// WPF C# Sample Application
// It allows the use of multiple CL-Eye cameras in your own applications
// For updates and file downloads go to: http://codelaboratories.com/research/view/cl-eye-muticamera-sdk
// Copyright 2008-2010 (c) Code Laboratories, Inc. All rights reserved.
// It has been modified to fulfill the needs of the OGAMA ITU client.
////////////////////////////////////////////////////////////////////////

namespace CLEyeMulticam
{
  using System;
  using System.Runtime.InteropServices;
  using System.Threading;
  using System.Windows;
  using System.Windows.Interop;
  using System.Windows.Media;
  using System.Windows.Media.Imaging;

  #region [ Camera Parameters ]
  // camera color mode
  public enum CLEyeCameraColorMode
  {
    CLEYE_GRAYSCALE,
    CLEYE_COLOR
  };

  // camera resolution
  public enum CLEyeCameraResolution
  {
    CLEYE_QVGA,					// Allowed frame rates: 15, 30, 60, 75, 100, 125
    CLEYE_VGA					// Allowed frame rates: 15, 30, 40, 50, 60, 75
  };
  // camera parameters
  public enum CLEyeCameraParameter
  {
    // camera sensor parameters
    CLEYE_AUTO_GAIN,			// [false, true]
    CLEYE_GAIN,					// [0, 79]
    CLEYE_AUTO_EXPOSURE,		// [false, true]
    CLEYE_EXPOSURE,				// [0, 511]
    CLEYE_AUTO_WHITEBALANCE,	// [false, true]
    CLEYE_WHITEBALANCE_RED,		// [0, 255]
    CLEYE_WHITEBALANCE_GREEN,	// [0, 255]
    CLEYE_WHITEBALANCE_BLUE,	// [0, 255]
    // camera linear transform parameters
    CLEYE_HFLIP,				// [false, true]
    CLEYE_VFLIP,				// [false, true]
    CLEYE_HKEYSTONE,			// [-500, 500]
    CLEYE_VKEYSTONE,			// [-500, 500]
    CLEYE_XOFFSET,				// [-500, 500]
    CLEYE_YOFFSET,				// [-500, 500]
    CLEYE_ROTATION,				// [-500, 500]
    CLEYE_ZOOM,					// [-500, 500]
    // camera non-linear transform parameters
    CLEYE_LENSCORRECTION1,		// [-500, 500]
    CLEYE_LENSCORRECTION2,		// [-500, 500]
    CLEYE_LENSCORRECTION3,		// [-500, 500]
    CLEYE_LENSBRIGHTNESS		// [-500, 500]
  };
  #endregion

  public class CLEyeCameraDevice : DependencyObject, IDisposable
  {
    #region [ CLEyeMulticam Imports ]
    [DllImport("CLEyeMulticam.dll")]
    public static extern int CLEyeGetCameraCount();
    [DllImport("CLEyeMulticam.dll")]
    public static extern Guid CLEyeGetCameraUUID(int camId);
    [DllImport("CLEyeMulticam.dll")]
    public static extern IntPtr CLEyeCreateCamera(Guid camUUID, CLEyeCameraColorMode mode, CLEyeCameraResolution res, int frameRate);
    [DllImport("CLEyeMulticam.dll")]
    public static extern bool CLEyeDestroyCamera(IntPtr camera);
    [DllImport("CLEyeMulticam.dll")]
    public static extern bool CLEyeCameraStart(IntPtr camera);
    [DllImport("CLEyeMulticam.dll")]
    public static extern bool CLEyeCameraStop(IntPtr camera);
    [DllImport("CLEyeMulticam.dll")]
    public static extern bool CLEyeSetCameraParameter(IntPtr camera, CLEyeCameraParameter param, int value);
    [DllImport("CLEyeMulticam.dll")]
    public static extern int CLEyeGetCameraParameter(IntPtr camera, CLEyeCameraParameter param);
    [DllImport("CLEyeMulticam.dll")]
    public static extern bool CLEyeCameraGetFrameDimensions(IntPtr camera, ref int width, ref int height);
    [DllImport("CLEyeMulticam.dll")]
    public static extern bool CLEyeCameraGetFrame(IntPtr camera, IntPtr pData, int waitTimeout);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool UnmapViewOfFile(IntPtr hMap);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CloseHandle(IntPtr hHandle);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenFileMapping(uint dwDesiredAccess, bool bInheritHandle, string lpName);



    #endregion

    #region [ Private ]
    private IntPtr map = IntPtr.Zero;
    private IntPtr section = IntPtr.Zero;
    private IntPtr _camera = IntPtr.Zero;
    private bool _running;
    private Thread _workerThread;
    private Emgu.CV.Image<Emgu.CV.Structure.Gray, byte> grayFrame;
    private Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte> colorFrame;
    #endregion

    public event EventHandler BitmapReady;

    public delegate void FrameCapHandler(Emgu.CV.IImage newFrame);
    public event FrameCapHandler FrameCaptureComplete;

    const UInt32 STANDARD_RIGHTS_REQUIRED = 0x000F0000;
    const UInt32 SECTION_QUERY = 0x0001;
    const UInt32 SECTION_MAP_WRITE = 0x0002;
    const UInt32 SECTION_MAP_READ = 0x0004;
    const UInt32 SECTION_MAP_EXECUTE = 0x0008;
    const UInt32 SECTION_EXTEND_SIZE = 0x0010;
    const UInt32 SECTION_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | SECTION_QUERY |
        SECTION_MAP_WRITE |
        SECTION_MAP_READ |
        SECTION_MAP_EXECUTE |
        SECTION_EXTEND_SIZE);
    const UInt32 FILE_MAP_ALL_ACCESS = SECTION_ALL_ACCESS;

    #region [ Properties ]
    public int Framerate { get; set; }

    private CLEyeCameraColorMode colorMode;

    public CLEyeCameraColorMode ColorMode
    {
      get
      {
        return this.colorMode;
      }
      set
      {
        this.colorMode = value;
        //this.ResetDevice();
      }
    }

    private CLEyeCameraResolution resolution;

    public CLEyeCameraResolution Resolution
    {
      get
      {
        return this.resolution;
      }
      set
      {
        this.resolution = value;
        //this.ResetDevice();
      }
    }

    public bool AutoGain
    {
      get
      {
        if (_camera == null) return false;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_AUTO_GAIN) != 0;
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_AUTO_GAIN, value ? 1 : 0);
      }
    }
    public int Gain
    {
      get
      {
        if (_camera == null) return 0;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_GAIN);
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_GAIN, value);
      }
    }
    public bool AutoExposure
    {
      get
      {
        if (_camera == null) return false;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_AUTO_EXPOSURE) != 0;
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_AUTO_EXPOSURE, value ? 1 : 0);
      }
    }
    public int Exposure
    {
      get
      {
        if (_camera == null) return 0;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_EXPOSURE);
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_EXPOSURE, value);
      }
    }
    public bool AutoWhiteBalance
    {
      get
      {
        if (_camera == null) return true;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_AUTO_WHITEBALANCE) != 0;
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_AUTO_WHITEBALANCE, value ? 1 : 0);
      }
    }
    public int WhiteBalanceRed
    {
      get
      {
        if (_camera == null) return 0;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_WHITEBALANCE_RED);
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_WHITEBALANCE_RED, value);
      }
    }
    public int WhiteBalanceGreen
    {
      get
      {
        if (_camera == null) return 0;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_WHITEBALANCE_GREEN);
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_WHITEBALANCE_GREEN, value);
      }
    }
    public int WhiteBalanceBlue
    {
      get
      {
        if (_camera == null) return 0;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_WHITEBALANCE_BLUE);
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_WHITEBALANCE_BLUE, value);
      }
    }
    public bool HorizontalFlip
    {
      get
      {
        if (_camera == null) return false;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_HFLIP) != 0;
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_HFLIP, value ? 1 : 0);
      }
    }
    public bool VerticalFlip
    {
      get
      {
        if (_camera == null) return false;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_VFLIP) != 0;
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_VFLIP, value ? 1 : 0);
      }
    }
    public int HorizontalKeystone
    {
      get
      {
        if (_camera == null) return 0;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_HKEYSTONE);
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_HKEYSTONE, value);
      }
    }
    public int VerticalKeystone
    {
      get
      {
        if (_camera == null) return 0;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_VKEYSTONE);
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_VKEYSTONE, value);
      }
    }
    public int XOffset
    {
      get
      {
        if (_camera == null) return 0;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_XOFFSET);
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_XOFFSET, value);
      }
    }
    public int YOffset
    {
      get
      {
        if (_camera == null) return 0;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_YOFFSET);
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_YOFFSET, value);
      }
    }
    public int Rotation
    {
      get
      {
        if (_camera == null) return 0;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_ROTATION);
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_ROTATION, value);
      }
    }
    public int Zoom
    {
      get
      {
        if (_camera == null) return 0;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_ZOOM);
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_ZOOM, value);
      }
    }
    public int LensCorrection1
    {
      get
      {
        if (_camera == null) return 0;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_LENSCORRECTION1);
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_LENSCORRECTION1, value);
      }
    }
    public int LensCorrection2
    {
      get
      {
        if (_camera == null) return 0;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_LENSCORRECTION2);
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_LENSCORRECTION2, value);
      }
    }
    public int LensCorrection3
    {
      get
      {
        if (_camera == null) return 0;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_LENSCORRECTION3);
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_LENSCORRECTION3, value);
      }
    }
    public int LensBrightness
    {
      get
      {
        if (_camera == null) return 0;
        return CLEyeGetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_LENSBRIGHTNESS);
      }
      set
      {
        if (_camera == null) return;
        CLEyeSetCameraParameter(_camera, CLEyeCameraParameter.CLEYE_LENSBRIGHTNESS, value);
      }
    }
    #endregion

    #region [ Static ]
    public static int CameraCount { get { return CLEyeGetCameraCount(); } }
    public static Guid CameraUUID(int idx) { return CLEyeGetCameraUUID(idx); }
    #endregion

    #region [ Dependency Properties ]
    public InteropBitmap BitmapSource
    {
      get { return (InteropBitmap)GetValue(BitmapSourceProperty); }
      private set { SetValue(BitmapSourcePropertyKey, value); }
    }
    private static readonly DependencyPropertyKey BitmapSourcePropertyKey =
        DependencyProperty.RegisterReadOnly("BitmapSource", typeof(InteropBitmap), typeof(CLEyeCameraDevice), new UIPropertyMetadata(default(InteropBitmap)));
    public static readonly DependencyProperty BitmapSourceProperty = BitmapSourcePropertyKey.DependencyProperty;
    #endregion

    #region [ Methods ]
    public CLEyeCameraDevice()
    {
      // set default values
      Framerate = 15;
      ColorMode = default(CLEyeCameraColorMode);
      Resolution = default(CLEyeCameraResolution);
    }

    ~CLEyeCameraDevice()
    {
      // Finalizer calls Dispose(false)
      Dispose(false);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
      if (disposing)
      {
        // free managed resources
        Stop();
      }
      // free native resources if there are any.
      Destroy();
    }

    private int width;
    private int height;
    private int bufferLength;
    private int stride;

    public bool Create(Guid cameraGuid)
    {
      _camera = CLEyeCreateCamera(cameraGuid, ColorMode, Resolution, Framerate);
      if (_camera == IntPtr.Zero) return false;
      CLEyeCameraGetFrameDimensions(_camera, ref width, ref height);
      if (ColorMode == CLEyeCameraColorMode.CLEYE_COLOR)
      {
        bufferLength = width * height * 4;
        // create memory section and map
        section = CreateFileMapping(new IntPtr(-1), IntPtr.Zero, 0x04, 0, (uint)bufferLength, "EyeVideo");
        map = MapViewOfFile(section, 0xF001F, 0, 0, (uint)bufferLength);
        BitmapSource = Imaging.CreateBitmapSourceFromMemorySection(
          section,
          width,
          height,
          PixelFormats.Bgr32,
          width * 4,
          0) as InteropBitmap;
        this.stride = width * ((BitmapSource.Format.BitsPerPixel + 7) / 8);
        this.colorFrame = new Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>(
          this.width,
          this.height,
          this.stride,
          map);
      }
      else
      {
        bufferLength = width * height;
        // create memory section and map
        section = CreateFileMapping(new IntPtr(-1), IntPtr.Zero, 0x04, 0, (uint)bufferLength, "EyeVideo");
        map = MapViewOfFile(section, FILE_MAP_ALL_ACCESS, 0, 0, (uint)bufferLength);
        BitmapSource = Imaging.CreateBitmapSourceFromMemorySection(
          section,
          width,
          height,
          PixelFormats.Gray8,
          width,
          0) as InteropBitmap;
        this.stride = width * ((BitmapSource.Format.BitsPerPixel + 7) / 8);
        this.grayFrame = new Emgu.CV.Image<Emgu.CV.Structure.Gray, byte>(
          this.width,
          this.height,
          this.stride,
          map);
      }

      // Invoke event
      if (BitmapReady != null)
      {
        BitmapReady(this, null);
      }

      BitmapSource.Invalidate();

      this.AutoExposure = true;
      this.AutoGain = true;
      this.AutoWhiteBalance = true;

      return true;
    }

    public void Destroy()
    {
      if (map != IntPtr.Zero)
      {
        UnmapViewOfFile(map);
        map = IntPtr.Zero;
      }
      if (section != IntPtr.Zero)
      {
        CloseHandle(section);
        section = IntPtr.Zero;
      }
    }

    public void Start()
    {
      _running = true;
      _workerThread = new Thread(new ThreadStart(CaptureThread));
      _workerThread.Start();
    }

    public void Stop()
    {
      if (!_running) return;
      _running = false;
      _workerThread.Join(1000);
    }

    public void ResetDevice()
    {
      this.Stop();
      this.Destroy();
      if (this.Create(CLEyeCameraDevice.CameraUUID(0)))
      {
        this.Start();
      }
    }

    void CaptureThread()
    {
      CLEyeCameraStart(_camera);
      while (_running)
      {
        if (CLEyeCameraGetFrame(_camera, map, 500))
        {
          if (!_running) break;
          //Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, (SendOrPostCallback)delegate
          //{
          //  BitmapSource.Invalidate();
          //}, null);
          //i++;

          if (this.FrameCaptureComplete != null)
          {
            switch (this.colorMode)
            {
              case CLEyeCameraColorMode.CLEYE_GRAYSCALE:
                this.FrameCaptureComplete(this.grayFrame);
                break;
              case CLEyeCameraColorMode.CLEYE_COLOR:
                this.FrameCaptureComplete(this.colorFrame);
                break;
            }
          }
        }
      }

      CLEyeCameraStop(_camera);
      CLEyeDestroyCamera(_camera);
    }
    #endregion

    [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory")]
    private static extern void CopyMemory(IntPtr Destination, IntPtr Source, int Length);

  }
}
