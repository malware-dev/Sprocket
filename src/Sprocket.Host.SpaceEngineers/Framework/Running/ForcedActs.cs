using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>
    /// The blocks told to do something by force: each is kept at it until it gets there, whatever the sequence that told it does
    /// next, and let go the moment it can't go on, the sequence is stopped or something else tells it what to do.
    /// </summary>
    /// <remarks>
    /// Kept in memory only: a forced block is on its way for seconds, and a save and load lets it go. On the rota only while
    /// something is held, and looked at every ten ticks, since a player can reverse a block at any moment and not every block says so.
    /// A block that announces getting there is let go as it says so, so a player who reverses it at once finds it let go.
    /// </remarks>
    public sealed class ForcedActs
    {
        readonly Rota _rota;
        readonly Func<int> _fastest;
        Shift _look;
        readonly List<Held> _held = new List<Held>();

        /// <summary>Forced acts looked at on <paramref name="rota" />, no oftener than <paramref name="fastest" /> allows.</summary>
        public ForcedActs(Rota rota, Func<int> fastest)
        {
            _rota = rota;
            _fastest = fastest;
        }

        /// <summary>How many blocks are being held to what they were told.</summary>
        public int Count => _held.Count;

        /// <summary>Says <paramref name="by" /> has just given the block of <paramref name="act" /> an order, which holds it where the order is forced and lets go of any before it.</summary>
        public void Told(ActTarget act, SequenceRunner by)
        {
            Let(act.Entity);
            if (act.Bound != null && act.Bound.IsForced && act.Action.Until != null)
                Hold(act, by);
        }

        // Holds the block of act to the order by gave it. Its own method, since the listener's closure would otherwise be
        // made on every order, forced or not, as the method holding the variable it captures begins.
        void Hold(ActTarget act, SequenceRunner by)
        {
            var entity = act.Entity;
            var listening = act.Action.ArrivalAnnounced ? entity.Listen(() => Heard(entity)) : null;
            _held.Add(new Held { Act = act, By = by, Listening = listening });

            // Taken up by the first forced act, so a program with none has no place on the rota.
            if (_look == null)
                _look = new Shift(_rota.Join(), Look);

            _rota.Every(_look, Math.Max(Rota.Ten, _fastest()));
        }

        /// <summary>Lets go of <paramref name="entity" />, which has been told something else.</summary>
        public void Let(GridEntity entity)
        {
            for (var i = _held.Count - 1; i >= 0; i--)
            {
                if (_held[i].Act.Entity == entity)
                    Drop(i);
            }

            Rest();
        }

        /// <summary>Lets go of everything <paramref name="by" /> told, as it is stopped.</summary>
        public void Release(SequenceRunner by)
        {
            for (var i = _held.Count - 1; i >= 0; i--)
            {
                if (_held[i].By == by)
                    Drop(i);
            }

            Rest();
        }

        /// <summary>Lets go of everything, when the program goes away.</summary>
        public void Retire()
        {
            for (var i = _held.Count - 1; i >= 0; i--)
                Drop(i);

            Rest();
        }

        void Look(int tick)
        {
            for (var i = _held.Count - 1; i >= 0; i--)
            {
                var act = _held[i].Act;
                var block = act.Entity.Block;
                if (act.Action.Until.Holds(block, act.Seen) || act.Entity.Stalls(act.Action))
                {
                    Drop(i);
                    continue;
                }

                if (!act.Bound.Strays(block, act.Seen))
                    continue;

                act.Bound.Do(block, act.Seen);
                act.Entity.Asked();
            }

            Rest();
        }

        /// <summary>Lets go of <paramref name="entity" /> where it has got where it was forced, as it says it may have changed.</summary>
        void Heard(GridEntity entity)
        {
            for (var i = _held.Count - 1; i >= 0; i--)
            {
                var act = _held[i].Act;
                if (act.Entity == entity && act.Action.Until.Holds(entity.Block, act.Seen))
                    Drop(i);
            }

            Rest();
        }

        /// <summary>Lets go of the block held at <paramref name="index" /> and stops listening to it.</summary>
        void Drop(int index)
        {
            _held[index].Listening?.Dispose();
            _held.RemoveAt(index);
        }

        /// <summary>Takes the look off the rota where nothing is held.</summary>
        void Rest()
        {
            if (_held.Count == 0 && _look != null)
                _rota.Leave(_look);
        }

        struct Held
        {
            public ActTarget Act;
            public SequenceRunner By;
            public IDisposable Listening;
        }
    }
}
