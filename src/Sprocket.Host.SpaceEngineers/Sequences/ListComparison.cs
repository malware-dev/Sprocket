using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Whether two lists hold the same things.</summary>
    public static class ListComparison
    {
        /// <summary>Whether <paramref name="one" /> and <paramref name="other" /> hold the same things in the same order; null is the same as null alone.</summary>
        public static bool Same<T>(IList<T> one, IList<T> other)
        {
            if (one == null || other == null)
                return one == null && other == null;

            if (one.Count != other.Count)
                return false;

            var equal = EqualityComparer<T>.Default;
            for (var i = 0; i < one.Count; i++)
            {
                if (!equal.Equals(one[i], other[i]))
                    return false;
            }

            return true;
        }

        /// <summary>Whether <paramref name="one" /> and <paramref name="other" /> hold the same things in any order, each as often; null holds nothing.</summary>
        public static bool SameAnyOrder<T>(IList<T> one, IList<T> other)
        {
            if (one == null || one.Count == 0)
                return other == null || other.Count == 0;

            if (other == null || other.Count != one.Count)
                return false;

            var left = new List<T>(other);
            foreach (var item in one)
            {
                if (!left.Remove(item))
                    return false;
            }

            return true;
        }
    }
}
