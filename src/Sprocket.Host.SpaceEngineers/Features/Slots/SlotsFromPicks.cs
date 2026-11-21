using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Features.Slots.Edits;
using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Format.Repairs;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;
using System.Collections.Generic;
using System.Globalization;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Slots
{
    /// <summary>
    /// How picked blocks name the slots of a blueprint: the slots they fill already, the slot the picks replace where it is
    /// theirs to change, or one new slot holding them all, kept from the pick until the edit made next carries it.
    /// </summary>
    public sealed class SlotsFromPicks
    {
        readonly SprocketEditor _editor;
        readonly List<Reslot> _picked = new List<Reslot>();

        /// <summary>The picks made in <paramref name="editor" />.</summary>
        public SlotsFromPicks(SprocketEditor editor)
        {
            _editor = editor;
        }

        /// <summary>Keeps the new slot <paramref name="part" /> of <paramref name="system" />, which <paramref name="shown" /> fills with nothing, for the edit made next to carry.</summary>
        public void Make(AutomationSystem system, SystemInstance shown, Part part)
        {
            var reslot = new Reslot(system, shown);
            reslot.Make(part, new List<string>());
            Keep(reslot);
        }

        /// <summary><paramref name="edit" />, together with the slots the picks before it made in <paramref name="system" />, where they made any.</summary>
        public Edit<T> Slotted<T>(AutomationSystem system, Edit<T> edit)
        {
            var picked = Unpicked(system);
            return picked.Count == 0 ? edit : new Reslotted<T>(edit, picked);
        }

        /// <summary>
        /// The changes that would give <paramref name="system" /> as few slots as its steps and triggers can name, as the
        /// installation shown fills them, made by nothing yet.
        /// </summary>
        /// <remarks>Works the slots out from every list the system names, so it is for a person asking, never for every edit.</remarks>
        public Reslot Arranged(AutomationSystem system)
        {
            var shown = _editor.StandIn(system);
            return shown == null ? null : new SlotFinder(system, shown,
                name =>
                {
                    GridEntity entity = _editor.Runner.Bindings.Entity(name);
                    return entity != null ? entity.Kind.Lineage : new string[0];
                },
                number => Template.Fill(_editor.Texts.SlotNumbered, "number", number.ToString(CultureInfo.InvariantCulture))).Settle();
        }

        /// <summary>
        /// Makes <paramref name="system" /> a system blueprint, with a first installation answering a slot for each name its
        /// steps and triggers use, called by that name, with what the name stood for; that installation, or the first of a
        /// blueprint already.
        /// </summary>
        public SystemInstance Blueprinted(AutomationSystem system)
        {
            if (system.IsBlueprint)
                return system.First;

            var name = SystemSummary.NewInstanceName(_editor.Texts, system);
            var self = system.Uses[0];
            var first = system.AddInstance();
            first.Name = name;
            system.IsBlueprint = true;

            // The system becomes its first installation, which holds what its states held.
            _editor.Runner.States.Move(system, self, first);

            foreach (var trigger in system.Triggers)
            {
                var needed = trigger as SignalTrigger;
                var declared = needed == null ? null : _editor.Program.FindSignalRef(needed.Part);
                if (declared == null || system.FindPart(needed.Part) != null)
                    continue;

                var part = system.PartFor(needed.Part);
                part.IsSignal = true;

                var at = new ToolbarSlot[declared.Blocks.Count];
                for (var n = 0; n < at.Length; n++)
                    at[n] = declared.At;

                first.Fill(part.Id, declared.Blocks, at);
            }

            var named = new List<string>();
            system.Acted(named);
            foreach (var variable in named)
            {
                if (system.FindPart(variable) != null)
                    continue;

                GridEntity entity = _editor.Runner.Bindings.Entity(variable);
                var part = system.PartFor(variable);
                part.Expects = entity == null ? "" : Part.Nearest(new[] { entity.Kind.Lineage });
                first.Fill(part.Id, new[] { variable });
            }

            _editor.Program.Edited();
            _editor.Program.Rewire();
            return first;
        }

        /// <summary>
        /// Hands <paramref name="named" /> the names <paramref name="system" /> uses for what <paramref name="answer" /> picked, in
        /// place of the list <paramref name="replacing" />, or null for a new one: the slots it chose, a system's variables, or in a
        /// blueprint the slots <see cref="Forecast" /> says the picks name, of what starts something where <paramref name="watched" />.
        /// </summary>
        /// <remarks>
        /// A slot changed in place is asked about first where something else uses it, and where its type narrows; picks making a
        /// slot make one called what the answer names it. What changes is kept for the edit made next to carry.
        /// </remarks>
        public void Name(AutomationSystem system, PickAnswer answer, bool watched, IList<string> replacing, Action<List<string>> named)
        {
            if (answer.Slots.Count > 0)
            {
                named(new List<string>(answer.Slots));
                return;
            }

            var declared = new List<string>();
            var blocks = new List<GridEntity>();
            foreach (var id in answer.Targets)
            {
                GridEntity entity = _editor.World.FindEntity(id);
                if (entity == null)
                    continue;

                declared.Add(_editor.Program.Declare(_editor.World, entity).Name);
                blocks.Add(entity);
            }

            SystemInstance shown = system.IsBlueprint ? _editor.StandIn(system) : null;
            if (shown == null)
            {
                named(declared);
                return;
            }

            var forecast = Forecast(system, answer.Targets, answer.Key, answer.Facet, watched, null, false, replacing, "");
            if (forecast.Matched != null)
            {
                named(forecast.Matched);
                return;
            }

            var type = forecast.Type;
            var numbers = type == null ? null : Numbers(blocks, type, answer.Facet);
            Func<List<string>> make = () => With(system, () => Made(system, shown,
                new Part(Free(system, answer.SlotName, false, type)) { Expects = type == null ? Nearest(blocks) : "", Holds = type == null ? "" : type.Id },
                declared, numbers, replacing, ProgramRepairs.Carried(_editor.Runner.Bindings, system, shown, replacing, type)));
            var own = forecast.Changes;
            if (own == null)
            {
                named(make());
                return;
            }

            var holds = type != null && own.Holds != type.Id ? type.Id : null;
            var expects = type == null ? Nearest(blocks) : holds != null ? "" : own.Expects;
            Action change = () => Narrowing(own, expects,
                () => named(With(system, () => Changed(system, shown, own, declared, expects, numbers, holds))));
            if (forecast.Shared)
                Ask(own.Id, change, () => named(make()));
            else
                change();
        }

        /// <summary>
        /// Hands <paramref name="named" /> the signal slot a trigger in <paramref name="system" /> takes from the buttons
        /// <paramref name="blocks" /> picked, by entity id, at the toolbar slot <paramref name="at" />, in place of the signal slot
        /// <paramref name="keeps" /> of the trigger changed, empty for a new one, as <see cref="Forecast" /> has it; a new one is
        /// called <paramref name="called" /> where that is free.
        /// </summary>
        /// <remarks>The trigger's own slot is changed once asked where another trigger uses it too.</remarks>
        public void NameSignal(AutomationSystem system, IList<long> blocks, ToolbarSlot at, string keeps, string called, Action<string> named)
        {
            var forecast = Forecast(system, blocks, default(MemberKey), "", true, at, true, null, keeps);
            if (forecast.Matched != null)
                named(forecast.Matched[0]);
            else if (forecast.Changes == null)
                named(NewSlotName(system, called, true));
            else if (forecast.Shared)
                Ask(forecast.Changes.Id, () => named(keeps), () => named(NewSlotName(system, called, true)));
            else
                named(keeps);
        }

        /// <summary>
        /// Which slots of <paramref name="system" /> the blocks <paramref name="blocks" />, by entity id, name when picked for
        /// <paramref name="chosen" /> on the facet called <paramref name="facet" />, in place of the list
        /// <paramref name="replacing" /> or null for a new one: the slots they fill already, the slot the line names changed in
        /// place where it is the one slot of their kind, or a new one. Of what starts something where <paramref name="watched" />;
        /// of what a toolbar sends at <paramref name="at" /> where <paramref name="signal" />, in place of the trigger's signal slot
        /// <paramref name="keeps" />.
        /// </summary>
        /// <remarks>The one prediction the picker's heading shows and the edit made from the pick follows. Walks every slot's blocks, so it is asked as the picks change, never on a tick.</remarks>
        public SlotForecast Forecast(AutomationSystem system, IList<long> blocks, MemberKey chosen, string facet, bool watched, ToolbarSlot at,
            bool signal, IList<string> replacing, string keeps)
        {
            var type = watched || signal || facet.Length == 0 || chosen.IsEmpty ? null : Vocabulary.TypeOf(_editor.World, blocks, chosen);
            SystemInstance shown = system != null && system.IsBlueprint ? _editor.StandIn(system) : null;
            if (shown == null)
                return new SlotForecast(null, null, false, signal, type);

            var matched = blocks.Count == 0 ? null : With(system, () =>
                signal ? MatchSignal(system, shown, blocks, at)
                : type != null ? Holding(system, shown, type, blocks, facet)
                : Match(system, shown, blocks));

            var kept = signal ? Signal(system, keeps) : Own(system, replacing, type == null ? "" : type.Id);
            Repair mending;
            var holding = kept == null && !signal ? ProgramRepairs.Reslots(_editor.Runner.Bindings, system, shown, replacing, type, out mending) : null;
            var own = kept ?? holding;
            var changes = matched == null ? own : null;
            var shared = changes != null && holding == null && Shared(system, changes.Id, signal ? new[] { changes.Id } : replacing);
            return new SlotForecast(matched, changes, shared, signal, type);
        }

        /// <summary>
        /// Carries out <paramref name="change" /> where <paramref name="part" /> taking <paramref name="expects" /> widens it or
        /// keeps it, and once asked where it narrows it, which may leave installations elsewhere with blocks that no longer fit.
        /// </summary>
        void Narrowing(Part part, string expects, Action change)
        {
            if (!TypeTree.Narrows(_editor.World, part, expects))
            {
                change();
                return;
            }

            var texts = _editor.Texts;
            _editor.Ask(texts.SlotNarrowQuestion);
            _editor.Answer(texts.SlotNarrowYes, change);
            _editor.Answer(texts.SlotNarrowCancel, delegate { }, true);
        }

        /// <summary>
        /// Asks whether the picks change the slot <paramref name="slot" /> for everything that uses it, which carries out
        /// <paramref name="everyone" />, or give what is being changed a slot of its own, which carries out <paramref name="alone" />
        /// and is what Enter takes.
        /// </summary>
        void Ask(string slot, Action everyone, Action alone)
        {
            var texts = _editor.Texts;
            var arguments = new Arguments();
            arguments.Set("slot", slot);
            _editor.Ask(texts.SlotSharedQuestion, arguments);
            _editor.Answer(texts.SlotSharedEveryone, everyone);
            _editor.Answer(texts.SlotSharedOwn, alone, true);
        }

        /// <summary>
        /// The slot the list <paramref name="replacing" /> names where it names that one slot alone and the slot holds
        /// <paramref name="holds" />, empty for a block slot; null where the list names none, several, a signal slot or a slot of
        /// another kind.
        /// </summary>
        static Part Own(AutomationSystem system, IList<string> replacing, string holds)
        {
            if (replacing == null || replacing.Count == 0)
                return null;

            var part = system.FindPart(replacing[0]);
            if (part == null || part.IsSignal || part.Holds != holds)
                return null;

            foreach (var name in replacing)
            {
                if (name != part.Id)
                    return null;
            }

            return part;
        }

        /// <summary>The signal slot of <paramref name="system" /> called <paramref name="keeps" />, or null where it has none by that name.</summary>
        static Part Signal(AutomationSystem system, string keeps)
        {
            var part = string.IsNullOrEmpty(keeps) ? null : system.FindPart(keeps);
            return part != null && part.IsSignal ? part : null;
        }

        /// <summary>Whether a step or a trigger of <paramref name="system" /> besides the list <paramref name="replacing" /> names the slot <paramref name="slot" />.</summary>
        static bool Shared(AutomationSystem system, string slot, IList<string> replacing)
        {
            return system.Mentions(slot) > AutomationSystem.Count(replacing, slot);
        }

        /// <summary>
        /// Fills <paramref name="part" /> in <paramref name="shown" /> with <paramref name="declared" /> on the facets
        /// <paramref name="numbers" />, expecting <paramref name="expects" /> and holding <paramref name="holds" /> where that is not
        /// null, for the edit made next to carry; its name.
        /// </summary>
        List<string> Changed(AutomationSystem system, SystemInstance shown, Part part, List<string> declared, string expects, List<int> numbers,
            string holds)
        {
            var reslot = new Reslot(system, shown);
            reslot.Refill(part, declared, expects, numbers, holds);
            reslot.Apply();
            _picked.Add(reslot);
            return new List<string> { part.Id };
        }

        /// <summary>The signal slot that <paramref name="shown" /> answers with the toolbar slot <paramref name="at" /> on exactly the blocks <paramref name="picked" />, by entity id; null where none does.</summary>
        List<string> MatchSignal(AutomationSystem system, SystemInstance shown, IList<long> picked, ToolbarSlot at)
        {
            if (at == null)
                return null;

            var wanted = new List<long>(picked);
            foreach (var part in system.Parts)
            {
                if (!part.IsSignal)
                    continue;

                var filled = Ids(shown.FilledWith(part.Id));
                var positions = shown.Positions(part.Id);
                if (positions == null || !ListComparison.SameAnyOrder(filled, wanted))
                    continue;

                var same = true;
                foreach (var position in positions)
                    same &= ToolbarSlot.Same(position, at);

                if (same)
                    return new List<string> { part.Id };
            }

            return null;
        }

        /// <summary>The slot of <paramref name="type" /> that <paramref name="shown" /> fills with the blocks <paramref name="picked" />, by entity id, in order, each on the facet called <paramref name="facet" />; null where none does.</summary>
        List<string> Holding(AutomationSystem system, SystemInstance shown, HostType type, IList<long> picked, string facet)
        {
            var found = Entities(picked);
            var ids = new List<long>();
            foreach (var entity in found)
                ids.Add(entity.Id);

            return Holding(system, shown, type, ids, Numbers(found, type, facet));
        }

        /// <summary>The blocks <paramref name="ids" /> the world finds, in order.</summary>
        List<GridEntity> Entities(IList<long> ids)
        {
            var found = new List<GridEntity>();
            foreach (var id in ids)
            {
                GridEntity entity = _editor.World.FindEntity(id);
                if (entity != null)
                    found.Add(entity);
            }

            return found;
        }

        /// <summary>The number of the facet called <paramref name="facet" /> each of <paramref name="blocks" /> has of <paramref name="type" />.</summary>
        static List<int> Numbers(List<GridEntity> blocks, HostType type, string facet)
        {
            var numbers = new List<int>();
            foreach (var entity in blocks)
                numbers.Add(Numbered(entity.Kind.Facets(type), facet));

            return numbers;
        }

        /// <summary>
        /// What a new slot of <paramref name="system" /> is called: <paramref name="called" /> where it is free, and otherwise the
        /// numbered slot name free next, the picks not yet made counted.
        /// </summary>
        public string NewSlotName(AutomationSystem system, string called = "", bool signal = false, HostType holds = null, string avoiding = "")
        {
            return With(system, () => Free(system, called, signal, holds, avoiding));
        }

        /// <summary>Whether a new slot of <paramref name="system" /> can be called <paramref name="name" />: no slot, state or type has it in any case, the picks not yet made included.</summary>
        public bool Frees(AutomationSystem system, string name)
        {
            var clean = NameRules.Clean(name);
            return clean.Length > 0 && With(system, () => system.FindPartInAnyCase(clean) == null);
        }

        /// <summary><paramref name="called" /> where a new slot of <paramref name="system" /> can take it, and otherwise the numbered slot name free next.</summary>
        string Free(AutomationSystem system, string called, bool signal, HostType holds, string avoiding = "")
        {
            var clean = NameRules.Clean(called ?? "");
            if (clean.Length > 0 && system.FindPartInAnyCase(clean) == null)
                return clean;

            return NumberedName.Free(SlotNames.Numbered(_editor.Texts, signal, holds), system.SlotsAlike(signal, holds == null ? "" : holds.Id),
                name => name == avoiding || system.FindPartInAnyCase(name) != null);
        }

        /// <summary>The slots <paramref name="shown" /> fills exactly with the blocks <paramref name="picked" />, by entity id, or null.</summary>
        List<string> Match(AutomationSystem system, SystemInstance shown, IList<long> picked)
        {
            var wanted = new List<long>();
            foreach (var id in picked)
            {
                if (!wanted.Contains(id))
                    wanted.Add(id);
            }

            // Each block slot the picks hold all of, largest first, so the fewest are taken.
            var within = new List<Part>();
            var fills = new List<List<long>>();
            foreach (var part in system.Parts)
            {
                if (part.IsSignal || part.IsSetting || part.Holds.Length != 0)
                    continue;

                var filled = Ids(shown.FilledWith(part.Id));
                if (filled.Count == 0 || !Within(filled, wanted))
                    continue;

                if (filled.Count == wanted.Count)
                    return new List<string> { part.Id };

                var at = 0;
                while (at < fills.Count && fills[at].Count >= filled.Count)
                    at++;

                within.Insert(at, part);
                fills.Insert(at, filled);
            }

            var names = new List<string>();
            var covered = new List<long>();
            for (var i = 0; i < within.Count && covered.Count < wanted.Count; i++)
            {
                if (Within(fills[i], covered))
                    continue;

                names.Add(within[i].Id);
                foreach (var id in fills[i])
                {
                    if (!covered.Contains(id))
                        covered.Add(id);
                }
            }

            return covered.Count == wanted.Count && names.Count > 0 ? names : null;
        }

        /// <summary>The slot of <paramref name="type" /> that <paramref name="shown" /> fills with the blocks <paramref name="ids" />, in order, each on its facet of <paramref name="numbers" />; null where none does.</summary>
        List<string> Holding(AutomationSystem system, SystemInstance shown, HostType type, List<long> ids, List<int> numbers)
        {
            foreach (var part in system.Parts)
            {
                if (part.Holds == type.Id && ListComparison.Same(Ids(shown.FilledWith(part.Id)), ids) && ListComparison.Same(shown.Facets(part.Id), numbers))
                    return new List<string> { part.Id };
            }

            return null;
        }

        /// <summary>Makes <paramref name="part" />, filled in <paramref name="shown" /> with <paramref name="declared" /> on the facets <paramref name="numbers" /> where it holds a type, for the edit made next to carry; its name.</summary>
        List<string> Made(AutomationSystem system, SystemInstance shown, Part part, List<string> declared, List<int> numbers,
            IList<string> replacing, List<IList<string>> carried)
        {
            var reslot = new Reslot(system, shown);
            reslot.Make(part, declared, numbers);

            // A repair's other uses of the slot the pick stands in for go onto the new slot with it.
            var old = replacing == null || replacing.Count == 0 ? null : replacing[0];
            foreach (var names in carried)
            {
                var now = new List<string>(names);
                for (var i = 0; i < now.Count; i++)
                {
                    if (now[i] == old)
                        now[i] = part.Id;
                }

                reslot.Rename(names, now);
            }

            reslot.Apply();
            _picked.Add(reslot);
            return new List<string> { part.Id };
        }

        /// <summary>What <paramref name="work" /> answers of <paramref name="system" /> with the slots the picks not yet made would make in it, made for it and taken back after.</summary>
        T With<T>(AutomationSystem system, Func<T> work)
        {
            if (_picked.Count > 0 && _picked[0].System != system)
                Unpicked(null);

            foreach (var pick in _picked)
                pick.Apply();

            // A slot the work makes is made as it is kept, and taken back with the rest.
            var answer = work();

            for (var i = _picked.Count - 1; i >= 0; i--)
                _picked[i].Undo();

            return answer;
        }

        /// <summary>Keeps <paramref name="reslot" /> for the edit made next to carry, after any kept for another system are let go.</summary>
        void Keep(Reslot reslot)
        {
            if (_picked.Count > 0 && _picked[0].System != reslot.System)
                Unpicked(null);

            _picked.Add(reslot);
        }

        /// <summary>The entity ids of the blocks <paramref name="names" /> stand for as the construct binds them, each once.</summary>
        List<long> Ids(IList<string> names)
        {
            var ids = new List<long>();
            if (names == null)
                return ids;

            foreach (var name in names)
            {
                GridEntity entity = _editor.Runner.Bindings.Entity(name);
                if (entity != null && !ids.Contains(entity.Id))
                    ids.Add(entity.Id);
            }

            return ids;
        }

        /// <summary>The number of the facet among <paramref name="facets" /> called <paramref name="name" />, or of the first where none is.</summary>
        public static int Numbered(IList<HostTypeFacet> facets, string name)
        {
            foreach (var facet in facets)
            {
                if (facet.Name == name)
                    return facet.Number;
            }

            return 1;
        }

        /// <summary>
        /// The name of the facet the slot among <paramref name="names" /> holding a type the host provides holds of the first
        /// block it holds, in the stand-in of <paramref name="system" />; empty where none holds one.
        /// </summary>
        public string Facet(AutomationSystem system, IList<string> names)
        {
            return Facet(system, _editor.StandIn(system), names);
        }

        /// <summary>The name of the facet the slot among <paramref name="names" /> holding a type the host provides holds of the first block it holds, as the installation <paramref name="shown" /> fills the slots; empty where none holds one.</summary>
        public string Facet(AutomationSystem system, SystemInstance shown, IList<string> names)
        {
            if (shown == null)
                return "";

            foreach (var name in names)
            {
                var part = system.FindPart(name);
                var blocks = shown.FilledWith(name);
                var numbers = shown.Facets(name);
                var type = part == null ? null : HostTypes.Named(part.Holds);
                GridEntity entity = type == null || blocks == null || blocks.Count == 0 || numbers == null || numbers.Count == 0
                    ? null
                    : _editor.Runner.Bindings.Entity(blocks[0]);
                var facet = entity == null ? null : type.Find(entity.Kind, numbers[0]);
                if (facet != null)
                    return facet.Name;
            }

            return "";
        }

        /// <summary>Whether the picks since the last edit change any slot of <paramref name="system" />.</summary>
        public bool Reslots(AutomationSystem system)
        {
            foreach (var pick in _picked)
            {
                if (pick.System == system && !pick.IsEmpty)
                    return true;
            }

            return false;
        }

        /// <summary>The changes the picks since the last edit worked out for <paramref name="system" />, in order, forgetting them; none where they were for another.</summary>
        List<Reslot> Unpicked(AutomationSystem system)
        {
            var picked = new List<Reslot>();
            if (_picked.Count > 0 && _picked[0].System == system)
                picked.AddRange(_picked);

            _picked.Clear();
            return picked;
        }

        /// <summary>The nearest type every one of <paramref name="blocks" /> is, or empty for any block.</summary>
        static string Nearest(List<GridEntity> blocks)
        {
            var lineages = new List<IList<string>>();
            foreach (var entity in blocks)
                lineages.Add(entity.Kind.Lineage);

            return Part.Nearest(lineages);
        }

        /// <summary>Whether every one of <paramref name="part" /> is in <paramref name="whole" />.</summary>
        static bool Within(List<long> part, List<long> whole)
        {
            foreach (var id in part)
            {
                if (!whole.Contains(id))
                    return false;
            }

            return true;
        }

    }
}
