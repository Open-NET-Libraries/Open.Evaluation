using Open.Hierarchy;
using System.Collections.Immutable;
using Open.Evaluation.Core;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Throw;

namespace Open.Evaluation.Boolean;

public sealed partial class Conditional<T>
	: OperationBase<T>,
		IReproducable<(IEvaluate<bool>, IEvaluate<T>, IEvaluate<T>), IEvaluate<T>>,
		IParent<IEvaluate<T>>
		where T : notnull, IEquatable<T>, IComparable<T>
{
	private Conditional(
		ICatalog<IEvaluate<T>> catalog,
		IEvaluate<bool> condition,
		IEvaluate<T> ifTrue,
		IEvaluate<T> ifFalse)
		: base(catalog, Symbols.Conditional)
	{
		Condition = condition ?? throw new ArgumentNullException(nameof(condition));
		IfTrue = ifTrue ?? throw new ArgumentNullException(nameof(ifTrue));
		IfFalse = ifFalse ?? throw new ArgumentNullException(nameof(ifFalse));
		_allChildren = [Condition, IfTrue, IfFalse];
		// The typed view holds the children that ARE IEvaluate<T>: both branches always, and the
		// condition too when T is bool (then all three share the type). For any other T the
		// condition is an IEvaluate<bool> that cannot be a typed child; it remains reachable
		// through the untyped IParent.Children, which is what full traversals (descendants,
		// parameter discovery, gene counts) use.
		_typedChildren = Condition is IEvaluate<T> typedCondition
			? [typedCondition, IfTrue, IfFalse]
			: [IfTrue, IfFalse];
	}

	[NotNull]
	public IEvaluate<bool> Condition { get; }

	[NotNull]
	public IEvaluate<T> IfTrue { get; }

	[NotNull]
	public IEvaluate<T> IfFalse { get; }

	private readonly ImmutableArray<IEvaluate> _allChildren;
	private readonly ImmutableArray<IEvaluate<T>> _typedChildren;

	/// <summary>All three children -- condition, if-true, if-false -- for untyped traversal.</summary>
	IReadOnlyList<object> IParent.Children => _allChildren;

	/// <summary>The children that are <see cref="IEvaluate{T}"/> (see the constructor note).</summary>
	IReadOnlyList<IEvaluate<T>> IParent<IEvaluate<T>>.Children => _typedChildren;

	private static string Format(object condition, object ifTrue, object ifFalse)
		// Parenthesized: interning is keyed by rendering, and a bare ternary is ambiguous next to
		// a prefix operator -- "!{0} ? {2} : {1}" would be both Not(Conditional(...)) and
		// Conditional(Not(...), ...), which collided in the catalog under one key.
		=> $"({condition} ? {ifTrue} : {ifFalse})";

	protected override string Describe()
		=> Conditional<T>.Format(
			Condition.Description,
			IfTrue.Description,
			IfFalse.Description);

	protected override EvaluationResult<T> EvaluateInternal(Context context)
		=> Condition.Evaluate(context)
			? IfTrue.Evaluate(context)
			: IfFalse.Evaluate(context);

	internal static Conditional<T> Create(
		ICatalog<IEvaluate<T>> catalog,
		(IEvaluate<bool>, IEvaluate<T>, IEvaluate<T>) param)
	{
		catalog.ThrowIfNull();
		Contract.EndContractBlock();

		return catalog.Register(
			new Conditional<T>(
				catalog,
				param.Item1,
				param.Item2,
				param.Item3));
	}

	public Conditional<T> NewUsing(
		ICatalog<IEvaluate<T>> catalog,
		(IEvaluate<bool>, IEvaluate<T>, IEvaluate<T>) param)
		=> Create(catalog, param);

	public Conditional<T> NewUsing((IEvaluate<bool>, IEvaluate<T>, IEvaluate<T>) param)
		=> NewUsing(Catalog, param);

	IEvaluate<T> IReproducable<(IEvaluate<bool>, IEvaluate<T>, IEvaluate<T>), IEvaluate<T>>.NewUsing(ICatalog<IEvaluate<T>> catalog, (IEvaluate<bool>, IEvaluate<T>, IEvaluate<T>) param)
		=> NewUsing(Catalog, param);

	IEvaluate<T> IReproducable<(IEvaluate<bool>, IEvaluate<T>, IEvaluate<T>), IEvaluate<T>>.NewUsing((IEvaluate<bool>, IEvaluate<T>, IEvaluate<T>) param)
		=> NewUsing(param);
}

public static class ConditionalExtensions
{
	public static IEvaluate<TResult> Conditional<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		(IEvaluate<bool>, IEvaluate<TResult>, IEvaluate<TResult>) param)
		where TResult : notnull, IEquatable<TResult>, IComparable<TResult>
		=> Boolean.Conditional<TResult>.Create(catalog, param);
}
