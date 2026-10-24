using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>The pollers a program has: one for each <c>when</c> and <c>every</c> of each logic system on each of its installations.</summary>
    public sealed class Pollers
    {
        readonly AutomationRunner _owner;
        readonly Automation _automation;
        readonly Dictionary<Trigger, List<Poller>> _pollers = new Dictionary<Trigger, List<Poller>>();
        readonly HashSet<Poller> _keptPollers = new HashSet<Poller>();
        readonly List<Trigger> _goneTriggers = new List<Trigger>();

        int _missingStamp;

        /// <summary>The pollers of <paramref name="automation" />, each owned by <paramref name="owner" />.</summary>
        public Pollers(AutomationRunner owner, Automation automation)
        {
            _owner = owner;
            _automation = automation;
        }

        /// <summary>What watches <paramref name="trigger" /> on <paramref name="instance" />, or null where nothing is looked for.</summary>
        public Poller Poller(Trigger trigger, SystemInstance instance)
        {
            List<Poller> pollers;
            if (!_pollers.TryGetValue(trigger, out pollers))
                return null;

            foreach (var poller in pollers)
            {
                if (poller.Instance == instance)
                    return poller;
            }

            return null;
        }

        /// <summary>Counts up whenever what <see cref="Lost" /> answers may have changed, so a reader asks again only then.</summary>
        public int MissingStamp => _missingStamp;

        /// <summary>Says a poller now finds a block it watches gone, or back, or the pollers themselves changed.</summary>
        public void MissingChanged()
        {
            _missingStamp++;
            _owner.Revise();
        }

        /// <summary>A block a trigger of <paramref name="sequence" /> watches on <paramref name="instance" /> that is no longer on the construct, by its reference, or null.</summary>
        /// <remarks>Walks every poller, so it is read through <see cref="MissingStamp" /> rather than on every look.</remarks>
        public string Lost(Sequence sequence, SystemInstance instance)
        {
            foreach (var pair in _pollers)
            {
                if (!pair.Key.IsAbout(sequence))
                    continue;

                var pollers = pair.Value;
                foreach (var poller in pollers)
                {
                    if (poller.Instance == instance && poller.Lost != null)
                        return poller.Lost;
                }
            }

            return null;
        }

        /// <summary>Starts a match of the triggers, keeping no poller until each is kept again.</summary>
        public void Begin()
        {
            _keptPollers.Clear();
        }

        /// <summary>Keeps the poller of <paramref name="trigger" />, at <paramref name="place" /> among the triggers of <paramref name="system" />, on <paramref name="instance" />, making it where there is none.</summary>
        public void Keep(AutomationSystem system, int place, Trigger trigger, SystemInstance instance)
        {
            _keptPollers.Add(Watching(system, place, trigger, instance));
        }

        /// <summary>Retires the pollers not kept since <see cref="Begin" />, saying the pollers changed.</summary>
        public void Sweep()
        {
            _missingStamp++;
            _owner.Revise();
            _goneTriggers.Clear();
            foreach (var pair in _pollers)
            {
                var pollers = pair.Value;
                for (var i = pollers.Count - 1; i >= 0; i--)
                {
                    if (_keptPollers.Contains(pollers[i]))
                        continue;

                    pollers[i].Retire();
                    _automation.Memory.Watches.Remove(pollers[i].Memory);
                    pollers.RemoveAt(i);
                }

                if (pollers.Count == 0)
                    _goneTriggers.Add(pair.Key);
            }

            foreach (var gone in _goneTriggers)
                _pollers.Remove(gone);
        }

        /// <summary>Takes every poller off the rota and lets go of them all, so each is made again from the construct's memory as it stands.</summary>
        /// <remarks>For a copy whose memory was put back in place from the one that lent it; the next match of the program makes them.</remarks>
        public void Forget()
        {
            Retire();
            _pollers.Clear();
            _keptPollers.Clear();
            _goneTriggers.Clear();
        }

        /// <summary>Takes every poller off the rota.</summary>
        public void Retire()
        {
            foreach (var pollers in _pollers.Values)
            {
                foreach (var poller in pollers)
                    poller.Retire();
            }
        }

        /// <summary>The poller for <paramref name="trigger" />, at <paramref name="place" /> among its system's, on <paramref name="instance" />, made where there is none.</summary>
        /// <remarks>
        /// One just made takes up what the world remembers of it, and is handed whatever a copy was sent. Its place is written
        /// again on every check.
        /// </remarks>
        Poller Watching(AutomationSystem system, int place, Trigger trigger, SystemInstance instance)
        {
            var poller = Poller(trigger, instance);
            if (poller != null)
            {
                poller.Memory.Trigger = place + 1;
                return poller;
            }

            List<Poller> pollers;
            if (!_pollers.TryGetValue(trigger, out pollers))
            {
                pollers = new List<Poller>();
                _pollers.Add(trigger, pollers);
            }

            poller = new Poller(system, trigger, instance, _owner, Watched(system, instance.Number, place + 1));
            pollers.Add(poller);


            return poller;
        }

        /// <summary>
        /// What the world remembers of trigger <paramref name="trigger" />, counted from one, on installation
        /// <paramref name="instance" /> of <paramref name="system" />, kept from now on where it remembers nothing yet.
        /// </summary>
        /// <remarks>Asked as a poller is made, which is when the program gains something, never on a tick.</remarks>
        WatchMemory Watched(AutomationSystem system, int instance, int trigger)
        {
            var watches = _automation.Memory.Watches;
            foreach (var watch in watches)
            {
                if (watch.System == system.Identity && watch.Instance == instance && watch.Trigger == trigger)
                    return watch;
            }

            var made = new WatchMemory { System = system.Identity, Instance = instance, Trigger = trigger };
            watches.Add(made);
            return made;
        }
    }
}
