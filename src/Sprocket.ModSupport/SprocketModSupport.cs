using System;

namespace Sprocket.ModSupport
{
    /// <summary>A mod's way to make its blocks work with Sprocket: one registration, made as the mod loads.</summary>
    /// <example>
    /// <code>
    /// _sprocket = SprocketModSupport.Register(new SprocketMod
    ///     {
    ///         Namespace = "malforge.hover_repulsors",
    ///         Name = "Hover Repulsors",
    ///         Author = "Malware",
    ///     },
    ///     api =>
    ///     {
    ///         var controller = api.Block("FunctionalBlock", "HoverControllerLarge").InGroup("Hover");
    ///         controller.Number("ride_height", b => Height(b), (b, v) => SetHeight(b, v))
    ///                   .Named("ride height", "How high the vehicle rides above the ground.").Unit("m");
    ///     });
    /// </code>
    /// </example>
    public static partial class SprocketModSupport
    {
        static readonly ISprocketWiring Wiring = new Key();

        /// <summary>
        /// Registers <paramref name="mod" /> with Sprocket: <paramref name="build" /> describes its blocks once Sprocket
        /// answers, whichever of the two loads first, and never runs in a world without Sprocket.
        /// </summary>
        /// <remarks>
        /// Called from the mod's session component's <c>LoadData</c> or later, never a constructor or static field; the result
        /// is disposed in <c>UnloadData</c>. A bad namespace or a missing name throws here; a mistake in what
        /// <paramref name="build" /> describes refuses the registration, logged under the mod's name.
        /// </remarks>
        public static IDisposable Register(SprocketMod mod, Action<SprocketModBuilder> build)
        {
            var registration = new SprocketRegistration(Wiring, mod, build);
            ((ISprocketRegistry)registration).Listen();
            return registration;
        }
    }
}
