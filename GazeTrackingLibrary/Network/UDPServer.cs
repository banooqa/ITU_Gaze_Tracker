using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using GazeTrackerClient;
using GazeTrackingLibrary.Logging;
using GazeTrackingLibrary.Settings;

namespace GazeTrackingLibrary.Network
{
    public class UDPServer
    {
        #region Variables

        private readonly Socket socket;
        private readonly StreamFormat streamFormat;

        private IPAddress _ipAddress = IPAddress.Loopback; // Default
        private int _port = 6666; // Default

        private IPEndPoint endPoint;

        private bool isEnabled;

        private double prevX;
        private double prevY;
        private bool sendSmoothedData = true;

        #endregion

        private long prevTime;

        public UDPServer()
        {
            streamFormat = new StreamFormat();
            streamFormat.TimeStampMilliseconds = true;
            streamFormat.GazePosition = true;

            // Listen for changes in settings, start/stop
            GTSettings.Current.Network.PropertyChanged += Network_PropertyChanged;

            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }
            catch (Exception ex)
            {
                ErrorLogger.ProcessException(ex, false);
            }
        }

        public IPAddress IPAddress
        {
            get { return _ipAddress; }
            set { _ipAddress = value; }
        }

        public int Port
        {
            get { return _port; }
            set
            {
                _port = value;
                endPoint = new IPEndPoint(IPAddress, Port);
            }
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                if (isEnabled && value == false)
                    Stop();
                else if (isEnabled == false && value)
                    Start();

                isEnabled = value;
            }
        }

        public bool IsStreamingGazeData { get; set; }

        public bool SendSmoothedData
        {
            get { return sendSmoothedData; }
            set { sendSmoothedData = value; }
        }

        private void Network_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "UDPServerEnabled")
            {
                if (GTSettings.Current.Network.UDPServerEnabled)
                    Start();
                else
                    Stop();
            }
        }


        private void Start()
        {
            IsStreamingGazeData = true;
            Console.Out.WriteLine("Network: UDP server started on port: " + _port);
        }

        private void Stop()
        {
            IsStreamingGazeData = false;
            Console.Out.WriteLine("Network: UDP server stopped.");
        }


        public void SendMessage(string message)
        {
            if (endPoint == null)
                endPoint = new IPEndPoint(IPAddress, Port);

            try
            {
                byte[] sendbuf = Encoding.ASCII.GetBytes(message);
                socket.SendTo(sendbuf, endPoint);
            }
            catch (Exception ex)
            {
                ErrorLogger.ProcessException(ex, false);
            }
        }

        // Overload
        public void SendMessage(string message, string parameter)
        {
            SendMessage(message + " " + parameter);
        }

        //Overload
        public void SendMessage(string message, int parameter)
        {
            SendMessage(message + " " + parameter);
        }


        public void SendGazeData(double x, double y)
        {
            // If the endpoint hasn't been set with user specified port, use default port 6666
            if (endPoint == null)
                endPoint = new IPEndPoint(IPAddress, Port);

            long currentTime = DateTime.Now.Ticks/TimeSpan.TicksPerMillisecond;

            string message = Commands.StreamData + " ";

            if (streamFormat.TimeStampMilliseconds)
            {
                // ToDo: support milliseconds or micro seconds..
                //currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                message += currentTime + " ";
            }

            if (streamFormat.GazePosition)
                message += Math.Round(x, 3) + " " + Math.Round(y, 3);

            SendMessage(message);
        }


        public void Close()
        {
            try
            {
                isEnabled = false;
                socket.Close();
                socket.Disconnect(true);
            }
            catch (Exception e)
            {
                ErrorLogger.ProcessException(e, false);
            }
        }
    }
}