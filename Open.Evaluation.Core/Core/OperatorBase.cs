namespace Open.Evaluation.Core;

public abstract class OperatorBase<TChild, T>
	: OperationBase<T>, IOperator<TChild, T>, IComparer<TChild>

	where TChild : class, IEvaluate
	where T : notnull, IEquatable<T>, IComparable<T>
{
	protected OperatorBase(ICatalog<IEvaluate<T>> catalog, Symbol symbol, IEnumerable<TChild> children, bool reorderChildren = false, int minimumChildren = 1)
		: base(catalog, symbol)
	{
		children.ThrowIfNull();
		Contract.EndContractBlock();

		if (reorderChildren) children = children.OrderBy(c => c, this);
		Children = children is ImmutableArray<TChild> c ? c : children.ToImmutableArray();
		minimumChildren.Throw().IfLessThan(0);
		Children.Length
			.Throw(() => new ArgumentException($"{GetType()} must be constructed with at least " + (minimumChildren == 1 ? "1 child" : $"{minimumChildren} children."), nameof(minimumChildren)))
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
			case IConstant<T> aC when y is IConstant<T> bC:
				return bC.Value.CompareTo(aC.Value); // Descending...

			case IConstant<T> _:
				return +1 * ConstantPriority;
		}

		if (y is IConstant<T>)
			return -1 * ConstantPriority;

		switch (x)
		{
			case IParameter<T> aP when y is IParameter<T> bP:
				return aP.Id.CompareTo(bP.Id);

			case IParameter<T> _:
				return +1;
		}

		if (y is IParameter<T>)
			return -1;

		var aChildCount = ((x as IParent)?.GetDescendants().Count(d => d is not IConstant<T>) ?? 0) + 1;
		var bChildCount = ((y as IParent)?.GetDescendants().Count(d => d is not IConstant<T>) ?? 0) + 1;

		if (aChildCount > bChildCount) return -1;
		if (aChildCount < bChildCount) return +1;

		var ats = x.Description.Value;
		var bts = y.Description.Value;

		return string.CompareOrdinal(ats, bts);
	}
}

public abstract class OperatorBase<T>(
	ICatalog<IEvaluate<T>> catalog, Symbol symbol, IEnumerable<IEvaluate<T>> children,
	bool reorderChildren = false, int minimumChildren = 1)
	: OperatorBase<IEvaluate<T>, T>(catalog, symbol, children, reorderChildren, minimumChildren)
	where T : notnull, IEquatable<T>, IComparable<T>
{
	protected new IEnumerable<EvaluationResult<T>> ChildResults(Context context)
	{
		foreach (var child in Children)
			yield return child.Evaluate(context);
	}

	internal protected static IEvaluate<T> ConditionalTransform(
		IEnumerable<IEvaluate<T>> param,
		Func<ImmutableArray<IEvaluate<T>>, IEvaluate<T>> transform)
	{
		param.ThrowIfNull().OnlyInDebug();
		transform.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		using var e = param.GetEnumerator();
		if (!e.MoveNext()) return transform([]);
		var v0 = e.Current;
		if (!e.MoveNext()) return v0;
		var builder = ImmutableArray.CreateBuilder<IEvaluate<T>>();
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
