namespace LogCleaner.ViewModels
{
  internal class MachineDetailViewModel : BaseViewModel
  {
    private bool _isSelected;
    private string _message;

    public MachineDetailViewModel(string number, string ip)
    {
      Number = number;
      Ip = ip;
    }

    #region Properties

    public string Number { get; }
    public string Ip { get; }

    public string Message
    {
      get { return _message; }
      set
      {
        _message = value;
        OnPropertyChanged();
      }
    }

    public bool IsSelected
    {
      get { return _isSelected; }
      set
      {
        _isSelected = value;
        OnPropertyChanged("IsSelected");
      }
    }

    #endregion
  }
}
