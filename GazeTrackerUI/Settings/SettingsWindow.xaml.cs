using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using GazeGUI;
using GazeTrackingLibrary.Settings;
using GazeTrackingLibrary.Utils;
using GTCommons;
using Microsoft.Win32;

using Application = System.Windows.Forms.Application;
using Button = GazeGUIVelocity.Button;
using Color = System.Drawing.Color;
using Rectangle = System.Windows.Shapes.Rectangle;
using Size = System.Drawing.Size;
using GazeTrackingLibrary.Camera;
using System.Collections;

namespace GazeTrackerUI.Settings
{
    public partial class SettingsWindow : Window
    {

        #region Varibles

        [Flags]
        public enum SettingsTabs
        {
            None = 0,
            Tracking = 1,
            Calibration = 2,
            Network = 4,
            Options = 8,
        }

        private static SettingsWindow instance;

        private SolidColorBrush _backgroundColor = new SolidColorBrush(Colors.SlateGray);
        private SolidColorBrush _pointColor = new SolidColorBrush(Colors.White);
        private Rectangle activeRectangle;
        private Color lastColor;
        private bool hasBeenMoved;


        #endregion


        #region Constructor

        private SettingsWindow()
        {
            InitializeComponent();
            SetValues();
            HookUpEvents();
        }

        #endregion


        #region Initialize

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            StackPanelTracking.DataContext = GTSettings.Current.Processing;
            StackPanelAuto.DataContext = GTSettings.Current.Processing;
            GridEyeSliders.DataContext = GTSettings.Current.Eyetracker;
            GridPupilSliders.DataContext = GTSettings.Current.Processing;
            GridGlintSliders.DataContext = GTSettings.Current.Processing;

            GridTrackerSetup.DataContext = GTSettings.Current.Processing;

            GridCalibration.DataContext = GTSettings.Current.Calibration;
            GridAdvancedCalibration.DataContext = GTSettings.Current.Calibration;
            GridCamera.DataContext = GTSettings.Current.Camera;
            GridAdvancedCamera.DataContext = GTSettings.Current.Camera;
            GridNetwork.DataContext = GTSettings.Current.Network;
            GridOptions.DataContext = GTSettings.Current;

            GridVisualFeedback.DataContext = GTSettings.Current.Processing;
            ExpanderColors.DataContext = GTSettings.Current.Visualization;

            CheckBoxEyeMouseEnabled.DataContext = GTSettings.Current.Processing;
            CheckBoxCrosshairEnabled.DataContext = GTSettings.Current.Processing;
            CheckBoxLoggingEnabled.DataContext = GTSettings.Current.FileSettings;
            TextBoxLogFilePath.DataContext = GTSettings.Current.FileSettings;

            comboboxIRPlacement.ItemsSource = Enum.GetValues(typeof (IRPlacementEnum));

            // May move the tracking monitor to calibration settings, because it is shown in the calibration tab ?
            comboBoxTrackingMonitor.ItemsSource = Enum.GetValues(typeof (Monitor));
            comboBoxTrackingMonitor.DataContext = GTSettings.Current.Processing;
        }

        public void ShowOrHideTabs(SettingsTabs shownTabs)
        {
            TabItemTracking.Visibility = Visibility.Collapsed;
            TabItemCalibration.Visibility = Visibility.Collapsed;
            TabItemNetwork.Visibility = Visibility.Collapsed;
            TabItemOptions.Visibility = Visibility.Collapsed;

            if ((shownTabs & SettingsTabs.Tracking) == SettingsTabs.Tracking)
            {
                TabItemTracking.Visibility = Visibility.Visible;
            }

            if ((shownTabs & SettingsTabs.Calibration) == SettingsTabs.Calibration)
            {
                TabItemCalibration.Visibility = Visibility.Visible;
            }

            if ((shownTabs & SettingsTabs.Network) == SettingsTabs.Network)
            {
                TabItemNetwork.Visibility = Visibility.Visible;
            }

            if ((shownTabs & SettingsTabs.Options) == SettingsTabs.Options)
            {
                TabItemOptions.Visibility = Visibility.Visible;
            }
        }

