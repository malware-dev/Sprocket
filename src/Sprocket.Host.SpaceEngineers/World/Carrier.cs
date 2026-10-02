using System;
using System.Collections.Generic;
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

        /// <summary>Finds the world's carrier, answering what was wrong where there is more than one.</summary>
        /// <remarks>
        /// One pass over the world's top-level entities - grids, characters, floating objects, asteroids and planets, never
        /// blocks - once, as the world loads. None found is a world with no backups yet.
        /// </remarks>
        public static string Find(out Carrier carrier)
        {
            carrier = null;
            var found = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(found, e => e.Storage != null && e.Storage.ContainsKey(Key));

            if (found.Count > 1)
                return "the world has " + found.Count + " carriers of backups, where it should have one";

            foreach (var entity in found)
                carrier = new Carrier(entity);

            return "";
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
            return new Carrier(entity);
        }
    }
}
