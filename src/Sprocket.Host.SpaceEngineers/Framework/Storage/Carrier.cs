using System;
using System.Globalization;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage
{
    /// <summary>Where the world's save stores Sprocket's files: an entity of the mod's own, saved with the world and tied to no block.</summary>
    /// <remarks>Only the server has one, and the game sends it to no client.</remarks>
    public sealed class Carrier
    {
        /// <summary>
        /// Where the files sit in the entity's mod storage.
        /// </summary>
        /// <remarks>
        /// Persisted, and claimed in <c>Content/Data/EntityComponents.sbc</c>, without which the game drops it at save: another
        /// key reads as a world with no files.
        /// </remarks>
        public static readonly Guid Key = new Guid("24B4675F-1A0D-410A-8044-BFD7124C8588");

        // Persisted: another name leaves every world's carrier unfound. Sent to every client that joins.
        const string Variable = "sprocket.carrier";

        readonly IMyEntity _entity;

        Carrier(IMyEntity entity)
        {
            _entity = entity;

            // Not saved by the game, so set on every carrier as it is made or found. The game sends a preview to no client.
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

        /// <summary>Stores <paramref name="text" /> in the carrier.</summary>
        public void Write(string text)
        {
            _entity.Storage[Key] = text;
        }

        /// <summary>The world's carrier: the entity the world's variable names, where it holds the files' key, or null for a world with no files yet.</summary>
        /// <remarks>A new world starts with the variables of the world the game ran before it, so the key is what is trusted.</remarks>
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

            // As text, the one kind of variable the save takes without question. Set during a save, it still lands in it: the
            // game writes the live variables out once the save has been built.
            MyAPIGateway.Utilities.SetVariable(Variable, entity.EntityId.ToString(CultureInfo.InvariantCulture));
            return new Carrier(entity);
        }
    }
}
