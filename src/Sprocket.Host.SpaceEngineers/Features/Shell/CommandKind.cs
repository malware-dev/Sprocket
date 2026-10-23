
namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>What a press is for, which decides whether a copy lent to look at may make it and whether it changes the program.</summary>
    public enum CommandKind
    {
        /// <summary>No press at all.</summary>
        None = 0,

        // Screen furniture, one of each per screen.
        /// <summary>Going back a screen.</summary>
        Back,

        /// <summary>Answering a question a screen asks.</summary>
        Answer,

        /// <summary>The screen's main press, such as Done.</summary>
        Primary,

        /// <summary>Renaming what the screen's heading names.</summary>
        Rename,

        /// <summary>Switching the screen's subject on or off.</summary>
        Switch,

        /// <summary>Opening or choosing a row to see more of it.</summary>
        Expand,

        /// <summary>Putting a program in place of one that would not read, once told what that costs.</summary>
        Resolve,

        /// <summary>Switching a step off, or back on.</summary>
        SwitchStep,

        /// <summary>Putting the program a text says in place of the one there, having read the text first.</summary>
        Mend,

        /// <summary>Making a new logic system.</summary>
        NewSystem,

        /// <summary>Opening the library.</summary>
        LoadSystem,

        /// <summary>Opening a logic system.</summary>
        OpenSystem,

        /// <summary>Opening the codex, or moving within it.</summary>
        OpenCodex,

        /// <summary>Adding an installation to a logic system.</summary>
        AddInstance,

        /// <summary>Choosing which installation of a logic system a screen shows.</summary>
        ChooseInstance,

        /// <summary>Deleting a logic system.</summary>
        DeleteSystem,

        /// <summary>Deleting an installation.</summary>
        DeleteInstance,

        /// <summary>Opening a slot's page, where it is named and described.</summary>
        OpenSlot,

        /// <summary>Answering a slot with the blocks chosen for it.</summary>
        FillPart,

        /// <summary>Making a slot optional, or required again.</summary>
        MakeOptional,

        /// <summary>Opening the choice of the type of block a slot takes.</summary>
        SlotType,

        /// <summary>Starting a sequence that something happening runs.</summary>
        StartWhen,

        /// <summary>Starting a sequence that a clock runs.</summary>
        StartEvery,

        /// <summary>Opening a sequence.</summary>
        OpenSequence,

        /// <summary>Making a new sequence.</summary>
        NewSequence,

        /// <summary>Opening a trigger.</summary>
        OpenTrigger,

        /// <summary>Changing what a trigger does.</summary>
        TriggerTarget,

        /// <summary>Deleting a trigger.</summary>
        DeleteTrigger,

        /// <summary>Copying a trigger.</summary>
        DuplicateTrigger,

        /// <summary>Starting paused automation again as it was.</summary>
        Resume,

        /// <summary>Keeping paused automation stopped, and going on into the editor.</summary>
        LeavePaused,

        /// <summary>Removing every logic system of paused automation, after asking.</summary>
        RemoveAll,

        /// <summary>Opening the resume screen again from the banner.</summary>
        ReviewPaused,

        /// <summary>Switching a logic system on or off.</summary>
        EnableSystem,

        /// <summary>Switching an installation on or off.</summary>
        EnableInstance,

        /// <summary>Deleting a sequence.</summary>
        DeleteSequence,

        /// <summary>Copying a sequence.</summary>
        DuplicateSequence,

        /// <summary>Running a sequence by hand, at once.</summary>
        RunNow,

        /// <summary>Stopping a running sequence by hand.</summary>
        StopNow,

        /// <summary>Taking the last change back.</summary>
        Undo,

        /// <summary>Making a change taken back again.</summary>
        Redo,

        /// <summary>Adding a step that has a block do something.</summary>
        AddAction,

        /// <summary>Adding a wait.</summary>
        AddWait,

        /// <summary>Adding a wait for everything started above it, where waits are rows of their own.</summary>
        AddAwait,

        /// <summary>Adding a step that runs another sequence.</summary>
        AddRun,

        /// <summary>Adding a step that stops another sequence.</summary>
        AddStop,

        /// <summary>Adding an if.</summary>
        AddIf,

        /// <summary>Adding a step that sets one of the logic system's states.</summary>
        AddSetState,

        /// <summary>Adding a branch to an if.</summary>
        AddBranch,

        /// <summary>Changing one of the values in a step, such as how long a wait lasts.</summary>
        StepSlot,

        /// <summary>Moving a step to another place in its sequence.</summary>
        MoveStep,

        /// <summary>Moving a slot, or a category with its slots, to another place on the installation panel.</summary>
        MoveSlot,

        /// <summary>Adding a category of slots to the installation panel.</summary>
        AddCategory,

        /// <summary>Renaming a category of slots.</summary>
        RenameCategory,

        /// <summary>Removing a category of slots, which leaves its slots under none.</summary>
        RemoveCategory,

        /// <summary>Adding a type to a logic system.</summary>
        AddType,

        /// <summary>Renaming a type of a logic system.</summary>
        RenameType,

        /// <summary>Removing a type of a logic system.</summary>
        RemoveType,

        /// <summary>Moving a type to another place among a logic system's types.</summary>
        MoveType,

        /// <summary>Adding a choice to a type.</summary>
        AddChoice,

        /// <summary>Renaming or removing a choice of a type.</summary>
        ChangeChoice,

        /// <summary>Adding a state to a logic system.</summary>
        AddState,

        /// <summary>Renaming a state of a logic system.</summary>
        RenameState,

        /// <summary>Removing a state of a logic system.</summary>
        RemoveState,

        /// <summary>Moving a state to another place among a logic system's states.</summary>
        MoveState,

        /// <summary>Changing the value a state starts at, and with it what the state holds.</summary>
        ChangeStart,

        /// <summary>Deleting a step.</summary>
        DeleteStep,

        /// <summary>Copying a step, right after it.</summary>
        DuplicateStep,

        /// <summary>Picking a block.</summary>
        PickEntity,

        /// <summary>Picking what a block does or is waited for.</summary>
        PickChoice,

        /// <summary>Typing into the block search.</summary>
        NarrowEntities,

        /// <summary>Typing into the search over what can be done or waited for.</summary>
        NarrowChoices,

        /// <summary>Typing into the search over kinds of block.</summary>
        NarrowKinds,

        /// <summary>Listing what a block does in Sprocket's own words.</summary>
        ShowOrganized,

        /// <summary>Listing what a block does in the game's terminal words.</summary>
        ShowTerminal,

        /// <summary>Showing blocks of every kind.</summary>
        AnyKind,

        /// <summary>Showing or hiding the blocks hidden from toolbars.</summary>
        HiddenBlocks,

        /// <summary>Showing blocks of one kind.</summary>
        OneKind,

        /// <summary>Choosing how a check or a trigger reads its blocks: any, all, their sum or their average.</summary>
        ReadBlocks,

        /// <summary>Switching a trigger between running its sequence and stopping it.</summary>
        TriggerStops,

        /// <summary>Having a trigger have a block do one thing.</summary>
        TriggerDoes,

        /// <summary>Going on to the next page of a list.</summary>
        PageOn,

        /// <summary>Going back to the previous page of a list.</summary>
        PageBack,

        // Shelve, Unshelve and ForgetBackup change no program, so a copy lent to look at may make them (Gate).
        /// <summary>Putting a blueprint in the library, or bringing it up to date there.</summary>
        Shelve,

        /// <summary>Deleting a blueprint from the library.</summary>
        Unshelve,

        /// <summary>Putting a library blueprint on the construct.</summary>
        PutOn,

        /// <summary>Taking a logic system off the construct.</summary>
        TakeOff,

        /// <summary>Forgetting a backup in the library.</summary>
        ForgetBackup,

        /// <summary>Putting a logic system's blueprint in the library in the place of the logic system, which changes the program.</summary>
        Renew,

        /// <summary>Keeping one of two versions of a logic system, which changes the program.</summary>
        ChooseVersion,

        /// <summary>Picking one of a list of answers.</summary>
        Choice,

        /// <summary>Showing the tools for working on Sprocket itself, registered only in developer mode.</summary>
        ShowScript
    }
}
