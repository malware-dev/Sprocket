using Sprocket.Host.SpaceEngineers.Ui.Input;

namespace Sprocket.Host.SpaceEngineers.Ui.Windows
{
    /// <summary>A modal window in the middle of the screen that asks something and waits for the answer.</summary>
    /// <remarks>Enter carries out <see cref="Accept" /> and Escape <see cref="Cancel" />, whatever inside it holds the keyboard.</remarks>
    public class Dialog : Window
    {
        ICommand _accept;
        ICommand _cancel;

        /// <summary>A dialog with nothing in it yet.</summary>
        public Dialog()
        {
            IsModal = true;
        }

        /// <summary>The answer Enter gives, or null where Enter gives none.</summary>
        public ICommand Accept
        {
            get { return _accept; }
            set
            {
                _accept = value;
                CommandsChanged();
            }
        }

        /// <summary>What Escape does: put the dialog away without an answer, or null where it cannot be put away.</summary>
        public ICommand Cancel
        {
            get { return _cancel; }
            set
            {
                _cancel = value;
                CommandsChanged();
            }
        }

        /// <summary>Gives the answer Enter gives, and says whether there was one.</summary>
        public bool TakeAccept()
        {
            return Carry(_accept);
        }

        /// <summary>Puts the dialog away the way Escape does, and says whether it could be.</summary>
        public bool TakeCancel()
        {
            return Carry(_cancel);
        }

        /// <summary>Told when <see cref="Accept" /> or <see cref="Cancel" /> changes, for a kind of dialog that shows them.</summary>
        protected virtual void CommandsChanged()
        {
        }

        static bool Carry(ICommand command)
        {
            if (command == null || !command.CanExecute)
                return false;

            command.Execute();
            return true;
        }
    }
}
