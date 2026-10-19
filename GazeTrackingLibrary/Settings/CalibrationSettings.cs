// <copyright file="CalibrationSettings.cs" company="ITU">
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
using System.Windows;
using System.Windows.Media;
using System.Xml;
using GazeTrackerClient;
using GazeTrackingLibrary.Utils;

namespace GazeTrackingLibrary.Settings
{
    public class CalibrationSettings : INotifyPropertyChanged
    {
        #region CONSTANTS

        public const string Name = "CalibrationSettings";

        public const string strNumberOfPoints = "NumberOfPoints";
        public const string strPointDuration = "PointDuration";
        public const string strPointTransitionDuration = "PointTransitionDuration";
        public const string strAcceleration = "Acceleration";
        public const string strDeacceleration = "Deacceleration";
        public const string strBackgroundColor = "BackgroundColor";
        public const string strPointColor = "PointColor";
        public const string strPointDiameter = "PointDiameter";
        public const string strUseInfantGraphics = "UseInfantGraphics";
        public const string strRandomizePointOrder = "RandomizePointOrder";
        public const string strAutoAcceptPoints = "AutoAcceptPoints";
        public const string strWaitForValidData = "WaitForValidData";

        #endregion //CONSTANTS

        #region FIELDS

        private System.Windows.Size size;
        private Monitor trackingMonitor = Monitor.Primary;
        private int areaWidth = 0;
        private int areaHeight = 0;
        private int distanceFromScreen = 0;
        private int defaultDistanceFromScreen = 600;
        private double acceleration = 6;
        private bool autoAccept = true;
        private SolidColorBrush backgroundColor = new SolidColorBrush(Colors.DarkGray);
        private double deacceleration = 4;
        private int numberOfPoints = 9;
        private SolidColorBrush pointColor = new SolidColorBrush(Colors.White);
        private double pointDiameter = 40;
        private double pointDuration = 1500;
        private double pointTransitionDuration = 750;
        private bool randomizePointOrder = true;
        private bool useInfantGraphics;
        private bool waitForValidData;
		private RecalibrationTypeEnum recalibrationType = RecalibrationTypeEnum.None;

        #endregion //FIELDS

        #region EVENTS

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion EVENTS

        #region PROPERTIES

        #region Tracking monitor

        public Monitor TrackingMonitor
        {
            get { return trackingMonitor; }
            set
            {
                trackingMonitor = value;
                OnPropertyChanged("TrackingMonitor");
            }
        }

        public bool IsTrackingOnPrimaryScreen
        {
            get { return trackingMonitor == Monitor.Primary ? true : false; }
            set
            {
                trackingMonitor = Monitor.Primary;
                OnPropertyChanged("TrackingMonitor");
            }
        }

        public bool IsTrackingOnSecondaryScreen
        {
            get { return trackingMonitor == Monitor.Secondary ? true : false; }
            set
            {
                trackingMonitor = Monitor.Secondary;
                OnPropertyChanged("TrackingMonitor");
            }
        }

        #endregion
        public int AreaWidth
        {
            get { return this.areaWidth; }
            set
            {
                this.areaWidth = value;
                OnPropertyChanged("AreaWidth");
            }
        }

        public int AreaHeight
        {
            get { return this.areaHeight; }
            set
            {
                this.areaHeight = value;
                OnPropertyChanged("AreaHeight");
            }
        }

        public int DistanceFromScreen
        {
            get
            {
                if(distanceFromScreen == 0)
                    return defaultDistanceFromScreen;
                else
                    return distanceFromScreen;
            }

            set { distanceFromScreen = value; }
        }

        public SolidColorBrush BackgroundColor
        {
            get { return backgroundColor; }
            set
            {
                backgroundColor = value;
                OnPropertyChanged("BackgroundColor");
            }
        }

        public SolidColorBrush PointColor
        {
            get { return pointColor; }
            set
            {
                pointColor = value;
                OnPropertyChanged("PointColor");
            }
        }

        public double PointDiameter
        {
            get { return pointDiameter; }
            set
            {
                pointDiameter = value;
                OnPropertyChanged("PointDiameter");
            }
        }

        public double PointDuration
        {
            get { return pointDuration; }
            set
            {
                pointDuration = value;
                OnPropertyChanged("PointDuration");
            }
        }

        public double PointTransitionDuration
        {
            get { return pointTransitionDuration; }
            set
            {
                pointTransitionDuration = value;
                OnPropertyChanged("PointTransitionDuration");
            }
        }

