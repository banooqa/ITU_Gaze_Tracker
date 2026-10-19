using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;

namespace GazeTrackerUI.TrackerViewer
{
	
	public partial class PerformanceCountersUC : UserControl
	{

        #region Variabels

        private long sampleCounter = 0;
        private double cpuLoad = 0;
        private double memLoad = 0;
        private ulong installedMemory = 0;
        private double availableMemory = 0;
        private Process process = null;
        private PerformanceCounter pcCPU = null;
        private PerformanceCounter pcMem = null;
	    private SolidColorBrush normal = new SolidColorBrush(Color.FromArgb(255, 190, 190, 190));
        private SolidColorBrush high = new SolidColorBrush(Colors.Red);

        #endregion


        #region Constructor 

        public PerformanceCountersUC()
		{
			this.InitializeComponent();
        }

        #endregion


        #region Public methods

        public void Update(double videoFPS, double trackingFPS) 
        {
            // Set labels
            LabelFPS.Content = trackingFPS;
            LabelCPU.Content = GetCPULoad(trackingFPS) + "%";
            LabelMem.Content = memLoad + "Mb";

            // Set colors
            SetLabelColor(LabelFPS, videoFPS/2, trackingFPS, true);
            SetLabelColor(LabelCPU, 50, cpuLoad, true);
            SetLabelColor(LabelMem, GetTotalMemory()/2, memLoad, false);
        }

        #endregion


        #region Private methods

        private void SetLabelColor(Label label, double thresholdValue, double value, bool isLessThan) 
        {
            if(isLessThan == true)
            {
                if (value < thresholdValue)
                    label.Foreground = high;
                else
                    label.Foreground = normal;
            }
            else
            {
                if (value > thresholdValue)
                    label.Foreground = high;
                else
                    label.Foreground = normal;
            }
        }

        private double GetCPULoad(double trackingFPS) 
        {
            process = Process.GetCurrentProcess();

            if(pcCPU == null)
            {
                pcCPU = new PerformanceCounter("Process", "% Processor Time", process.ProcessName);
                pcCPU.NextValue();
                pcMem = new PerformanceCounter("Memory", "Available MBytes");
                pcMem.NextValue();
            }

            sampleCounter++;

            // Get CPU time (once per second)
            if (sampleCounter > trackingFPS)
            {
               cpuLoad = pcCPU.NextValue();
               memLoad = process.PrivateMemorySize64 / 1024 / 1024; // Kb/Mb.
               sampleCounter = 0;
            }

            return Math.Round(cpuLoad/System.Environment.ProcessorCount, 0);
        }

        private ulong GetTotalMemory()
        {
            if(installedMemory == 0)
            {
                MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                    installedMemory = memStatus.ullTotalPhys;
            }

            return installedMemory/1024/1024;
        }

        #endregion


        #region Get physical memory size

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
          private class MEMORYSTATUSEX
          {
             public uint dwLength;
             public uint dwMemoryLoad;
             public ulong ullTotalPhys;
             public ulong ullAvailPhys;
             public ulong ullTotalPageFile;
             public ulong ullAvailPageFile;
             public ulong ullTotalVirtual;
             public ulong ullAvailVirtual;
             public ulong ullAvailExtendedVirtual;
             public MEMORYSTATUSEX()
             {
                 this.dwLength = (uint) Marshal.SizeOf(this);
             }
        }
        
      [return: MarshalAs(UnmanagedType.Bool)]
      [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        #endregion

    }

}