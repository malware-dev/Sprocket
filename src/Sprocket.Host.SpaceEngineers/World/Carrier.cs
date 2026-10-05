using System;
using System.Globalization;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>Where the world keeps Sprocket's backups: an entity of the mod's own, saved with the world and tied to no block.</summary>
    /// <remarks>
    /// A plain entity, which the game saves into the sector and so into every backup of the world, sends to no client and
    /// leaves out of its cleanup; it sits at the origin with no model, no physics and no place in the spatial index. Only
    /// the server has one. It exists because world storage does not reach the game's backups of a world; where that
    /// changes, this is the one type to replace.
    /// </remarks>
    public sealed class Carrier
    {
        /// <summary>
        /// Where the backups sit in the entity's mod storage.
        /// </summary>
        /// <remarks>
        /// Claimed in <c>Content/Data/EntityComponents.sbc</c>, without which the game drops it at save, and never to change:
        /// another key reads as a world with no backups.
        /// </remarks>
        static readonly Guid Key = new Guid("24B4675F-1A0D-410A-8044-BFD7124C8588");

        /// <summary>The world variable that holds the carrier's entity id.</summary>
        /// <remarks>
        /// One number, so its going to every client that joins costs nothing; the backups stay in the carrier, which reaches
        /// no client. Never to change: another name leaves every world's carrier unfound.
        /// </remarks>
        const string Variable = "sprocket.carrier";

        readonly IMyEntity _entity;

        Carrier(IMyEntity entity)
        {
            _entity = entity;

            // Not saved by the game, so set on every carrier as it is made or found. A preview is never made ready to send
            // to clients, which a plain entity is not sent to anyway; it guards against that changing.
            var plain = entity as MyEntity;
            if (plain != null)
                plain.IsPreview = true;
        }

        /// <summary>What the carrier holds.</summary>
        public string Text
        {
            get
            {
                string text;
                return _entity.Storage != null && _entity.Storage.TryGetValue(Key, out text) && text != null ? text : "";
            }
        }

        /// <summary>Keeps <paramref name="text" /> in the carrier.</summary>
        public void Write(string text)
        {
            _entity.Storage[Key] = text;
        }

        /// <summary>The world's carrier: the entity the world's variable names, where it holds the backups' key, or null for a world with no backups yet.</summary>
        /// <remarks>
        /// A saved world loads its own variables, but a new one starts with those of the world the game ran before it, so the
        /// id can name nothing or another world's entity; the key is what is trusted.
        /// </remarks>
        public static Carrier Find()
        {
            string said;
            long id;
            IMyEntity entity;
            return MyAPIGateway.Utilities.GetVariable(Variable, out said)
                   && long.TryParse(said, NumberStyles.Integer, CultureInfo.InvariantCulture, out id)
                   && MyAPIGateway.Entities.TryGetEntityById(id, out entity)
                   && entity.Storage != null && entity.Storage.ContainsKey(Key)
                ? new Carrier(entity)
                : null;
        }

        /// <summary>Makes the world's carrier, holding nothing yet.</summary>
        public static Carrier Make()
        {
            var entity = new MyEntity();
            entity.Init(null, null, null, null);
            entity.Save = true;
            entity.Storage = new MyModStorageComponent();
            entity.Storage[Key] = "";
            MyAPIGateway.Entities.AddEntity(entity);

            // As text, the one kind of variable the save takes without question. Made during a save, it still lands in it: the
            // save holds the live variables and writes them out once it has been built.
            MyAPIGateway.Utilities.SetVariable(Variable, entity.EntityId.ToString(CultureInfo.InvariantCulture));
            return new Carrier(entity);
        }
    }
}
