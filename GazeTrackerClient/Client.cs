using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

//using GazeTrackingLibrary.EyeMovementDetection;

namespace GazeTrackerClient
{
    // <Summary>
    // This class is work-in-progress (2009-10-08)
    // Not all commands are implemented yet.
    // </Summary>

    public class Client
    {
        #region Delegates

        public delegate void ConnectHandler(bool success);

        #endregion

        private readonly Calibration calibration;
        private readonly Camera camera;

        private readonly Commands commands;
        private readonly GazeData gazeData = new GazeData();
        private readonly Log log;
        private readonly Settings settings;
        private readonly Socket soUdpReceive;
        private readonly Stream stream;
        private readonly StreamFormat streamformat;
        private readonly Tracker tracker;
        private readonly UIControl uiControl;

        private Thread ThreadReceiveUDP;
        private bool isRunning; // Boolean to keep track if the server is running or not
        private MouseControl mouseControl;
        private TcpClient tcpClient;


        public Client()
        {
            settings = new Settings();
                // Will attempt to load "GazeTrackerSettings.xml" from execution dir. or set default

            soUdpReceive = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            commands = new Commands();

            stream = new Stream(this);
            streamformat = new StreamFormat();
            streamformat.TimeStampMilliseconds = true;
            streamformat.GazePosition = true;
            streamformat.EyetrackingType = StreamFormat.EyeTrackingType.Right;
            stream.StreamFormat = streamformat;

            tracker = new Tracker();
            camera = new Camera(this);
            calibration = new Calibration(this);
            uiControl = new UIControl(this);
            log = new Log(this);

            mouseControl = new MouseControl(); // Not fully implemented yet..

            // On new gaze data
            gazeData.OnSmoothedGazeData += mouseControl.Move;
        }

        public event ConnectHandler OnClientConnectionChanged;


        // Starts the client..
        public void Connect()
        {
            if (!isRunning)
            {
                try
                {
                    tcpClient = new TcpClient();
                    tcpClient.Connect(new IPEndPoint(settings.IPAddress, settings.TCPIPServerPort));
                    tcpClient.NoDelay = true;
                    tcpClient.SendTimeout = 500;

                    ThreadReceiveUDP = new Thread(StartListen);
                    ThreadReceiveUDP.Start();

                    isRunning = true;
                }
                catch (Exception e)
                {
                    Disconnect();
                    Console.Out.WriteLine("Error while connecting to eye tracker.");
                }
            }
        }

        // Stops the server..
        public bool Disconnect()
        {
            try
            {
                if (tcpClient.Connected)
                    tcpClient.Close();

                if (ThreadReceiveUDP != null)
                    ThreadReceiveUDP.Abort();

                if (soUdpReceive.Connected)
                    soUdpReceive.Disconnect(false);

                isRunning = false;

                if (OnClientConnectionChanged != null)
                    OnClientConnectionChanged(false);

                return isRunning;
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Could not stop the UDP thread.." + e);
                isRunning = false;
                return isRunning;
            }
        }

        #region Data In/Out

