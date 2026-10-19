// <copyright file="ProcessingSettings.cs" company="ITU">
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
using System.Xml;
using GazeTrackingLibrary.Utils;

namespace GazeTrackingLibrary.Settings
{
    public class ProcessingSettings : INotifyPropertyChanged
    {

        #region CONSTANTS

        public const string Name = "ProcessingSettings";

        #endregion //CONSTANTS


        #region FIELDS

        private bool autoEye;
        private bool autoGlint;
        private bool autoPupil;
        private bool eyeCrosshairEnabled;
        private bool eyeMouseEnabled = true;
        private bool eyeMouseHideCursor;
        private bool eyeMouseSmooth = true;
        private int smoothLevel = 50; // 1-100
        private int glintSizeMaximum = 10;
        private int glintSizeMinimum = 1;

        private int numberOfGlints = 0;
        private int glintThreshold = 200;
        private int glintThresholdLeft = 200;
        private int glintThresholdRight = 200;
        private IRPlacementEnum irPlacement = IRPlacementEnum.Below;
        private int pupilSizeMaximum = 35;
        private int pupilSizeMinimum = 2;
        private int pupilThreshold = 40;
        private int pupilThresholdLeft = 40;
        private int pupilThresholdRight = 40;
        private bool trackingEye;
        private bool trackingGlints;
        private TrackingMethodEnum trackingMethod = TrackingMethodEnum.Headmounted;
        private TrackingModeEnum trackingMode = TrackingModeEnum.Binocular;
        private bool trackingPupil = true;

        #endregion //FIELDS


        #region EVENTS

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion EVENTS


        #region PROPERTIES

        #region TrackingMethod

        public TrackingMethodEnum TrackingMethod
        {
            get { return trackingMethod; }
            set
            {
                trackingMethod = value;

                switch (trackingMethod)
                {
                    case TrackingMethodEnum.RemoteBinocular:
                        TrackingEye = true;
                        TrackingPupil = true;
                        TrackingGlints = true;
                        TrackingMode = TrackingModeEnum.Binocular;
                        break;

                    case TrackingMethodEnum.RemoteMonocular:
                        TrackingEye = true;
                        TrackingPupil = true;
                        TrackingGlints = true;
                        TrackingMode = TrackingModeEnum.Monocular;
                        break;

                    case TrackingMethodEnum.Headmounted:
                        TrackingEye = true;
                        TrackingPupil = true;
                        TrackingGlints = false;
                        TrackingMode = TrackingModeEnum.Monocular;
                        break;
                }

                OnPropertyChanged("TrackingMethod");
            }
        }

        public TrackingModeEnum TrackingMode
        {
            get { return trackingMode; }
            set
            {
                trackingMode = value;
                OnPropertyChanged("TrackingMode");
            }
        }

        public int NumberOfGlints
        {
            get { return numberOfGlints; }
            set
            {
                numberOfGlints = value;
                OnPropertyChanged("NumberOfGlints");
            }
        }

        public IRPlacementEnum IRPlacement
        {
            get { return irPlacement; }
            set
            {
                irPlacement = value;
                OnPropertyChanged("IRPlacement");
            }
        }

        #endregion

        #region Tracking

        public bool TrackingEye
        {
            get { return trackingEye; }
            set
            {
                trackingEye = value;
                OnPropertyChanged("TrackingEye");
            }
        }

        public bool TrackingPupil
        {
            get { return trackingPupil; }
            set
            {
                trackingPupil = value;
                OnPropertyChanged("TrackingPupil");
            }
        }

        public bool TrackingGlints
        {
            get { return trackingGlints; }
            set
            {
                trackingGlints = value;
                OnPropertyChanged("TrackingGlints");
            }
        }

        #endregion

        #region Auto

        public bool AutoEye
        {
            get { return autoEye; }
            set
            {
                autoEye = value;
                OnPropertyChanged("AutoEye");
            }
        }

        public bool AutoPupil
        {
            get { return autoPupil; }
            set
            {
                autoPupil = value;
                OnPropertyChanged("AutoPupil");
            }
        }

        public bool AutoGlint
        {
            get { return autoGlint; }
            set
            {
                autoGlint = value;
                OnPropertyChanged("AutoGlint");
            }
        }

        #endregion

        #region Pupil

        public int PupilThreshold
        {
            get { return pupilThreshold; }
            set
            {
                pupilThreshold = value;
                PupilThresholdLeft = value;
                PupilThresholdRight = value;
                OnPropertyChanged("PupilThreshold");
            }
        }

        public int PupilThresholdLeft
        {
            get { return pupilThresholdLeft; }
            set
            {
                pupilThresholdLeft = value;
                OnPropertyChanged("PupilThresholdLeft");
            }
        }

        public int PupilThresholdRight
        {
            get { return pupilThresholdRight; }
            set
            {
                pupilThresholdRight = value;
                OnPropertyChanged("PupilThresholdRight");
            }
        }

        public int PupilSizeMinimum
        {
            get { return pupilSizeMinimum; }
            set
            {
                pupilSizeMinimum = value;
                OnPropertyChanged("PupilSizeMinimum");
            }
        }

