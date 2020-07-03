namespace Wox.ViewModel
{
    using System;
    using System.Windows.Input;

    public class RelayCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private readonly Action<object> _action;

        public RelayCommand(Action<object> action)
        {
            _action = action;
        }

        #region Public

        public virtual bool CanExecute(object parameter)
        {
            return true;
        }

        public virtual void Execute(object parameter)
        {
            _action?.Invoke(parameter);
        }

        #endregion
    }
}