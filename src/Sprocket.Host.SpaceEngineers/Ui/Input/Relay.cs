using System;

namespace Sprocket.Host.SpaceEngineers.Ui.Input
{
    /// <summary>A command that hands its execution to a method, and is enabled for as long as it is told it is.</summary>
    public sealed class Relay : ICommand
    {
        readonly Action _execute;
        bool _canExecute = true;

        /// <summary>A command carried out by <paramref name="execute" />.</summary>
        public Relay(Action execute)
        {
            _execute = execute;
        }

        /// <inheritdoc cref="ICommand.CanExecute" />
        public bool CanExecute
        {
            get { return _canExecute; }
            set
            {
                if (_canExecute == value)
                    return;

                _canExecute = value;

                var changed = CanExecuteChanged;
                if (changed != null)
                    changed();
            }
        }

        /// <inheritdoc />
        public event Action CanExecuteChanged;

        /// <inheritdoc />
        public void Execute()
        {
            if (_canExecute)
                _execute();
        }
    }
}
