using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Thea2ModCompiler.Command
{
    class CommandProvider : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<bool> _canExecute;

        public CommandProvider(Action<object> execute, Func<bool> canExecute = null)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");
            _execute = execute;
            _canExecute = canExecute;
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

        public bool CanExecute(object parameter)
        {
            return true; //_canExecute == null || _canExecute();

        }
    }
}