        public void SetValues()
        {
            EditSliderEyeMin.Value = GTSettings.Current.Eyetracker.SizeMin.Width;
            EditSliderEyeMax.Value = GTSettings.Current.Eyetracker.SizeMax.Width;

            EditSliderPupilMin.Value = GTSettings.Current.Processing.PupilSizeMinimum;
            EditSliderPupilMax.Value = GTSettings.Current.Processing.PupilSizeMaximum;

            EditSliderGlintMin.Value = GTSettings.Current.Processing.GlintSizeMinimum;
            EditSliderGlintMax.Value = GTSettings.Current.Processing.GlintSizeMaximum;

            ComboBoxDevices.ItemsSource = DevicesList();
            ComboBoxDevices.SelectedIndex = GTSettings.Current.Camera.DeviceNumber;
            ComboBoxDeviceModes.ItemsSource = DeviceModeList(ComboBoxDevices.SelectedIndex);
            ComboBoxDeviceModes.SelectedIndex = GTSettings.Current.Camera.DeviceMode;

            ComboBoxTCPIPServerIPAddress.ItemsSource = GetIPAddresses();
            ComboBoxUDPServerIPAddress.ItemsSource = GetIPAddresses();

            // highlight big button
            switch (GTSettings.Current.Processing.TrackingMethod)
            {
                case TrackingMethodEnum.Headmounted:
                    BtnTrackingMethodHeadmounted.SelectedGlow = true;
                    break;
                case TrackingMethodEnum.RemoteBinocular:
                    BtnTrackingMethodRemoteBinocular.SelectedGlow = true;
                    break;
                case TrackingMethodEnum.RemoteMonocular:
                    BtnTrackingMethodRemoteMonocular.SelectedGlow = true;
                    break;
            }
        }

        private void HookUpEvents()
        {
            TabControlMain.SelectionChanged += TabControlMain_SelectionChanged;

            ComboBoxSettingsFiles.SelectionChanged += ComboBoxSettingsFiles_SelectionChanged;

            EditSliderEyeMin.OnValueChange += EyeSizeChanged;
            EditSliderEyeMax.OnValueChange += EyeSizeChanged;
            EditSliderPupilMin.OnValueChange += PupilSizeChanged;
            EditSliderPupilMax.OnValueChange += PupilSizeChanged;
            EditSliderGlintMin.OnValueChange += GlintSizeChange;
            EditSliderGlintMax.OnValueChange += GlintSizeChange;

            // Auto-tune checkboxes
            CheckBoxAutoEye.Checked += CheckBoxAutoEye_Checked;
            CheckBoxAutoEye.Unchecked += CheckBoxAutoEye_Unchecked;
            CheckBoxAutoPupil.Checked += CheckBoxAutoPupil_Checked;
            CheckBoxAutoPupil.Unchecked += CheckBoxAutoPupil_Unchecked;
            CheckBoxAutoGlint.Checked += CheckBoxAutoGlint_Checked;
            CheckBoxAutoGlint.Unchecked += CheckBoxAutoGlint_Unchecked;

            // Component placing
            labelComponentPlacing.MouseDown += new MouseButtonEventHandler(labelComponentPlacing_MouseDown);

            // Calibration monitor
            comboBoxTrackingMonitor.SelectionChanged +=new SelectionChangedEventHandler(SetTrackingMonitor);

            colorPickerUC.OnColorSelected += colorPickerUC_OnColorSelected;

            // Camera
            ComboBoxDevices.SelectionChanged += ComboBoxDevices_SelectionChanged;
            ComboBoxDeviceModes.SelectionChanged += ComboBoxDeviceModes_SelectionChanged;

        }

        private void TabControlMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HideAllAdvancedPanels();

