using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>
    /// An expression as it is being built, with empty places still to fill: what is filled, wrapped in a box, moved and
    /// removed, and each change taken back and done again.
    /// </summary>
    public sealed class ExpressionDraft
    {
        readonly List<DraftPiece> _earlier = new List<DraftPiece>();
        readonly List<DraftPiece> _later = new List<DraftPiece>();
        DraftPiece _root;
        DraftValue _retyping;

        /// <summary>A draft of <paramref name="start" />, or one empty place where it is null.</summary>
        public ExpressionDraft(Expression start)
        {
            _root = start == null ? new DraftHole() : From(start);
        }

        /// <summary>Raised after every change, undone and redone ones included.</summary>
        public event Action Changed;

        /// <summary>Raised as a fixed value is typed over, the rest of the expression left as it is.</summary>
        public event Action Retyped;

        /// <summary>The whole expression.</summary>
        public DraftPiece Root => _root;

        /// <summary>Whether there is a change to take back.</summary>
        public bool CanUndo => _earlier.Count > 0;

        /// <summary>Whether there is a change taken back to do again.</summary>
        public bool CanRedo => _later.Count > 0;

        /// <summary>Whether no place is left empty.</summary>
        public bool IsWhole => Built() != null;

        /// <summary>Puts <paramref name="piece" />, a new value or box, in the empty place <paramref name="hole" />.</summary>
        public void Fill(DraftHole hole, DraftPiece piece)
        {
            Change(root => Filled(root, hole, piece));
        }

        /// <summary>Puts <paramref name="piece" /> in a new box of <paramref name="kind" />, in its first place, where it stood.</summary>
        public void Wrap(DraftPiece piece, JoinKind kind)
        {
            Change(root => Wrapped(root, piece, kind));
        }

        /// <summary>Moves <paramref name="piece" /> to the empty place <paramref name="hole" />, leaving its own place empty.</summary>
        public void Move(DraftPiece piece, DraftHole hole)
        {
            Change(root => Moved(root, piece, hole));
        }

        /// <summary>Takes <paramref name="piece" /> out, with everything in it, leaving its place empty.</summary>
        public void Remove(DraftPiece piece)
        {
            Change(root => Replaced(root, piece, new DraftHole()));
        }

        /// <summary>
        /// Makes <paramref name="value" /> <paramref name="atom" /> as it is typed over; the typing of one value, until anything
        /// else changes, is one change to take back.
        /// </summary>
        public void Retype(DraftValue value, Expression atom)
        {
            if (_retyping != value)
            {
                _earlier.Add(_root.Copied());
                _later.Clear();
                _retyping = value;
            }

            value.Atom = atom;
            Retyped?.Invoke();
        }

        /// <summary>
        /// Works out every part with no empty place in it as far as it can be while it comes to the same, what values are read
        /// as told by <paramref name="types" />, as one change; returns whether anything changed.
        /// </summary>
        public bool Optimize(ITypeScope types)
        {
            var optimized = Optimized(_root, types);
            if (Written(optimized) == Written(_root))
                return false;

            Change(root => optimized);
            return true;
        }

        /// <summary>Takes the last change back.</summary>
        public void Undo()
        {
            if (!CanUndo)
                return;

            _retyping = null;
            _later.Add(_root);
            _root = _earlier[_earlier.Count - 1];
            _earlier.RemoveAt(_earlier.Count - 1);
            Changed?.Invoke();
        }

        /// <summary>Does the last change taken back again.</summary>
        public void Redo()
        {
            if (!CanRedo)
                return;

            _retyping = null;
            _earlier.Add(_root);
            _root = _later[_later.Count - 1];
            _later.RemoveAt(_later.Count - 1);
            Changed?.Invoke();
        }

        /// <summary>What the whole expression would be after <paramref name="piece" /> filled <paramref name="hole" />, the draft left as it is.</summary>
        public DraftPiece TriedFill(DraftHole hole, DraftPiece piece)
        {
            var copy = _root.Copied();
            return Filled(copy, (DraftHole)At(copy, PathOf(hole)), piece.Copied());
        }

        /// <summary>What the whole expression would be after <paramref name="piece" /> was put in a box of <paramref name="kind" />, the draft left as it is.</summary>
        public DraftPiece TriedWrap(DraftPiece piece, JoinKind kind)
        {
            var copy = _root.Copied();
            return Wrapped(copy, At(copy, PathOf(piece)), kind);
        }

        /// <summary>
        /// What the whole expression would be after <paramref name="piece" /> moved to <paramref name="hole" />, the draft left
        /// as it is; null where the place is inside the piece itself.
        /// </summary>
        public DraftPiece TriedMove(DraftPiece piece, DraftHole hole)
        {
            if (Holds(piece, hole))
                return null;

            var copy = _root.Copied();
            return Moved(copy, At(copy, PathOf(piece)), (DraftHole)At(copy, PathOf(hole)));
        }

        /// <summary>The expression the draft is, or null while any place is empty.</summary>
        public Expression Built() => Built(_root);

        /// <summary>The expression <paramref name="piece" /> is, or null where any place in it is empty.</summary>
        public static Expression Built(DraftPiece piece)
        {
            var value = piece as DraftValue;
            if (value != null)
                return value.Atom.Copied();

            var join = piece as DraftJoin;
            if (join == null)
                return null;

            var parts = new List<Expression>();
            foreach (var part in join.Parts)
            {
                var built = Built(part);
                if (built == null)
                    return null;

                parts.Add(built);
            }

            switch (join.Kind)
            {
                case JoinKind.Add: return new Arithmetic(Operator.Add, parts[0], parts[1]);
                case JoinKind.Subtract: return new Arithmetic(Operator.Subtract, parts[0], parts[1]);
                case JoinKind.Multiply: return new Arithmetic(Operator.Multiply, parts[0], parts[1]);
                case JoinKind.Divide: return new Arithmetic(Operator.Divide, parts[0], parts[1]);
                case JoinKind.Negate: return new Negation(parts[0]);
                case JoinKind.Lowest: return new Function(FunctionKind.Min, parts);
                case JoinKind.Highest: return new Function(FunctionKind.Max, parts);
                case JoinKind.Rounded: return new Function(FunctionKind.Round, parts);
                default: return new Function(FunctionKind.Abs, parts);
            }
        }

        /// <summary>The draft piece <paramref name="expression" /> is shown as: its boxes, and its values within them.</summary>
        public static DraftPiece From(Expression expression)
        {
            var arithmetic = expression as Arithmetic;
            if (arithmetic != null)
                return new DraftJoin(JoinKinds.Of(arithmetic.Op), new[] { From(arithmetic.Left), From(arithmetic.Right) });

            var negation = expression as Negation;
            if (negation != null)
                return new DraftJoin(JoinKind.Negate, new[] { From(negation.Operand) });

            var function = expression as Function;
            if (function == null)
                return new DraftValue(expression.Copied());

            var parts = new List<DraftPiece>();
            foreach (var argument in function.Arguments)
                parts.Add(From(argument));

            return new DraftJoin(JoinKinds.Of(function.Kind), parts);
        }

        /// <summary>Whether <paramref name="inner" /> is <paramref name="outer" /> or stands somewhere inside it.</summary>
        public static bool Holds(DraftPiece outer, DraftPiece inner)
        {
            for (var at = inner; at != null; at = at.Parent)
            {
                if (at == outer)
                    return true;
            }

            return false;
        }

        void Change(Func<DraftPiece, DraftPiece> change)
        {
            _retyping = null;
            _earlier.Add(_root.Copied());
            _later.Clear();
            _root = change(_root);
            Changed?.Invoke();
        }

        // A copy of piece, each part of it with no empty place worked out.
        static DraftPiece Optimized(DraftPiece piece, ITypeScope types)
        {
            var built = Built(piece);
            if (built != null)
                return From(Folding.Fold(built, types));

            var join = piece as DraftJoin;
            if (join == null)
                return piece.Copied();

            var parts = new List<DraftPiece>();
            foreach (var part in join.Parts)
                parts.Add(Optimized(part, types));

            return new DraftJoin(join.Kind, parts);
        }

        // How piece reads, a question mark for each empty place, to tell whether two drafts are the same.
        static string Written(DraftPiece piece)
        {
            var value = piece as DraftValue;
            if (value != null)
                return value.Atom.Written;

            var join = piece as DraftJoin;
            if (join == null)
                return "?";

            var parts = new List<string>();
            foreach (var part in join.Parts)
                parts.Add(Written(part));

            return join.Kind + "(" + string.Join(", ", parts) + ")";
        }

        static DraftPiece Filled(DraftPiece root, DraftHole hole, DraftPiece piece) => Replaced(root, hole, piece);

        static DraftPiece Wrapped(DraftPiece root, DraftPiece piece, JoinKind kind)
        {
            var join = new DraftJoin(kind);
            root = Replaced(root, piece, join);
            join.Set(0, piece);
            return root;
        }

        static DraftPiece Moved(DraftPiece root, DraftPiece piece, DraftHole hole)
        {
            root = Replaced(root, piece, new DraftHole());
            return Replaced(root, hole, piece);
        }

        // Puts with where old stood, and gives the whole expression after it.
        static DraftPiece Replaced(DraftPiece root, DraftPiece old, DraftPiece with)
        {
            var parent = old.Parent;
            if (parent == null)
            {
                with.Parent = null;
                return with;
            }

            parent.Set(parent.IndexOf(old), with);
            return root;
        }

        // The places to go through from the whole expression down to the piece.
        static List<int> PathOf(DraftPiece piece)
        {
            var path = new List<int>();
            for (var at = piece; at.Parent != null; at = at.Parent)
                path.Insert(0, at.Parent.IndexOf(at));

            return path;
        }

        static DraftPiece At(DraftPiece root, List<int> path)
        {
            var at = root;
            foreach (var index in path)
                at = ((DraftJoin)at).Parts[index];

            return at;
        }
    }
}
