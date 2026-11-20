using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Utils;

namespace Sprocket.ModSupport
{
    /// <summary>
    /// One member a mod gives its blocks in Sprocket, made on a <see cref="SprocketBlock" /> and described one detail at a
    /// time: a value, a function, an action, an awaitable action, a whitelisted terminal member, or an override.
    /// </summary>
    /// <remarks>A detail that does not fit the member's kind throws as it is given, so a mistake shows on the first load.</remarks>
    public sealed class SprocketMember : ISprocketMemberWiring, ISprocketWired
    {
        readonly Dictionary<string, object> _said = new Dictionary<string, object>();
        readonly ISprocketRegistry _registration;
        readonly string _kind;
        readonly List<KeyValuePair<Func<IMyTerminalBlock, bool>, SprocketFailure>> _polledFailures = new List<KeyValuePair<Func<IMyTerminalBlock, bool>, SprocketFailure>>();
        Action<IMyTerminalBlock, object[]> _kick;
        Func<IMyTerminalBlock, bool> _polledDone;
        Action<IMyTerminalBlock, SprocketCancel> _cancelling;

        /// <summary>The member <paramref name="id" /> of the kind word <paramref name="kind" /> on the scope numbered <paramref name="scope" />, registered through <paramref name="registration" />, which only the wrapper holds.</summary>
        public SprocketMember(ISprocketRegistry registration, int scope, string kind, string id)
        {
            _registration = registration;
            _kind = kind;
            Id = id;
            _said[SprocketWire.Scope] = scope;
            _said[SprocketWire.Kind] = kind;
            _said[SprocketWire.Id] = id;
        }

        /// <summary>The member's id: in the mod's namespace, or the game's id for a whitelisted member.</summary>
        public string Id { get; }

        /// <summary>Names the member <paramref name="name" /> in Sprocket's sentences and describes it with <paramref name="description" />, its tooltip.</summary>
        public SprocketMember Named(string name, string description)
        {
            return Named(SprocketText.Of(name), SprocketText.Of(description));
        }

        /// <summary>Names and describes the member by the texts <paramref name="name" /> and <paramref name="description" /> name in the mod's own texts.</summary>
        public SprocketMember Named(MyStringId name, MyStringId description)
        {
            return Named(SprocketText.Of(name), SprocketText.Of(description));
        }

        /// <summary>Describes the member with <paramref name="description" />, for an override saying how it behaves on these blocks.</summary>
        public SprocketMember Described(string description)
        {
            return Describing(SprocketText.Of(description));
        }

        /// <summary>Describes the member by the text <paramref name="description" /> names in the mod's own texts.</summary>
        public SprocketMember Described(MyStringId description)
        {
            return Describing(SprocketText.Of(description));
        }

        /// <summary>Stands the member in a group of the mod's own, headed <paramref name="heading" />.</summary>
        public SprocketMember InGroup(string heading)
        {
            _said[SprocketWire.Group] = SprocketText.Of(heading);
            return this;
        }

        /// <summary>Stands the member in a group of the mod's own, headed by the text <paramref name="heading" /> names in its texts.</summary>
        public SprocketMember InGroup(MyStringId heading)
        {
            _said[SprocketWire.Group] = SprocketText.Of(heading);
            return this;
        }

        /// <summary>Stands the member in <paramref name="group" />, one of Sprocket's own.</summary>
        public SprocketMember InGroup(SprocketGroup group)
        {
            _said[SprocketWire.SprocketsGroup] = group.Id;
            return this;
        }

        /// <summary>Gives a number its <paramref name="unit" /> as players read it, such as "m/s"; the mod reads and writes in that unit.</summary>
        public SprocketMember Unit(string unit)
        {
            Valued("A unit");
            if (_said.ContainsKey(SprocketWire.Percent))
                throw new InvalidOperationException($"Member {Id} is either a percentage or has a unit, not both.");

            _said[SprocketWire.Unit] = unit;
            return this;
        }

        /// <summary>Has players read the number as 0 to 100 %, while the mod reads and writes a share from 0 to 1.</summary>
        public SprocketMember Percent()
        {
            Valued("Percent");
            if (_said.ContainsKey(SprocketWire.Unit))
                throw new InvalidOperationException($"Member {Id} is either a percentage or has a unit, not both.");

            _said[SprocketWire.Percent] = true;
            return this;
        }

        /// <summary>The least and most the number may be, the same on every block; a percentage's as shares from 0 to 1, as the mod reads and writes it.</summary>
        public SprocketMember Range(double least, double most)
        {
            return Range(block => least, block => most);
        }

