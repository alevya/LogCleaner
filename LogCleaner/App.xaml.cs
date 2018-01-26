using System.Windows;
using System.Windows.Forms.VisualStyles;
using System.Windows.Threading;
using LogCleaner.ViewModels;
using LogCleaner.Views;

namespace LogCleaner
{
  /// <summary>
  /// Логика взаимодействия для App.xaml
  /// </summary>
  public partial class App : Application
  {
    protected override void OnStartup(StartupEventArgs args)
    {
      base.OnStartup(args);
      var dataContext = new MainViewModel(LogCleaner.Views.MainWindow.OpenFileFunc);
      dataContext.InitConfig();
      MainWindow = new MainWindow
      {
        DataContext = dataContext
      };
      MainWindow.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
      MessageBox.Show("Произошло не обрабатываемое исключение: " + args.Exception.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
      args.Handled = true;
      App.Current.Shutdown();
    }

     
  }
}
