using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Game.Wiring;
using System;
using System.Collections.Generic;
using Castle.DynamicProxy;
using Sprocket.Sim.Fakes;
using Sprocket.Sim.Items;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>
/// The ship testers are given, as blocks the game would recognize.
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
public sealed class Rig
{
    readonly Grid _grid = new Grid("Test Rig");

    /// <summary>Every grid of the construct, the one the block Sprocket runs in stands on first.</summary>
    readonly List<Grid> _grids = new List<Grid>();

    readonly List<Grid> _parted = new List<Grid>();

    /// <summary>The grid a block is welded to next.</summary>
    Grid _welding;

    /// <summary>What the game would hand the mod for the construct's mechanical group, which says when a grid joins or leaves it.</summary>
    readonly IMyGridGroupData _group;

    Action<IMyGridGroupData, IMyCubeGrid, IMyGridGroupData> _joined;
    Action<IMyGridGroupData, IMyCubeGrid, IMyGridGroupData> _left;

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
        _group = Pretend.To<IMyGridGroupData>()
            .Answers("add_OnGridAdded", invocation => Listen(ref _joined, invocation, true))
            .Answers("remove_OnGridAdded", invocation => Listen(ref _joined, invocation, false))
            .Answers("add_OnGridRemoved", invocation => Listen(ref _left, invocation, true))
            .Answers("remove_OnGridRemoved", invocation => Listen(ref _left, invocation, false))
            .Does("add_OnReleased", () => null)
            .Does("remove_OnReleased", () => null)
            .Done();

