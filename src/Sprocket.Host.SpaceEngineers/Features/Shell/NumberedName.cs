using System;
using System.Globalization;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>The name a new thing is given by default: a numbered text, counting on from how many there are.</summary>
    public static class NumberedName
    {
        /// <summary><paramref name="numbered" /> with the first number past <paramref name="count" /> that <paramref name="taken" /> does not refuse.</summary>
        public static string Free(string numbered, int count, Func<string, bool> taken)
        {
            for (var number = count + 1;; number++)
            {
                var name = Template.Fill(numbered, "number", number.ToString(CultureInfo.InvariantCulture));
                if (!taken(name))
                    return name;
            }
        }
    }
}
