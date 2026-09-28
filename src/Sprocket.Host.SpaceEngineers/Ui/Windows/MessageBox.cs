using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Styling;
using Sprocket.Host.SpaceEngineers.Ui.Text;

namespace Sprocket.Host.SpaceEngineers.Ui.Windows
{
    /// <summary>A dialog that says something and offers up to three standard answers: Yes, No and Cancel, in that order across its foot, as the game's own message box does.</summary>
    /// <remarks>
    ///     A Yes or No button is shown while it has a command, and a Cancel button while <see cref="Dialog.Cancel" /> has one
    ///     and <see cref="ShowsCancel" /> is set; each says whatever its text is set to. Enter gives the <see cref="Default" />
    ///     answer and Escape cancels, shown or not.
    /// </remarks>
    public class MessageBox : Dialog
    {
        readonly RichText _message;
        readonly Button _yes;
        readonly Button _no;
        readonly Button _cancel;
        readonly Relay _accepts;

        ICommand _yesCommand;
        ICommand _noCommand;
        MessageButton? _default;
        bool _showsCancel = true;
        ButtonStyle _buttonStyle;
        ButtonStyle _defaultStyle;

        /// <summary>A message box with nothing to say yet, whose buttons read Yes, No and Cancel.</summary>
        public MessageBox()
        {
            _accepts = new Relay(Accepted);

            Content = new VStack
            {
                Spacing = 24f,
                Children =
                {
                    new RichText().As(out _message),
                    new HStack
                    {
                        Spacing = 8f,
                        Children =
                        {
                            new VStack { Placement = Flex.Grow },
                            new Button { Text = "Yes", Command = new Relay(() => Carry(_yesCommand)) }.As(out _yes),
                            new Button { Text = "No", Command = new Relay(() => Carry(_noCommand)) }.As(out _no),
                            new Button { Text = "Cancel", Command = new Relay(() => TakeCancel()) }.As(out _cancel),
                            new VStack { Placement = Flex.Grow }
                        }
                    }
                }
            };

            Accept = _accepts;
        }

        /// <summary>What the box says.</summary>
        public Document Message
        {
            get { return _message.Document; }
            set { _message.Document = value; }
        }

        /// <summary>The words for the message's named slots, or null where it has none.</summary>
        public Arguments MessageArguments
        {
            get { return _message.Arguments; }
            set { _message.Arguments = value; }
        }

        /// <summary>How the message looks.</summary>
        public IStyle MessageStyle
        {
            get { return _message.Style; }
            set { _message.Style = value; }
        }

        /// <summary>How an answer looks, but for the default one.</summary>
        public ButtonStyle ButtonStyle
        {
            get { return _buttonStyle; }
            set
            {
                _buttonStyle = value;
                Dress();
            }
        }

        /// <summary>How the answer Enter gives looks.</summary>
        public ButtonStyle DefaultStyle
        {
            get { return _defaultStyle; }
            set
            {
                _defaultStyle = value;
                Dress();
            }
        }

        /// <summary>What saying yes does, or null to show no Yes button.</summary>
        public ICommand YesCommand
        {
            get { return _yesCommand; }
            set
            {
                _yesCommand = value;
                Dress();
            }
        }

        /// <summary>What saying no does, or null to show no No button.</summary>
        public ICommand NoCommand
        {
            get { return _noCommand; }
            set
            {
                _noCommand = value;
                Dress();
            }
        }

        /// <summary>What the Yes button says.</summary>
        public string YesText
        {
            get { return _yes.Text; }
            set { _yes.Text = value; }
        }

        /// <summary>What the No button says.</summary>
        public string NoText
        {
            get { return _no.Text; }
            set { _no.Text = value; }
        }

        /// <summary>What the Cancel button says.</summary>
        public string CancelText
        {
            get { return _cancel.Text; }
            set { _cancel.Text = value; }
        }

        /// <summary>Whether cancelling has a button of its own, rather than Escape alone.</summary>
        public bool ShowsCancel
        {
            get { return _showsCancel; }
            set
            {
                _showsCancel = value;
                Dress();
            }
        }

        /// <summary>The answer Enter gives, which stands out from the others, or null where Enter gives none.</summary>
        public MessageButton? Default
        {
            get { return _default; }
            set
            {
                _default = value;
                Dress();
            }
        }

        /// <inheritdoc />
        protected override void CommandsChanged()
        {
            Dress();
        }

        /// <summary>Shows the buttons that have something to do, and marks the default one.</summary>
        void Dress()
        {
            _accepts.CanExecute = _default.HasValue;

            _yes.IsVisible = _yesCommand != null;
            _no.IsVisible = _noCommand != null;
            _cancel.IsVisible = Cancel != null && _showsCancel;

            _yes.Style = _default == MessageButton.Yes ? _defaultStyle : _buttonStyle;
            _no.Style = _default == MessageButton.No ? _defaultStyle : _buttonStyle;
            _cancel.Style = _default == MessageButton.Cancel ? _defaultStyle : _buttonStyle;
        }

        void Accepted()
        {
            if (!_default.HasValue)
                return;

            switch (_default.Value)
            {
                case MessageButton.Yes:
                    Carry(_yesCommand);
                    break;
                case MessageButton.No:
                    Carry(_noCommand);
                    break;
                default:
                    TakeCancel();
                    break;
            }
        }

        static void Carry(ICommand command)
        {
            if (command != null && command.CanExecute)
                command.Execute();
        }
    }
}
