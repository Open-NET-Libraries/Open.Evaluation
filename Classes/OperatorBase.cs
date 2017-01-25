using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace EvaluationFramework
{
    public abstract class OperatorBase<TChild, TContext, TResult>
		: OperationBase<TContext, TResult>, IOperator<TChild, TContext, TResult>
		where TChild : IEvaluate<TContext, TResult>
	{

		protected OperatorBase(string symbol, IEnumerable<TChild> children = null) : base(symbol)
		{
			ChildrenInternal = children == null ? new List<TChild>() : new List<TChild>(children);
			Children = ChildrenInternal.AsReadOnly();
		}

		protected readonly List<TChild> ChildrenInternal;

		public IReadOnlyList<TChild> Children
		{
			get;
			private set;
		}

		protected override string ToStringInternal(object contents)
		{
			var collection = contents as IEnumerable;
			if (contents == null) return base.ToStringInternal(contents);
			var result = new StringBuilder('(');
			int index = -1;
			foreach (var o in collection)
			{
				if (++index != 0) result.Append(Symbol);
				result.Append(o);
			}
			result.Append(')');
			return result.ToString();
		}

		protected IEnumerable<TResult> ChildResults(TContext context)
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

	}


}