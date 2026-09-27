
namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What Sprocket already knows about the game's own actions.
    /// </summary>
    /// <remarks>
    /// The same thing a mod ships in a file, built here instead, for one reason: these have to be
    /// read before anybody else's so that anybody else can override them, and a file of Sprocket's would
    /// be read wherever the player happened to have put this mod in their list.
    /// <para>
    /// By action id and not by block, because these are the game's own words and mean the same
    /// thing wherever they turn up. A mod that uses one of these ids for something else says so
    /// against its own block, which is the stronger claim and wins - see <see cref="Catalogue" />.
    /// </para>
    /// <para>
    /// Most of what is here corrects a type. Nearly every one of the game's actions that takes values
    /// declares each of them as text, and then refuses anything that is not a number: these are the
    /// types their handlers accept, read from the game's own blocks. The hud text setters and the
    /// programmable block's run take text and declare it, so they need nothing here.
    /// </para>
    /// </remarks>
    static class Known
    {
        const string Real = "single";

        const string Integer = "int32";

        static readonly string[] Reals =
        {
            "SetBlinkInterval", "SetGravityAcceleration", "SetPropulsionOverride", "SetSteerOverride",
            "SetSpeedLimit", "SetOverride", "SetAngleDeviation", "SetVelocity", "SetLowerLimit",
            "SetUpperLimit", "SetPower", "SetYaw", "SetPitch", "SetRoll", "SetLeft", "SetRight",
            "SetBottom", "SetTop", "SetBack", "SetFront"
        };

        public static Descriptions Descriptions()
        {
            var said = new Descriptions();

            // A light's colour: three numbers to the game, one thing to a person. The group is what
            // says so, and what an editor for a colour will be asked for by.
            said.Actions.Add(Action("SetColor", Colour()));
            said.Actions.Add(Action("SetColorMin", Colour()));
            said.Actions.Add(Action("SetColorMax", Colour()));

            // The game's handlers take a number with a fraction as a float, the type of every such value it holds,
            // and a whole number as either width; int32 is what its own toolbar dialog sends.
            said.Actions.Add(Action("SetRadius", Parameter("Radius", Real)));

            // A rotor turned to an angle at a speed, which way round as the game numbers it.
            said.Actions.Add(Action("RotateToAngle", Parameter("", Real), Parameter("", Real),
                Parameter("", Integer)));

            // A piston moved to a position at a speed.
            said.Actions.Add(Action("SetAndMove", Parameter("", Real), Parameter("", Real)));

            said.Actions.Add(Action("SetChannelValue", Parameter("", Integer)));
            said.Actions.Add(Action("SetChannelValue_SendSignal", Parameter("", Integer)));

            for (var i = 0; i < Reals.Length; i++)
                said.Actions.Add(Action(Reals[i], Parameter("", Real)));

            return said;
        }

        static Piece Colour()
        {
            return Group("color", Channel("Red"), Channel("Green"), Channel("Blue"));
        }

        static Description Action(string id, params Piece[] pieces)
        {
            var description = new Description();
            description.Id = id;

            for (var i = 0; i < pieces.Length; i++)
                description.Pieces.Add(pieces[i]);

            return description;
        }

        static Piece Parameter(string name, string kind)
        {
            var parameter = new Parameter();
            parameter.Name = name;
            parameter.Sort = kind;
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

        /// <summary>One channel of a colour, which the game takes as a whole number from nought to 255.</summary>
        static Parameter Channel(string name)
        {
            var channel = (Parameter)Parameter(name, Integer);
            channel.Least = "0";
            channel.Most = "255";
            return channel;
        }
    }
}
