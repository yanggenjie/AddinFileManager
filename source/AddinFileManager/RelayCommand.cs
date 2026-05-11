using System;
using System.Windows.Input;

namespace AddinFileManager;
public class RelayCommand : ICommand
{
    #region Fields

    private readonly Action<object> _execute;
    private readonly Predicate<object> _canExecute;

    #endregion Fields

    #region Constructors

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="execute"></param>
    public RelayCommand(Action<object> execute)
        : this(execute, null)
    {
    }

    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="execute"></param>
    /// <param name="canExecute"></param>
    public RelayCommand(Action<object> execute, Predicate<object> canExecute)
    {
        if (execute == null)
            throw new ArgumentNullException("execute");

        _execute = execute;
        _canExecute = canExecute;
    }

    #endregion Constructors

    #region ICommand Members

    public bool CanExecute(object parameter)
    {
        return _canExecute == null ? true : _canExecute(parameter);
    }

    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public void Execute(object parameter)
    {
        _execute(parameter);
    }

    #endregion ICommand Members
}