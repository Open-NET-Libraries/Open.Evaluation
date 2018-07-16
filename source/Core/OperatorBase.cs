/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Hierarchy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Open.Evaluation.Core
{
	public abstract class OperatorBase<TChild, TResult>
		: OperationBase<TResult>, IOperator<TChild, TResult>

		where TChild : class, IEvaluate
		where TResult : IComparable
	{

		protected OperatorBase(char symbol, string separator, IEnumerable<TChild> children = null, bool reorderChildren = false) : base(symbol, separator)
		{
			ChildrenInternal = children == null ? new List<TChild>() : new List<TChild>(children);
			if (reorderChildren) ChildrenInternal.Sort(Compare);
			Children = ChildrenInternal.AsReadOnly();
		}

		protected readonly List<TChild> ChildrenInternal;

		public IReadOnlyList<TChild> Children
		{
			get;
		}

		IReadOnlyList<object> IParent.Children => Children;

		protected override string ToStringInternal(object contents)
		{
			if (!(contents is IEnumerable collection))
				return base.ToStringInternal(contents);

			var result = new StringBuilder();
			result.Append('(');
			var index = -1;
			foreach (var o in collection)
			{
				if (++index != 0) result.Append(SymbolString);
				result.Append(o);
			}
			result.Append(')');
			return result.ToString();
		}

		public override string ToString(object context)
		{
			return ToStringInternal(Children.Select(c => c.ToString(context)));
		}

		protected IEnumerable<object> ChildResults(object context)
		{
			foreach (var child in ChildrenInternal)
				yield return child.Evaluate(context);
		}

		protected IEnumerable<string> ChildRepresentations()
		{
			foreach (var child in ChildrenInternal)
				yield return child.ToStringRepresentation();
		}

		protected override string ToStringRepresentationInternal()
		{
			return ToStringInternal(ChildRepresentations());
		}

		protected virtual int ConstantPriority => +1;

		// Need a standardized way to order so that comparisons are easier.
		// ReSharper disable once VirtualMemberNeverOverridden.Global
		protected virtual int Compare(TChild a, TChild b)
		{
			switch (a)
			{
				case Constant<TResult> aC when b is Constant<TResult> bC:
					return bC.Value.CompareTo(aC.Value); // Descending...
				case Constant<TResult> _:
					return +1 * ConstantPriority;
				default:
					if (b is Constant<TResult>)
						return -1 * ConstantPriority;
					break;
			}

			switch (a)
			{
				case Parameter<TResult> aP when b is Parameter<TResult> bP:
					return aP.ID.CompareTo(bP.ID);
				case Parameter<TResult> _:
					return +1;
				default:
					if (b is Parameter<TResult>)
						return -1;
					break;
			}

			var aChildCount = (a as IParent)?.Children.Count ?? 1;
			var bChildCount = (b as IParent)?.Children.Count ?? 1;

			if (aChildCount > bChildCount) return -1;
			if (aChildCount < bChildCount) return +1;

			var ats = a.ToStringRepresentation();
			var bts = b.ToStringRepresentation();

			return string.CompareOrdinal(ats, bts);

		}
	}

	public abstract class OperatorBase<TResult> : OperatorBase<IEvaluate<TResult>, TResult>
		where TResult : IComparable
	{
		protected OperatorBase(
			char symbol,
			string separator,
			IEnumerable<IEvaluate<TResult>> children = null,
			bool reorderChildren = false) : base(symbol, separator, children, reorderChildren)
		{
		}

	}

}