        public int PupilSizeMaximum
        {
            get { return pupilSizeMaximum; }
            set
            {
                pupilSizeMaximum = value;
                OnPropertyChanged("PupilSizeMaximum");
            }
        }

        #endregion

        #region Glint

        public int GlintThreshold
        {
            get { return glintThreshold; }
            set
            {
                GlintThresholdLeft = value;
                GlintThresholdRight = value;
                glintThreshold = value;
                OnPropertyChanged("GlintThreshold");
            }
        }

        public int GlintThresholdLeft
        {
            get { return glintThresholdLeft; }
            set
            {
                glintThresholdLeft = value;
                OnPropertyChanged("GlintThresholdLeft");
            }
        }

        public int GlintThresholdRight
        {
            get { return glintThresholdRight; }
            set
            {
                glintThresholdRight = value;
                OnPropertyChanged("GlintThresholdRight");
            }
        }

        public int GlintSizeMinimum
        {
            get { return glintSizeMinimum; }
            set
            {
                glintSizeMinimum = value;
                OnPropertyChanged("GlintSizeMinimum");
            }
        }

        public int GlintSizeMaximum
        {
            get { return glintSizeMaximum; }
            set
            {
                glintSizeMaximum = value;
                OnPropertyChanged("GlintSizeMaximum");
            }
        }

        #endregion


        #region GazeMouse/Visual feedback

        public bool EyeMouseEnabled
        {
            get { return eyeMouseEnabled; }
            set
            {
                eyeMouseEnabled = value;
                OnPropertyChanged("EyeMouseEnabled");
            }
        }

        public bool EyeMouseHideCursor
        {
            get { return eyeMouseHideCursor; }
            set
            {
                eyeMouseHideCursor = value;
                OnPropertyChanged("EyeMouseHideCursor");
            }
        }

        public bool EyeMouseSmooth
        {
            get { return eyeMouseSmooth; }
            set
            {
                eyeMouseSmooth = value;
                OnPropertyChanged("EyeMouseSmooth");
            }
        }

        public int SmoothLevel
        {
            get { return smoothLevel; }
            set { smoothLevel = value; }
        }

        public bool EyeCrosshairEnabled
        {
            get { return eyeCrosshairEnabled; }
            set
            {
                eyeCrosshairEnabled = value;
                OnPropertyChanged("EyeCrosshairEnabled");
            }
        }

        #endregion

        #endregion //PROPERTIES


        #region PUBLICMETHODS

