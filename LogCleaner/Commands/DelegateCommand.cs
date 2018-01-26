using System;
using System.Windows.Input;

namespace LogCleaner.Commands
{
  internal class DelegateCommand : ICommand
  {
    private readonly Predicate<object> _canExecPredicate;
    private readonly Action<object> _execAction;

    public DelegateCommand(Action<object> execute) : this(execute, null)
    { }

    public DelegateCommand(Action<object> execute, Predicate<object> predicate = null)
    {
      _execAction = execute ?? throw new ArgumentNullException(nameof(execute));
      _canExecPredicate = predicate;
    }

    #region ICommand Implements

    public bool CanExecute(object parameter)
    {
      return _canExecPredicate == null || _canExecPredicate(parameter);
    }

    public void Execute(object parameter)
    {
      if (!CanExecute(parameter))
      {
        throw new InvalidOperationException("Команда не действительна для выполнения");
      }

      _execAction(parameter);
    }
    public event EventHandler CanExecuteChanged
    {
      add => CommandManager.RequerySuggested += value;
      remove => CommandManager.RequerySuggested -= value;
    }

    #endregion
  }
}
