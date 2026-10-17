using System.Collections.Generic;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Codex;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Ownership
{
    /// <summary>
    /// What a big owner meets while the automation is paused for its construct changing hands: why, how much it holds and cannot
    /// reach, and the three ways on.
    /// </summary>
    /// <remarks>
    /// In place of the home screen whenever the editor opens on paused automation, so nobody edits or resumes it without having
    /// read it. The elements it is drawn with are declared in <c>PausedPage.View.cs</c>.
    /// </remarks>
    public sealed partial class PausedPage : EditorPage
    {
        /// <summary>The page for the automation <paramref name="editor" /> shows.</summary>
        public PausedPage(SprocketEditor editor) : base(editor)
        {
        }

        /// <inheritdoc />
        public override string Title => Editor.Texts.PausedTitle;

        /// <inheritdoc />
        public override string Help => Terms.ChangingHands;

        /// <inheritdoc />
        /// <remarks>So its column can stand in the middle of the page rather than at the top of a scrolled list.</remarks>
        public override bool ScrollsItself => true;

        /// <summary>Builds nothing: what it says is made with the view.</summary>
        public override void Build()
        {
        }

        /// <summary>Says why it stopped and how much it holds and cannot reach, and gives each way on its command.</summary>
        /// <remarks>
        /// Counts rather than lists, so the screen is the same few lines on a ship of any size; the automation itself is read in
        /// the editor, which leaving it paused opens.
        /// </remarks>
        protected override void ViewCreated()
        {
            var texts = Editor.Texts;
            var program = Editor.Program;
            var why = Template.Fill(texts.PausedWhy, "construct", Editor.World.Name);
            why = Template.Fill(why, "before", Names(program.PausedFrom));
            why = Template.Fill(why, "now", Names(program.Owners));

            var size = Counted(program.Systems.Count, texts.PausedSizeOne, texts.PausedSize);
            var lost = Editor.World.Unshared.Count;
            if (lost > 0)
                size += $" {Counted(lost, texts.PausedLostOne, texts.PausedLost)}";

            var returned = Editor.Returned.Count;
            if (returned > 0)
                size += $" {Counted(returned, texts.PausedReturnedOne, texts.PausedReturned)}";

            _body.Markdown = $"::: center\n![](Sprocket_Sleepy =160)\n\n# {texts.PausedHeading}\n\n{why} {size}\n:::";

            _resume.Text = texts.PausedResume;
            _resume.Command = Editor.Act(CommandKind.Resume, Editor.Resume);
            _leave.Text = texts.PausedLeave;
            _leave.Command = Editor.Act(CommandKind.LeavePaused, Editor.LeavePaused);
            _remove.Text = texts.PausedRemoveAll;
            _remove.Command = Editor.Act(CommandKind.RemoveAll, Editor.RemoveAll);
        }

        /// <summary><paramref name="one" /> where <paramref name="count" /> is one, and <paramref name="many" /> filled with it otherwise.</summary>
        static string Counted(int count, string one, string many)
        {
            return count == 1 ? one : Template.Fill(many, "count", count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>The players <paramref name="identities" /> name, by the names the game knows them by.</summary>
        string Names(List<long> identities)
        {
            var names = new List<string>();
            foreach (var identity in identities)
            {
                var name = Editor.PlayerName == null ? "" : Editor.PlayerName(identity);
                names.Add(string.IsNullOrEmpty(name) ? Editor.Texts.PausedSomeone : name);
            }

            return names.Count == 0 ? Editor.Texts.PausedSomeone : string.Join(", ", names);
        }
    }
}
