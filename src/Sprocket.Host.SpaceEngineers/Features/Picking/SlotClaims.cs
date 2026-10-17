using System;
using System.Collections.Generic;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>Who already claims a toolbar slot somebody picks, and the asking and taking over before it is given to another.</summary>
    public sealed class SlotClaims
    {
        readonly SprocketEditor _editor;

        /// <summary>The claims on the toolbar slots of what <paramref name="editor" /> edits.</summary>
        public SlotClaims(SprocketEditor editor)
        {
            _editor = editor;
        }

        /// <summary>
        /// Carries out <paramref name="then" />, unless something already answers a button <paramref name="made" /> is given;
        /// then asks first, and goes on, taking the button, only where that is the answer.
        /// </summary>
        /// <remarks>
        /// Asks only for a trigger the construct announces from a toolbar slot; a moment Sprocket watches goes straight on.
        /// </remarks>
        /// <param name="system">The logic system the trigger is in.</param>
        /// <param name="made">The trigger as it would be once done.</param>
        /// <param name="answer">What was picked, buttons and all.</param>
        /// <param name="except">The trigger being changed, or null for a new one.</param>
        /// <param name="then">What is done once it may be, given what the button is taken from, or null.</param>
        /// <param name="otherwise">What is done where the answer is to leave the button as it is.</param>
        public void Unless(AutomationSystem system, MomentTrigger made, PickAnswer answer, Trigger except, Action<SlotOccupant> then,
            Action otherwise)
        {
            var signal = made as SignalTrigger;
            if (signal == null || signal.IsWatched)
            {
                then(null);
                return;
            }

            // A system's trigger holds only the first button picked.
            IList<long> panels = system.IsBlueprint || answer.Targets.Count <= 1 ? answer.Targets : new[] { answer.Targets[0] };
            var holder = Holding(panels, Pressed(answer), except, _editor.StandIn(system), null);

            if (holder == null)
            {
                then(null);
                Wired(panels);
                return;
            }

            AskAbout(holder);
            _editor.Answer(_editor.Texts.ButtonTakeOver, delegate
            {
                then(holder.Trigger == null ? null : holder);
                Wired(panels);
            });
            _editor.Answer(_editor.Texts.ButtonLeaveAlone, otherwise, true);
        }

        /// <summary>
        /// The toolbar slot answering the picked trigger on each picked block: the trigger's own, or one of the slots a trigger
        /// that stands for many runs; slot 0 where what was picked is no trigger.
        /// </summary>
        public List<ToolbarSlot> Pressed(PickAnswer answer)
        {
            var picked = ToolbarSlot.Of(answer.Chosen);
            var at = new List<ToolbarSlot>();
            for (var i = 0; i < answer.Targets.Count; i++)
            {
                GridEntity block = picked != null && picked.IsMany ? _editor.World.FindEntity(answer.Targets[i]) : null;
                at.Add(picked == null ? new ToolbarSlot(1, 1, 0) : block != null ? Free(block, picked) : picked);
            }

            return at;
        }

        /// <summary>The first of the slots <paramref name="many" /> stands for on <paramref name="block" /> that holds nothing, or the first of them where every one holds something.</summary>
        /// <remarks>Each trigger on a timer takes a slot of its own, so two sequences started by one timer both run.</remarks>
        static ToolbarSlot Free(GridEntity block, ToolbarSlot many)
        {
            IMyToolbar toolbar = Toolbars.Of(block.Block, many.Toolbar);
            ToolbarSlot first = null;
            for (var page = 1; toolbar != null && page <= toolbar.PageCount; page++)
            {
                for (var slot = 1; slot <= toolbar.SlotCount; slot++)
                {
                    var one = new ToolbarSlot(many.Toolbar, page, slot);
                    if (!many.Covers(one))
                        continue;

                    if (first == null)
                        first = one;

                    MyToolbarItemInfo held;
                    if (!toolbar.TryGetItem(Toolbars.Index(toolbar, one), out held))
                        return one;
                }
            }

            return first ?? new ToolbarSlot(many.Toolbar, 1, 0);
        }

        /// <summary>
        /// Fills <paramref name="part" /> of <paramref name="chosen" /> with the blocks <paramref name="blocks" /> by
        /// <paramref name="fill" />, asking first where it is a signal part and something else already answers one of the
        /// toolbar slots <paramref name="at" /> it is given.
        /// </summary>
        /// <remarks>
        /// Nothing is taken until every question is answered, and leaving any of them alone leaves the part as it was.
        /// </remarks>
        public void Filling(AutomationSystem system, Part part, SystemInstance chosen, IList<long> blocks, IList<ToolbarSlot> at,
            Func<SystemEdit> fill, Action done)
        {
            Filling(system, part, chosen, blocks, at, fill, done, new List<SlotOccupant>());
        }

        void Filling(AutomationSystem system, Part part, SystemInstance chosen, IList<long> blocks, IList<ToolbarSlot> at,
            Func<SystemEdit> fill, Action done, List<SlotOccupant> taking)
        {
            var trigger = part.IsSignal && at != null ? TriggerOf(system, part.Id) : null;
            var holder = trigger == null ? null : Holding(blocks, at, trigger, chosen, taking);
            if (holder != null)
            {
                AskAbout(holder);
                _editor.Answer(_editor.Texts.ButtonTakeOver, delegate
                {
                    taking.Add(holder);
                    Filling(system, part, chosen, blocks, at, fill, done, taking);
                });
                _editor.Answer(_editor.Texts.ButtonLeaveAlone, () => { }, true);
                return;
            }

            // One edit: the fill, and every button it takes from somewhere else.
            var edit = fill();
            foreach (var occupant in taking)
            {
                if (occupant.Trigger != null)
                    edit = TakingOver(edit, occupant);
            }

            _editor.Do(system, edit);
            Wired(blocks);
            done();
        }

        /// <summary>The trigger that is the signal part <paramref name="part" /> of <paramref name="system" />, or null.</summary>
        static SignalTrigger TriggerOf(AutomationSystem system, string part)
        {
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var trigger = triggers[i] as SignalTrigger;
                if (trigger != null && trigger.Part == part)
                    return trigger;
            }

            return null;
        }

        /// <summary>
        /// The first of <paramref name="panels" /> whose toolbar slot in <paramref name="at" /> something other than
        /// <paramref name="trigger" /> on <paramref name="instance" /> already answers, passing over what is in
        /// <paramref name="taking" />; or null where nothing does.
        /// </summary>
        SlotOccupant Holding(IList<long> panels, IList<ToolbarSlot> at, Trigger trigger, SystemInstance instance, List<SlotOccupant> taking)
        {
            for (var p = 0; p < panels.Count && p < at.Count; p++)
            {
                GridEntity panel = _editor.World.FindEntity(panels[p]);
                var where = at[p];
                if (panel == null || where.Slot < 1)
                    continue;

                var named = false;
                for (var s = 0; s < _editor.Program.Systems.Count; s++)
                {
                    AutomationSystem each = _editor.Program.Systems[s];
                    var triggers = each.Triggers;
                    for (var i = 0; i < triggers.Count; i++)
                    {
                        var other = triggers[i] as SignalTrigger;
                        if (other == null)
                            continue;

                        var instances = each.Uses;
                        for (var n = 0; n < instances.Count; n++)
                        {
                            var through = instances[n];
                            if ((other == trigger && (through == instance || !each.IsBlueprint))
                                || !Answers(each, through, other, panel.Id, where))
                                continue;

                            named = true;
                            var holder = new SlotOccupant { Panel = panel, At = where, System = each, Trigger = other, Instance = through };
                            if (!Taken(taking, holder))
                                return holder;
                        }
                    }
                }

                if (!named && _editor.World.Foreign(panel.Block, where))
                {
                    var holder = new SlotOccupant { Panel = panel, At = where };
                    if (!Taken(taking, holder))
                        return holder;
                }
            }

            return null;
        }

        /// <summary>
        /// Which trigger of this automation the toolbar slot <paramref name="at" /> of <paramref name="panel" /> answers, in
        /// words: its logic system or installation and the sequence it runs; another automation block's where none of this one's does.
        /// </summary>
        public string Answering(GridEntity panel, ToolbarSlot at)
        {
            foreach (var system in _editor.Program.Systems)
            {
                foreach (var trigger in system.Triggers)
                {
                    var answered = trigger as SignalTrigger;
                    if (answered == null)
                        continue;

                    foreach (var through in system.Uses)
                    {
                        if (!Answers(system, through, answered, panel.Id, at))
                            continue;

                        var sequence = system.FindSequence(answered.Runs);
                        var named = system.IsBlueprint && through != null
                            ? SystemSummary.DescribeInstanceOf(_editor.Texts, _editor.Runner.Bindings, system, through)
                            : system.Name;
                        var said = Template.Fill(_editor.Texts.SlotAnswers, "system", named);
                        return Template.Fill(said, "sequence",
                            sequence == null ? "" : sequence.HasName ? sequence.Name : SequenceText.DefaultName(_editor.Texts, sequence.Number));
                    }
                }
            }

            return _editor.Texts.SlotElsewhere;
        }

        static bool Taken(List<SlotOccupant> taking, SlotOccupant holder)
        {
            for (var i = 0; taking != null && i < taking.Count; i++)
            {
                if (taking[i].Same(holder))
                    return true;
            }

            return false;
        }

        /// <summary>Whether <paramref name="instance" /> of <paramref name="system" /> answers trigger <paramref name="trigger" /> with the slot <paramref name="where" /> on the block <paramref name="block" />.</summary>
        bool Answers(AutomationSystem system, SystemInstance instance, SignalTrigger trigger, long block, ToolbarSlot where)
        {
            var blocks = new List<string>();
            var positions = new List<ToolbarSlot>();
            _editor.Program.Answers(system, instance, trigger, blocks, positions);
            for (var i = 0; i < blocks.Count && i < positions.Count; i++)
            {
                GridEntity entity = _editor.Runner.Bindings.Entity(blocks[i]);
                if (entity != null && entity.Id == block && positions[i].Same(where))
                    return true;
            }

            return false;
        }

        /// <summary>Asks whether to take the button <paramref name="holder" /> has, saying what it does now.</summary>
        void AskAbout(SlotOccupant holder)
        {
            var arguments = new Arguments();
            arguments.Set("block", holder.Panel.Name);

            if (holder.Trigger == null)
            {
                arguments.Set("button", holder.At.Slot.ToString(System.Globalization.CultureInfo.InvariantCulture));
                _editor.AskHere(_editor.Texts.ButtonElsewhere, arguments);
                return;
            }

            var sequence = holder.System.FindSequence(holder.Trigger.Runs);
            arguments.Set("sequence", sequence == null ? "" : sequence.HasName ? sequence.Name : SequenceText.DefaultName(_editor.Texts, sequence.Number));
            arguments.Set("blockset", holder.System.IsBlueprint && holder.Instance != null
                ? SystemSummary.DescribeInstanceOf(_editor.Texts, _editor.Runner.Bindings, holder.System, holder.Instance)
                : holder.System.Name);
            _editor.AskHere(holder.Trigger.Acts != null ? _editor.Texts.ButtonActs : holder.Trigger.Stops ? _editor.Texts.ButtonStops : _editor.Texts.ButtonStarts,
                arguments);
        }

        /// <summary>Whether <paramref name="taken" /> is another trigger in the installation of <paramref name="system" /> being edited.</summary>
        public bool Shares(SlotOccupant taken, AutomationSystem system)
        {
            return taken != null && taken.Trigger != null && system.IsBlueprint && taken.System == system && taken.Instance == _editor.StandIn(system);
        }

        /// <summary>Does <paramref name="made" />, where there is anything, and takes <paramref name="holder" />'s button, as one edit.</summary>
        public SystemEdit TakingOver(SystemEdit made, SlotOccupant holder)
        {
            if (!holder.System.IsBlueprint)
                return new TakeOverTrigger(made, holder.System, holder.Trigger);

            var kept = new List<string>();
            var keptAt = new List<ToolbarSlot>();
            Kept(holder, kept, keptAt);
            return new TakeOver(made, holder.Instance, holder.Trigger.Part, kept, keptAt);
        }

        /// <summary>What answers <paramref name="holder" />'s signal part in its blockset once its toolbar slot is taken out.</summary>
        void Kept(SlotOccupant holder, List<string> kept, List<ToolbarSlot> keptAt)
        {
            var part = holder.Trigger.Part;
            var blocks = holder.Instance.FilledWith(part);
            var positions = holder.Instance.Positions(part);
            for (var i = 0; blocks != null && positions != null && i < blocks.Count && i < positions.Count; i++)
            {
                GridEntity entity = _editor.Runner.Bindings.Entity(blocks[i]);
                if (entity != null && entity.Id == holder.Panel.Id && positions[i].Same(holder.At))
                    continue;

                kept.Add(blocks[i]);
                keptAt.Add(positions[i]);
            }
        }

        /// <summary>Has every slot the program wants on <paramref name="panels" /> written, whatever it holds, as somebody has just picked them.</summary>
        void Wired(IList<long> panels)
        {
            if (_editor.Wire == null)
                return;

            for (var i = 0; i < panels.Count; i++)
                _editor.Wire(panels[i]);
        }
    }
}
