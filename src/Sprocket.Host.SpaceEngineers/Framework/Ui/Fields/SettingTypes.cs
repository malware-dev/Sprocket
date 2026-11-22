namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Fields
{
    /// <summary>Which type of setting may stand in for a value a field asks for.</summary>
    public static class SettingTypes
    {
        /// <summary>
        /// The type a setting standing in for what <paramref name="field" /> asks for holds, as the language names it: <c>flag</c>,
        /// <c>text</c>, <c>integer</c> or <c>real</c>; or null where no setting can.
        /// </summary>
        /// <remarks>A value of several parts, such as a color's channels, or one of a host's words, has no setting of its type.</remarks>
        public static string Of(Field field)
        {
            if (field.IsOnOff)
                return "flag";

            if (field.Covers != 1 || field.TakesSlots || field.Edits == FieldEditor.Chosen && field.Words != null && field.Words.Count > 0)
                return null;

            switch (field.Entry)
            {
                case FieldInput.Integer:
                    return "integer";
                case FieldInput.Real:
                    return "real";
                case FieldInput.Text:
                    return "text";
            }

            return null;
        }

        /// <summary>Whether a setting holding <paramref name="type" /> may stand in for what <paramref name="field" /> asks for: one of its type, or an integer for a real.</summary>
        public static bool Fits(Field field, string type)
        {
            var wanted = Of(field);
            return wanted != null && Fits(wanted, type);
        }

        /// <summary>Whether a value of the type <paramref name="type" /> may stand where the type <paramref name="wanted" /> is asked for: one of its type, or an integer for a real.</summary>
        public static bool Fits(string wanted, string type)
        {
            return type == wanted || wanted == "real" && type == "integer";
        }
    }
}
