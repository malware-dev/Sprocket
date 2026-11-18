namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>A type the host provides whose values are a fixed list of choices, like a door's status: <c>sprocket.door_status</c>.</summary>
    public sealed class ChoiceType
    {
        /// <summary>A type called <paramref name="id" />, its choices <paramref name="choices" /> in order.</summary>
        public ChoiceType(string id, params string[] choices)
        {
            Id = id;
            Choices = choices;
        }

        /// <summary>The type's name in the host's namespace, as a program writes it: <c>door_status</c>.</summary>
        /// <remarks>Persisted in every program that writes one of its choices with its type.</remarks>
        public string Id { get; }

        /// <summary>The choices, in order, each as a program writes it.</summary>
        /// <remarks>Persisted in every program that writes one of them; never changed.</remarks>
        public string[] Choices { get; }

        /// <summary>Whether <paramref name="choice" /> is one of the type's choices.</summary>
        public bool Has(string choice)
        {
            foreach (var one in Choices)
            {
                if (one == choice)
                    return true;
            }

            return false;
        }
    }
}
