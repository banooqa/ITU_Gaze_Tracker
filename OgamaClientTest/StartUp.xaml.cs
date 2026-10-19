using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OgamaClientTest
{
  /// <summary>
  /// Interaction logic for StartUp.xaml
  /// </summary>
  public partial class StartUp : Window
  {
    public StartUp()
    {
      InitializeComponent();
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
      if (rdbITUDefault.IsChecked.Value)
      {
        ITUOgamaClientTest newTestWindow = new ITUOgamaClientTest();
        newTestWindow.Show();
      }
      else
      {
        PlayStationEyeTest newTestWindow = new PlayStationEyeTest();
        newTestWindow.Show();
      }

      this.Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }

    private void rdbITUDefault_Checked(object sender, RoutedEventArgs e)
    {
      if (rdbITUPS3!=null)
      {
        this.rdbITUPS3.IsChecked = false;
      }
    }

    private void rdbITUPS3_Checked(object sender, RoutedEventArgs e)
    {
      if (rdbITUDefault != null)
      {
        this.rdbITUDefault.IsChecked = false;
      }
    }
  }
}
