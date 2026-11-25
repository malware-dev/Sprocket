using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;
using Lumel;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>The page a value's expression is built on, opened from the value's link mark.</summary>
    public sealed class ExpressionPage : EditorPage
    {
        readonly AutomationSystem _system;
        readonly string _wanted;
        readonly string _of;
        readonly Literal? _start;
        readonly Action<Literal> _done;
        readonly bool _tests;
        ExpressionPanel _panel;

        /// <summary>
        /// Builds the value <paramref name="of" /> names, of the type the language names <paramref name="wanted" />, in
        /// <paramref name="system" />, starting from <paramref name="start" />, or from one empty place where it is null; <paramref name="done" /> is given the value built.
        /// A condition an if or a when tests where <paramref name="tests" />.
        /// </summary>
        public ExpressionPage(SprocketEditor editor, AutomationSystem system, string wanted, string of, Literal? start, Action<Literal> done, bool tests = false)
            : base(editor)
        {
            _tests = tests;
            _system = system;
            _wanted = wanted;
            _of = of ?? "";
            _start = start;
            _done = done;
        }

        /// <inheritdoc />
        public override string Title => Editor.Texts.ExpressionTitle;

        /// <inheritdoc />
        public override string Subtitle => _of.Length == 0 ? "" : Template.Fill(Editor.Texts.ExpressionFor, "value", _of);

        /// <inheritdoc />
        public override bool Asks => true;

        /// <inheritdoc />
        public override string PrimaryText => Editor.Texts.ChromeDone;

        /// <inheritdoc />
        public override bool CanPrimary => _panel != null && _panel.Reads;

        /// <inheritdoc />
        public override string PrimaryAbout => "";

        /// <inheritdoc />
        public override bool ScrollsItself => true;

        /// <inheritdoc />
        public override bool HasHistory => true;

        /// <inheritdoc />
        public override bool CanUndo => _panel != null && _panel.Draft.CanUndo;

        /// <inheritdoc />
        public override void Undo() => _panel?.Draft.Undo();

        /// <inheritdoc />
        public override bool CanRedo => _panel != null && _panel.Draft.CanRedo;

        /// <inheritdoc />
        public override void Redo() => _panel?.Draft.Redo();

        /// <summary>The panel the expression is built in.</summary>
        public ExpressionPanel Panel => (ExpressionPanel)View;

        /// <inheritdoc />
        public override void Primary()
        {
            if (_panel == null || !_panel.Reads)
                return;

            var value = _panel.Value;
            Editor.Pop();
            _done(value);
        }

        /// <inheritdoc />
        public override void Build()
        {
            if (_panel == null)
                return;

            _panel.Show();
        }

        /// <inheritdoc />
        public override void Refresh()
        {
            // What the expression comes to changes as the construct does, which nothing announces, so it is said again once a second.
            if (_panel != null && SecondPassed())
                _panel.Reckon();
        }

        /// <inheritdoc />
        protected override Element CreateView()
        {
            _panel = new ExpressionPanel(Editor, _system, _wanted, _start, _tests);
            return _panel;
        }
    }
}