        // This is the main loop for receiving data from the tracker..
        private void StartListen()
        {
            IPHostEntry localHost;
            EndPoint remoteEp = (new IPEndPoint(settings.IPAddress, settings.UDPServerPort));

            try
            {
                try
                {
                    localHost = Dns.GetHostEntry(settings.IPAddress);
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine("Error connecting socket: " + e.Message);

                    if (OnClientConnectionChanged != null)
                        OnClientConnectionChanged(false);

                    return;
                }

                // Hook it up.. 
                if (!soUdpReceive.IsBound)
                {
                    var localIpEndPoint = new IPEndPoint(settings.IPAddress, settings.UDPServerPort);
                    soUdpReceive.Bind(localIpEndPoint);
                }

                if (OnClientConnectionChanged != null)
                    OnClientConnectionChanged(true);

                String datareceived = "";

                // True as long as the program is running..  read tracker data (yes, sort of ugly)
                while (isRunning)
                {
                    var received = new byte[256];

                    //EndPoint remoteEp = (new IPEndPoint(ipAddress, portRecive));

                    try
                    {
                        int bytesReceived = soUdpReceive.ReceiveFrom(received, ref remoteEp);
                    }
                    catch (Exception e)
                    {
                        if (OnClientConnectionChanged != null)
                            OnClientConnectionChanged(false);

                        Console.Out.WriteLine("Could not receive data. " + e.Message);
                    }

                    // Reformat to string and remove the empty bits \0\0\0 etc.
                    datareceived = Encoding.ASCII.GetString(received).Trim('\0');

                    if (datareceived.Length > 0)
                    {
                        char[] seperator = {'_'};
                        string[] data = datareceived.Split(seperator, 20);

                        switch (data[0])
                        {
                            case "TRACKER":
                                tracker.ExtractDataAndRaiseEvent(datareceived);
                                break;
                            case "STREAM":
                                stream.ExtractDataAndRaiseEvent(datareceived);
                                break;
                            case "CAL":
                                calibration.ExtractDataAndRaiseEvent(datareceived);
                                break;
                            case "LOG":
                                log.ExtractDataAndRaiseEvent(datareceived);
                                break;
                            case "UI":
                                uiControl.ExtractDataAndRaiseEvent(datareceived);
                                break;
                        }
                    }

                    //Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                if (OnClientConnectionChanged != null)
                    OnClientConnectionChanged(false);

                Console.Out.WriteLine("A socket error has occured: " + ex);
            }
        }


        // Sending simple commands to the tracker
        public void SendCommand(string cmd)
        {
            SendCommand(cmd, null);
        }

        // Sending commands with parameters
        public void SendCommand(string cmd, string value)
        {
            try
            {
                // TcpClient client = new TcpClient();
                // client.Connect(new IPEndPoint(ipAddress, portSend));

                NetworkStream clientStream = tcpClient.GetStream();
                var encoder = new ASCIIEncoding();

                byte[] buffer;

                if (value != null)
                    buffer = encoder.GetBytes(cmd + " " + value);
                else
                    buffer = encoder.GetBytes(cmd);

                clientStream.Write(buffer, 0, buffer.Length);
                clientStream.Flush();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Error in Client.SendCommand, message: " + ex.Message);

                if (OnClientConnectionChanged != null)
                    OnClientConnectionChanged(false);
            }
        }

        #endregion

        #region Get/Set

        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; }
        }

        public MouseControl MouseControl
        {
            get { return mouseControl; }
            set { mouseControl = value; }
        }


        //public bool SmoothData
        //{
        //    get { return gazeData.SmoothData; }
        //    set { gazeData.SmoothData = value; }
        //}

        //public int SmoothDataNumberOfSamples
        //{
        //    get { return gazeData.SmoothDataNumberOfSamples; }
        //    set { gazeData.SmoothDataNumberOfSamples = value; }
        //}


        public GazeData GazeData
        {
            get { return stream.GazeData; }
        }

        public IPAddress IPAddress
        {
            get { return settings.IPAddress; }
            set { settings.IPAddress = value; }
        }

        public string IPAddressString
        {
            get { return settings.IPAddress.ToString(); }
            set { settings.IPAddress = IPAddress.Parse(value); }
        }


        public int PortReceive
        {
            get { return settings.UDPServerPort; }
            set { settings.UDPServerPort = value; }
        }

        public int PortSend
        {
            get { return settings.TCPIPServerPort; }
            set { settings.TCPIPServerPort = value; }
        }

        public Calibration Calibration
        {
            get { return calibration; }
        }

        public Stream Stream
        {
            get { return stream; }
        }

        public Commands Commands
        {
            get { return commands; }
        }

        public Log Log
        {
            get { return log; }
        }

        public Camera Camera
        {
            get { return camera; }
        }

        public UIControl UIControl
        {
            get { return uiControl; }
        }

        public Settings Settings
        {
            get { return settings; }
        }

        //public EyeImage EyeImage
        //{
        //    get { return eyeImage; }
        //}

        #endregion
    }
}