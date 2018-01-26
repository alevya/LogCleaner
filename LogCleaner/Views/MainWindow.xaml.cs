using System;
using System.Windows;
using System.Windows.Forms;

namespace LogCleaner.Views
{
  /// <summary>
  /// Логика взаимодействия для MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
      public static Func<string> OpenFileFunc = OpenFile;

    public MainWindow()
    {
      InitializeComponent();
      
    }

    private static string OpenFile()
    {
        var dlg = new OpenFileDialog
        {
            InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
            Filter = @"Config-файл (.config)|*.config",
            Title = @"Открыть файл конфигурации",
            CheckFileExists = true,
            CheckPathExists = true,
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
        {
            return string.Empty;
        }

        return dlg.FileName;
    }
      
  }
}