        /// <summary>The least and most the number may be on each block; a percentage's as shares from 0 to 1, as the mod reads and writes it.</summary>
        /// <remarks>Asked as the editor shows a field and as a step is checked, never on a tick; the mod's setter still clamps.</remarks>
        public SprocketMember Range(Func<IMyTerminalBlock, double> least, Func<IMyTerminalBlock, double> most)
        {
            Valued("A range");
            _said[SprocketWire.Least] = least;
            _said[SprocketWire.Most] = most;
            return this;
        }

        /// <summary>How a flag reads in a sentence: <paramref name="on" /> and <paramref name="off" />, such as "has grip" and "has no grip".</summary>
        public SprocketMember Reading(string on, string off)
        {
            return Reading(SprocketText.Of(on), SprocketText.Of(off));
        }

        /// <summary>How a flag reads in a sentence, by the texts <paramref name="on" /> and <paramref name="off" /> name in the mod's own texts.</summary>
        public SprocketMember Reading(MyStringId on, MyStringId off)
        {
            return Reading(SprocketText.Of(on), SprocketText.Of(off));
        }

        /// <summary>The word a sentence puts between the value and its block, where "of" reads wrong: "on" gives "the load on Lift 1".</summary>
        public SprocketMember Joined(string join)
        {
            Valued("A joining word");
            _said[SprocketWire.Joined] = join;
            return this;
        }

        /// <summary>Promises the mod calls <see cref="Changed" /> every time the value changes, so Sprocket stops looking on its own.</summary>
        public SprocketMember Announced()
        {
            Valued("Announced");
            _said[SprocketWire.Announced] = true;
            return this;
        }

        /// <summary>Says what the options of a whitelisted terminal action are, every one of them in the order the game declares them.</summary>
        public SprocketMember Taking(params ISprocketOption[] options)
        {
            if (_kind != SprocketWire.KindWhitelisted && _kind != SprocketWire.KindOverride)
                throw new InvalidOperationException($"Member {Id}: Taking(...) describes a whitelisted terminal action's options; give a member of your own its options where you make it.");

            _said[SprocketWire.Options] = Wired(options);
            return this;
        }

        /// <summary>Has a step wait on the awaitable action until <paramref name="done" /> holds of the block, asked over and over.</summary>
        /// <remarks>
        /// Looked at on Sprocket's own cadence while a step waits, so the step moves on a little after the block gets there. Where
        /// the block has an event for it, return a <see cref="SprocketTask" /> and complete it from that event instead: it costs
        /// nothing while it waits, and the step moves on at once.
        /// </remarks>
        public SprocketMember PolledDone(Func<IMyTerminalBlock, bool> done)
        {
            Waited("PolledDone");
            _polledDone = done;
            return this;
        }

        /// <summary>Ends the awaitable action as <paramref name="failure" /> once <paramref name="fails" /> holds of the block, before it is done, asked over and over as <see cref="PolledDone" /> is.</summary>
        public SprocketMember PolledFailure(Func<IMyTerminalBlock, bool> fails, SprocketFailure failure)
        {
            Waited("PolledFailure");
            _polledFailures.Add(new KeyValuePair<Func<IMyTerminalBlock, bool>, SprocketFailure>(fails, failure));
            return this;
        }

        /// <summary>Has <paramref name="stopping" /> stop what the awaitable action does on a block when Sprocket stops it, told why.</summary>
        /// <remarks>Make it as fast as it can possibly be. The action has already said it stopped; the order of what it does here does not matter.</remarks>
        public SprocketMember Cancelling(Action<IMyTerminalBlock, SprocketCancel> stopping)
        {
            Waited("Cancelling");
            _cancelling = stopping;
            return this;
        }

        /// <summary>Tells Sprocket the value may have changed on <paramref name="block" />, so what reads it is evaluated again now.</summary>
        /// <remarks>Cheap where nothing in Sprocket reads the block; called on the game's main thread.</remarks>
        public void Changed(IMyTerminalBlock block)
        {
            _registration.Changed(block, Id);
        }

        /// <inheritdoc />
        SprocketMember ISprocketMemberWiring.Valuing(string type, Func<IMyTerminalBlock, object> read, Action<IMyTerminalBlock, object> write)
        {
            _said[SprocketWire.Type] = type;
            _said[SprocketWire.Read] = read;
            if (write != null)
                _said[SprocketWire.Write] = write;
            return this;
        }

        /// <inheritdoc />
        SprocketMember ISprocketMemberWiring.Asking(string type, ISprocketOption[] options, Func<IMyTerminalBlock, object[], object> ask)
        {
            _said[SprocketWire.Type] = type;
            _said[SprocketWire.Options] = Wired(options);
            _said[SprocketWire.Ask] = ask;
            return this;
        }

