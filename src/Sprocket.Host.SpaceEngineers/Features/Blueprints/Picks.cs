using System.Collections.Generic;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>How picked blocks become the slots of a blueprint, kept from the pick until the edit made next carries them.</summary>
    public sealed class Picks
    {
        readonly SprocketEditor _editor;
        /// <summary>What the picks in a blueprint since the last edit do to its slots beyond the names they gave, in order, for the edit made next to carry.</summary>
        readonly List<Reslot> _picked = new List<Reslot>();
        /// <summary>The names those picks gave, which count as named until their edit is made.</summary>
        readonly List<List<string>> _pickedNames = new List<List<string>>();

        /// <summary>The picks made in <paramref name="editor" />.</summary>
        public Picks(SprocketEditor editor)
        {
            _editor = editor;
        }

        /// <summary>Keeps the new slot <paramref name="part" /> of <paramref name="system" />, which <paramref name="shown" /> fills with nothing, for the edit made next to carry.</summary>
        public void Make(AutomationSystem system, SystemInstance shown, Part part)
        {
            var reslot = new Reslot(system, shown);
            reslot.Make(part, new List<string>());
            _picked.Add(reslot);
            _pickedNames.Add(new List<string> { part.Id });
        }

        /// <summary>
        /// <paramref name="edit" />, together with what it does to the slots of <paramref name="system" /> where that is a
        /// blueprint: what the pick before it worked out, or what the lists come to once it is made.
        /// </summary>
        public Edit<T> Slotted<T>(AutomationSystem system, Edit<T> edit)
        {
            var picked = Unpicked(system);
            var shown = system != null && system.IsBlueprint ? _editor.StandIn(system) : null;
            if (shown == null)
                return edit;

            return new Reslotted<T>(edit, picked, () => Slots(system, shown, null).Settle());
        }

        /// <summary>
        /// Makes <paramref name="system" /> a system blueprint, with a first instance answering its slots with what it named;
        /// that instance, or the first of a blueprint already.
        /// </summary>
        /// <remarks>
        /// A system names the program's variables, and a blueprint its own slots: so each trigger it is started by becomes a
        /// signal slot, called what the block is called now, and the blocks its steps and triggers name become slots by the
        /// sets they name, as <see cref="Slotting" /> works them out. The first instance answers them with those variables.
        /// </remarks>
        public SystemInstance Blueprinted(AutomationSystem system)
        {
            if (system.IsBlueprint)
                return system.First;

            var name = SystemSummary.NewInstanceName(_editor.Texts, system);
            var first = system.AddInstance();
            first.Name = name;
            system.IsBlueprint = true;

            var slots = new Dictionary<string, string>();
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var needed = triggers[i] as SignalTrigger;
                var declared = needed == null ? null : _editor.Program.FindSignalRef(needed.Part);
                if (declared == null || slots.ContainsKey(needed.Part))
                    continue;

                GridEntity panel = _editor.Runner.Bindings.Entity(declared.Name);
                var slot = system.Free(panel == null ? declared.Name : Called(panel));
                var part = system.PartFor(slot);
                part.IsSignal = true;

                var at = new ToolbarSlot[declared.Blocks.Count];
                for (var n = 0; n < at.Length; n++)
                    at[n] = declared.At;

                first.Fill(slot, declared.Blocks, at);

                slots[needed.Part] = slot;
            }

            for (var i = 0; i < triggers.Count; i++)
            {
                var moment = triggers[i] as MomentTrigger;
                if (moment != null)
                    Slotted(moment.Sources, slots);
            }

            // The blocks the steps and triggers name become slots as any picks do, the smallest sets first.
            Slots(system, first, null).Settle().Apply();

            _editor.Program.Edited();
            _editor.Program.Rewire();
            return first;
        }

        /// <summary>Names the signal slot for each variable in <paramref name="names" /> that <paramref name="slots" /> has one for.</summary>
        static void Slotted(IList<string> names, Dictionary<string, string> slots)
        {
            for (var i = 0; i < names.Count; i++)
            {
                string slot;
                if (slots.TryGetValue(names[i], out slot))
                    names[i] = slot;
            }
        }

        /// <summary>
        /// The names <paramref name="system" /> uses for these blocks: a system's variables, each block declared where the
        /// construct has not named it yet, or the slots a blueprint's stand-in fills with them.
        /// </summary>
        /// <remarks>
        /// Where picking a block turns into naming one. What a blueprint's sentence stores is a slot, and which block fills
        /// it belongs to each instance - so the same sentence acts on the port airlock's door in one and the starboard's in
        /// another.
        /// </remarks>
        public List<string> Named(AutomationSystem system, IList<long> blocks)
        {
            return Named(system, blocks, null);
        }

        /// <summary>The same, for blocks picked in place of the names <paramref name="replacing" />, the list itself, where they replace one.</summary>
        /// <remarks>
        /// In a blueprint the slots are worked out by <see cref="Slotting" />, and what that changes beyond the names given
        /// back is kept for the edit made next, which carries it so taking the edit back takes it back too.
        /// </remarks>
        public List<string> Named(AutomationSystem system, IList<long> blocks, IList<string> replacing)
        {
            var declared = new List<string>();
            var met = new Dictionary<string, GridEntity>();
            for (var i = 0; i < blocks.Count; i++)
            {
                GridEntity entity = _editor.World.FindEntity(blocks[i]);
                if (entity == null)
                    continue;

                var name = _editor.Program.Declare(_editor.World, entity).Name;
                declared.Add(name);
                met[name] = entity;
            }

            SystemInstance shown = system.IsBlueprint ? _editor.StandIn(system) : null;
            if (shown == null)
                return declared;

            // Worked out as though the picks before it in this command were made, and undone again, since their edits come later.
            if (_picked.Count > 0 && _picked[0].System != system)
                Unpicked(null);

            foreach (var pick in _picked)
                pick.Apply();

            Reslot reslot;
            var named = Slots(system, shown, met).Pick(declared, replacing, _pickedNames, out reslot);

            for (var i = _picked.Count - 1; i >= 0; i--)
                _picked[i].Undo();

            _picked.Add(reslot);
            _pickedNames.Add(new List<string>(named));
            return named;
        }

        /// <summary>
        /// The names <paramref name="system" /> uses for the facet called <paramref name="facet" /> of each of
        /// <paramref name="blocks" />, of the type <paramref name="type" />, picked in place of the names
        /// <paramref name="replacing" /> where they replace a list: in a blueprint, a slot of that type of its own, filled in
        /// its stand-in with those blocks and the number each has that facet by.
        /// </summary>
        /// <remarks>
        /// Outside a blueprint there is no installation to keep which facet, so the blocks are named as any picks are. The slot
        /// and every other change to the slots are kept for the edit made next, as <see cref="Named(AutomationSystem, IList{long}, IList{string})" /> keeps them.
        /// </remarks>
        public List<string> Named(AutomationSystem system, HostType type, string facet, IList<long> blocks, IList<string> replacing)
        {
            SystemInstance shown = system.IsBlueprint ? _editor.StandIn(system) : null;
            if (shown == null)
                return Named(system, blocks, replacing);

            var declared = new List<string>();
            var numbers = new List<int>();
            var met = new Dictionary<string, GridEntity>();
            foreach (var block in blocks)
            {
                GridEntity entity = _editor.World.FindEntity(block);
                if (entity == null)
                    continue;

                var name = _editor.Program.Declare(_editor.World, entity).Name;
                declared.Add(name);
                numbers.Add(Numbered(entity.Kind.Facets(type), facet));
                met[name] = entity;
            }

            if (_picked.Count > 0 && _picked[0].System != system)
                Unpicked(null);

            foreach (var pick in _picked)
                pick.Apply();

            Reslot reslot;
            var named = Slots(system, shown, met).Hold(type.Id, declared, numbers, replacing, _pickedNames, out reslot);

            for (var i = _picked.Count - 1; i >= 0; i--)
                _picked[i].Undo();

            _picked.Add(reslot);
            _pickedNames.Add(new List<string>(named));
            return named;
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
        /// <remarks>What a step's picker opens on, so changing a step keeps the screen it acts on.</remarks>
        public string Facet(AutomationSystem system, IList<string> names)
        {
            return Facet(system, _editor.StandIn(system), names);
        }

        /// <summary>The same, as the installation <paramref name="shown" /> fills the slots.</summary>
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
            _pickedNames.Clear();
            return picked;
        }

        /// <summary>Works out the slots of <paramref name="system" /> as <paramref name="shown" /> fills them, knowing the blocks just picked as <paramref name="met" />.</summary>
        Slotting Slots(AutomationSystem system, SystemInstance shown, Dictionary<string, GridEntity> met)
        {
            return new Slotting(system, shown,
                name =>
                {
                    GridEntity entity = Known(name, met);
                    return entity != null ? Called(entity) : _editor.Program.Called(name, _editor.World.Values);
                },
                name =>
                {
                    GridEntity entity = Known(name, met);
                    return entity != null ? entity.Kind.Lineage : new string[0];
                },
                number => Template.Fill(_editor.Texts.SlotNumbered, "number", number.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        /// <summary>The block the variable <paramref name="name" /> holds: one just picked, or one the construct has bound.</summary>
        GridEntity Known(string name, Dictionary<string, GridEntity> met)
        {
            GridEntity entity;
            if (met != null && met.TryGetValue(name, out entity))
                return entity;

            return _editor.Runner.Bindings.Entity(name);
        }

        /// <summary>What a slot taken for <paramref name="entity" /> is called at first: the block's own name, or its kind where it has none.</summary>
        public static string Called(GridEntity entity)
        {
            var name = Naming.Clean(entity.Name);
            return name.Length > 0 ? name : entity.Kind.DisplayName;
        }
    }
}
