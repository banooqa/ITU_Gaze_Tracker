using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace GazeTrackerUI
{
    /// <summary>
    /// Interaction logic for ErrorOnStartupWindow.xaml
    /// </summary>
    public partial class MessageWindow : Window
    {
        private const string forumUrl =
            "http://www.gazegroup.org/forum/viewforum.php?f=6&sid=fccbc6848988315a3d69331c6bddad3c";

        private const string gettingStartedUrl = "http://www.gazegroup.org/software/GT_Users_Guide.pdf";
        private string messageText = "";
        private const string videoInstructionsUrl = "http://www.youtube.com/watch?v=vgtr3sH4aY8&feature=channel_page";


        public MessageWindow()
        {
            InitializeComponent();
            Topmost = true;
        }

        public MessageWindow(string txt)
        {
            InitializeComponent();
            Topmost = true;
            Text = txt;
        }


        public string Text
        {
            get { return messageText; }
            set
            {
                messageText = value;
                TextBlockMessage.Text = messageText;
            }
        }


        private void VisitForum(object sender, RoutedEventArgs e)
        {
            Process.Start(forumUrl);
        }

        private void ReadDocumentation(object sender, RoutedEventArgs e)
        {
            Process.Start(gettingStartedUrl);
        }

        private void VideoInstructions(object sender, RoutedEventArgs e)
        {
            Process.Start(videoInstructionsUrl);
        }

        #region WindowManagement

        private void AppClose(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void DragWindow(object sender, MouseButtonEventArgs args)
        {
            DragMove();
        }

        #endregion
    }
}