            if (TabItemTracking.IsSelected)
                GridAdvancedTracking.Visibility = Visibility.Visible;
            else if (TabItemCalibration.IsSelected)
                GridAdvancedCalibration.Visibility = Visibility.Visible;
            else if (TabItemNetwork.IsSelected)
                GridAdvancedNetwork.Visibility = Visibility.Visible;
            else if (TabItemCamera.IsSelected)
                GridAdvancedCamera.Visibility = Visibility.Visible;
            else if (TabItemOptions.IsSelected)
            {
                GridAdvancedOptions.Visibility = Visibility.Visible;
                ExpanderColors.Visibility = Visibility.Visible;
            }

        }

        private void HideAllAdvancedPanels()
        {
            GridAdvancedTracking.Visibility = Visibility.Collapsed;
            GridAdvancedCalibration.Visibility = Visibility.Collapsed;
            GridAdvancedNetwork.Visibility = Visibility.Collapsed;
            GridAdvancedCamera.Visibility = Visibility.Collapsed;
            GridAdvancedOptions.Visibility = Visibility.Collapsed;

            ExpanderColors.Visibility = Visibility.Collapsed;
        }

        private static List<string> GetIPAddresses()
        {
            var ipaddresses = new List<string> {"127.0.0.1"};

            try
            {
                String hostName = Dns.GetHostName();
                IPHostEntry iphostentry = Dns.GetHostEntry(hostName);

                foreach (IPAddress ip in iphostentry.AddressList)
                {
                    ipaddresses.Add(ip.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("SettingsWindow.cs, error while obtaining IP Addresses, message: " + ex.Message);
            }

            return ipaddresses;
        }

        #endregion


        #region Auto-tune toggle On/Off (sliders visibility)

        private void CheckBoxAutoEye_Checked(object sender, RoutedEventArgs e)
        {
            GridEyeSliders.Visibility = Visibility.Hidden;
        }

        private void CheckBoxAutoEye_Unchecked(object sender, RoutedEventArgs e)
        {
            GridEyeSliders.Visibility = Visibility.Visible;
        }

        private void CheckBoxAutoPupil_Checked(object sender, RoutedEventArgs e)
        {
            GridPupilSliders.Visibility = Visibility.Hidden;
        }

        private void CheckBoxAutoPupil_Unchecked(object sender, RoutedEventArgs e)
        {
            GridPupilSliders.Visibility = Visibility.Visible;
        }

        private void CheckBoxAutoGlint_Checked(object sender, RoutedEventArgs e)
        {
            GridGlintSliders.Visibility = Visibility.Hidden;
        }

        private void CheckBoxAutoGlint_Unchecked(object sender, RoutedEventArgs e)
        {
            GridGlintSliders.Visibility = Visibility.Visible;
        }

        #endregion


        #region SlidersChanged (pupil min/max, glints min/max)

        private void EyeSizeChanged(object  sender, RoutedEventArgs e)
        {
            GTSettings.Current.Eyetracker.SizeMin =  new Size((int)EditSliderEyeMin.Value, (int)EditSliderEyeMin.Value);
            GTSettings.Current.Eyetracker.SizeMax =  new Size((int)EditSliderEyeMax.Value, (int)EditSliderEyeMax.Value);

            EditSliderEyeMax.Minimum = (int) EditSliderEyeMin.Value + 1;

            if(EditSliderEyeMax.Value < EditSliderEyeMin.Value)
                GTSettings.Current.Eyetracker.SizeMax = new Size((int)EditSliderEyeMin.Value+1, (int)EditSliderEyeMin.Value+1);
        }

        private void PupilSizeChanged(object  sender, RoutedEventArgs e)
        {
            GTSettings.Current.Processing.PupilSizeMinimum =  (int) EditSliderPupilMin.Value;
            GTSettings.Current.Processing.PupilSizeMaximum =  (int) EditSliderPupilMax.Value;

            EditSliderPupilMax.Minimum = (int) EditSliderPupilMin.Value + 1;

            if(EditSliderPupilMax.Value < EditSliderPupilMin.Value)
                GTSettings.Current.Processing.PupilSizeMaximum = GTSettings.Current.Processing.PupilSizeMinimum + 1;
        }

        private void GlintSizeChange(object sender, RoutedEventArgs e)
        {
            GTSettings.Current.Processing.GlintSizeMinimum =  (int) EditSliderGlintMin.Value;
            GTSettings.Current.Processing.GlintSizeMaximum =  (int) EditSliderGlintMax.Value;

            EditSliderGlintMax.Minimum = (int) EditSliderGlintMin.Value + 1;

            if(EditSliderGlintMax.Value < EditSliderGlintMin.Value)
                GTSettings.Current.Processing.GlintSizeMaximum = GTSettings.Current.Processing.GlintSizeMinimum + 1;
        }

        #endregion


        #region TrackingMode / Number of glints

        private void SetTrackingMethod(object sender, RoutedEventArgs e)
        {
            BtnTrackingMethodHeadmounted.SelectedGlow = false;
            BtnTrackingMethodRemoteBinocular.SelectedGlow = false;
            BtnTrackingMethodRemoteMonocular.SelectedGlow = false;

            var btn = sender as Button;
            btn.SelectedGlow = true;

            switch (btn.Name)
            {
                case "BtnTrackingMethodHeadmounted":
                    GTSettings.Current.Processing.TrackingMethod = TrackingMethodEnum.Headmounted;
                    break;
                case "BtnTrackingMethodRemoteBinocular":
                    CheckComponentPlacing();
                    GTSettings.Current.Processing.TrackingMethod = TrackingMethodEnum.RemoteBinocular;
                    break;
                case "BtnTrackingMethodRemoteMonocular":
                    CheckComponentPlacing();
                    GTSettings.Current.Processing.TrackingMethod = TrackingMethodEnum.RemoteMonocular;
                    break;
            }
        }

        private void CheckComponentPlacing() 
        {
            if(GTSettings.Current.Processing.NumberOfGlints == 0)
                ShowNewComponenetPlacingWindow();
        }

        private void ShowNewComponenetPlacingWindow() 
        {
            ComponentPlacingWindow cPlacingWin = new ComponentPlacingWindow();
            cPlacingWin.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
            cPlacingWin.Top = this.Top;
            cPlacingWin.Left = this.Left - cPlacingWin.Width - 8;
            cPlacingWin.Show();
        }

        private void SetTrackingMonitor(object sender, SelectionChangedEventArgs e)
        {
            GTSettings.Current.Calibration.TrackingMonitor = (Monitor) comboBoxTrackingMonitor.SelectedValue;

            if(GTSettings.Current.Calibration.TrackingMonitor == Monitor.Primary)
            {
                TextBoxCalibrationWidth.Text  = ScreenParameters.PrimaryResolution.Width.ToString();
                TextBoxCalibrationHeight.Text = ScreenParameters.PrimaryResolution.Height.ToString();
            }
            else
            {
                TextBoxCalibrationWidth.Text  = ScreenParameters.SecondaryResolution.Width.ToString();
                TextBoxCalibrationHeight.Text = ScreenParameters.SecondaryResolution.Height.ToString();
            }
        }

        private void labelComponentPlacing_MouseDown(object sender, MouseButtonEventArgs e) 
        {
            ShowNewComponenetPlacingWindow();
        }

        #endregion


        #region Calibration colors

        private void ShowColorPickerCalibration(object sender, RoutedEventArgs e)
        {
            var rect = sender as Rectangle;

            if (rect != null)
                if (rect.Name.Equals("GridBackgroundColor"))
                    GridColorPickerPoints.Visibility = Visibility.Visible;
                else
                    GridColorPickerBackground.Visibility = Visibility.Visible;
        }

        private void ColorSelected(object sender, RoutedEventArgs e)
        {
            var btn = sender as ButtonGlass;

            if (btn.Name.Equals("BtnPoints"))
            {
                GTSettings.Current.Calibration.PointColor =
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(colorPickerPoints.SelectedColor.R,
                                                                           colorPickerPoints.SelectedColor.G,
                                                                           colorPickerPoints.SelectedColor.B));
                RectPointColor.Fill = GTSettings.Current.Calibration.PointColor;
                GridColorPickerPoints.Visibility = Visibility.Collapsed;
            }
            else
            {
                GTSettings.Current.Calibration.BackgroundColor =
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(colorPickerBackground.SelectedColor.R,
                                                                           colorPickerBackground.SelectedColor.G,
                                                                           colorPickerBackground.SelectedColor.B));
                RectBackgroundColor.Fill = GTSettings.Current.Calibration.BackgroundColor;
                GridColorPickerBackground.Visibility = Visibility.Collapsed;
            }
        }

        #endregion


        #region Visualization

        private void colorPickerUC_OnColorSelected(System.Windows.Media.Color color)
        {
            if (activeRectangle == null)
                return;
            else
                activeRectangle.Fill = new SolidColorBrush(color);

            if (color.A == 0 && color.R == 0 && color.G == 0 && color.B == 0)
                return;

            Color c = Color.FromArgb(color.A, color.R, color.G, color.B);

            // If fires multiple events..
            if (c == lastColor)
                return;

            lastColor = c;

            switch (activeRectangle.Name)
            {
                    #region Eye

                case "EyeROIColor":
                    {
                        GTSettings.Current.Visualization.EyeROIColor = c;
                        break;
                    }

                    #endregion

                    #region Pupil

                case "PupilThresholdColor":
                    {
                        GTSettings.Current.Visualization.PupilThresholdColor = c;
                        break;
                    }
                case "PupilCrossColor":
                    {
                        GTSettings.Current.Visualization.PupilCrossColor = c;
                        break;
                    }
                case "PupilMinColor":
                    {
                        GTSettings.Current.Visualization.PupilMinColor = c;
                        break;
                    }
                case "PupilMaxColor":
                    {
                        GTSettings.Current.Visualization.PupilMaxColor = c;
                        break;
                    }

                    #endregion

                    #region Glint

                case "GlintThresholdColor":
                    {
                        GTSettings.Current.Visualization.GlintThresholdColor = c;
                        break;
                    }

                case "GlintCrossColor":
                    {
                        GTSettings.Current.Visualization.GlintCrossColor = c;
                        break;
                    }
                case "GlintMinColor":
                    {
                        GTSettings.Current.Visualization.GlintMinColor = c;
                        break;
                    }
                case "GlintMaxColor":
                    {
                        GTSettings.Current.Visualization.GlintMaxColor = c;
                        break;
                    }

                    #endregion
            }
        }

        #region ColorPicker

        private void ShowColorPicker(object sender, MouseButtonEventArgs args)
        {
            activeRectangle = sender as Rectangle;

            if (GridColorPicker.Visibility == Visibility.Collapsed)
            {
                GridColorPicker.Visibility = Visibility.Visible;
                TabControlColors.IsEnabled = false;
            }
            else
            {
                GridColorPicker.Visibility = Visibility.Collapsed;
                TabControlColors.IsEnabled = true;
            }
        }

        private void HideColorPicker(object sender, MouseEventArgs e)
        {
            GridColorPicker.Visibility = Visibility.Collapsed;
            TabControlColors.IsEnabled = true;
        }

        #endregion

        #endregion


        #region Device/Camera

        private static ArrayList DevicesList()
        {
            var itemsList = new ArrayList();
            foreach (CameraInfo cam in Devices.Current.Cameras)
            {
                if (cam.DirectshowDevice != null)
                    itemsList.Add(cam.Name);
            }
            return itemsList;
        }

        private static ArrayList DeviceModeList(int deviceNumber)
        {
            // Ugly fix for bug where devNum -1
            if (deviceNumber == -1)
                deviceNumber = 0;

            if (Devices.Current.Cameras.Count == 0)
                return new ArrayList();

            var itemsList = new ArrayList();
            foreach (CamSizeFPS camInfo in Devices.Current.Cameras[deviceNumber].SupportedSizesAndFPS)
            {
                itemsList.Add(camInfo.Width + " x " + camInfo.Height + " @ " + camInfo.FPS + " FPS");
            }
            return itemsList;
        }

        private void ComboBoxDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxDevices.SelectedIndex == -1) return;
            ComboBoxDeviceModes.ItemsSource = DeviceModeList(ComboBoxDevices.SelectedIndex);
            GTSettings.Current.Camera.DeviceName = ComboBoxDevices.SelectedValue.ToString();
        }

        private void ComboBoxDeviceModes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxDeviceModes.SelectedIndex == -1 || ComboBoxDevices.SelectedItem == null) return;
            GTSettings.Current.Camera.DeviceMode = ComboBoxDeviceModes.SelectedIndex;
            GTCommands.Instance.Camera.CameraChange();
        }

        #endregion


        #region Settings files

        private void ComboBoxSettingsFiles_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (ComboBoxSettingsFiles.SelectedItem.ToString().Length > 1)
            {
                GTSettings.Current.LoadConfigFile(ComboBoxSettingsFiles.SelectedItem.ToString());
            }
        }

        public void SaveSettings()
        {
            if (TextBoxSettingsName.Text.Length < 1)
                GTSettings.Current.FileSettings.SettingsName = "Settings " + DateTime.Now.ToString("dddd dd MMMM",CultureInfo.CreateSpecificCulture("en-US"));

            GTSettings.Current.WriteConfigFile();

            Visibility = Visibility.Collapsed;
        }

        #endregion


        #region LogFile

        private void OpenLogFileDialog(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog();
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text documents(.txt)|*.txt";
            dlg.InitialDirectory = Application.StartupPath;

            if (dlg.ShowDialog() != true) return;
            GTSettings.Current.FileSettings.LogFilePath = dlg.FileName;
            CheckBoxLoggingEnabled.IsChecked = true;
        }

        #endregion


        #region Get/Set

        public static SettingsWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SettingsWindow();
                }

                return instance;
            }
        }

        public int DeviceNumber
        {
            get { return ComboBoxDevices.SelectedIndex; }
        }

        public int DeviceMode
        {
            get { return ComboBoxDeviceModes.SelectedIndex; }
        }

        #endregion


        #region Misc UI stuff

        public bool HasBeenMoved
        {
            get { return hasBeenMoved; }
        }

        private void HideWindow(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            Visibility = Visibility.Collapsed;
        }

        private void DragWindow(object sender, MouseButtonEventArgs args)
        {
            try
            {
                hasBeenMoved = true;
                DragMove();
            }
            catch (Exception)
            {
            }
        }

        #endregion

    }


    public class ColorToBrushConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var col = (Color) value;
            System.Windows.Media.Color c = System.Windows.Media.Color.FromArgb(col.A, col.R, col.G, col.B);
            return new SolidColorBrush(c);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var c = (SolidColorBrush) value;
            Color col = Color.FromArgb(c.Color.A, c.Color.R, c.Color.G, c.Color.B);
            return col;
        }

        #endregion
    }
}