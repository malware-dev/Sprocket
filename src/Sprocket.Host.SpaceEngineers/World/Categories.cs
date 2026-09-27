
namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// Which category one of the game's own members belongs to, asked by its id.
    /// </summary>
    /// <remarks>
    /// <b>By id rather than by block.</b> Space Engineers names a member the same way wherever it
    /// appears - every functional block's switch is the same pair of ids, every light's colour the
    /// same one - so one table covers every block that uses them, including blocks nobody has
    /// written anything else about. A kind that means something different by a shared id says so
    /// itself with <see cref="EntityKind.In" />, which is the extension's business rather than this
    /// table's.
    /// <para>
    /// A switch rather than a dictionary: this is a constant, asked once per member while a block's
    /// vocabulary is worked out, and a table of a few hundred strings that nothing ever writes to is
    /// better spelled where it can be read than built at startup.
    /// </para>
    /// <para>
    /// The ids are the game's own, transcribed. An id this does not know falls to
    /// <see cref="Category.Other" />, which is a place in the list rather than a failure.
    /// </para>
    /// </remarks>
    public static class Categories
    {
        /// <summary>
        /// What the member with this id is about.
        /// </summary>
        /// <remarks>
        /// The game builds a great many ids out of a few: a slider called <c>Radius</c> also yields
        /// <c>IncreaseRadius</c>, <c>DecreaseRadius</c>, <c>ResetRadius</c> and <c>SetRadius</c>, and
        /// a switch called <c>Open</c> also yields <c>Open_On</c> and <c>Open_Off</c>. Those are cut
        /// back to the stem and the stem is asked about, which is most of the coverage a table this
        /// size gets.
        /// </remarks>
        public static Category Of(string id)
        {
            if (string.IsNullOrEmpty(id))
                return Category.Other;

            string plain = Plain(Stem(id));

            // A few families are built one id per definition, so there is no list of them to hold:
            // a targeting group per targeting-group definition, a message per slot on a broadcast
            // controller, a switch per thing a safe zone forbids.
            if (Starts(plain, "targetinggroup"))
                return Category.Weapons;
            // A safe zone spends most of its ids on one switch per thing it forbids - the game spells
            // them with a Cb for checkbox - and the rest on the wall itself.
            if (Starts(plain, "safezone"))
            {
                if (Ends(plain, "cb") || plain == "safezonecreate")
                    return Category.Access;
                if (Ends(plain, "color") || Ends(plain, "texturecombo"))
                    return Category.Appearance;

                return Category.Behaviour;
            }
            if (Starts(plain, "detect"))
                return Category.Behaviour;
            if (Starts(plain, "transmitmessage") || Starts(plain, "message"))
                return Category.Signals;
            if (Starts(plain, "flightmode"))
                return Category.Behaviour;
            if (Starts(plain, "offensivecombat") || Starts(plain, "attackmode")
                || Starts(plain, "targetpriority"))
                return Category.Weapons;
            if (Starts(plain, "filter"))
                return Category.Signals;

            return Known(plain);
        }

        /// <summary>
        /// Whether an id is the game's name for the member <paramref name="stem" /> names.
        /// </summary>
        /// <remarks>
        /// For the blocks that mean something of their own by a shared id. One member answers to
        /// several spellings, so a block saying what its <c>Radius</c> is about has to catch
        /// <c>IncreaseRadius</c> and <c>SetRadius</c> with it.
        /// </remarks>
        public static bool Names(string id, string stem)
        {
            return !string.IsNullOrEmpty(id) && Plain(Stem(id)) == Plain(Stem(stem));
        }

        /// <summary>The id with the prefixes and suffixes the game generates taken off.</summary>
        static string Stem(string id)
        {
            var from = 0;
            var to = id.Length;

            if (Starts(id, "Increase") || Starts(id, "Decrease"))
                from = 8;
            else if (Starts(id, "Reset"))
                from = 5;
            else if (Starts(id, "Set"))
                from = 3;

            if (Ends(id, "_On"))
                to -= 3;
            else if (Ends(id, "_Off"))
                to -= 4;

            return to <= from ? id : id.Substring(from, to - from);
        }

        /// <summary>
        /// Whether the id names growing a part onto the machine rather than working the machine.
        /// </summary>
        /// <remarks>
        /// Read rather than listed, because a block offers more of these than it can use and the
        /// list is not fixed: a rotor declares a head in three sizes and a hinge head besides, and
        /// the game adds a spelling whenever it adds a part. What they have in common is the whole
        /// of what they are - something added, and the thing added is a head or a top part.
        /// </remarks>
        internal static bool Builds(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            var plain = Plain(id);
            return Starts(plain, "add") && (Ends(plain, "toppart") || Ends(plain, "head"));
        }

        /// <summary>
        /// The stem with nothing in it but lower-case letters and digits.
        /// </summary>
        /// <remarks>
        /// One member reaches this table spelled several ways. A slider's own id keeps the spaces a
        /// person reads - <c>Blink Interval</c>, <c>Speed Limit</c> - while the action that sets it
        /// drops them, and the game is inconsistent about the first letter besides
        /// (<c>useConveyor</c> against <c>UseConveyor</c>). Taking both out is what lets one entry
        /// answer for all of them.
        /// </remarks>
        static string Plain(string stem)
        {
            var kept = new char[stem.Length];
            var at = 0;

            for (var i = 0; i < stem.Length; i++)
            {
                char one = stem[i];
                if (one >= 'A' && one <= 'Z')
                    kept[at++] = (char)(one + 32);
                else if ((one >= 'a' && one <= 'z') || (one >= '0' && one <= '9'))
                    kept[at++] = one;
            }

            return new string(kept, 0, at);
        }

        static bool Starts(string id, string part)
        {
            return id.Length > part.Length && string.CompareOrdinal(id, 0, part, 0, part.Length) == 0;
        }

        static bool Ends(string id, string part)
        {
            return id.Length > part.Length
                && string.CompareOrdinal(id, id.Length - part.Length, part, 0, part.Length) == 0;
        }

        static Category Known(string plain)
        {
            switch (plain)
            {
                // Running, and how it is fed.
                case "onoff":
                case "chargemode":
                case "recharge":
                case "discharge":
                case "auto":
                    return Category.Power;

                // Joints, wheels, and what they are told to do.
                case "velocity":
                case "upperlimit":
                case "lowerlimit":
                case "rotorlock":
                case "hingelock":
                case "attach":
                case "detach":
                case "reverse":
                case "extend":
                case "retract":
                case "rotatetoangle":
                case "andmove":
                case "addtoppart":
                case "addrotortoppart":
                case "addmediumrotortoppart":
                case "addsmallrotortoppart":
                case "addhingetoppart":
                case "addmediumhingetoppart":
                case "addsmallhingetoppart":
                case "shareinertiatensor":
                case "propulsionoverride":
                case "steeroverride":
                    return Category.Movement;

                // Letting something through.
                case "open":
                    return Category.Opening;

                // Holding fast to something else, which a door does not do and a gear does.
                case "lock":
                case "unlock":
                case "switchlock":
                    return Category.Locking;

                // Left to itself, this is what it does.
                case "autolock":
                case "showonhud":
                case "showinterminal":
                case "showininventory":
                case "showintoolbarconfig":
                case "searchable":
                case "minheightaboveterrain":
                case "maxpitchangle":
                case "maxrollangle":
                case "idrepeatchekbox":
                case "idshowpathcheckbox":
                case "idshowpointscheckbox":
                case "idrecordstepsslider":
                case "idminimaldistanceslider":
                case "range":
                case "speedlimit":
                case "targetlock":
                case "powerdependency":
                case "steering":
                case "propulsion":
                case "braking":
                case "height":
                case "gravity":
                case "width":
                case "depth":
                case "virtualmass":
                case "restitution":
                case "flightmode":
                case "power":
                case "airshock":
                case "invertsteering":
                case "invertpropulsion":
                case "maxsteerangle":
                case "friction":
                case "strength":
                case "controlthrusters":
                case "controlwheels":
                case "controlgyros":
                case "maincockpit":
                case "mainremotecontrol":
                case "horizonindicator":
                case "targetlocking":
                case "direction":
                case "forward":
                case "backward":
                case "up":
                case "down":
                case "useconveyor":
                case "detectplayers":
                case "detectfloatingobjects":
                case "detectsmallships":
                case "detectlargeships":
                case "detectstations":
                case "detectsubgrids":
                case "detectasteroids":
                case "detectowner":
                case "detectfriendly":
                case "detectneutral":
                case "detectenemy":
                case "audibleproximityalert":
                case "enablepanning":
                case "isperm":
                case "showshipname":
                case "useantenna":
                case "broadcasttarget":
                case "silent":
                case "slavemode":
                case "terrainclearingmode":
                case "requiretargetlock":
                case "invertcamera":
                case "maximpulseaxis":
                case "maximpulsenonaxis":
                case "safetydetach":
                case "torque":
                case "brakingtorque":
                case "displacement":
                case "breakforce":
                case "autounlocktime":
                case "enableparking":
                case "autodeploy":
                case "autodeployheight":
                case "autorefill":
                case "refill":
                case "automaticallypullbottles":
                case "enableidlemovement":
                case "safety":
                case "keepprojection":
                case "instantbuilding":
                case "showonlybuildable":
                case "trading":
                case "powertransferoverride":
                case "collisionavoidance":
                case "dockingmode":
                case "aligntogravity":
                case "activatebehavior":
                case "suntracking":
                case "targetmeteors":
                case "targetmissiles":
                case "targetsmallships":
                case "targetlargeships":
                case "targetcharacters":
                case "targetstations":
                case "targetneutrals":
                case "targetfriends":
                case "targetenemies":
                case "enabletargetlocking":
                case "cantargetcharacters":
                case "targetpriority":
                case "attackpattern":
                case "locktarget":
                case "flee":
                case "fleemode":
                case "fleedistance":
                case "evasivemaneuvers":
                case "evasivemaneuverangle":
                case "evasivemaneuverintervalrange":
                    return Category.Behaviour;

                // Flying the construct.
                case "override":
                case "yaw":
                case "pitch":
                case "roll":
                case "handbrake":
                case "park":
                case "dampenersoverride":
                case "autopilot":
                case "cameralist":
                case "jump":
                case "jumpdistance":
                case "gravityacceleration":
                case "control":
                    return Category.Flight;

                // What a block holds and what it does with it.
                case "collectall":
                case "throwout":
                case "drainall":
                case "blacklistwhitelist":
                case "currentlist":
                case "candidateslist":
                case "searchfield":
                    return Category.Stock;

                // Air, and the tanks that hold it.
                case "depressurize":
                case "stockpile":
                    return Category.Air;

                // Making things.
                case "cooperativemode":
                case "repeat":
                case "disassembleall":
                case "spawnprojection":
                case "markmissingblocks":
                case "markunfinishedblocks":
                case "getownership":
                case "numberofprojections":
                case "numberofblocks":
                case "x":
                case "y":
                case "z":
                case "rotx":
                case "roty":
                case "rotz":
                case "scale":
                case "repairservicerange":
                case "salvageservicerange":
                case "precisionmodetoggle":
                case "showpivottoggle":
                case "offsetx":
                case "offsety":
                case "offsetz":
                    return Category.Making;

                // Shooting.
                case "shoot":
                case "shootonce":
                case "detonate":
                case "startcountdown":
                case "stopcountdown":
                case "selectedammotype":
                case "cycleselectedammotype":
                case "forcereload":
                case "focuslockedtarget":
                case "launchhatch":
                case "detonationtime":
                case "multiplieraz":
                case "multiplierel":
                case "angledeviation":
                case "ai":
                case "rotorazimuth":
                case "rotorelevation":
                case "toollist":
                case "selectedtoolslist":
                case "waypointzonesize":
                case "updateinterval":
                case "countdown":
                case "delay":
                case "enablerestoration":
                    return Category.Weapons;

                // Watching for something.
                case "left":
                case "right":
                case "bottom":
                case "top":
                case "back":
                case "front":
                case "view":
                case "viewrotation":
                case "threshold":
                case "andcheckbox":
                case "availableevents":
                case "condition":
                case "searchbox":
                case "angle":
                case "speed":
                case "naturalgravitychangedslider":
                case "missilelockeddistanceslider":
                case "surfaceheightslider":
                    return Category.Detection;

                // Blocks whose job is to set other blocks going.
                case "triggerdelay":
                case "triggernow":
                case "start":
                case "stop":
                case "run":
                case "runwithdefaultargument":
                case "consolecommand":
                case "selectmission":
                case "idmodeselectorcombobox":
                case "currentpositionhome":
                case "currentgridpositionhome":
                case "followhomeminrange":
                case "followhomemaxrange":
                case "wander":
                case "followme":
                case "stopfollowing":
                case "followdistance":
                case "idplaycheckbox":
                case "idrecordcheckbox":
                case "idaddwaypoint":
                case "reversepath":
                    return Category.Automation;

                // Talking to other constructs.
                case "enablebroadcast":
                case "broadcasttargeteveryone":
                case "broadcasttargetowner":
                case "broadcasttargetfaction":
                case "transmitrandommessage":
                case "sendgps":
                case "gpscoords":
                case "pastegpscoords":
                case "connectgps":
                case "idle":
                case "receivefrom":
                case "sendsignal":
                case "channel":
                case "channelvalue":
                case "channelvaluesendsignal":
                case "broadcastusingantennas":
                    return Category.Signals;

                // What it looks like from outside.
                case "color":
                case "radius":
                case "falloff":
                case "intensity":
                case "offset":
                case "blinkinterval":
                case "blinklenght":
                case "blinkoffset":
                case "rotationspeed":
                case "colormin":
                case "colormax":
                case "effectscombo":
                    return Category.Appearance;

                // What it writes on itself.
                case "content":
                case "title":
                case "font":
                case "fontsize":
                case "fontcolor":
                case "alignment":
                case "textpaddingslider":
                case "backgroundcolor":
                case "scriptforegroundcolor":
                case "scriptbackgroundcolor":
                case "changeintervalslider":
                case "preserveaspectratio":
                case "rotate":
                case "showtextpanel":
                    return Category.Screen;

                // Noises.
                case "volumeslider":
                case "rangeslider":
                case "loopableslider":
                case "playsound":
                case "stopsound":
                    return Category.Sound;

                // What it is called.
                case "name":
                case "customname":
                case "customdata":
                case "hudtext":
                case "buttonname":
                    return Category.Identity;

                // Who may use it.
                case "anyonecanuse":
                case "spawnname":
                case "takeownership":
                case "faction":
                    return Category.Access;

                default:
                    return Category.Other;
            }
        }
    }
}