        public double Acceleration
        {
            get { return acceleration; }
            set
            {
                acceleration = value;
                OnPropertyChanged("Acceleration");
            }
        }

        public double Deacceleration
        {
            get { return deacceleration; }
            set
            {
                deacceleration = value;
                OnPropertyChanged("Deacceleration");
            }
        }

        public int NumberOfPoints
        {
            get { return numberOfPoints; }
            set
            {
                numberOfPoints = value;
                OnPropertyChanged("NumberOfPoints");
            }
        }

        public bool Using9Points
        {
            get { return numberOfPoints.Equals(9); }
            set { if (value) NumberOfPoints = 9; }
        }

        public bool Using12Points
        {
            get { return numberOfPoints.Equals(12); }
            set { if (value) NumberOfPoints = 12; }
        }

        public bool Using16Points
        {
            get { return numberOfPoints.Equals(16); }
            set { if (value) NumberOfPoints = 16; }
        }

        public bool UseInfantGraphics
        {
            get { return useInfantGraphics; }
            set
            {
                useInfantGraphics = value;
                OnPropertyChanged("UseInfantGraphics");
            }
        }

        public bool RandomizePointOrder
        {
            get { return randomizePointOrder; }
            set
            {
                randomizePointOrder = value;
                OnPropertyChanged("RandomizePointOrder");
            }
        }

        public bool WaitForValidData
        {
            get { return waitForValidData; }
            set
            {
                waitForValidData = value;
                OnPropertyChanged("WaitForValidData");
            }
        }

        public bool AutoAcceptPoints
        {
            get { return autoAccept; }
            set
            {
                autoAccept = value;
                OnPropertyChanged("AutoAccept");
            }
        }

        public string ParametersAsString
        {
            get
            {
                string paramStr =
                    strNumberOfPoints + "=" + numberOfPoints + "," +
                    strPointDuration + "=" + pointDuration + "," +
                    strPointTransitionDuration + "=" + pointTransitionDuration + "," +
                    strAcceleration + "=" + acceleration + "," +
                    strDeacceleration + "=" + deacceleration + "," +
                    strBackgroundColor + "=" + backgroundColor.Color.R + "-" + backgroundColor.Color.G + "-" +
                    backgroundColor.Color.B + "," +
                    strPointColor + "=" + pointColor.Color.R + "-" + pointColor.Color.G + "-" + pointColor.Color.B + "," +
                    strPointDiameter + "=" + pointDiameter + "," +
                    strUseInfantGraphics + "=" + useInfantGraphics + "," +
                    strRandomizePointOrder + "=" + RandomizePointOrder + "," +
                    strAutoAcceptPoints + "=" + AutoAcceptPoints + "," +
                    strWaitForValidData + "=" + waitForValidData;

                return paramStr;
            }
        }

		public RecalibrationTypeEnum RecalibrationType
		{
			get { return recalibrationType; }
			set { recalibrationType = value; }
		}


        #endregion //PROPERTIES

        #region PUBLICMETHODS

        public void SetDefaultParameters()
        {
            NumberOfPoints = 9;
            RandomizePointOrder = true;
            AutoAcceptPoints = true;
            WaitForValidData = true;
            Acceleration = 6;
            Deacceleration = 4;
            PointDiameter = 35;
        }

        public void ExtractParametersFromString(string parameterStr)
        {
            // Seperating commands
            char[] sepCalibrationParameters = {','};
            string[] calibrationParams = parameterStr.Split(sepCalibrationParameters, 20);

            // Seperating values/parameters
            char[] sepValues = {'='};

            foreach (string t in calibrationParams)
            {
                string[] cmdString = t.Split(sepValues, 5);
                string subCmdStr = cmdString[0];
                string value = cmdString[1];

                switch (subCmdStr)
                {
                    case strNumberOfPoints:
                        NumberOfPoints = int.Parse(value);
                        break;

                    case strPointDuration:
                        PointDuration = double.Parse(value);
                        break;

                    case strPointTransitionDuration:
                        PointTransitionDuration = double.Parse(value);
                        break;

                    case strAcceleration:
                        Acceleration = double.Parse(value);
                        break;

                    case strDeacceleration:
                        Deacceleration = double.Parse(value);
                        break;

                    case strBackgroundColor:
                        BackgroundColor = Converter.GetColorFromString(value);
                        break;

                    case strPointColor:
                        PointColor = Converter.GetColorFromString(value);
                        break;

                    case strPointDiameter:
                        PointDiameter = double.Parse(value);
                        break;

                    case strUseInfantGraphics:
                        UseInfantGraphics = bool.Parse(value);
                        break;

                    case strRandomizePointOrder:
                        RandomizePointOrder = bool.Parse(value);
                        break;

                    case strAutoAcceptPoints:
                        AutoAcceptPoints = bool.Parse(value);
                        break;

                    case strWaitForValidData:
                        WaitForValidData = bool.Parse(value);
                        break;
                }
            }
        }


