using Lumel.Text;

namespace Sprocket.Sim.Driving
{
    /// <summary>A text with its slots filled in, as a test expects to read it on the screen.</summary>
    public static class Filled
    {
        /// <summary><paramref name="template" /> with each slot named in <paramref name="slots" /> replaced by the words after its name.</summary>
        public static string With(string template, params string[] slots)
        {
            var said = template;
            for (var i = 0; i + 1 < slots.Length; i += 2)
                said = Template.Fill(said, slots[i], slots[i + 1]);

            return said;
        }
    }
}
