using System;

namespace Lumel.Input
{
    /// <summary>What an element does when it is used, supplied by the application.</summary>
    public interface ICommand
    {
        /// <summary>Whether the command can be carried out now; an element carrying one that cannot is disabled.</summary>
        bool CanExecute { get; }

        /// <summary>Raised when <see cref="CanExecute" /> may have changed.</summary>
        event Action CanExecuteChanged;

        /// <summary>Carries the command out.</summary>
        void Execute();
    }
}
