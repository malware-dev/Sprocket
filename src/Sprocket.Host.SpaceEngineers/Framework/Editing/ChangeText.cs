using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using System.Collections.Generic;
using System.Globalization;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Editing
{
    /// <summary>What a change is called after Undo and Redo.</summary>
    public static class ChangeText
    {
        /// <summary>
        /// <paramref name="edit" /> in words, naming what it changed as <paramref name="subject" /> stands now, with the change
        /// made where <paramref name="done" /> and taken back where not; empty for no edit.
        /// </summary>
        /// <remarks>
        /// Blocks are named through <paramref name="bindings" /> as <paramref name="through" /> fills the slots, or as written where
        /// it is null, and a trigger's words are looked up in <paramref name="program" />.
        /// </remarks>
        public static string Of<T>(Texts texts, Edit<T> edit, T subject, bool done, Automation program, Bindings bindings,
            SystemInstance through)
        {
            if (edit == null)
                return "";

            var name = new ChangeName();
            edit.Name(subject, done, name);

            var change = edit.Change;
            var system = subject as AutomationSystem ?? name.Place as AutomationSystem;
            var said = Wording(texts, change);
            said = Template.Fill(said, "thing", Words(texts, change, name.Thing, name, program, bindings, system, through));
            said = Template.Fill(said, "place", Words(texts, change, name.Place, name, program, bindings, system, through));
            said = Template.Fill(said, "was", Words(texts, change, name.Was, name, program, bindings, system, through));
            said = Template.Fill(said, "now", Words(texts, change, name.Now, name, program, bindings, system, through));
            said = Template.Fill(said, "number", name.Number.ToString(CultureInfo.InvariantCulture));
            return Template.Fill(said, "count", name.Count.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>What <paramref name="thing" />, one of the things a change names, is called on the screens.</summary>
        static string Words(Texts texts, ChangeKind change, object thing, ChangeName name, Automation program, Bindings bindings,
            AutomationSystem system, SystemInstance through)
        {
            if (thing == null)
                return "";

            // A sequence nobody named has no name, and the screens call it by its number.
            var words = thing as string;
            var unnamed = name.Thing as Sequence;
            if (words != null && words.Length == 0 && unnamed != null)
                return SequenceText.DefaultName(texts, unnamed.Number);

            if (words != null)
                return change == ChangeKind.SlotType ? TypeText.Of(texts, words) : words;

            if (thing is int)
                return ((int)thing).ToString(CultureInfo.InvariantCulture);

            var step = thing as Step;
            if (step != null)
            {
                var steps = name.Place as Sequence;
                return steps == null ? "" : SentenceText.Short(SequenceText.Step(bindings, steps, step, through, Slotted(system)));
            }

            var sequence = thing as Sequence;
            if (sequence != null)
                return SequenceText.Called(texts, sequence);

            var trigger = thing as Trigger;
            if (trigger != null)
                return SentenceText.Short(SequenceText.Trigger(program, bindings, system, trigger, through, Slotted(system)));

            var part = thing as Part;
            if (part != null)
                return part.Id;

            var state = thing as State;
            if (state != null)
                return state.Name;

            var type = thing as DeclaredType;
            if (type != null)
                return type.Name;

            var instance = thing as SystemInstance;
            if (instance != null)
                return instance.Name ?? "";

            var other = thing as AutomationSystem;
            if (other != null)
                return SystemSummary.Describe(texts, bindings, other);

            var blocks = thing as IList<string>;
            if (blocks != null)
                return SentenceText.Named(bindings, blocks, false);

            if (thing is Duration)
                return TimeText.Duration((Duration)thing);

            return thing is Literal ? ((Literal)thing).Said : "";
        }

        /// <summary>Whether the rows of <paramref name="system" /> name its slots rather than blocks: it is a blueprint.</summary>
        static bool Slotted(AutomationSystem system) => system != null && system.IsBlueprint;

        /// <summary>The words for <paramref name="change" /> in <paramref name="texts" />, with a slot for each thing it names, or empty for none.</summary>
        static string Wording(Texts texts, ChangeKind change)
        {
            switch (change)
            {
                case ChangeKind.Describe: return texts.ChangeDescribe;
                case ChangeKind.PickButton: return texts.ChangePickButton;
                case ChangeKind.Step: return texts.ChangeStep;
                case ChangeKind.When: return texts.ChangeWhen;
                case ChangeKind.ForgetTrigger: return texts.ChangeForgetTrigger;
                case ChangeKind.MoveStep: return texts.ChangeMoveStep;
                case ChangeKind.Target: return texts.ChangeTarget;
                case ChangeKind.RemoveStep: return texts.ChangeRemoveStep;
                case ChangeKind.Rename: return texts.ChangeRename;
                case ChangeKind.RenameSlot: return texts.ChangeRenameSlot;
                case ChangeKind.ListedOn: return texts.ChangeListedOn;
                case ChangeKind.Trigger: return texts.ChangeTrigger;
                case ChangeKind.Cadence: return texts.ChangeCadence;
                case ChangeKind.Optional: return texts.ChangeOptional;
                case ChangeKind.Required: return texts.ChangeRequired;
                case ChangeKind.SlotType: return texts.ChangeSlotType;
                case ChangeKind.Wait: return texts.ChangeWait;
                case ChangeKind.PauseStep: return texts.ChangePauseStep;
                case ChangeKind.ResumeStep: return texts.ChangeResumeStep;
                case ChangeKind.TakeOver: return texts.ChangeTakeOver;
                case ChangeKind.AddStep: return texts.ChangeAddStep;
                case ChangeKind.AddWait: return texts.ChangeAddWait;
                case ChangeKind.DescribeSlot: return texts.ChangeDescribeSlot;
                case ChangeKind.ShortenSlot: return texts.ChangeShortenSlot;
                case ChangeKind.AddIf: return texts.ChangeAddIf;
                case ChangeKind.AddBranch: return texts.ChangeAddBranch;
                case ChangeKind.RemoveIf: return texts.ChangeRemoveIf;
                case ChangeKind.Unwrap: return texts.ChangeUnwrap;
                case ChangeKind.RemoveBranch: return texts.ChangeRemoveBranch;
                case ChangeKind.MoveBranch: return texts.ChangeMoveBranch;
                case ChangeKind.DeleteSequence: return texts.ChangeDeleteSequence;
                case ChangeKind.NewSequence: return texts.ChangeNewSequence;
                case ChangeKind.CopySequence: return texts.ChangeCopySequence;
                case ChangeKind.CopyStep: return texts.ChangeCopyStep;
                case ChangeKind.CopyTrigger: return texts.ChangeCopyTrigger;
                case ChangeKind.RenameSystem: return texts.ChangeRenameSystem;
                case ChangeKind.RenameInstallation: return texts.ChangeRenameInstallation;
                case ChangeKind.FillSlot: return texts.ChangeFillSlot;
                case ChangeKind.AddTrigger: return texts.ChangeAddTrigger;
                case ChangeKind.PutOnSystem: return texts.ChangePutOnSystem;
                case ChangeKind.CopyBlueprint: return texts.ChangeCopyBlueprint;
                case ChangeKind.RenameConstruct: return texts.ChangeRenameConstruct;
                case ChangeKind.DeleteSystem: return texts.ChangeDeleteSystem;
                case ChangeKind.TakeOffSystem: return texts.ChangeTakeOffSystem;
                case ChangeKind.DeleteInstallation: return texts.ChangeDeleteInstallation;
                case ChangeKind.AddSystem: return texts.ChangeAddSystem;
                case ChangeKind.AddInstallation: return texts.ChangeAddInstallation;
                case ChangeKind.UpdateFromLibrary: return texts.ChangeUpdateFromLibrary;
                case ChangeKind.ResetToBlueprint: return texts.ChangeResetToBlueprint;
                case ChangeKind.ChooseVersion: return texts.ChangeChooseVersion;
                case ChangeKind.MoveSlot: return texts.ChangeMoveSlot;
                case ChangeKind.MoveCategory: return texts.ChangeMoveCategory;
                case ChangeKind.MoveSequence: return texts.ChangeMoveSequence;
                case ChangeKind.MoveSequenceCategory: return texts.ChangeMoveSequenceCategory;
                case ChangeKind.MoveTrigger: return texts.ChangeMoveTrigger;
                case ChangeKind.MoveTriggerCategory: return texts.ChangeMoveTriggerCategory;
                case ChangeKind.AddCategory: return texts.ChangeAddCategory;
                case ChangeKind.RenameCategory: return texts.ChangeRenameCategory;
                case ChangeKind.RemoveCategory: return texts.ChangeRemoveCategory;
                case ChangeKind.CategoryOneAtATime: return texts.ChangeCategoryOneAtATime;
                case ChangeKind.AddSlot: return texts.ChangeAddSlot;
                case ChangeKind.AddType: return texts.ChangeAddType;
                case ChangeKind.RenameType: return texts.ChangeRenameType;
                case ChangeKind.RemoveType: return texts.ChangeRemoveType;
                case ChangeKind.MoveType: return texts.ChangeMoveType;
                case ChangeKind.AddChoice: return texts.ChangeAddChoice;
                case ChangeKind.RenameChoice: return texts.ChangeRenameChoice;
                case ChangeKind.RemoveChoice: return texts.ChangeRemoveChoice;
                case ChangeKind.AddState: return texts.ChangeAddState;
                case ChangeKind.RenameState: return texts.ChangeRenameState;
                case ChangeKind.RemoveState: return texts.ChangeRemoveState;
                case ChangeKind.MoveState: return texts.ChangeMoveState;
                case ChangeKind.ChangeStart: return texts.ChangeChangeStart;
                case ChangeKind.ChangeNow: return texts.ChangeChangeNow;
                case ChangeKind.OptimizeSlots: return texts.ChangeOptimizeSlots;
                case ChangeKind.RemoveSlot: return texts.ChangeRemoveSlot;
                default: return "";
            }
        }
    }
}
