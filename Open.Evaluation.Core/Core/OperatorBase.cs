/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Disposable;
using Open.Hierarchy;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;
using Throw;

namespace Open.Evaluation.Core;

public abstract class OperatorBase<TChild, TResult>
	: OperationBase<TResult>, IOperator<TChild, TResult>, IComparer<TChild>

	where TChild : class, IEvaluate
	where TResult : notnull, IEquatable<TResult>, IComparable<TResult>
{
	protected OperatorBase(Symbol symbol, IEnumerable<TChild> children, bool reorderChildren = false, int minimumChildren = 1)
		: base(symbol)
	{
		children.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		if (reorderChildren) children = children.OrderBy(c => c, this);
		Children = children is ImmutableArray<TChild> c ? c : children.ToImmutableArray();
		minimumChildren.Throw().IfLessThan(0);
		Children.Length
			.Throw(() => new ArgumentException($"{GetType()} must be constructed with at least " + (minimumChildren==1 ? "1 child" : $"{minimumChildren} children."), nameof(minimumChildren)))
			.IfLessThan(minimumChildren);
	}

	public ImmutableArray<TChild> Children { get; }

	IReadOnlyList<TChild> IParent<TChild>.Children => Children;

	IReadOnlyList<object> IParent.Children => Children;

	protected virtual Lazy<string> Describe(IEnumerable<Lazy<string>> children)
		=> new(() =>
		{
			string r;
			using (var lease = StringBuilderPool.Rent())
			{
				var result = lease.Item;
				result.Append('(');
				var index = -1;
				foreach (var o in children)
				{
					ToStringInternal_OnAppendNextChild(result, ++index, o);
				}
				result.Append(')');
				r = result.ToString();
			}

			var isEmpty = r == "()";
			Debug.Assert(!isEmpty, "Operator has no children.");
			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			return isEmpty ? $"({Symbol.Character})" : r;
		});

	protected virtual void ToStringInternal_OnAppendNextChild(StringBuilder result, int index, Lazy<string> child)
	{
		if (index is not 0) result.Append(Symbol.Text);
		result.Append(child.Value);
	}

	protected IEnumerable<EvaluationResult<object>> ChildResults(Context context)
	{
		foreach (var child in Children)
			yield return child.Evaluate(context);
	}

	protected IEnumerable<Lazy<string>> ChildDescriptions()
	{
		foreach (var child in Children)
			yield return child.Description;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected override string Describe()
		=> Describe(ChildDescriptions()).Value;

	protected virtual int ConstantPriority => +1;

	// Need a standardized way to order so that comparisons are easier.
	// ReSharper disable once VirtualMemberNeverOverridden.Global
	public virtual int Compare(TChild? x, TChild? y)
	{
		if (x is null) return y is null ? 0 : -1;
		if (y is null) return +1;

		switch (x)
		{
			case IConstant<TResult> aC when y is IConstant<TResult> bC:
				return bC.Value.CompareTo(aC.Value); // Descending...

			case IConstant<TResult> _:
				return +1 * ConstantPriority;
		}

		if (y is IConstant<TResult>)
			return -1 * ConstantPriority;

		switch (x)
		{
			case IParameter<TResult> aP when y is IParameter<TResult> bP:
				return aP.ID.CompareTo(bP.ID);

			case IParameter<TResult> _:
				return +1;
		}

		if (y is IParameter<TResult>)
			return -1;

		var aChildCount = ((x as IParent)?.GetDescendants().Count(d => d is not IConstant<TResult>) ?? 0) + 1;
		var bChildCount = ((y as IParent)?.GetDescendants().Count(d => d is not IConstant<TResult>) ?? 0) + 1;

		if (aChildCount > bChildCount) return -1;
		if (aChildCount < bChildCount) return +1;

		var ats = x.Description.Value;
		var bts = y.Description.Value;

		return string.CompareOrdinal(ats, bts);
	}
}

public abstract class OperatorBase<TResult>
	: OperatorBase<IEvaluate<TResult>, TResult>
	where TResult : notnull, IEquatable<TResult>, IComparable<TResult>
{
	protected OperatorBase(
		Symbol symbol,
		IEnumerable<IEvaluate<TResult>> children,
		bool reorderChildren = false,
		int minimumChildren = 1)
		: base(symbol, children, reorderChildren, minimumChildren)
	{
	}

	protected new IEnumerable<EvaluationResult<TResult>> ChildResults(Context context)
	{
		foreach (var child in Children)
			yield return child.Evaluate(context);
	}

	internal static IEvaluate<TResult> ConditionalTransform(
		[DisallowNull, NotNull] IEnumerable<IEvaluate<TResult>> param,
		[DisallowNull, NotNull] Func<ImmutableArray<IEvaluate<TResult>>, IEvaluate<TResult>> transform)
	{
		param.ThrowIfNull().OnlyInDebug();
		transform.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		using var e = param.GetEnumerator();
		if (!e.MoveNext()) return transform([]);
		var v0 = e.Current;
		if (!e.MoveNext()) return v0;
		var builder = ImmutableArray.CreateBuilder<IEvaluate<TResult>>();
		builder.Add(v0);
		do { builder.Add(e.Current); }
		while (e.MoveNext());

		return transform(builder.MoveToImmutable());
	}

	//internal static IEvaluate<TResult> ConditionalTransform(
	//	IEnumerable<IEvaluate<TResult>> param,
	//	Func<IEvaluate<TResult>> onZero,
	//	Func<ImmutableArray<IEvaluate<TResult>>, IEvaluate<TResult>> transform)
	//{
	//	if (param is null) throw new ArgumentNullException(nameof(param));
	//	var e = param.GetEnumerator();
	//	if (!e.MoveNext()) return onZero();
	//	var v0 = e.Current;
	//	if (!e.MoveNext()) return v0;
	//	var builder = ImmutableArray.CreateBuilder<IEvaluate<TResult>>();
	//	builder.Add(v0);
	//	do { builder.Add(e.Current); }
	//	while (e.MoveNext());

	//	return transform(builder.MoveToImmutable());
	//}
}
