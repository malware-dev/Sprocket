namespace Lumel.Input
{
    /// <summary>Copied text, kept in memory for as long as whatever holds it.</summary>
    public class Clipboard : IClipboard
    {
        string _value = string.Empty;

        /// <inheritdoc />
        public string Get()
        {
            return _value;
        }

        /// <inheritdoc />
        public void Set(string text)
        {
            _value = text ?? string.Empty;
        }
    }
}
