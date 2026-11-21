using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>A test of the host: whether it has one of its namespaces, <c>gearbox.lifts is installed</c>, or lacks it, <c>gearbox.lifts is not installed</c>.</summary>
    /// <remarks>
    /// The host gains or loses a namespace only as a world loads or unloads, so the answer when a run reaches the test is the
    /// one the program was bound with.
    /// </remarks>
    public sealed class HostTest : BranchTest
    {
        /// <summary>The namespace tested, its parts joined by dots: <c>gearbox.lifts</c>.</summary>
        public string Namespace { get; set; } = "";

        /// <summary>Whether the test holds where the host has the namespace, <c>is installed</c>, rather than where it lacks it, <c>is not installed</c>.</summary>
        public bool Installed { get; set; } = true;

        /// <summary>Whether the test holds on the host as it is now.</summary>
        public bool Holds => HostNamespaces.Has(Namespace) == Installed;

        /// <summary>A copy of this test, to change one thing of as an edit.</summary>
        public HostTest Copy()
        {
            return new HostTest { Namespace = Namespace, Installed = Installed };
        }

        /// <summary>
        /// The namespaces the step at <paramref name="at" /> of <paramref name="steps" /> is reached only while the host has,
        /// into <paramref name="into" />: each that the branch it is in, or a branch around it, tests is installed, and each that
        /// an earlier branch of the same if tests is not installed.
        /// </summary>
        /// <remarks>Walks the steps before it once, and is asked when the program is checked, on no tick.</remarks>
        public static void Assumed(IList<Step> steps, int at, ICollection<string> into)
        {
            into.Clear();

            // One frame for each if open around the step: what its earlier branches said is not installed, and the test of
            // the branch the walk is in.
            var passed = new List<List<string>>();
            var tested = new List<BranchTest>();
            // The step itself is walked too: an else if is tested only once the branches before it have failed.
            for (var i = 0; i <= at && i < steps.Count; i++)
            {
                var step = steps[i];
                if (step is If)
                {
                    passed.Add(new List<string>());
                    tested.Add(((If)step).Test);
                    continue;
                }

                var top = passed.Count - 1;
                if (top < 0)
                    continue;

                var branch = step as Else;
                if (branch != null)
                {
                    var lacked = tested[top] as HostTest;
                    if (lacked != null && !lacked.Installed)
                        passed[top].Add(lacked.Namespace);

                    tested[top] = branch.Test;
                    continue;
                }

                if (step is EndIf)
                {
                    passed.RemoveAt(top);
                    tested.RemoveAt(top);
                }
            }

            for (var f = 0; f < passed.Count; f++)
            {
                foreach (var space in passed[f])
                    into.Add(space);

                var held = tested[f] as HostTest;
                if (held != null && held.Installed)
                    into.Add(held.Namespace);
            }
        }
    }
}