        public void WriteConfigFile(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement(Name);

            GTSettings.WriteElement(xmlWriter, "TrackingMethod", Enum.GetName(typeof (TrackingMethodEnum), trackingMethod));
            GTSettings.WriteElement(xmlWriter, "TrackingMode", Enum.GetName(typeof (TrackingModeEnum), trackingMode));
            GTSettings.WriteElement(xmlWriter, "IRPlacement", Enum.GetName(typeof (IRPlacementEnum), irPlacement));

            GTSettings.WriteElement(xmlWriter, "TrackingEye", TrackingEye.ToString());
            GTSettings.WriteElement(xmlWriter, "TrackingPupil", TrackingPupil.ToString());
            GTSettings.WriteElement(xmlWriter, "TrackingGlints", TrackingGlints.ToString());

            GTSettings.WriteElement(xmlWriter, "AutoEye", AutoEye.ToString());
            GTSettings.WriteElement(xmlWriter, "AutoPupil", AutoPupil.ToString());
            GTSettings.WriteElement(xmlWriter, "AutoGlint", AutoGlint.ToString());

            GTSettings.WriteElement(xmlWriter, "PupilThreshold", PupilThreshold.ToString());
            GTSettings.WriteElement(xmlWriter, "PupilThresholdLeft", PupilThresholdLeft.ToString());
            GTSettings.WriteElement(xmlWriter, "PupilThresholdRight", PupilThresholdRight.ToString());
            GTSettings.WriteElement(xmlWriter, "PupilSizeMinimum", PupilSizeMinimum.ToString());
            GTSettings.WriteElement(xmlWriter, "PupilSizeMaximum", PupilSizeMaximum.ToString());

            GTSettings.WriteElement(xmlWriter, "NumberOfGlints", NumberOfGlints.ToString());
            GTSettings.WriteElement(xmlWriter, "GlintThreshold", GlintThreshold.ToString());
            GTSettings.WriteElement(xmlWriter, "GlintThresholdLeft", GlintThresholdLeft.ToString());
            GTSettings.WriteElement(xmlWriter, "GlintThresholdRight", GlintThresholdRight.ToString());
            GTSettings.WriteElement(xmlWriter, "GlintSizeMinimum", GlintSizeMinimum.ToString());
            GTSettings.WriteElement(xmlWriter, "GlintSizeMaximum", GlintSizeMaximum.ToString());

            GTSettings.WriteElement(xmlWriter, "EyeMouseEnabled", EyeMouseEnabled.ToString());
            GTSettings.WriteElement(xmlWriter, "EyeMouseHideCursor", EyeMouseHideCursor.ToString());
            GTSettings.WriteElement(xmlWriter, "EyeMouseSmooth", EyeMouseSmooth.ToString());
            GTSettings.WriteElement(xmlWriter, "SmoothLevel", SmoothLevel.ToString());
            GTSettings.WriteElement(xmlWriter, "EyeCrosshairEnabled", EyeCrosshairEnabled.ToString());

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
                            case "TrackingMethod":
                                TrackingMethod = (TrackingMethodEnum) Enum.Parse(typeof (TrackingMethodEnum), xmlReader.Value, true);
                                break;
                            case "TrackingMode":
                                TrackingMode = (TrackingModeEnum) Enum.Parse(typeof (TrackingModeEnum), xmlReader.Value, true);
                                break;
                            case "IRPlacement":
                                IRPlacement =(IRPlacementEnum) Enum.Parse(typeof (IRPlacementEnum), xmlReader.Value, true);
                                break;

                            case "TrackingEye":
                                TrackingEye = Boolean.Parse(xmlReader.Value);
                                break;
                            case "TrackingPupil":
                                TrackingPupil = Boolean.Parse(xmlReader.Value);
                                break;
                            case "TrackingGlints":
                                TrackingGlints = Boolean.Parse(xmlReader.Value);
                                break;

                            // Auto 
                            case "AutoEye":
                                AutoEye = Boolean.Parse(xmlReader.Value);
                                break;
                            case "AutoPupil":
                                AutoPupil = Boolean.Parse(xmlReader.Value);
                                break;
                            case "AutoGlint":
                                AutoGlint = Boolean.Parse(xmlReader.Value);
                                break;

                            // Pupil
                            case "PupilThreshold":
                                PupilThreshold = Int32.Parse(xmlReader.Value);
                                break;
                            case "PupilThresholdLeft":
                                PupilThresholdLeft = Int32.Parse(xmlReader.Value);
                                break;
                            case "PupilThresholdRight":
                                PupilThresholdRight = Int32.Parse(xmlReader.Value);
                                break;
                            case "PupilSizeMinimum":
                                PupilSizeMinimum = Int32.Parse(xmlReader.Value);
                                break;
                            case "PupilSizeMaximum":
                                PupilSizeMaximum = Int32.Parse(xmlReader.Value);
                                break;

                            // Glints (remote tracking)
                            case "NumberOfGlints":
                                NumberOfGlints = Int32.Parse(xmlReader.Value);
                                break;
                            case "GlintThreshold":
                                GlintThreshold = Int32.Parse(xmlReader.Value);
                                break;
                            case "GlintThresholdLeft":
                                GlintThresholdLeft = Int32.Parse(xmlReader.Value);
                                break;
                            case "GlintThresholdRight":
                                GlintThresholdRight = Int32.Parse(xmlReader.Value);
                                break;
                            case "GlintSizeMinimum":
                                GlintSizeMinimum = Int32.Parse(xmlReader.Value);
                                break;
                            case "GlintSizeMaximum":
                                GlintSizeMaximum = Int32.Parse(xmlReader.Value);
                                break;

                                // Options
                            case "EyeMouseEnabled":
                                EyeMouseEnabled = Boolean.Parse(xmlReader.Value);
                                break;
                            case "EyeMouseHideCursor":
                                EyeMouseHideCursor = Boolean.Parse(xmlReader.Value);
                                break;
                            case "EyeMouseSmooth":
                                EyeMouseSmooth = Boolean.Parse(xmlReader.Value);
                                break;
                           case "SmoothLevel":
                                SmoothLevel = Int32.Parse(xmlReader.Value);
                                break;
                            case "EyeCrosshairEnabled":
                                EyeCrosshairEnabled = Boolean.Parse(xmlReader.Value);
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

        /// <summary>
        /// When the input video changes in size, rescale the min/max sizes for pupil&glint
        /// </summary>
        /// <param name="oldSize">Previous size of the video (width,height)</param>
        /// <param name="newSize">New size of the video (width,height)</param>
        internal void RescaleToNewVideoSize(Size oldSize, Size newSize)
        {
            double multiplier;

            if (oldSize.Width > newSize.Width)
                multiplier = newSize.Width/oldSize.Width;
            else
                multiplier = newSize.Height/oldSize.Height;

            GTSettings.Current.Processing.PupilSizeMinimum =
                Convert.ToInt32(multiplier*GTSettings.Current.Processing.PupilSizeMinimum);
            GTSettings.Current.Processing.PupilSizeMaximum =
                Convert.ToInt32(multiplier*GTSettings.Current.Processing.PupilSizeMaximum);
            GTSettings.Current.Processing.GlintSizeMinimum =
                Convert.ToInt32(multiplier*GTSettings.Current.Processing.GlintSizeMinimum);
            GTSettings.Current.Processing.GlintSizeMaximum =
                Convert.ToInt32(multiplier*GTSettings.Current.Processing.GlintSizeMaximum);
        }

        #endregion //PRIVATEMETHODS

    }
}