namespace Sprocket.Host.SpaceEngineers.Screen
{
    /// <summary>The name of the section under it.</summary>
    /// <remarks>
    /// Sprocket's own nouns, said plainly: <i>Triggers</i>, <i>Sequences</i>. A screen holding two
    /// lists of different things has to say which is which, and a phrase that talks round the word -
    /// <i>what starts things</i> - is longer, has to be re-invented at every site, and reads as
    /// explaining a noun to somebody who is about to learn it anyway.
    /// <para>
    /// A section can name something somebody named - <i>Made from Airlock blueprint</i> - so it comes in
    /// three parts: the name is shown the way they typed it, and the words around it are the screen's own.
    /// </para>
    /// </remarks>
    public sealed class HeadingComponent : EditorComponent
    {
        string _text;
        string _name = "";
        string _after = "";
        bool _follows;

        public HeadingComponent(string text)
        {
            _text = text;
        }

        /// <summary>The words that lead, or the whole of it where nothing is named.</summary>
        public string Text
        {
            get { return _text; }
            set { Set(ref _text, value, nameof(Text)); }
        }

        /// <summary>A name somebody gave, after the words that lead, or empty.</summary>
        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value ?? "", nameof(Name)); }
        }

        /// <summary>The words after the name, or empty.</summary>
        public string After
        {
            get { return _after; }
            set { Set(ref _after, value ?? "", nameof(After)); }
        }

        /// <summary>
        /// Whether something on this screen comes before this section.
        /// </summary>
        /// <remarks>
        /// A section name takes its air above and none below, so it reads as the start of what
        /// follows rather than the end of what came before. The first one on a screen has nothing to
        /// be told apart from, and that air under the screen's own header is a hole.
        /// </remarks>
        public bool Follows
        {
            get { return _follows; }
            set { Set(ref _follows, value, nameof(Follows)); }
        }
    }
}