        public void WriteConfigFile(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("CalibrationSettings");

            GTSettings.WriteElement(xmlWriter, "TrackingMonitor", Enum.GetName(typeof (Monitor), trackingMonitor));
            GTSettings.WriteElement(xmlWriter, "AreaWidth", AreaWidth.ToString());
            GTSettings.WriteElement(xmlWriter, "AreaHeight", AreaHeight.ToString());
            GTSettings.WriteElement(xmlWriter, "DistanceFromScreen", DistanceFromScreen.ToString());
            GTSettings.WriteElement(xmlWriter, "NumberOfPoints", NumberOfPoints.ToString());
            GTSettings.WriteElement(xmlWriter, "PointDuration", PointDuration.ToString());
            GTSettings.WriteElement(xmlWriter, "PointTransitionDuration", PointTransitionDuration.ToString());
            GTSettings.WriteElement(xmlWriter, "Acceleration", Acceleration.ToString());
            GTSettings.WriteElement(xmlWriter, "Deacceleration", Deacceleration.ToString());
            GTSettings.WriteElement(xmlWriter, "BackgroundColor", BackgroundColor.Color.R + " " + BackgroundColor.Color.G + " " + BackgroundColor.Color.B);
            GTSettings.WriteElement(xmlWriter, "PointColor", PointColor.Color.R + " " + PointColor.Color.G + " " + PointColor.Color.B);
            GTSettings.WriteElement(xmlWriter, "PointDiamter", PointDiameter.ToString());
            GTSettings.WriteElement(xmlWriter, "UseInfantGraphics", UseInfantGraphics.ToString());
            GTSettings.WriteElement(xmlWriter, "WaitForValidData", WaitForValidData.ToString());
            GTSettings.WriteElement(xmlWriter, "RandomizePointOrder", RandomizePointOrder.ToString());
            GTSettings.WriteElement(xmlWriter, "AutoAcceptPoints", AutoAcceptPoints.ToString());

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
                            case "TrackingMonitor":
                                trackingMonitor = (Monitor) Enum.Parse(typeof (Monitor), xmlReader.Value);
                                break;
                            case "AreaWidth":                               
                                AreaWidth = Int32.Parse(xmlReader.Value);
                                break;
                            case "AreaHeight":                               
                                AreaHeight = Int32.Parse(xmlReader.Value);
                                break;
                            case "DistanceFromScreen":
                                DistanceFromScreen = Int32.Parse(xmlReader.Value);
                                break;
                            case "NumberOfPoints":
                                NumberOfPoints = Int32.Parse(xmlReader.Value);
                                break;
                            case "PointDuration":
                                PointDuration = Double.Parse(xmlReader.Value);
                                break;
                            case "PointTransitionDuration":
                                PointTransitionDuration = Double.Parse(xmlReader.Value);
                                break;
                            case "Acceleration":
                                Acceleration = Double.Parse(xmlReader.Value);
                                break;
                            case "Deacceleration":
                                Deacceleration = Double.Parse(xmlReader.Value);
                                break;

                            case "BackgroundColor":
                                BackgroundColor = Converter.GetColorFromString(xmlReader.Value);
                                break;
                            case "PointColor":
                                PointColor = Converter.GetColorFromString(xmlReader.Value);
                                break;
                            case "PointDiameter":
                                PointDiameter = Double.Parse(xmlReader.Value);
                                break;
                            case "UseInfantGraphics":
                                UseInfantGraphics = Boolean.Parse(xmlReader.Value);
                                break;
                            case "WaitForValidData":
                                WaitForValidData = Boolean.Parse(xmlReader.Value);
                                break;
                            case "RandomizePointOrder":
                                RandomizePointOrder = Boolean.Parse(xmlReader.Value);
                                break;
                            case "AutoAcceptPoints":
                                AutoAcceptPoints = Boolean.Parse(xmlReader.Value);
                                break;
                        }
                        break;
                }
            }
        }

        #endregion //PUBLICMETHODS

        #region EVENTHANDLER

        private void OnPropertyChanged(string parameter)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(parameter));
            }
        }

        #endregion //EVENTHANDLER
    }
}