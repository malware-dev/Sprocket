
namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What Sprocket already knows about the game's own actions.
    /// </summary>
    /// <remarks>
    /// Nearly every one of the game's actions that takes values declares each as text and refuses anything that is not a
    /// number; the types here are the ones their handlers accept, read from the game's own blocks.
    /// </remarks>
    public static class KnownActions
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

        /// <summary>The descriptions of the game's own actions that Sprocket ships, read before any mod's.</summary>
        public static Descriptions Descriptions()
        {
            var said = new Descriptions();

            // The game takes a light's color as three numbers.
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

            foreach (var name in Reals)
                said.Actions.Add(Action(name, Parameter("", Real)));

            return said;
        }

        static ParameterPiece Colour()
        {
            return Group("color", Channel("Red"), Channel("Green"), Channel("Blue"));
        }

        static Description Action(string id, params ParameterPiece[] pieces)
        {
            var description = new Description();
            description.Id = id;

            foreach (var piece in pieces)
                description.Pieces.Add(piece);

            return description;
        }

        static ParameterPiece Parameter(string name, string kind)
        {
            var parameter = new Parameter();
            parameter.Name = name;
            parameter.Sort = kind;
            return parameter;
        }

        static ParameterPiece Group(string kind, params Parameter[] parameters)
        {
            var group = new ParameterGroup();
            group.Sort = kind;

            foreach (var parameter in parameters)
                group.Parameters.Add(parameter);

            return group;
        }

        /// <summary>One channel of a color, which the game takes as a whole number from nought to 255.</summary>
        static Parameter Channel(string name)
        {
            var channel = (Parameter)Parameter(name, Integer);
            channel.Least = "0";
            channel.Most = "255";
            return channel;
        }
    }
}
