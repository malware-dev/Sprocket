namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What Sprocket already knows about the game's own actions.
    /// </summary>
    /// <remarks>
    /// The same thing a mod ships in a file, built here instead, for one reason: these have to be
    /// read before anybody else's so that anybody else can override them, and a file of ours would
    /// be read wherever the player happened to have put this mod in their list.
    /// <para>
    /// By action id and not by block, because these are the game's own words and mean the same
    /// thing wherever they turn up. A mod that uses one of these ids for something else says so
    /// against its own block, which is the stronger claim and wins - see <see cref="Catalogue" />.
    /// </para>
    /// </remarks>
    static class Known
    {
        public static Descriptions Descriptions()
        {
            var said = new Descriptions();

            // A light's colour: three numbers to the game, one thing to a person. The group is what
            // says so, and what an editor for a colour will be asked for by.
            said.Actions.Add(Action("SetColor",
                Group("color", Channel("Red"), Channel("Green"), Channel("Blue"))));

            said.Actions.Add(Action("SetRadius", Parameter("Radius")));

            return said;
        }

        static Description Action(string id, params Piece[] pieces)
        {
            var description = new Description();
            description.Id = id;

            for (var i = 0; i < pieces.Length; i++)
                description.Pieces.Add(pieces[i]);

            return description;
        }

        static Piece Parameter(string name)
        {
            var parameter = new Parameter();
            parameter.Name = name;
            return parameter;
        }

        static Piece Group(string kind, params Parameter[] parameters)
        {
            var group = new Group();
            group.Sort = kind;

            for (var i = 0; i < parameters.Length; i++)
                group.Parameters.Add(parameters[i]);

            return group;
        }

        /// <summary>One channel of a colour, which the game takes as a byte and never says so.</summary>
        static Parameter Channel(string name)
        {
            var channel = (Parameter)Parameter(name);
            channel.Least = "0";
            channel.Most = "255";
            return channel;
        }
    }
}
