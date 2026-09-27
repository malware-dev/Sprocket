using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Fakes;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>
/// The ship testers are given, as blocks the game would recognise.
/// </summary>
/// <remarks>
/// Chosen to support the three tasks the prototype is meant to answer: open the hangar when a ship
/// approaches, build a working airlock, and flash the lights when hydrogen runs low. The third
/// quietly probes the event-versus-state confusion without ever asking the tester about it.
/// <para>
/// Every block here is a real block type with a real definition id, because that is what the mod
/// builds its vocabulary from: a door offers "open" because it is an <c>IMyDoor</c>, and a light
/// offers only a switch because that is all the mod knows about lights.
/// </para>
/// </remarks>
sealed class Rig
{
    readonly Grid _grid = new Grid("Test Rig");

    /// <summary>Every grid of the construct, the one the block Sprocket runs in stands on first.</summary>
    readonly List<Grid> _grids = new List<Grid>();

    /// <summary>The grid a block is welded to next.</summary>
    Grid _welding;

    /// <summary>
    /// A construct with nothing on it but the block Sprocket runs in.
    /// </summary>
    /// <remarks>
    /// What a test wants: exactly the blocks it is about, welded on by name, so what it asserts is
    /// not mixed in with a demonstration ship.
    /// </remarks>
    public Rig() : this(100)
    {
    }

    /// <summary>
    /// The same, with the block Sprocket runs in given a particular id.
    /// </summary>
    /// <remarks>
    /// So a test can cut that block and paste it back: the game gives the arrival a new id, and
    /// every toolbar slot on the construct goes on naming the one that left.
    /// </remarks>
    public Rig(long sprocket)
    {
        _grids.Add(_grid);
        _welding = _grid;
        Sprocket = _grid.Weld(new Programmable(sprocket, "Automation",
            Type<MyObjectBuilder_MyProgrammableBlock>(), "LargeProgrammableBlock",
            "Programmable Block"));
    }

    /// <summary>
    /// The ship testers are given.
    /// </summary>
    /// <remarks>
    /// Chosen to support the three tasks the prototype is meant to answer: open the hangar when a
    /// ship approaches, build a working airlock, and flash the lights when hydrogen runs low.
    /// </remarks>
    public static Rig Demo()
    {
        return Demo(new Rig());
    }

    /// <summary>The same ship, with the block Sprocket runs in given a particular id.</summary>
    public static Rig Demo(long sprocket)
    {
        return Demo(new Rig(sprocket));
    }

    static Rig Demo(Rig rig)
    {

        rig.Weld(new Door(1, "Hangar Door", Type<MyObjectBuilder_AirtightHangarDoor>(),
            "AirtightHangarDoor", "Hangar Door"));
        rig.Weld(new Door(2, "Airlock Inner Door", Type<MyObjectBuilder_Door>(),
            "LargeBlockDoor", "Door"));
        rig.Weld(new Door(3, "Airlock Outer Door", Type<MyObjectBuilder_Door>(),
            "LargeBlockDoor", "Door"));
        rig.Weld(new Vent(4, "Airlock Vent", Type<MyObjectBuilder_AirVent>(),
            "SmallAirVent", "Air Vent"));

        Light(rig, 10, "Landing Lights");
        Light(rig, 11, "Airlock Light");
        Light(rig, 12, "Cockpit Light");
        // A beacon, answering the game's own beacon interface and carrying the value that makes it
        // one: how far it is heard. A light carries a Radius too, and the two mean different things
        // by the word - which is the whole reason a block gets to say what it meant.
        rig.Weld(new Beacon(13, "Warning Beacon", Type<MyObjectBuilder_Beacon>(),
                "LargeBlockBeacon", "Beacon")
            .Carrying("Radius", "Radius", ValueKind.Real, "1", "50000"));

        // A value that may only be one of a listed few, which is the last shape a block's own value
        // comes in. The game reports a charge mode as a whole number and keeps its three words on
        // the terminal control, and these are the game's own numbers for them.
        rig.Weld(new Plain(14, "Standby Battery", Type<MyObjectBuilder_BatteryBlock>(),
                "LargeBlockBatteryBlock", "Battery"))
            .Carrying("ChargeMode", "Charge Mode", "Auto", "0", "Recharge", "1", "Discharge", "2");

        // And one with more of them than a row can hold, which is where the list has to fold into a
        // box. The game's own numbers again, from the six directions a block can face.
        rig.Weld(new Plain(15, "Tug Autopilot", Type<MyObjectBuilder_RemoteControl>(),
                "LargeBlockRemoteControl", "Remote Control"))
            .Carrying("Direction", "Forward Direction", "Forward", "0", "Backward", "1",
                "Left", "2", "Right", "3", "Up", "4", "Down", "5");

        // Carrying something Sprocket has never heard of, which is what a modded block looks like
        // from here: the member is offered like any other and grouped under Other.
        rig.Weld(new Plain(20, "Approach Sensor", Type<MyObjectBuilder_SensorBlock>(),
                "LargeBlockSensor", "Sensor")
            .Carrying("ChimeOnApproach", "Chime On Approach", ValueKind.Flag));
        rig.Weld(new Panel(21, "Airlock Button", Type<MyObjectBuilder_ButtonPanel>(),
            "ButtonPanelLarge", "Button Panel", 1));
        // A block Sprocket knows nothing about beyond its switch, which is most of a ship. What it
        // can be told to do is whatever it declares, and the tank declares the game's own stockpile
        // pair - so this is the floor the whole vocabulary stands on, on the rig.
        rig.Weld(new Plain(22, "Hydrogen Tank", Type<MyObjectBuilder_OxygenTank>(),
                "LargeHydrogenTank", "Hydrogen Tank"))
            .Offering("Stockpile", "Stockpile On/Off")
            .Offering("Stockpile_On", "Stockpile On")
            .Offering("Stockpile_Off", "Stockpile Off");

        return rig;
    }

