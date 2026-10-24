using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>What is known to be wrong with a run, kept as each part of Sprocket finds it, and which of it a line says first.</summary>
    public sealed class RunFaults
    {
        readonly Sequence _sequence;
        readonly SystemInstance _instance;
        readonly AutomationRunner _owner;
        readonly RunMemory _memory;
        SequenceFault _quit;
        SequenceFault _found;
        SequenceFault _looked;
        SequenceFault _met;
        SequenceFault _stuck;

        // Whether the run was made from what the world remembered and has not been checked since, so the first check keeps what
        // its last run met.
        bool _restored;

        int _missingSeen = -1;
        string _lost;

        /// <summary>
        /// The faults of <paramref name="sequence" /> running for <paramref name="instance" />, owned by <paramref name="owner" />,
        /// kept in <paramref name="memory" />; taken up from it where <paramref name="restored" /> says the world remembered it.
        /// </summary>
        public RunFaults(Sequence sequence, SystemInstance instance, AutomationRunner owner, RunMemory memory, bool restored)
        {
            _sequence = sequence;
            _instance = instance;
            _owner = owner;
            _memory = memory;
            _quit = KeptReasons.Fault(memory.Quit);
            _met = KeptReasons.Fault(memory.Met);
            _found = KeptReasons.Fault(memory.Found);
            _restored = restored;
        }

        /// <summary>
        /// Why the last run ended before its last step, or none where it did not.
        /// </summary>
        /// <remarks>Kept after the run has stopped, and cleared when the sequence next runs.</remarks>
        public SequenceFault Quit
        {
            get { return _quit; }
            set
            {
                if (!_quit.Same(value))
                    _owner.Revise();

                _quit = value;
                _memory.Quit = KeptReasons.Kept(value);
            }
        }

        /// <summary>What was wrong with the program for the run's sequence when it was last built, or none.</summary>
        /// <remarks>Set by the runner of the whole program when the program or its wiring changes, and on no tick.</remarks>
        public SequenceFault Found
        {
            get { return _found; }
            set
            {
                if (!_found.Same(value))
                    _owner.Revise();

                _found = value;
                _memory.Found = KeptReasons.Kept(value);
            }
        }

        /// <summary>What an open editor last saw gone, or possibly swapped, among the blocks the run's sequence names, or none.</summary>
        public SequenceFault Looked
        {
            get { return _looked; }
            set
            {
                if (!_looked.Same(value))
                    _owner.Revise();

                _looked = value;
            }
        }

        /// <summary>What is known to be wrong with the run's sequence on the installation it runs on, or none.</summary>
        /// <remarks>Read, never worked out, and the worst news first.</remarks>
        public SequenceFault Fault
        {
            get
            {
                if (_missingSeen != _owner.Watches.MissingStamp)
                {
                    _lost = _owner.Watches.Lost(_sequence, _instance);
                    _missingSeen = _owner.Watches.MissingStamp;
                }

                if (_lost != null)
                    return new SequenceFault(SequenceProblem.MissingSource, SequenceFault.Trigger, BlockTrouble.None, _lost);
                if (_met.Exists)
                    return _met;
                if (Looked.Exists)
                    return Looked;
                if (Found.Exists && !Found.IsWarning)
                    return Found;
                if (_stuck.Exists && _memory.Waiting)
                    return _stuck;
                if (Quit.Exists)
                    return Quit;
                return Found;
            }
        }

        /// <summary>Keeps what the blocks of a step being waited on said stops it finishing, and says so where it changed.</summary>
        public void Hold(SequenceFault stuck)
        {
            if (!_stuck.Same(stuck))
                _owner.Revise();

            _stuck = stuck;
        }

        /// <summary>Takes what the program says is wrong, now it has been built again, forgetting what the last run met.</summary>
        /// <remarks>The first check of a run the world remembered keeps what the run met.</remarks>
        public void Checked(SequenceFault found)
        {
            Found = found;
            if (!_restored)
                Met(SequenceFault.None);

            _restored = false;
        }

        /// <summary>
        /// Forgets every problem known, as a logic system or installation switched on starts afresh, and takes
        /// <paramref name="found" />, what the program says is wrong now.
        /// </summary>
        public void Afresh(SequenceFault found)
        {
            _restored = false;
            Found = found;
            Quit = SequenceFault.None;
            Looked = SequenceFault.None;
            Hold(SequenceFault.None);
            Met(SequenceFault.None);
        }

        /// <summary>Keeps what the run met.</summary>
        public void Met(SequenceFault fault)
        {
            if (!_met.Same(fault))
                _owner.Revise();

            _met = fault;
            _memory.Met = KeptReasons.Kept(fault);
        }
    }
}
