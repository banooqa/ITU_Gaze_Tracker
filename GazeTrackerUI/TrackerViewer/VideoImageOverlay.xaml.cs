using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GazeTrackingLibrary.Settings;
using GazeTrackingLibrary.Utils;

namespace GazeTrackerUI.TrackerViewer 
{

    public partial class VideoImageOverlay : Window
    {

        public VideoImageOverlay() 
        {
            InitializeComponent();
            HookUpEvents();
            GridPerformanceCounters.Visibility = System.Windows.Visibility.Collapsed;
            GridVisualization.IsVisibleChanged += new DependencyPropertyChangedEventHandler(GridVisualization_IsVisibleChanged);
        }



        void GridVisualization_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) 
        {
            switch (GTSettings.Current.Visualization.VideoMode)
            {
                case VideoModeEnum.Normal:
                    CheckBoxRenderNormal.IsChecked = true;
                    break;
                case VideoModeEnum.Processed:
                    CheckBoxRenderProcessed.IsChecked = true;
                    break;
                case VideoModeEnum.RawNoTracking:
                    CheckBoxRenderRaw.IsChecked = true;
                    break;       
            }
        }


        #region Private methods

        private void HookUpEvents() 
        {
            CheckBoxRenderRaw.Checked += VideoModeChange;
            CheckBoxRenderNormal.Checked += VideoModeChange;
            CheckBoxRenderProcessed.Checked += VideoModeChange;
            CheckBoxRenderRaw.Unchecked += VideoModeChangeDummyUnchecked;
            CheckBoxRenderNormal.Unchecked += VideoModeChangeDummyUnchecked;
            CheckBoxRenderProcessed.Unchecked += VideoModeChangeDummyUnchecked;
        }


        private void VideoModeChange(object sender, RoutedEventArgs e)
        {
            CheckBox videoModeCb = sender as CheckBox;
           
            // Switch video mode

            if (videoModeCb != null)
                switch (videoModeCb.Name)
                {
                    case "CheckBoxRenderRaw":
                        GTSettings.Current.Visualization.VideoMode = VideoModeEnum.RawNoTracking;
                        CheckBoxRenderNormal.IsChecked = false;
                        CheckBoxRenderProcessed.IsChecked = false;
                        break;
                    case "CheckBoxRenderNormal":
                        GTSettings.Current.Visualization.VideoMode = VideoModeEnum.Normal;
                        CheckBoxRenderProcessed.IsChecked = false;
                        CheckBoxRenderRaw.IsChecked = false;
                        break;
                    case "CheckBoxRenderProcessed":
                        GTSettings.Current.Visualization.VideoMode = VideoModeEnum.Processed;
                        CheckBoxRenderNormal.IsChecked = false;
                        CheckBoxRenderRaw.IsChecked = false;
                        break;
                }
        }

        private void VideoModeChangeDummyUnchecked(object sender, RoutedEventArgs e)
        {
            CheckBox videoModeCb = sender as CheckBox;

            // Clicking same button twice? Somethings got to be turned on!
            if(videoModeCb.Name == "CheckBoxRenderRaw" && GTSettings.Current.Visualization.VideoMode == VideoModeEnum.RawNoTracking)
            {
                videoModeCb.IsChecked = true;
                return;
            }
            if(videoModeCb.Name == "CheckBoxRenderNormal" && GTSettings.Current.Visualization.VideoMode == VideoModeEnum.Normal)
            {
                videoModeCb.IsChecked = true;
                return;
            }
            if(videoModeCb.Name == "CheckBoxRenderProcessed" && GTSettings.Current.Visualization.VideoMode == VideoModeEnum.Processed)
            {
                videoModeCb.IsChecked = true;
                return;
            } 
        }

        #endregion


    }

}
