using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using GazeTrackerClient;
using GazeTrackingLibrary.Logging;
using GazeTrackingLibrary.Settings;
using GTCommons;

namespace GazeTrackerUI.Network
{
    public class TCPIPServer
    {
        private IPAddress ipAddress = IPAddress.Loopback; // Default
        private bool isEnabled;
        private Thread listenThread;
        private const int messageLength = 512; //bytes
        private int port = 6666;
        private TcpClient tcpClient;
        private TcpListener tcpListener;

        #region Commands

        #region Delegates

        public delegate void TrackerStatusHandler();

        #endregion

        public event TrackerStatusHandler OnTrackerStatus;

        #region Calibration

        #region Delegates

        public delegate void CalibrationAbortHandler();

        /* Henrik & Javier Test */
        public delegate void CalibrationFeedbackPointHandler(
            long time, int packagenumber, int targetX, int targetY, int gazeX, int gazeY, float distance,
            int acquisitionTime);
        public delegate void CalibrationUpdateMethod(int method);
        /* test end */

        public delegate void CalibrationParametersHandler(CalibrationParameters calParams);

        public delegate void CalibrationStartHandler();

        #endregion

        public event CalibrationStartHandler OnCalibrationStart;

        public event CalibrationAbortHandler OnCalibrationAbort;

        public event CalibrationParametersHandler OnCalibrationParameters;

        /* Henrik & Javier Test */
        public event CalibrationFeedbackPointHandler OnCalibrationFeedbackPoint; //new calibration feedback point
        public event CalibrationUpdateMethod onCalibrationUpdateMethod; //change the update method in the tracker
        /* test end */

        #endregion

        #region DataStream

        #region Delegates

        public delegate void DataStreamStartHandler();

        public delegate void DataStreamStopHandler();

        #endregion

        public event DataStreamStartHandler OnDataStreamStart;

        public event DataStreamStartHandler OnDataStreamStop;

        #endregion

        #region Log

        #region Delegates

        public delegate void LogPathGetHandler();

        public delegate void LogPathSetHandler(string path);

        public delegate void LogStartHandler();

        public delegate void LogStopHandler();

        public delegate void LogWriteLineHandler(string line);

        #endregion

        public event LogStartHandler OnLogStart;
        public event LogStopHandler OnLogStop;

        public event LogWriteLineHandler OnLogWriteLine;

        public event LogPathGetHandler OnLogPathGet;

        public event LogPathSetHandler OnLogPathSet;

        #endregion

        #region Camera

        #region Delegates

        public delegate void CameraSettingsHandler(CameraSettings camSettings);

        #endregion

        public event CameraSettingsHandler OnCameraSettings;

        #endregion

        #region GazeTrackerUI

        #region Delegates

        public delegate void UIMinimizeHandler();

        public delegate void UIRestoreHandler();

        public delegate void UISettingsCameraHandle();

        public delegate void UISettingsHandle();

        #endregion

        public event UIMinimizeHandler OnUIMinimize;
        public event UIRestoreHandler OnUIRestore;

        public event UISettingsHandle OnUISettings;

        public event UISettingsCameraHandle OnUISettingsCamera;

        #endregion

        #endregion

        public TCPIPServer()
        {
            tcpClient = new TcpClient();
            GTSettings.Current.Network.PropertyChanged += Network_PropertyChanged;
        }

        private void Network_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TCPIPServerEnabled")
            {
                if (GTSettings.Current.Network.TCPIPServerEnabled)
                    Start();
                else
                    Stop();
            }
        }

        private void Start()
        {
            tcpListener = new TcpListener(IPAddress, port);
            listenThread = new Thread(ListenForClients);
            listenThread.SetApartmentState(ApartmentState.STA);
            listenThread.Start();
            Console.Out.WriteLine("Network: Tcp/ip server started on port " + port);
        }

        private void Stop()
        {
            tcpListener.Stop();
            listenThread.Abort();
            Console.Out.WriteLine("Network: Tcp/Ip server stopped.");
        }