        /// <inheritdoc />
        SprocketMember ISprocketMemberWiring.Running(ISprocketOption[] options, Action<IMyTerminalBlock, object[]> run)
        {
            _said[SprocketWire.Options] = Wired(options);
            _said[SprocketWire.Run] = run;
            return this;
        }

        /// <inheritdoc />
        SprocketMember ISprocketMemberWiring.Starting(ISprocketOption[] options, Func<IMyTerminalBlock, object[], SprocketTask> start)
        {
            _said[SprocketWire.Options] = Wired(options);
            _said[SprocketWire.Start] = (Func<IMyTerminalBlock, object[], Dictionary<string, object>>)((block, given) => ((ISprocketWired)(start(block, given) ?? SprocketTask.Completed)).Wire());
            return this;
        }

        /// <inheritdoc />
        SprocketMember ISprocketMemberWiring.Kicking(ISprocketOption[] options, Action<IMyTerminalBlock, object[]> kick)
        {
            _kick = kick;
            return ((ISprocketMemberWiring)this).Starting(options, Kicked);
        }

        /// <summary>Says the value is a word of <paramref name="type" />.</summary>
        public SprocketMember Choosing(SprocketType type)
        {
            _said[SprocketWire.Of] = type.FullName;
            return this;
        }

        /// <inheritdoc />
        /// <remarks>Throws where the member lacks what it needs: a name and description, a flag's readings, an awaitable's end.</remarks>
        Dictionary<string, object> ISprocketWired.Wire()
        {
            var made = _kind != SprocketWire.KindWhitelisted && _kind != SprocketWire.KindOverride;
            if (made && !_said.ContainsKey(SprocketWire.Named))
                throw new InvalidOperationException($"Member {Id} has no name. Give it one, and a description, with Named(...).");

            if (made && Type == SprocketWire.TypeFlag && !_said.ContainsKey(SprocketWire.ReadsOn))
                throw new InvalidOperationException($"Flag {Id} has no readings. Say how it reads both ways with Reading(...).");

            if (_kick != null && _polledDone == null)
                throw new InvalidOperationException($"Awaitable action {Id} never ends. Say when it is done with PolledDone(...), or return a SprocketTask.");

            return _said;
        }

        string Type => _said.ContainsKey(SprocketWire.Type) ? _said[SprocketWire.Type] as string : null;

        SprocketMember Named(Func<string> name, Func<string> description)
        {
            if (_kind == SprocketWire.KindOverride)
                throw new InvalidOperationException($"Override of {Id}: a member keeps its name on every block. Change its description with Described(...).");

            _said[SprocketWire.Named] = name;
            _said[SprocketWire.Described] = description;
            return this;
        }

        SprocketMember Describing(Func<string> description)
        {
            _said[SprocketWire.Described] = description;
            return this;
        }

        SprocketMember Reading(Func<string> on, Func<string> off)
        {
            if (_kind != SprocketWire.KindOverride && _kind != SprocketWire.KindWhitelisted && Type != SprocketWire.TypeFlag)
                throw new InvalidOperationException($"Member {Id}: Reading(...) is for a flag.");

            _said[SprocketWire.ReadsOn] = on;
            _said[SprocketWire.ReadsOff] = off;
            return this;
        }

        void Valued(string detail)
        {
            if (_kind == SprocketWire.KindAction || _kind == SprocketWire.KindAwaitable || _kind == SprocketWire.KindSignal)
                throw new InvalidOperationException($"Member {Id}: {detail} is for a value or function, not an action or signal.");
        }

        void Waited(string detail)
        {
            if (_kick == null)
                throw new InvalidOperationException($"Member {Id}: {detail}(...) is for an awaitable action made without a task of its own.");
        }

        SprocketTask Kicked(IMyTerminalBlock block, object[] given)
        {
            _kick(block, given);
            var task = SprocketTask.Polled(() => _polledDone(block), () => Failing(block));
            if (_cancelling != null)
                task.Cancelling += stop => _cancelling(block, stop);
            return task;
        }

        SprocketFailure? Failing(IMyTerminalBlock block)
        {
            foreach (var fails in _polledFailures)
            {
                if (fails.Key(block))
                    return fails.Value;
            }

            return null;
        }

        static Dictionary<string, object>[] Wired(ISprocketOption[] options)
        {
            var wired = new Dictionary<string, object>[options.Length];
            for (var i = 0; i < options.Length; i++)
                wired[i] = options[i].Wire();
            return wired;
        }
    }
}