    /// <summary>Welds a block on, as building one does.</summary>
    public T Weld<T>(T fitting) where T : Fitting
    {
        return _welding.Weld(fitting);
    }

    /// <summary>
    /// Welds what comes next onto the head of <paramref name="rotor" />, giving it a head on a grid of
    /// its own where it has none.
    /// </summary>
    public void OnHeadOf(Rotor rotor)
    {
        if (rotor.TopGrid == null)
        {
            var head = new Grid(rotor.Name + " Head", _grid.Journal);
            head.Top((IMyMechanicalConnectionBlock)rotor.Block);
            rotor.TopGrid = head.Block;
            _grids.Add(head);
        }

        for (var i = 0; i < _grids.Count; i++)
        {
            if (_grids[i].Block == rotor.TopGrid)
                _welding = _grids[i];
        }
    }

    /// <summary>Welds what comes next onto the grid the block Sprocket runs in stands on.</summary>
    public void OnBase()
    {
        _welding = _grid;
    }

    /// <summary>
    /// Performs a block's own action on it, by id, as the game does when a slot invokes one.
    /// </summary>
    public void Perform(long blockId, string actionId)
    {
        foreach (Fitting fitting in Fitted)
        {
            if (fitting.Id != blockId)
                continue;

            if (fitting.Offers(actionId))
                fitting.Perform(actionId);

            return;
        }
    }

    /// <summary>Grinds one off, which leaves whatever named it pointing at nothing.</summary>
    public void Grind(Fitting fitting)
    {
        for (var i = 0; i < _grids.Count; i++)
            _grids[i].Grind(fitting);
    }

    /// <summary>
    /// A light, which is the block the value work is proved against.
    /// </summary>
    /// <remarks>
    /// A light declares its colour setter as three values, each of them text, with nothing saying
    /// they are a colour's channels - which is what compatibility with the game's own actions
    /// actually looks like.
    /// <para>
    /// Every light and not one of them, because a definition declares the same actions whichever
    /// block of it is asked, and Sprocket builds a kind from the first one it sees. One light that
    /// knew more than its siblings would be a rig telling Sprocket something the game never does.
    /// </para>
    /// </remarks>
    static void Light(Rig rig, long id, string name)
    {
        rig.Weld(new Lamp(id, name, Type<MyObjectBuilder_InteriorLight>(),
                "LargeBlockLight_1corner", "Interior Light"))
            .Offering("SetRadius", "Set Radius", 1)
            .Offering("SetColor", "Set Color", 3)

            // The values it carries, which is where most of what a light can be told lives: the
            // game hands out an action for three of these and none at all for the rest. One of each
            // kind a block can report, so every control a form can raise is reachable from here.
            .Carrying("OnOff", "On", ValueKind.Flag)
            // The bounds this block really declares, from LargeBlockLight_1corner's own definition,
            // rather than numbers that look plausible. A rig that invents them is telling Sprocket
            // something the game never said, which is the whole thing a rig must not do.
            .Carrying("Radius", "Radius", ValueKind.Real, "0.1", "20")
            .Carrying("Falloff", "Falloff", ValueKind.Real, "0", "2")
            .Carrying("Color", "Color", ValueKind.Color);
    }

    /// <summary>A door, named, with the game's own type behind it.</summary>
    public Door Door(long id, string name)
    {
        return Weld(new Door(id, name, Type<MyObjectBuilder_Door>(), "LargeBlockDoor", "Door"));
    }

    public Rotor Rotor(long id, string name)
    {
        return Weld(new Rotor(id, name, Type<MyObjectBuilder_MotorStator>(), "LargeStator", "Rotor"));
    }