        private void ListenForClients()
        {
            try
            {
                tcpListener.Start();

                while (true)
                {
                    //blocks until a client has connected to the server
                    TcpClient client = tcpListener.AcceptTcpClient();
                    var clientThread = new Thread(IncomingCommand);
                    clientThread.Start(client);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.ProcessException(ex, false);
                return;
            }
        }


        private void IncomingCommand(object client)
        {
            tcpClient = (TcpClient) client;
            NetworkStream clientStream = tcpClient.GetStream();

            var message = new byte[messageLength]; // used to be 4096

            while (true)
            {
                int bytesRead = 0;

                try
                {
                    //blocks until a client sends a message
                    bytesRead = clientStream.Read(message, 0, messageLength);
                }
                catch (Exception ex)
                {
                    //a socket error has occured
                    ErrorLogger.ProcessException(ex, true);
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }

                //message has successfully been received
                var encoder = new ASCIIEncoding();
                string command = encoder.GetString(message, 0, bytesRead);
                ExecuteCommand(command);
            }

            //tcpClient.Close(); //I'm keeping the connection open
        }

        private void ExecuteCommand(string command)
        {
            if (command == null) return;

            char[] seperator = {' '};
            string[] cmd = command.Split(seperator, 50);

            string cmdStr = cmd[0];
            string cmdParam = "";

            if (cmd.Length > 1)
                cmdParam = cmd[1];

            Console.Out.WriteLine("Network command: " + cmdStr + " " + cmdParam);

            switch (cmdStr)
            {
                    //    case Commands.TrackerStatus:
                    //        if (OnTrackerStatus != null)
                    //            OnTrackerStatus();
                    //        break;


                    //    #region Data Output

                    //   case Commands.StreamStart:
                    //        if (OnDataStreamStart != null)
                    //            OnDataStreamStart();
                    //        break;

                    //    case Commands.StreamStop:
                    //        if (OnDataStreamStop != null)
                    //            OnDataStreamStop();
                    //        break;

                    //    case Commands.StreamFormat:
                    //        // No returning parameters
                    //        break;

                    //    #endregion


                    //    #region Calibration

                case Commands.CalibrationStart:
                    GTCommands.Instance.Calibration.Start();
                    //if (OnCalibrationStart != null)
                    //    OnCalibrationStart();
                    //OnCalibrationStart(Int32.Parse(cmd[1])); // Number of points
                    break;

                    //    case Commands.CalibrationAcceptPoint:
                    //        // No returning parameter/data 
                    //        break;

                case Commands.CalibrationAbort:
                    GTCommands.Instance.Calibration.Abort();
                    //        //if (OnCalibrationAbort != null)
                    //        //    OnCalibrationAbort();
                    break;


                    //    case Commands.CalibrationPointChange:
                    //        //OnCalibrationPointChange(Int32.Parse(cmd[1])); // Next point number
                    //        break;

                    //    case Commands.CalibrationParameters:

                    //        CalibrationParameters calParams = new CalibrationParameters();
                    //        calParams.ExtractParametersFromString(cmdParam);

                    //        if (OnCalibrationParameters != null)
                    //            OnCalibrationParameters(calParams);

                    //        break;


                    //    //case CalibrationAreaSize:
                    //    //    break;

                    //    //case CalibratitonEnd:
                    //    //    if (OnCalibratitonEnd != null)
                    //    //        OnCalibratitonEnd();
                    //    //    break;

                    //    //case CalibrationCheckLevel:
                    //    //    if (OnCalibrationCheckLevel != null)
                    //    //        OnCalibrationCheckLevel(Int32.Parse(cmd[1]));
                    //    //    break;

                case Commands.CalibrationPoint:
					//Console.WriteLine("New calibration point from dedicated interface: " + command);

                    //How many points have been buffeded?
                    List<int> CalPointsIndex = new List<int>();
                    for (int c = 0; c < cmd.Length; c++)
                        if (cmd[c] == "CAL_POINT")
                            CalPointsIndex.Add(c);

                    for (int c = 0; c < CalPointsIndex.Count; c++)
                    {
                        OnCalibrationFeedbackPoint(
                            long.Parse(cmd[CalPointsIndex[c] + 1]),     //time
                            int.Parse(cmd[CalPointsIndex[c] + 2]),      //packace number
                            int.Parse(cmd[CalPointsIndex[c] + 3]),      //targetX
                            int.Parse(cmd[CalPointsIndex[c] + 4]),      //targetY
                            int.Parse(cmd[CalPointsIndex[c] + 5]),      //gazeX
                            int.Parse(cmd[CalPointsIndex[c] + 6]),      //gazeY
                            float.Parse(cmd[CalPointsIndex[c] + 7]),    //distance - will not be used
                            int.Parse(cmd[CalPointsIndex[c] + 8]));     //acquisition time 
                    }
                    break;

                case Commands.CalibrationUpdateMethod:
                    Console.Write("Calibration Update Method Changed to:" + cmdParam);
                    onCalibrationUpdateMethod(int.Parse(cmdParam));
                    //GTSettings.Current.Calibration.RecalibrationType = GazeTrackingLibrary.Utils.RecalibrationTypeEnum.Continuous
                    break; 
                    
                
                    //    //case CalibrationStartDriftCorrection:
                    //    //    break;

                    //    //case CalibrationValidate:
                    //    //  if (OnCalibrationValidate != null)
                    //    //     OnCalibrationValidate( cmd[1], double.Parse(cmd[2]), double.Parse(cmd[3]), double.Parse(cmd[4]));
                    //    //break;

                    //    #endregion


                    //    #region Logging

                case Commands.LogStart:
                    GTSettings.Current.FileSettings.LoggingEnabled = true;
                    break;

                case Commands.LogStop:
                    GTSettings.Current.FileSettings.LoggingEnabled = false;
                    break;

                case Commands.LogPathSet:
                    GTSettings.Current.FileSettings.LogFilePath = cmdParam;
                    break;

                    //case Commands.LogWriteLine:
                    //     GTCommands.Instance.
                    //        if (OnLogWriteLine != null)
                    //            OnLogWriteLine(cmdParam);
                    //        break;


                    //case Commands.LogPathGet:
                    //    break;

                    //    #endregion

                    //    case Commands.CameraParameters:

                    //        // Update camera settings
                    //        GTSettings.Current.Camera.ExtractParametersFromString(cmdParam);

                    //        //if (OnCameraSettings != null)
                    //        //    OnCameraSettings(camSettings);

                    //        break;

                    //#region GT U.I

                    //case Commands.UIMinimize:
                    //if (OnUIMinimize != null)
                    //        OnUIMinimize();
                    //    break;

                    //case Commands.UIRestore:
                    //    if (OnUIRestore != null)
                    //        OnUIRestore();
                    //    break;

                    //case Commands.UISettings:
                    //    if (OnUISettings != null)
                    //        OnUISettings();
                    //    break;

                    //case Commands.UISettingsCamera:
                    //    if (OnUISettingsCamera != null)
                    //        OnUISettingsCamera();
                    //    break;

                    //#endregion
            }
        }


        private void SendMessage(string message)
        {
            NetworkStream clientStream = tcpClient.GetStream();
            var encoder = new ASCIIEncoding();
            byte[] buffer = encoder.GetBytes(message);

            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();
        }

        #region Get/Set

        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                if (isEnabled && value == false)
                {
                    isEnabled = false;
                    Stop();
                }
                else if (isEnabled == false && value)
                {
                    isEnabled = true;
                    Start();
                }
            }
        }


        public IPAddress IPAddress
        {
            get { return ipAddress; }
            set { ipAddress = value; }
        }


        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        internal void Close()
        {
            if (tcpClient != null)
                tcpClient.Close();

            listenThread.Abort();
        }

        #endregion
    }
}