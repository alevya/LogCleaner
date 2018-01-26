using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LogCleaner.ViewModels
{
  internal abstract class BaseViewModel : INotifyPropertyChanged
  {
    #region INotifyPropertyChanged Implements

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
    {
      PropertyChanged?.Invoke(this, args);
    }

    #endregion
  }
}