        _grids.Add(_grid);
        _welding = _grid;
        Sprocket = Weld(new Programmable(sprocket, "Automation",
            Type<MyObjectBuilder_MyProgrammableBlock>(), "LargeProgrammableBlock",
            "Programmable Block"));
    }

    /// <summary>Which faction each player is in, for every block on the construct.</summary>
    public Factions Factions { get; } = new Factions();

    /// <summary>The players using every terminal as admins, for every block on the construct.</summary>
    public HashSet<long> Admins { get; } = new HashSet<long>();

    /// <summary>The game's items the mod lists and every inventory on the construct takes, or null for none, as in a harness with no definitions.</summary>
    public GameStock? Stock { get; private set; }

    /// <summary>Gives the construct the game's items in <paramref name="stock" />: the mod lists them, and each block's inventories take what its definition says, on the blocks welded on already and on those to come.</summary>
    public Rig Stocked(GameStock stock)
    {
        Stock = stock;
        RegisterKinds();
        foreach (var fitting in Fitted)
        {
            if (fitting is Stockroom room)
                Shelve(room);
        }

        ItemIds.Use(stock.Items);
        return this;
    }

    /// <summary>Has each of <paramref name="room" />'s inventories take what the game's definition of its block says, where the construct is stocked.</summary>
    Stockroom Shelve(Stockroom room)
    {
        if (Stock == null)
            return room;

        var definition = room.Definition;
        var type = definition.TypeIdString.StartsWith("MyObjectBuilder_", StringComparison.Ordinal)
            ? definition.TypeIdString.Substring("MyObjectBuilder_".Length)
            : definition.TypeIdString;
        for (var at = 0; at < room.Block.InventoryCount; at++)
            room.Accepts(Stock.Taken(type, definition.SubtypeName, at), at);

        return room;
    }

    /// <summary>Hands every block on the construct to <paramref name="identity" />, shared as <paramref name="share" /> says, as the game's transfer of a whole ship does.</summary>
    public void Own(long identity, Sharing share = Sharing.Nobody)
    {
        var fitted = Fitted;
        for (var i = 0; i < fitted.Count; i++)
        {
            fitted[i].Share = share;
            fitted[i].Owner = identity;
        }
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

    /// <summary>The ship testers are given, with the block Sprocket runs in given the id <paramref name="sprocket" />.</summary>
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
        rig.Cockpit(23, "Cockpit");
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
        fitting.Factions = Factions;
        fitting.Admins = Admins;
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
            var head = new Grid(rotor.Name + " Head", _grid.Journal, 9000 + _grids.Count);
            head.Top((IMyMechanicalConnectionBlock)rotor.Block);
            rotor.TopGrid = head.Block;
            _grids.Add(head);
            if (_joined != null)
                _joined(_group, head.Block, null);
        }

        foreach (var grid in _grids)
        {
            if (grid.Block == rotor.TopGrid)
                _welding = grid;
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

    /// <summary>Whether one of the rig's grids has the entity id <paramref name="entityId" />.</summary>
    public bool HasGrid(long entityId)
    {
        foreach (var grid in _grids)
        {
            if (grid.Block.EntityId == entityId)
                return true;
        }

        return false;
    }

    /// <summary>Grinds one off, which leaves whatever named it pointing at nothing.</summary>
    public void Grind(Fitting fitting)
    {
        for (var i = 0; i < _grids.Count; i++)
            _grids[i].Grind(fitting);
    }

    /// <summary>Lays a block of armor, which has no terminal, where the next block would be welded.</summary>
    public IMySlimBlock Armor()
    {
        return _welding.Armor();
    }

    /// <summary>Grinds <paramref name="armor" /> off whichever grid it was laid on.</summary>
    public void Strip(IMySlimBlock armor)
    {
        foreach (var grid in _grids)
        {
            if (grid.Strip(armor))
                return;
        }
    }

    /// <summary>Deletes the grid <paramref name="grid" /> whole, which leaves the construct without it.</summary>
    public void Delete(IMyCubeGrid grid)
    {
        for (var i = 0; i < _grids.Count; i++)
        {
            if (_grids[i].Block != grid)
                continue;

            _grids[i].Delete();
            _grids.RemoveAt(i);
            if (_left != null)
                _left(_group, grid, null);
            return;
        }
    }

    /// <summary>Splits <paramref name="part" /> off onto a grid of its own, outside the construct, as grinding the last joining block does.</summary>
    public Grid Split(IEnumerable<Fitting> part)
    {
        var grid = new Grid("Test Rig Part", _grid.Journal, 9500 + _parted.Count);
        foreach (var fitting in part)
        {
            foreach (var from in _grids)
                from.Release(fitting);

            grid.Take(fitting);
        }

        _parted.Add(grid);
        if (_left != null)
            _left(_group, grid.Block, null);

        return grid;
    }

    /// <summary>Merges <paramref name="part" /> into the construct's own grid, as merge blocks or a weld do.</summary>
    public void Merge(Grid part)
    {
        if (!_parted.Remove(part))
            return;

        foreach (var fitting in new List<Fitting>(part.Fitted))
        {
            part.Release(fitting);
            _grid.Take(fitting);
        }

        if (_joined != null)
            _joined(_group, part.Block, null);
    }

    /// <summary>Welds what comes next onto another ship, outside the construct.</summary>
    public Grid Apart(string name)
    {
        var grid = new Grid(name, _grid.Journal, 9700 + _parted.Count);
        _parted.Add(grid);
        _welding = grid;
        return grid;
    }

    /// <summary>Detaches the head of <paramref name="rotor" />, which floats away with whatever is on it, outside the construct.</summary>
    /// <remarks>No block is added or removed; the construct's mechanical group says the head's grid left it.</remarks>
    public void Detach(Rotor rotor)
    {
        for (var i = 0; i < _grids.Count; i++)
        {
            if (_grids[i].Block != rotor.TopGrid)
                continue;

            var head = _grids[i];
            _grids.RemoveAt(i);
            _parted.Add(head);
            rotor.TopGrid = null;
            if (_welding == head)
                _welding = _grid;

            if (_left != null)
                _left(_group, head.Block, null);
            return;
        }
    }

    static object Listen(ref Action<IMyGridGroupData, IMyCubeGrid, IMyGridGroupData> held,
        IInvocation invocation, bool adding)
    {
        var handler = (Delegate)invocation.Arguments[0];
        held = (Action<IMyGridGroupData, IMyCubeGrid, IMyGridGroupData>)(adding
            ? Delegate.Combine(held, handler)
            : Delegate.Remove(held, handler));
        return null;
    }

    /// <summary>
    /// A light, which is the block the value work is proved against.
    /// </summary>
    /// <remarks>
    /// A light declares its color setter as three values, each of them text, with nothing saying
    /// they are a color's channels - which is what compatibility with the game's own actions
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

    /// <summary>A connector, named, unlocked, with the game's own type behind it.</summary>
    public Coupling Connector(long id, string name)
    {
        return Weld(new Coupling(id, name, Type<MyObjectBuilder_ShipConnector>(), "Connector", "Connector"));
    }

    /// <summary>A magnetic plate, named, unlocked, with the game's own type behind it.</summary>
    public Clamp MagneticPlate(long id, string name)
    {
        return Weld(new Clamp(id, name, Type<MyObjectBuilder_LandingGear>(), "LargeBlockSmallMagneticPlate", "Magnetic Plate"));
    }

    /// <summary>A door of the game's subtype <paramref name="subtype" />, named.</summary>
    public Door Door(long id, string name, string subtype)
    {
        return Weld(new Door(id, name, Type<MyObjectBuilder_Door>(), subtype, "Door"));
    }

    /// <summary>A hangar door, named, with the game's own type behind it, which announces starting to move either way.</summary>
    public Door HangarDoor(long id, string name)
    {
        return Weld(new Door(id, name, Type<MyObjectBuilder_AirtightHangarDoor>(), "AirtightHangarDoor", "Hangar Door"));
    }

    /// <summary>A rotor, named, with the game's own type behind it.</summary>
    public Rotor Rotor(long id, string name)
    {
        return Weld(new Rotor(id, name, Type<MyObjectBuilder_MotorStator>(), "LargeStator", "Rotor"));
    }

    /// <summary>An air vent, named, with the game's own type behind it.</summary>
    public Vent Vent(long id, string name)
    {
        return Weld(new Vent(id, name, Type<MyObjectBuilder_AirVent>(), "SmallAirVent", "Air Vent"));
    }

    /// <summary>An air vent, named, of the game's <paramref name="subtype" />, such as "" for the large grid's.</summary>
    public Vent Vent(long id, string name, string subtype)
    {
        return Weld(new Vent(id, name, Type<MyObjectBuilder_AirVent>(), subtype, "Air Vent"));
    }

    /// <summary>A button panel, named, with as many buttons as asked for.</summary>
    /// <remarks>
    /// The game fixes the number of buttons by the block's definition, and a kind is described once per definition,
    /// so each count is a subtype of its own.
    /// </remarks>
    public Panel Panel(long id, string name, int buttons = 1)
    {
        var subtype = buttons == 1 ? "ButtonPanelLarge" : "ButtonPanelLarge" + buttons;
        return Weld(new Panel(id, name, Type<MyObjectBuilder_ButtonPanel>(), subtype, "Button Panel", buttons));
    }

    /// <summary>A programmable block, named, which is a block the automation can be kept in.</summary>
    public Programmable Programmable(long id, string name)
    {
        return Weld(new Programmable(id, name, Type<MyObjectBuilder_MyProgrammableBlock>(), "LargeProgrammableBlock",
            "Programmable Block"));
    }

    /// <summary>A timer block, named.</summary>
    public TimerBlock Timer(long id, string name)
    {
        return Weld(new TimerBlock(id, name, Type<MyObjectBuilder_TimerBlock>(), "TimerBlockLarge", "Timer Block"));
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

    /// <summary>A large 3x3 wheel suspension, named, with its wheel on.</summary>
    public Suspension Suspension(long id, string name)
    {
        return Weld(new Suspension(id, name, Type<MyObjectBuilder_MotorSuspension>(), "Suspension3x3", "Wheel Suspension 3x3"));
    }

    /// <summary>A large railgun, named, its capacitor empty.</summary>
    public Railgun Railgun(long id, string name)
    {
        return Weld(new Railgun(id, name, Type<MyObjectBuilder_SmallMissileLauncherReload>(), "LargeRailgun", "Large Railgun"));
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

    /// <summary>A large ion thruster, named, not overridden.</summary>
    public Thruster Thruster(long id, string name)
    {
        return Weld(new Thruster(id, name, Type<MyObjectBuilder_Thrust>(), "LargeBlockLargeThrust", "Large Ion Thruster"));
    }

    /// <summary>A jump drive, named, empty, recharging and set to its longest jump.</summary>
    public JumpDrive JumpDrive(long id, string name)
    {
        return Weld(new JumpDrive(id, name, Type<MyObjectBuilder_JumpDrive>(), "LargeJumpDrive", "Jump Drive"));
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

    /// <summary>A warhead, named, armed and not counting down, with the actions and values its terminal has.</summary>
    public Warhead Warhead(long id, string name)
    {
        var warhead = Weld(new Warhead(id, name, Type<MyObjectBuilder_Warhead>(), "LargeWarhead", "Warhead"));
        warhead.Offering("StartCountdown", "Start countdown")
            .Offering("StopCountdown", "Stop countdown")
            .Offering("Detonate", "Detonate")
            .Offering("Safety", "Toggle Arm/Disarm")
            .Offering("Safety_On", "Arm")
            .Offering("Safety_Off", "Disarm")
            .Offering("IncreaseDetonationTime", "Increase Detonation time")
            .Offering("DecreaseDetonationTime", "Decrease Detonation time")
            .Carrying("Safety", "Arm", ValueKind.Flag)
            .Carrying("DetonationTime", "Detonation time", ValueKind.Real, "1", "3600");
        return warhead;
    }

    /// <summary>A gatling turret, named, with the firing and target switches its terminal has.</summary>
    public Turret Turret(long id, string name)
    {
        var turret = Weld(new Turret(id, name, Type<MyObjectBuilder_LargeGatlingTurret>(), "LargeGatlingTurret",
            "Gatling Turret"));
        turret.Offering("Shoot", "Shoot On/Off")
            .Offering("Shoot_On", "Shoot On")
            .Offering("Shoot_Off", "Shoot Off")
            .Offering("ShootOnce", "Shoot once")
            .Offering("UseConveyor", "Use Conveyor System On/Off")
            .Offering("TargetNeutrals", "Target neutrals On/Off")
            .Offering("TargetNeutrals_On", "Target neutrals On")
            .Offering("TargetNeutrals_Off", "Target neutrals Off")
            .Offering("FocusLockedTarget", "Focus Locked Target")
            .Carrying("Shoot", "Shoot", ValueKind.Flag)
            .Carrying("UseConveyor", "Use Conveyor System", ValueKind.Flag)
            .Carrying("TargetNeutrals", "Target neutrals", ValueKind.Flag)
            .Carrying("Range", "Aiming radius", ValueKind.Real);
        return turret;
    }

    /// <summary>A remote control, named, with the autopilot and flight switches its terminal has.</summary>
    public Remote RemoteControl(long id, string name)
    {
        var remote = Weld(new Remote(id, name, Type<MyObjectBuilder_RemoteControl>(), "LargeBlockRemoteControl",
            "Remote Control"));
        remote.Offering("AutoPilot", "Autopilot On/Off")
            .Offering("AutoPilot_On", "Autopilot On")
            .Offering("AutoPilot_Off", "Autopilot Off")
            .Offering("Reset", "Reset waypoint")
            .Offering("CollisionAvoidance", "Collision avoidance On/Off")
            .Offering("CollisionAvoidance_On", "Collision avoidance On")
            .Offering("CollisionAvoidance_Off", "Collision avoidance Off")
            .Offering("HandBrake", "Handbrake On/Off")
            .Offering("HandBrake_On", "Handbrake On")
            .Offering("HandBrake_Off", "Handbrake Off")
            .Offering("Park", "Park On/Off")
            .Offering("Park_On", "Park On")
            .Offering("Park_Off", "Park Off")
            .Offering("DampenersOverride", "Inertia dampeners On/Off")
            .Offering("DampenersOverride_On", "Inertia dampeners On")
            .Offering("DampenersOverride_Off", "Inertia dampeners Off")
            .Offering("ControlThrusters", "Control thrusters On/Off")
            .Offering("ControlWheels", "Control wheels On/Off")
            .Offering("ControlGyros", "Control gyroscopes On/Off")
            .Offering("MainCockpit", "Main cockpit On/Off")
            .Offering("MainRemoteControl", "Main remote control On/Off")
            .Offering("TargetLocking", "Enable target locking On/Off")
            .Offering("HorizonIndicator", "Show horizon and altitude On/Off")
            .Offering("IncreaseSpeedLimit", "Increase Speed limit")
            .Offering("DecreaseSpeedLimit", "Decrease Speed limit")
            .Offering("SetFlightMode_BlockPropertyTitle_FlightMode_Patrol", "Flight mode Patrol")
            .Offering("Forward", "Forward")
            .Carrying("AutoPilot", "Autopilot", ValueKind.Flag)
            .Carrying("CollisionAvoidance", "Collision avoidance", ValueKind.Flag)
            .Carrying("HandBrake", "Handbrake", ValueKind.Flag)
            .Carrying("Park", "Park", ValueKind.Flag)
            .Carrying("DampenersOverride", "Inertia dampeners", ValueKind.Flag)
            .Carrying("ControlThrusters", "Control thrusters", ValueKind.Flag)
            .Carrying("ControlWheels", "Control wheels", ValueKind.Flag)
            .Carrying("ControlGyros", "Control gyroscopes", ValueKind.Flag)
            .Carrying("MainCockpit", "Main cockpit", ValueKind.Flag)
            .Carrying("MainRemoteControl", "Main remote control", ValueKind.Flag)
            .Carrying("TargetLocking", "Enable target locking", ValueKind.Flag)
            .Carrying("HorizonIndicator", "Show horizon and altitude", ValueKind.Flag)
            .Carrying("SpeedLimit", "Speed limit", ValueKind.Real, "0", "100")
            .Carrying("FlightMode", "Flight mode", "Patrol", "0", "Circle", "1", "One way", "2");
        return remote;
    }

    /// <summary>Welds on a cargo container called <paramref name="name" />, with one inventory.</summary>
    public Stockroom Cargo(long id, string name)
    {
        return Shelve(Weld(new Stockroom(id, name, Type<VRage.Game.MyObjectBuilder_CargoContainer>(), "LargeBlockLargeContainer", "Large Cargo Container", 1)));
    }

    /// <summary>Welds on a refinery called <paramref name="name" />, with an input and an output.</summary>
    public Stockroom Refinery(long id, string name)
    {
        return Shelve(Weld(new Stockroom(id, name, Type<MyObjectBuilder_Refinery>(), "LargeRefinery", "Refinery", 2)));
    }

    /// <summary>An interior light, named, with the game's own type behind it.</summary>
    public Lamp Light(long id, string name)
    {
        return Weld(new Lamp(id, name, Type<MyObjectBuilder_InteriorLight>(),
            "LargeBlockLight_1corner", "Interior Light"));
    }

    /// <summary>A spotlight, named, of the subtype given: a plain one, or one of the two rotating warning lights.</summary>
    public Spotlight Spotlight(long id, string name, string subtype = "LargeBlockFrontLight")
    {
        return Weld(new Spotlight(id, name, Type<MyObjectBuilder_ReflectorLight>(), subtype, "Spotlight"));
    }

    /// <summary>A jukebox, named, which the game makes a sound block that only its definition tells apart.</summary>
    public Speaker Jukebox(long id, string name)
    {
        return Weld(new Speaker(id, name, Type<MyObjectBuilder_Jukebox>(), "Jukebox", "Jukebox"));
    }

    /// <summary>A cockpit, named, empty: an enclosed one with air of its own, or an open seat with none.</summary>
    public Seat Cockpit(long id, string name, bool enclosed = true)
    {
        return Weld(new Seat(id, name, Type<MyObjectBuilder_Cockpit>(), enclosed ? "LargeBlockCockpitSeat" : "OpenCockpitLarge",
            "Cockpit", enclosed ? 1f : 0f));
    }

    /// <summary>An enclosed cockpit, named, empty, with the six screens of the game's large cockpit and the apps it starts them on.</summary>
    public Seat ScreenedCockpit(long id, string name)
    {
        return Weld(new Seat(id, name, Type<MyObjectBuilder_Cockpit>(), "LargeBlockCockpitSeat", "Cockpit", 1f, faces:
        [
            Running(new Face("Top Center Screen"), "TSS_ArtificialHorizon"), Running(new Face("Top Left Screen"), "TSS_EnergyHydrogen"),
            Running(new Face("Top Right Screen"), "TSS_Gravity"), new Face("Keyboard"),
            Running(new Face("Bottom Left Screen"), "TSS_ClockAnalog"), new Face("Bottom Right Screen")
        ]));
    }

    /// <summary><paramref name="face" />, set to show the app <paramref name="app" />.</summary>
    static Face Running(Face face, string app)
    {
        face.Content = VRage.Game.GUI.TextPanel.ContentType.SCRIPT;
        face.App = app;
        return face;
    }

    /// <summary>A toilet, named, empty: a seat that cannot fly the ship.</summary>
    public Seat Toilet(long id, string name)
    {
        return Weld(new Seat(id, name, Type<MyObjectBuilder_Cockpit>(), "LargeBlockToilet", "Toilet", 0f, flies: false));
    }

    /// <summary>A cryo chamber, named, empty, holding air of its own.</summary>
    public Seat CryoChamber(long id, string name)
    {
        return Weld(new Seat(id, name, Type<MyObjectBuilder_CryoChamber>(), "LargeBlockCryoChamber", "Cryo Chamber", 1f, true));
    }

    /// <summary>A text panel, named, showing nothing.</summary>
    public TextScreen TextPanel(long id, string name)
    {
        return Weld(new TextScreen(id, name, Type<MyObjectBuilder_TextPanel>(), "LargeTextPanel", "Text Panel"));
    }

    /// <summary>A text panel of the game's subtype <paramref name="subtype" />, named.</summary>
    public TextScreen TextPanel(long id, string name, string subtype)
    {
        return Weld(new TextScreen(id, name, Type<MyObjectBuilder_TextPanel>(), subtype, "Text Panel"));
    }

    /// <summary>A camera, named, nobody looking.</summary>
    public Camera Camera(long id, string name)
    {
        return Weld(new Camera(id, name, Type<MyObjectBuilder_CameraBlock>(), "LargeCameraBlock", "Camera"));
    }

    /// <summary>An emotion controller, named.</summary>
    public Emoter EmotionController(long id, string name)
    {
        return Weld(new Emoter(id, name, Type<MyObjectBuilder_EmotionControllerBlock>(), "EmotionControlsBlock",
            "Emotion Controller"));
    }

    /// <summary>The block Sprocket runs in, which is what the mod is handed.</summary>
    public Programmable Sprocket { get; private set; }

    /// <summary>The name the game gives the player at this machine, which a blueprint made here names as its author: the user's own unless set.</summary>
    public string PlayerName { get; set; } = Environment.UserName;

    /// <summary>Every block on every grid of the construct.</summary>
    public IReadOnlyList<Fitting> Fitted
    {
        get
        {
            if (_grids.Count == 1)
                return _grids[0].Fitted;

            var all = new List<Fitting>();
            foreach (var grid in _grids)
                all.AddRange(grid.Fitted);

            return all;
        }
    }

    /// <summary>Every block in the world, whatever construct it is on.</summary>
    public IReadOnlyList<Fitting> InWorld
    {
        get
        {
            var all = new List<Fitting>(Fitted);
            foreach (var grid in _parted)
                all.AddRange(grid.Fitted);

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

    /// <summary>Registers the game's kinds of object by name, as the game does at start, so a block type or an item's kind reads from its name.</summary>
    /// <remarks>Ores, ingots, components and tools are in one assembly; blocks, bottles, datapads and seeds in the other.</remarks>
    static void RegisterKinds()
    {
        MyObjectBuilderType.RegisterFromAssembly(typeof(VRage.Game.MyObjectBuilder_Ingot).Assembly);
        MyObjectBuilderType.RegisterFromAssembly(typeof(MyObjectBuilder_Refinery).Assembly);
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
        Factions.Installed = Factions;

        // Every toolbar read and write the mod makes, answered over this rig's own slots.
        Toolbars.Use(new RigSlots(this));

        // And every reading of a vent's gas, over this rig's own vents.
        VentFlow.Use(new RigGas(this));

        // And every list of the game's items, over the game's own definitions where the construct is stocked with them.
        ItemIds.Use(Stock?.Items);

        // The game's log, as the game makes it at start. This one is never opened, so it takes every line and keeps none.
        MyLog.Default ??= new MyLog();

        RegisterKinds();

        MyAPIGateway.GridGroups = Pretend.To<IMyGridGroups>()
            .Does("GetGroup", arguments =>
            {
                // A grid that left the construct is a group of its own.
                var asked = arguments[0] as IMyCubeGrid;
                var into = arguments[2] as List<IMyCubeGrid>;
                if (into != null && asked != null && !_grids.Exists(grid => grid.Block == asked))
                    into.Add(asked);
                else if (into != null)
                {
                    foreach (var grid in _grids)
                        into.Add(grid.Block);
                }

                return null;
            })
            .Does("GetGridGroup", () => _group)
            .Done();

        // The game's own types, answered as the game answers a mod asking after them.
        MyAPIGateway.Reflection = Pretend.To<IMyReflection>()
            .Does("BaseTypeOf", arguments => ((Type)arguments[0]).BaseType)
            .Does("IsAssignableFrom", arguments => ((Type)arguments[0]).IsAssignableFrom((Type)arguments[1]))
            .Done();

        MyAPIGateway.Multiplayer = Pretend.To<IMyMultiplayer>()
            .Gives("MyName", () => PlayerName)
            .Gives("IsServer", () => true)
            .Done();

        // Only where nothing has put one up already: a test that draws screens has a session of its
        // own with a camera and a player on it, and both report the same frame. See Clock.
        if (MyAPIGateway.Session == null)
        {
            MyAPIGateway.Session = Pretend.To<IMySession>()
                .Gives("GameplayFrameCounter", () => Clock.Frame)
                .Gives("Player", () => null)
                .Gives("Factions", () => Factions.Installed?.Collection)
                .Answers("TryGetAdminSettings", NoAdmin)
                .Done();
        }
    }

    /// <summary>A moment of the rig running: the clock moves on, and every block on every grid has its moment.</summary>
    public void Tick()
    {
        Clock.Tick();
        for (var i = 0; i < _grids.Count; i++)
            _grids[i].Tick();

        // Every grid in the world moves, whatever construct it is on.
        for (var i = 0; i < _parted.Count; i++)
            _parted[i].Tick();
    }

    /// <summary>The game's answer for a player who is no admin: no admin settings.</summary>
    public static object NoAdmin(IInvocation call)
    {
        call.Arguments[1] = MyAdminSettingsEnum.None;
        return false;
    }

    static MyObjectBuilderType Type<T>()
    {
        return new MyObjectBuilderType(typeof(T));
    }
}
