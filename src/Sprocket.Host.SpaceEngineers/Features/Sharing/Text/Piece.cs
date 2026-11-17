using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;

namespace Sprocket.Host.SpaceEngineers.Features.Sharing.Text
{
    /// <summary>One piece of a script as <see cref="TokenCode" /> codes it: what the lexer read, and how it was written.</summary>
    public struct Piece
    {
        /// <summary>The piece <paramref name="token" />, written as <paramref name="spelled" />.</summary>
        public Piece(Token token, string spelled)
        {
            Token = token;
            Spelled = spelled;
        }

        /// <summary>What the lexer read, its kind and any value it carries.</summary>
        public Token Token { get; private set; }

        /// <summary>The piece as written, without the spaces after it.</summary>
        public string Spelled { get; private set; }
    }
}
