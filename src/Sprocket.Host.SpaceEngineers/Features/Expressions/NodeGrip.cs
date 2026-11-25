using System;
using Lumel.Controls;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>The grip a part of an expression being built is dragged by, saying as the pointer comes onto it and leaves it.</summary>
    public sealed class NodeGrip : DragHandle
    {
        /// <summary>Raised as the pointer comes onto the grip, with true, and as it leaves, with false.</summary>
        public event Action<bool> Pointed;

        /// <inheritdoc />
        protected override void OnPointerOverChanged()
        {
            base.OnPointerOverChanged();
            Pointed?.Invoke(IsPointerOver);
        }
    }
}
