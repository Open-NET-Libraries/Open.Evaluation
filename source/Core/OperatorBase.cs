/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Disposable;
using Open.Hierarchy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Open.Evaluation.Core;

public abstract class OperatorBase<TChild, TResult>
	: OperationBase<TResult>, IOperator<TChild, TResult>, IComparer<TChild>

	where TChild : class, IEvaluate
	where TResult : notnull, IComparable<TResult>, IComparable
{
	protected OperatorBase(char symbol, string separator, IEnumerable<TChild> children, bool reorderChildren = false, int minimumChildren = 1) : base(symbol, separator)
	{
		if (children is null) throw new ArgumentNullException(nameof(children));
		Contract.EndContractBlock();

		if (reorderChildren) children = children.OrderBy(c => c, this);
		Children = children is ImmutableArray<TChild> c ? c : children.ToImmutableArray();
		if (Children.Length < minimumChildren)
			throw new ArgumentException($"{GetType()} must be constructed with at least {minimumChildren} child(ren).");
	}

	public ImmutableArray<TChild> Children { get; }

	IReadOnlyList<TChild> IParent<TChild>.Children => Children;
	IReadOnlyList<object> IParent.Children => Children;

	protected override string ToStringInternal(object context)
	{
		if (context is not IEnumerable collection)
			return base.ToStringInternal(context);

		string r;
		using (var lease = StringBuilderPool.Rent())
		{
			var result = lease.Item;
			result.Append('(');
			var index = -1;
			foreach (var o in collection)
			{
				ToStringInternal_OnAppendNextChild(result, ++index, o);
			}
			result.Append(')');
			r = result.ToString();
		}

		var isEmpty = r == "()";
		Debug.Assert(!isEmpty, "Operator has no children.");
		// ReSharper disable once ConditionIsAlwaysTrueOrFalse
		return isEmpty ? $"({Symbol})" : r;
	}

	protected virtual void ToStringInternal_OnAppendNextChild(StringBuilder result, int index, object child)
	{
		if (index is not 0) result.Append(SymbolString);
		result.Append(child);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override string ToString(object context)
		=> ToStringInternal(ChildToString(context));

	protected IEnumerable<string> ChildToString(object context)
	{
		foreach (var child in Children)
			yield return child.ToString(context);
	}

	protected IEnumerable<object> ChildResults(object context)
	{
		foreach (var child in Children)
			yield return child.Evaluate(context);
	}

	protected IEnumerable<string> ChildRepresentations()
	{
		foreach (var child in Children)
			yield return child.ToStringRepresentation();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected override string ToStringRepresentationInternal()
		=> ToStringInternal(ChildRepresentations());

	protected virtual int ConstantPriority => +1;

	// Need a standardized way to order so that comparisons are easier.
	// ReSharper disable once VirtualMemberNeverOverridden.Global
	public virtual int Compare(TChild x, TChild y)
	{
		switch (x)
		{
			case Constant<TResult> aC when y is Constant<TResult> bC:
				return bC.Value.CompareTo(aC.Value); // Descending...
			case Constant<TResult> _:
				return +1 * ConstantPriority;
			default:
				if (y is Constant<TResult>)
					return -1 * ConstantPriority;
				break;
		}

		switch (x)
		{
			case Parameter<TResult> aP when y is Parameter<TResult> bP:
				return aP.ID.CompareTo(bP.ID);
			case Parameter<TResult> _:
				return +1;
			default:
				if (y is Parameter<TResult>)
					return -1;
				break;
		}

		var aChildCount = ((x as IParent)?.GetDescendants().Count(d => d is not IConstant) ?? 0) + 1;
		var bChildCount = ((y as IParent)?.GetDescendants().Count(d => d is not IConstant) ?? 0) + 1;

		if (aChildCount > bChildCount) return -1;
		if (aChildCount < bChildCount) return +1;

		var ats = x.ToStringRepresentation();
		var bts = y.ToStringRepresentation();

		return string.CompareOrdinal(ats, bts);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected virtual Constant<TResult> GetConstant(ICatalog<IEvaluate<TResult>> catalog, in TResult value)
		=> catalog.GetConstant(value);
}

public abstract class OperatorBase<TResult> : OperatorBase<IEvaluate<TResult>, TResult>
	where TResult : notnull, IComparable<TResult>, IComparable
{
	protected OperatorBase(
		char symbol,
		string separator,
		IEnumerable<IEvaluate<TResult>> children,
		bool reorderChildren = false,
		int minimumChildren = 1)
		: base(symbol, separator, children, reorderChildren, minimumChildren)
	{
	}

	protected new IEnumerable<TResult> ChildResults(object context)
	{
		foreach (var child in Children)
			yield return child.Evaluate(context);
	}

	internal static IEvaluate<TResult> ConditionalTransform(
		IEnumerable<IEvaluate<TResult>> param,
		Func<ImmutableArray<IEvaluate<TResult>>, IEvaluate<TResult>> transform)
	{
		if (param is null) throw new ArgumentNullException(nameof(param));
		using var e = param.GetEnumerator();
		if (!e.MoveNext()) return transform(ImmutableArray<IEvaluate<TResult>>.Empty);
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