    public Vent Vent(long id, string name)
    {
        return Weld(new Vent(id, name, Type<MyObjectBuilder_AirVent>(), "SmallAirVent", "Air Vent"));
    }

    /// <summary>A button panel, named, with as many buttons as asked for.</summary>
    public Panel Panel(long id, string name, int buttons = 1)
    {
        return Weld(new Panel(id, name, Type<MyObjectBuilder_ButtonPanel>(), "ButtonPanelLarge",
            "Button Panel", buttons));
    }

    /// <summary>A block with nothing but a switch, which is most of a ship.</summary>
    public Piston Piston(long id, string name)
    {
        return Weld(new Piston(id, name, Type<MyObjectBuilder_ExtendedPistonBase>(), "LargePistonBase", "Piston"));
    }

    /// <summary>A hinge, which the game builds as a rotor that says it is a hinge.</summary>
    public Rotor Hinge(long id, string name)
    {
        return Weld(new Rotor(id, name, Type<MyObjectBuilder_MotorAdvancedStator>(), "LargeHinge", "Hinge"));
    }

    /// <summary>A beacon, answering the game's own beacon interface.</summary>
    public Beacon Beacon(long id, string name)
    {
        return Weld(new Beacon(id, name, Type<MyObjectBuilder_Beacon>(), "LargeBlockBeacon", "Beacon"));
    }

    /// <summary>A battery, named, empty and on auto.</summary>
    public Battery Battery(long id, string name)
    {
        return Weld(new Battery(id, name, Type<MyObjectBuilder_BatteryBlock>(), "LargeBlockBatteryBlock",
            "Battery"));
    }

    /// <summary>A hydrogen tank, named, empty and not stockpiling.</summary>
    public Tank Tank(long id, string name)
    {
        return Weld(new Tank(id, name, Type<MyObjectBuilder_OxygenTank>(), "LargeHydrogenTank",
            "Hydrogen Tank"));
    }

    /// <summary>A sound block, named, with no sound chosen.</summary>
    public Speaker Speaker(long id, string name)
    {
        return Weld(new Speaker(id, name, Type<MyObjectBuilder_SoundBlock>(), "LargeBlockSoundBlock",
            "Sound Block"));
    }

    public Lamp Light(long id, string name)
    {
        return Weld(new Lamp(id, name, Type<MyObjectBuilder_InteriorLight>(),
            "LargeBlockLight_1corner", "Interior Light"));
    }

    /// <summary>The block Sprocket runs in, which is what the mod is handed.</summary>
    public Programmable Sprocket { get; private set; }

    /// <summary>Every block on every grid of the construct.</summary>
    public IReadOnlyList<Fitting> Fitted
    {
        get
        {
            if (_grids.Count == 1)
                return _grid.Fitted;

            var all = new List<Fitting>();
            for (var i = 0; i < _grids.Count; i++)
                all.AddRange(_grids[i].Fitted);

            return all;
        }
    }

    /// <summary>Everything the construct has been told to do, oldest first.</summary>
    public List<string> Log => _grid.Journal;

    /// <summary>Forgets what was done, so a test can watch one moment.</summary>
    public void Forget()
    {
        _grid.Journal.Clear();
    }

    /// <summary>
    /// Puts the harness behind the statics the mod reaches the world through.
    /// </summary>
    /// <remarks>
    /// Only what the world layer reads: which grids are mechanically one construct, and whose game
    /// this is. Everything else on the gateway stays null, which is exactly what the mod tolerates
    /// on a machine that is not a server.
    /// </remarks>
    public void Install()
    {
        // Every toolbar read and write the mod makes, answered over this rig's own slots.
        Toolbars.Use(new RigSlots(this));

        MyAPIGateway.GridGroups = Pretend.To<IMyGridGroups>()
            .Does("GetGroup", arguments =>
            {
                var into = arguments[2] as List<IMyCubeGrid>;
                if (into != null)
                {
                    for (var i = 0; i < _grids.Count; i++)
                        into.Add(_grids[i].Block);
                }

                return null;
            })
            .Done();

        MyAPIGateway.Multiplayer = Pretend.To<IMyMultiplayer>()
            .Gives("MyName", () => Environment.UserName)
            .Gives("IsServer", () => true)
            .Done();

        // Only where nothing has put one up already: a test that draws screens has a session of its
        // own with a camera and a player on it, and both report the same frame. See Clock.
        if (MyAPIGateway.Session == null)
        {
            MyAPIGateway.Session = Pretend.To<IMySession>()
                .Gives("GameplayFrameCounter", () => Clock.Frame)
                .Done();
        }
    }

    public void Tick()
    {
        Clock.Tick();
        for (var i = 0; i < _grids.Count; i++)
            _grids[i].Tick();
    }

    static MyObjectBuilderType Type<T>()
    {
        return new MyObjectBuilderType(typeof(T));
    }
}
