// <copyright file="NetworkSettings.cs" company="ITU">
// ******************************************************
// GazeTrackingLibrary for ITU GazeTracker
// Copyright (C) 2010 Martin Tall  
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
// <author>Martin Tall</author>
// <email>tall@stanford.edu</email>
// <modifiedby>Adrian Voßkühler</modifiedby>

using System;
using System.ComponentModel;
using System.Xml;

namespace GazeTrackingLibrary.Settings
{
    public class NetworkSettings : INotifyPropertyChanged
    {
        #region CONSTANTS

        public const string Name = "NetworkSettings";

        #endregion //CONSTANTS

        #region FIELDS

        private string clientUIPath = "";
        private bool tcpIPServerEnabled;
        private string tcpIPServerIPAddress = "127.0.0.1";
        private int tcpIPServerPort = 5555; // default

        private bool udpServerEnabled;
        private string udpServerIPAddress = "127.0.0.1";
        private int udpServerPort = 6666; // default
        private bool udpServerSendSmoothedData = true;

        #endregion //FIELDS

        #region CONSTRUCTION

        #endregion //CONSTRUCTION

        #region EVENTS

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion EVENTS

        #region PROPERTIES

        public bool TCPIPServerEnabled
        {
            get { return tcpIPServerEnabled; }
            set
            {
                tcpIPServerEnabled = value;
                OnPropertyChanged("TCPIPServerEnabled");
            }
        }

        public string TCPIPServerIPAddress
        {
            get { return tcpIPServerIPAddress; }
            set
            {
                tcpIPServerIPAddress = value;
                OnPropertyChanged("TCPIPServerIPAddress");
            }
        }

        public int TCPIPServerPort
        {
            get { return tcpIPServerPort; }
            set
            {
                tcpIPServerPort = value;
                OnPropertyChanged("TCPIPServerPort");
            }
        }


        public bool UDPServerEnabled
        {
            get { return udpServerEnabled; }
            set
            {
                udpServerEnabled = value;
                OnPropertyChanged("UDPServerEnabled");
            }
        }

        public string UDPServerIPAddress
        {
            get { return udpServerIPAddress; }
            set
            {
                udpServerIPAddress = value;
                OnPropertyChanged("UDPServerIPAddress");
            }
        }

        public int UDPServerPort
        {
            get { return udpServerPort; }
            set
            {
                udpServerPort = value;
                OnPropertyChanged("UDPServerPort");
            }
        }

        public bool UDPServerSendSmoothedData
        {
            get { return udpServerSendSmoothedData; }
            set
            {
                udpServerSendSmoothedData = value;
                OnPropertyChanged("UDPServerSendSmoothedData");
            }
        }

        public string ClientUIPath
        {
            get { return clientUIPath; }
            set
            {
                clientUIPath = value;
                //if (null != this.PropertyChanged)
                //    PropertyChanged(this, new PropertyChangedEventArgs("LogFilePath"));
            }
        }

        #endregion //PROPERTIES

        #region PUBLICMETHODS

        public void WriteConfigFile(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement(Name);

            GTSettings.WriteElement(xmlWriter, "TCPIPServerEnabled", TCPIPServerEnabled.ToString());
            GTSettings.WriteElement(xmlWriter, "TCPIPServerIPAddress", TCPIPServerIPAddress);
            GTSettings.WriteElement(xmlWriter, "TCPIPServerPort", TCPIPServerPort.ToString());

            GTSettings.WriteElement(xmlWriter, "UDPServerEnabled", UDPServerEnabled.ToString());
            GTSettings.WriteElement(xmlWriter, "UDPServerIPAddress", UDPServerIPAddress);
            GTSettings.WriteElement(xmlWriter, "UDPServerPort", UDPServerPort.ToString());
            GTSettings.WriteElement(xmlWriter, "UDPServerSendSmoothedData", UDPServerSendSmoothedData.ToString());

            GTSettings.WriteElement(xmlWriter, "ClientUIPath", ClientUIPath);

            xmlWriter.WriteEndElement();
        }

        public void LoadConfigFile(XmlReader xmlReader)
        {
            string sName = string.Empty;

            while (xmlReader.Read())
            {
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        sName = xmlReader.Name;
                        break;
                    case XmlNodeType.Text:
                        switch (sName)
                        {
                            case "TCPIPServerEnabled":
                                TCPIPServerEnabled = Boolean.Parse(xmlReader.Value);
                                break;
                            case "TCPIPServerIPAddress":
                                TCPIPServerIPAddress = xmlReader.Value;
                                break;
                            case "TCPIPServerPort":
                                TCPIPServerPort = Int32.Parse(xmlReader.Value);
                                break;

                            case "UDPServerEnabled":
                                UDPServerEnabled = Boolean.Parse(xmlReader.Value);
                                break;
                            case "UDPServerIPAddress":
                                UDPServerIPAddress = xmlReader.Value;
                                break;
                            case "UDPServerPort":
                                UDPServerPort = Int32.Parse(xmlReader.Value);
                                break;
                            case "UDPServerSendSmoothedData":
                                UDPServerSendSmoothedData = Boolean.Parse(xmlReader.Value);
                                break;

                            case "ClientUIPath":
                                ClientUIPath = xmlReader.Value;
                                break;
                        }
                        break;
                }
            }
        }

        #endregion //PUBLICMETHODS

        #region PRIVATEMETHODS

        private void OnPropertyChanged(string parameter)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(parameter));
            }
        }

        #endregion //PRIVATEMETHODS
    }
}