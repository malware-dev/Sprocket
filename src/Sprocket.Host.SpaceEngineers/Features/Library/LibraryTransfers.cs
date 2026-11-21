using Sprocket.Host.SpaceEngineers.Features.LogicSystems.Edits;
using Sprocket.Host.SpaceEngineers.Features.Sharing;
using Sprocket.Host.SpaceEngineers.Features.Sharing.Messages;
using Sprocket.Host.SpaceEngineers.Features.Sharing.Text;
using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;
using System.Collections.Generic;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    /// <summary>Moving logic systems between the construct and the library: putting blueprints on, taking them off, keeping and renewing them.</summary>
    public sealed class LibraryTransfers
    {
        readonly SprocketEditor _editor;
        readonly ChangeDates _dates;

        /// <summary>The library's dealings with what <paramref name="editor" /> edits, dating what comes back through <paramref name="dates" />.</summary>
        public LibraryTransfers(SprocketEditor editor, ChangeDates dates)
        {
            _editor = editor;
            _dates = dates;
        }

        /// <summary>
        /// Puts the blueprint <paramref name="system" /> in the library as it is now, promoting the backup that keeps it as it is,
        /// where there is one; and has the library, where it is showing, choose it and bring it into view.
        /// </summary>
        public void Shelve(AutomationSystem system)
        {
            _editor.Stamp(true);
            var twin = _editor.Backups?.Keeping(system, _editor.World.Values);
            var kept = _editor.Shelf.Keep(system);
            _editor.Sharing?.Renew(kept.Identity, kept.Text);
            if (twin != null)
                _editor.Backups.Forget(twin);

            (_editor.Current as LibraryPage)?.ShowKept(kept.Identity);
            _editor.Invalidate();
        }

        /// <summary>Calls <paramref name="blueprint" />, read from the library's <paramref name="volume" />, <paramref name="name" />, and keeps it there so.</summary>
        public void Rename(AutomationSystem blueprint, LibraryEntry volume, string name)
        {
            var wanted = NameRules.Clean(name);
            if (wanted.Length == 0)
                return;

            blueprint.Name = wanted;
            blueprint.Origin = volume.Identity;
            var kept = _editor.Shelf.Keep(blueprint);
            _editor.Sharing?.Renew(kept.Identity, kept.Text);
            (_editor.Current as LibraryPage)?.ShowKept(kept.Identity);
            _editor.Invalidate();
        }

        /// <summary>Asks before deleting <paramref name="volume" /> from the library.</summary>
        public void Unshelve(LibraryEntry volume)
        {
            _editor.Deleting(_editor.Texts.LibraryForgetQuestion, volume.State == LibraryEntryState.Read ? volume.Name : volume.File, _editor.Texts.DeleteFinal, null, delegate
            {
                _editor.Shelf.Forget(volume);
                _editor.Invalidate();
            });
        }

        /// <summary>Asks before forgetting <paramref name="backup" />, a logic system kept of a construct that lost its automation.</summary>
        public void ForgetBackup(Backup backup)
        {
            _editor.Deleting(_editor.Texts.LibraryForgetBackupQuestion, backup.Name, _editor.Texts.DeleteFinal, null, delegate
            {
                _editor.Backups.Forget(backup);
                _editor.Invalidate();
            });
        }

        /// <summary>
        /// Puts the blueprint <paramref name="volume" /> from the library on the construct as a logic system of its own, linked
        /// to it, with its first installation, answering what was wrong, or empty.
        /// </summary>
        /// <remarks>Refused where a logic system made from it is here already.</remarks>
        public string PutOn(LibraryEntry volume)
        {
            if (_editor.Program.MadeFrom(volume.Identity) != null)
                return _editor.Texts.LibraryOnAlready;

            AutomationSystem system;
            var trouble = ProgramReader.Lone(volume.Text, _editor.World.Values, out system);
            if (trouble.Length > 0)
                return trouble;

            system.Identity = Guid.NewGuid();
            system.Origin = volume.Identity;
            system.OriginChanged = volume.Changed;
            return PutOn(ProgramWriter.Linked(system), system.Identity);
        }

        /// <summary>
        /// Puts the logic system <paramref name="backed" />, as a backup keeps it, back on the construct with its own identity
        /// and its link, with its first installation, answering what was wrong with it, or empty.
        /// </summary>
        /// <remarks>Refused where a logic system of its identity is here already.</remarks>
        public string PutOn(AutomationSystem backed)
        {
            if (_editor.Shelf != null)
                _editor.Shelf.Link(backed);

            return PutOn(ProgramWriter.Linked(backed), backed.Identity);
        }

        string PutOn(string text, Guid identity)
        {
            var trouble = ProgramReader.AddBlueprint(text, _editor.Program, _editor.World.Values);
            if (trouble.Length > 0)
                return trouble;

            var made = _editor.Program.FindSystem(identity);
            _editor.Installed(made);
            _editor.Do(new MakeSystem(made, ChangeKind.PutOnSystem));
            _editor.Invalidate();
            return "";
        }

        /// <summary>
        /// Puts a copy of the logic system written as <paramref name="text" /> on the construct, as a logic system of its own with
        /// its first installation, answering it; or null, with what was wrong in <paramref name="trouble" />.
        /// </summary>
        public AutomationSystem Copy(string text, out string trouble)
        {
            var trial = new Automation();
            trouble = ProgramReader.Load(text, trial, _editor.World.Values);
            if (trouble.Length > 0)
                return null;

            if (trial.Systems.Count != 1)
            {
                trouble = "expected one system blueprint and nothing else.";
                return null;
            }

            var copy = trial.Systems[0];
            copy.Identity = Guid.NewGuid();
            copy.Changed = DateTime.UtcNow;
            copy.Name = Numbered(copy.Name);
            if (_editor.Author.Length > 0)
                copy.Namespace = _editor.Author;

            trouble = ProgramReader.AddBlueprint(ProgramWriter.Blueprint(copy), _editor.Program, _editor.World.Values);
            if (trouble.Length > 0)
                return null;

            var made = _editor.Program.FindSystem(copy.Identity);
            _editor.Installed(made);
            _editor.Do(new MakeSystem(made, ChangeKind.CopyBlueprint));
            _editor.Invalidate();
            return made;
        }

        /// <summary>Puts the blueprint of <paramref name="shared" /> on the construct as a logic system of its own, which stays as it is when its sharer changes theirs, answering what was wrong, or empty.</summary>
        public string PutOnShared(ShareLine shared)
        {
            AutomationSystem system;
            var trouble = ProgramReader.Lone(shared.Blueprint, _editor.World.Values, out system);
            if (trouble.Length > 0)
                return trouble;

            system.Identity = Guid.NewGuid();
            return PutOn(ProgramWriter.Blueprint(system), system.Identity);
        }

        /// <summary>Adds a copy of the blueprint of <paramref name="shared" /> to the player's library, with an identity of its own and its maker's name.</summary>
        public void KeepCopy(ShareLine shared)
        {
            AutomationSystem system;
            if (_editor.Shelf == null || ProgramReader.Lone(shared.Blueprint, _editor.World.Values, out system).Length > 0)
                return;

            system.Identity = Guid.NewGuid();
            var kept = _editor.Shelf.Keep(system);
            (_editor.Current as LibraryPage)?.ShowKept(kept.Identity);
            _editor.Invalidate();
        }

        /// <summary>
        /// Reads the blueprints written as text in the Custom Data of the block the editor was opened from, names them, and adds
        /// them to the library, each as the player's own copy, once the player says so; says so where there are none to add.
        /// </summary>
        public void Import()
        {
            if (_editor.Shelf == null)
                return;

            var data = _editor.OpenedFromData?.Invoke();
            var found = data == null ? new List<SharedText>() : BlueprintText.Read(data);
            var readable = new List<AutomationSystem>();
            foreach (var text in found)
            {
                AutomationSystem system;
                if (text.Blueprint == null || ProgramReader.Lone(text.Blueprint, _editor.World.Values, out system).Length > 0)
                    continue;

                if (text.Changed.HasValue)
                    system.Changed = text.Changed;
                readable.Add(system);
            }

            var texts = _editor.Texts;
            if (found.Count == 0)
            {
                _editor.Ask(texts.LibraryImportNone);
                _editor.Answer(texts.LibraryImportOk, () => { }, true);
                return;
            }

            // A blueprint the library has as new or newer is left as it is; the rest are added or update the one kept.
            // The names go in as arguments, so a name is never read as markup.
            var arguments = new Arguments();
            var wanted = new List<AutomationSystem>();
            var lines = "";
            for (var i = 0; i < readable.Count; i++)
            {
                var system = readable[i];
                var slot = "name" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                arguments.Set(slot, system.Name);
                var had = _editor.Shelf.Find(system.Identity);
                var newer = had == null || (system.Changed ?? DateTime.MinValue) > (had.Changed ?? DateTime.MinValue);
                if (newer)
                    wanted.Add(system);

                var line = had == null ? "**{name}**" : newer ? texts.LibraryImportUpdates : texts.LibraryImportHave;
                lines += "\n" + line.Replace("{name}", "{" + slot + "}");
            }

            if (readable.Count < found.Count)
                lines += "\n\n" + texts.LibraryImportUnreadable;

            if (wanted.Count == 0)
            {
                _editor.Ask(lines.TrimStart('\n'), arguments);
                _editor.Answer(texts.LibraryImportOk, () => { }, true);
                return;
            }

            _editor.Ask(texts.LibraryImportQuestion + "\n" + lines, arguments);
            _editor.Answer(texts.LibraryImportConfirm, () => Keep(wanted), true);
            _editor.Answer(texts.LibraryImportKeep, () => { });
        }

        void Keep(List<AutomationSystem> systems)
        {
            LibraryEntry kept = null;
            foreach (var system in systems)
                kept = _editor.Shelf.Import(system);

            if (kept != null)
                (_editor.Current as LibraryPage)?.ShowKept(kept.Identity);
            _editor.Invalidate();
        }

        /// <summary><paramref name="name" /> with the first number from 2 that no system here and no blueprint in the library is called.</summary>
        string Numbered(string name)
        {
            for (var number = 2; ; number++)
            {
                // The number first, so a name with braces in it is never read as a slot.
                var wanted = Template.Fill(Template.Fill(_editor.Texts.LibraryCopyName, "number",
                    number.ToString(System.Globalization.CultureInfo.InvariantCulture)), "name", name);

                var taken = false;
                for (var i = 0; i < _editor.Program.Systems.Count && !taken; i++)
                    taken = _editor.Program.Systems[i].Name == wanted;

                for (var i = 0; _editor.Shelf != null && i < _editor.Shelf.All.Count && !taken; i++)
                    taken = _editor.Shelf.All[i].Name == wanted;

                if (!taken)
                    return wanted;
            }
        }

        /// <summary>Whether the library keeps the blueprint the logic system <paramref name="system" /> is linked to.</summary>
        public bool Stored(AutomationSystem system)
        {
            return _editor.Shelf != null && system.HasOrigin && _editor.Shelf.Find(system.Origin) != null;
        }

        /// <summary>Asks before taking the logic system <paramref name="system" /> off the construct; keeping it is the answer Enter takes.</summary>
        public void AskTakeOff(AutomationSystem system)
        {
            _editor.Stamp(true);

            var arguments = new Arguments();
            arguments.Set("named", system.Name);

            if (!ChangedSince(system))
            {
                _editor.Ask($"{_editor.Texts.LibraryTakeOffQuestion}\n\n**{{named}}**\n\n{_editor.Texts.LibraryTakeOffKept}", arguments);
                _editor.Answer(_editor.Texts.LibraryTakeOffKeep, () => { }, true);
                _editor.Answer(_editor.Texts.LibraryTakeOffConfirm, () => TakeOff(system, false));
                return;
            }

            _editor.Ask($"{_editor.Texts.LibraryTakeOffQuestion}\n\n**{{named}}**\n\n{_editor.Texts.LibraryTakeOffGoes}", arguments);
            _editor.Answer(_editor.Texts.LibraryTakeOffKeep, () => { }, true);
            _editor.Answer(_editor.Texts.LibraryTakeOffConfirm, () => AskTakeOffBlueprint(system));
        }

        /// <summary>
        /// Asks whether taking off the logic system <paramref name="system" /> brings its blueprint in the library up to date;
        /// leaving the blueprint is the answer Enter takes, and backing out takes nothing off.
        /// </summary>
        void AskTakeOffBlueprint(AutomationSystem system)
        {
            var arguments = new Arguments();
            arguments.Set("named", system.Name);

            _editor.Ask(_editor.Texts.LibraryTakeOffChanged, arguments);
            _editor.Answer(_editor.Texts.LibraryTakeOffUpdate, () => TakeOff(system, true));
            _editor.Answer(_editor.Texts.LibraryTakeOffLeave, () => TakeOff(system, false), true);
        }

        /// <summary>Asks before putting the logic system <paramref name="system" /> in place of its blueprint in the library, and goes on with <paramref name="then" /> once it is there; keeping the blueprint is the answer Enter takes, and goes on with nothing.</summary>
        public void AskUpdate(AutomationSystem system, Action then = null)
        {
            _editor.Stamp(true);

            var arguments = new Arguments();
            arguments.Set("named", system.Name);

            _editor.Ask($"{_editor.Texts.LibraryUpdateQuestion}\n\n**{{named}}**\n\n{_editor.Texts.LibraryUpdateReplaced}", arguments);
            _editor.Answer(_editor.Texts.LibraryUpdateKeep, () => { }, true);
            _editor.Answer(_editor.Texts.LibraryUpdateConfirm, delegate
            {
                Shelve(system);
                then?.Invoke();
            });
        }

        /// <summary>
        /// What the Share menu offers of the logic system <paramref name="system" />: the dialog for sharing it, where the world
        /// allows sharing, and its blueprint as it is on screen to copy.
        /// </summary>
        public ShareOffer Offering(AutomationSystem system)
        {
            var texts = _editor.Texts;
            var offer = new ShareOffer
            {
                FnName = () => system.Name,
                FnText = () => ProgramWriter.Blueprint(system, Stored(system) ? system.Origin : system.Identity)
            };

            if (_editor.Shelf != null && _editor.Sharing != null && _editor.Sharing.CanShare)
            {
                offer.Shares = _editor.Act(CommandKind.Shelve, () => Share(system));
                offer.SharesText = texts.LibraryShare;
                offer.SharesAbout = texts.Codex.ShareSystemAbout;
            }

            return offer;
        }

        /// <summary>
        /// Opens the dialog for sharing the logic system <paramref name="system" />, once its blueprint in the library is as it is
        /// on screen: made first where there is none, and after asking where the one there differs.
        /// </summary>
        public void Share(AutomationSystem system)
        {
            if (Stored(system))
            {
                _editor.Stamp(true);
                var volume = _editor.Shelf.Find(system.Origin);
                if (volume.Text != ProgramWriter.Blueprint(system, volume.Identity))
                {
                    AskUpdate(system, () => OpenShare(system));
                    return;
                }
            }
            else
            {
                Shelve(system);
            }

            OpenShare(system);
        }

        /// <summary>Opens the dialog for sharing the blueprint in the library that <paramref name="system" /> is linked to.</summary>
        void OpenShare(AutomationSystem system)
        {
            var volume = _editor.Shelf.Find(system.Origin);
            _editor.Windows?.Open(new ShareDialog(_editor, volume.Name, volume.Text, _editor.Sharing.MineOf(volume.Identity)));
        }

        /// <summary>Asks before making a blueprint of the logic system <paramref name="system" /> in the library; leaving it is the answer Enter takes.</summary>
        public void AskShelve(AutomationSystem system)
        {
            var texts = _editor.Texts;
            Confirm(texts.LibraryMakeQuestion, texts.LibraryMakeKept, system.Name, texts.LibraryMakeConfirm, texts.LibraryMakeLeave, () => Shelve(system));
        }

        /// <summary>Asks before <paramref name="copy" /> makes a copy of the logic system called <paramref name="named" />; leaving it is the answer Enter takes.</summary>
        public void AskCopy(string named, Action copy)
        {
            var texts = _editor.Texts;
            Confirm(texts.LibraryCopyQuestion, texts.LibraryCopySeparate, named, texts.LibraryCopyConfirm, texts.LibraryCopyLeave, copy);
        }

        /// <summary>Asks <paramref name="question" /> of what is called <paramref name="named" />, saying <paramref name="says" />, and runs <paramref name="act" /> on <paramref name="confirm" />; <paramref name="leave" /> is the answer Enter takes.</summary>
        void Confirm(string question, string says, string named, string confirm, string leave, Action act)
        {
            _editor.Stamp(true);

            var arguments = new Arguments();
            arguments.Set("named", named);

            _editor.Ask($"{question}\n\n**{{named}}**\n\n{says}", arguments);
            _editor.Answer(leave, () => { }, true);
            _editor.Answer(confirm, act);
        }

        /// <summary>
        /// Promotes the backup whose logic system reads as <paramref name="backed" /> to a blueprint in the library, and it is a
        /// backup no longer: where the blueprint it is linked to is there, after asking whether it updates that blueprint or is
        /// kept as a new one; leaving both is the answer Enter takes.
        /// </summary>
        public void PromoteBackup(AutomationSystem backed)
        {
            if (!Stored(backed))
            {
                AskShelve(backed);
                return;
            }

            _editor.Stamp(true);

            var arguments = new Arguments();
            arguments.Set("named", backed.Name);

            _editor.Ask($"{_editor.Texts.LibraryKeepBackupQuestion}\n\n**{{named}}**\n\n{_editor.Texts.LibraryKeepBackupLinked}", arguments);
            _editor.Answer(_editor.Texts.LibraryKeepBackupLeave, () => { }, true);
            _editor.Answer(_editor.Texts.LibraryKeepBackupUpdate, () => Shelve(backed));
            _editor.Answer(_editor.Texts.LibraryKeepBackupNew, delegate
            {
                backed.Origin = Guid.Empty;
                backed.OriginChanged = null;
                Shelve(backed);
            });
        }

        /// <summary>Asks before putting the blueprint of <paramref name="system" /> in the library in its place; keeping it is the answer Enter takes.</summary>
        public void AskRenew(AutomationSystem system)
        {
            _editor.Stamp(true);

            var arguments = new Arguments();
            arguments.Set("named", system.Name);

            _editor.Ask($"{_editor.Texts.LibraryRenewQuestion}\n\n**{{named}}**\n\n{_editor.Texts.LibraryRenewReplaced}", arguments);
            _editor.Answer(_editor.Texts.LibraryRenewKeep, () => { }, true);
            _editor.Answer(_editor.Texts.LibraryRenewConfirm, () => Renew(system, ChangeKind.UpdateFromLibrary));
        }

        /// <summary>Asks before putting the blueprint of <paramref name="system" /> in the library back in its place, losing what changed since; keeping it is the answer Enter takes.</summary>
        public void AskReset(AutomationSystem system)
        {
            _editor.Stamp(true);

            var arguments = new Arguments();
            arguments.Set("named", system.Name);

            _editor.Ask($"{_editor.Texts.LibraryResetQuestion}\n\n**{{named}}**\n\n{_editor.Texts.LibraryResetReplaced}", arguments);
            _editor.Answer(_editor.Texts.LibraryRenewKeep, () => { }, true);
            _editor.Answer(_editor.Texts.LibraryResetConfirm, () => Renew(system, ChangeKind.ResetToBlueprint));
        }

        /// <summary>
        /// Puts the blueprint <paramref name="system" /> is linked to in its place, its installations keeping their blocks in
        /// every slot the blueprint still uses, offered back as <paramref name="change" />, answering what was wrong, or empty.
        /// </summary>
        /// <remarks>Its runs end, and each installation stays switched as it was.</remarks>
        public string Renew(AutomationSystem system, ChangeKind change)
        {
            var volume = Stored(system) ? _editor.Shelf.Find(system.Origin) : null;
            if (volume == null)
                return "";

            AutomationSystem copy;
            var trouble = ProgramReader.Lone(volume.Text, _editor.World.Values, out copy);
            if (trouble.Length > 0)
                return trouble;

            copy.Identity = system.Identity;
            copy.Origin = volume.Identity;
            copy.OriginChanged = volume.Changed;

            var text = ProgramWriter.Replacing(_editor.Program, _editor.World.Values, copy);
            trouble = ProgramReader.Load(text, new Automation(), _editor.World.Values);
            if (trouble.Length > 0)
                return trouble;

            _editor.Stamp(true);

            // Read in beside the rest of the program, then set aside, so that the edit is the swap of the two alone.
            var at = 0;
            while (at < _editor.Program.Systems.Count && _editor.Program.Systems[at] != system)
                at++;

            _editor.Program.RemoveSystem(system);
            trouble = ProgramReader.ReadOne(text, system.Identity, _editor.Program);
            var fresh = _editor.Program.FindSystem(system.Identity);
            if (fresh != null)
                _editor.Program.RemoveSystem(fresh);

            _editor.Program.PutBack(system, at);
            if (trouble.Length > 0 || fresh == null)
                return trouble;

            fresh.Enabled = system.Enabled;
            var instances = fresh.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                var was = system.FindInstance(instances[i].Number);
                if (was != null)
                    instances[i].Enabled = was.Enabled;
            }

            // Keeps its date the library's, so nothing reads it as changed since.
            _dates.Know(fresh);

            _editor.Do(new ReplaceSystem(system, fresh, change));
            _editor.Gone(system);
            return "";
        }

        /// <summary>Whether the logic system <paramref name="system" /> was changed after the version of its blueprint in the library it is linked to.</summary>
        public bool ChangedSince(AutomationSystem system)
        {
            return Stored(system) && system.Changed.HasValue
                && (!system.OriginChanged.HasValue || system.Changed.Value > system.OriginChanged.Value);
        }

        /// <summary>
        /// Takes the logic system <paramref name="system" />, whose blueprint is in the library, off the construct with its
        /// installations, bringing the blueprint up to date first where <paramref name="update" /> says so.
        /// </summary>
        /// <remarks>Only for a logic system whose blueprint is in the library; one without is deleted with <see cref="SprocketEditor.DeleteSystem" />.</remarks>
        public void TakeOff(AutomationSystem system, bool update)
        {
            if (!Stored(system))
                return;

            _editor.Stamp(true);
            if (update && !_editor.Shelf.Holds(system))
                _editor.Shelf.Keep(system);

            _editor.Do(new TakeSystemOut(system, ChangeKind.TakeOffSystem));
            _editor.Gone(system);
        }
    }
}
