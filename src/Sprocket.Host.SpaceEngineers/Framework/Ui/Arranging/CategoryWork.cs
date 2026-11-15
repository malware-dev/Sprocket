using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Arranging
{
    /// <summary>
    /// The categories of one list a screen arranges, such as a logic system's sequences or triggers: their headings shown,
    /// renamed, removed and carried, and new ones added, each through <see cref="Arranger" />.
    /// </summary>
    public sealed class CategoryWork
    {
        readonly SprocketEditor _editor;
        readonly Func<CommandKind, Action, ICommand> _changes;
        readonly bool _settled;

        CategoryQuestion _question;

        // A category just added, whose name opens for typing once its row stands.
        string _naming;

        /// <summary>
        /// The categories <paramref name="arranger" /> changes, through <paramref name="editor" />, by commands <paramref name="changes" />
        /// makes; each opens a panel with its name and settings where <paramref name="settled" />, and is renamed in place where not.
        /// </summary>
        public CategoryWork(SprocketEditor editor, Arranger arranger, Func<CommandKind, Action, ICommand> changes, bool settled)
        {
            _settled = settled;
            _editor = editor;
            Arranger = arranger;
            _changes = changes;
        }

        /// <summary>What changes the list.</summary>
        public Arranger Arranger { get; }

        /// <summary>Adds an empty category under everything, whose name opens for typing once its row stands.</summary>
        public void Add()
        {
            _naming = Arranger.Add(_editor.Texts);
        }

        /// <summary>
        /// Shows the category <paramref name="heading" /> heads over the <paramref name="carries" /> rows under it, renamed,
        /// removed and carried where <paramref name="editable" /> says so, and why not in <paramref name="locked" /> otherwise; its
        /// grip says <paramref name="drags" /> and its bin <paramref name="removes" />.
        /// </summary>
        public void Heading(Texts texts, CategoryRow row, CategoryHeading heading, int carries, bool editable, string locked, string drags,
            string removes)
        {
            var name = heading.Name;
            row.Name = name;
            row.DraggedWithCount = carries;
            row.Arranged = true;
            row.Grips(editable, editable ? drags : locked);
            ICommand press;
            if (_settled)
            {
                press = _changes(CommandKind.RenameCategory, () => Open(row, heading));
                row.Opens = press;
                row.RenamesAbout = editable ? texts.Codex.CategoryOpen : locked;
            }
            else
            {
                press = _changes(CommandKind.RenameCategory, () => _editor.Rename(row.Plate, new Naming
                {
                    Name = name,
                    NameRequired = true,
                    Accepts = Arranger.Accepts,
                    Renames = wanted => Arranger.Rename(heading, wanted)
                }));
                row.Renames = press;
                row.RenamesAbout = editable ? texts.Codex.CategoryRenameAbout : locked;
            }

            if (name == _naming)
            {
                _naming = null;
                press?.Execute();
            }

            row.Removes = _changes(CommandKind.RemoveCategory, () => Arranger.Remove(heading));
            row.RemovesAbout = editable ? removes : locked;
        }

        /// <summary>The panel a category's name and settings are being given in, or null while none is open.</summary>
        public CategoryQuestion Settling => _question != null && _question.IsOpen ? _question : null;

        /// <summary>Keeps the open panel under the name it was opened from.</summary>
        /// <remarks>Called every tick.</remarks>
        public void Hang()
        {
            if (_question != null)
                _question.Hang();
        }

        /// <summary>Opens the panel of the category <paramref name="heading" /> heads, hung from the name on <paramref name="row" />, which changes it as it closes.</summary>
        void Open(CategoryRow row, CategoryHeading heading)
        {
            if (_question != null)
                _question.Finish(true);

            _question = new CategoryQuestion(_editor, heading.Name, Arranger.OneAtATime(heading.Name), () => row.Plate,
                (name, oneAtATime, cancelled) =>
                {
                    if (!cancelled)
                        Arranger.Settle(heading, name, oneAtATime);
                });
            _question.Hang();
        }
    }
}
