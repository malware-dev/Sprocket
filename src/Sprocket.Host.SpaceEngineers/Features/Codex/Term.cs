using Lumel;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Codex
{
    /// <summary>A term the codex explains: its name, the sentence that says what it is, and the page that explains it.</summary>
    public sealed class Term
    {
        /// <summary>The term <paramref name="id" />, called <paramref name="name" />, whose preview ends in <paramref name="more" />.</summary>
        public Term(string id, string name, string summary, string page, string more)
        {
            Id = id;
            Name = name;
            Summary = summary;
            Page = page;
            Preview = new Tip("**" + Markdown.Escape(name) + "**\n\n" + summary + "\n\n*" + more + "*");
        }

        /// <summary>What a link names the term by, as in <c>[installations](#installation)</c>.</summary>
        public string Id { get; }

        /// <summary>The term, as the page's title and the preview's first line say it.</summary>
        public string Name { get; }

        /// <summary>One sentence saying what the term is: the preview under the pointer, and the page's lead.</summary>
        public string Summary { get; }

        /// <summary>The rest of the page, in markdown-lite, after the lead.</summary>
        public string Page { get; }

        /// <summary>The note on a link to this term: its name, its sentence, and that pressing it reads more.</summary>
        /// <remarks>Made once and kept, because a link's note is asked for on every frame the pointer rests on it.</remarks>
        public Tip Preview { get; }
    }
}
