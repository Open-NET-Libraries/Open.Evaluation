using Open.Evaluation.Core;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Throw;

namespace Open.Evaluation.Boolean;

public sealed class Conditional<T>
	: OperationBase<T>,
		IReproducable<(IEvaluate<bool>, IEvaluate<T>, IEvaluate<T>), IEvaluate<T>>
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
	}

	[NotNull]
	public IEvaluate<bool> Condition { get; }

	[NotNull]
	public IEvaluate<T> IfTrue { get; }

	[NotNull]
	public IEvaluate<T> IfFalse { get; }

	private static string Format(object condition, object ifTrue, object ifFalse)
		=> $"{condition} ? {ifTrue} : {ifFalse}";

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
