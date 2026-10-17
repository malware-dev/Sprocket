using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Library;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    /// <summary>The choice between two versions of a logic system that met when their constructs joined.</summary>
    public sealed class Versions
    {
        readonly SprocketEditor _editor;
        readonly Dates _dates;

        /// <summary>The choices offered in <paramref name="editor" />, dating what is chosen through <paramref name="dates" />.</summary>
        public Versions(SprocketEditor editor, Dates dates)
        {
            _editor = editor;
            _dates = dates;
        }

        /// <summary>The other version of <paramref name="system" /> waiting for a choice, or null.</summary>
        public AutomationSystem Rival(AutomationSystem system)
        {
            var conflict = Conflict(system);
            if (conflict == null)
                return null;

            return _editor.Program.FindSystem(conflict.System == system.Identity ? conflict.Waiting : conflict.System);
        }

        /// <summary>Asks which version of <paramref name="system" /> to keep.</summary>
        public void AskChoose(AutomationSystem system)
        {
            var conflict = Conflict(system);
            if (conflict == null)
                return;

            var here = _editor.Program.FindSystem(conflict.System);
            var waiting = _editor.Program.FindSystem(conflict.Waiting);
            var arguments = new Arguments();
            arguments.Set("name", here.Name);
            _editor.Ask($"{_editor.Texts.ConflictQuestion}\n\n{_editor.Texts.ConflictExplains}", arguments);
            _editor.Answer(Kept(here), () => Choose(here, waiting, conflict, here));
            _editor.Answer(Kept(waiting), () => Choose(here, waiting, conflict, waiting));
        }

        ConflictMemory Conflict(AutomationSystem system)
        {
            foreach (var conflict in _editor.Program.Memory.Conflicts)
            {
                if (conflict.System != system.Identity && conflict.Waiting != system.Identity)
                    continue;

                if (_editor.Program.FindSystem(conflict.System) != null && _editor.Program.FindSystem(conflict.Waiting) != null)
                    return conflict;
            }

            return null;
        }

        string Kept(AutomationSystem version)
        {
            return version.Changed.HasValue
                ? Template.Fill(_editor.Texts.ConflictKeep, "date", LibraryPage.Time(version.Changed.Value))
                : _editor.Texts.ConflictKeepUndated;
        }

        // Under the identity of the one here, which the construct's buttons name.
        void Choose(AutomationSystem here, AutomationSystem waiting, ConflictMemory conflict, AutomationSystem keep)
        {
            var scratch = new Automation();
            if (Reader.Load(Scribe.Save(_editor.Program, _editor.World.Values), scratch, _editor.World.Values).Length > 0)
                return;

            var kept = scratch.FindSystem(keep.Identity);
            var dropped = scratch.FindSystem(keep == here ? waiting.Identity : here.Identity);
            foreach (var instance in dropped.Instances)
                Meeting.Bring(instance, kept, name => name);

            scratch.RemoveSystem(dropped);
            kept.Identity = here.Identity;
            var text = Scribe.Save(scratch, _editor.World.Values);

            _editor.Stamp(true);
            var at = 0;
            while (at < _editor.Program.Systems.Count && _editor.Program.Systems[at] != here)
                at++;

            _editor.Program.RemoveSystem(here);
            var trouble = Reader.ReadOne(text, here.Identity, _editor.Program);
            var chosen = _editor.Program.FindSystem(here.Identity);
            if (chosen != null)
                _editor.Program.RemoveSystem(chosen);

            _editor.Program.PutBack(here, at);
            if (trouble.Length > 0 || chosen == null)
                return;

            _dates.Know(chosen);
            _editor.Do(new ChooseVersion(here, waiting, chosen, conflict));
            _editor.Gone(here);
            _editor.Gone(waiting);
            _editor.Invalidate();
        }
    }
}
