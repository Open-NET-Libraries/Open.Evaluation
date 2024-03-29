﻿/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace Open.Evaluation.Boolean;

// ReSharper disable once PossibleInfiniteInheritance
public class Conditional<TResult> : OperationBase<TResult>,
	IReproducable<(IEvaluate<bool>, IEvaluate<TResult>, IEvaluate<TResult>), IEvaluate<TResult>>
{
	public Conditional(
		IEvaluate<bool> condition,
		IEvaluate<TResult> ifTrue,
		IEvaluate<TResult> ifFalse)
		: base(Conditional.SYMBOL, Conditional.SEPARATOR)
	{
		Condition = condition ?? throw new ArgumentNullException(nameof(condition));
		IfTrue = ifTrue ?? throw new ArgumentNullException(nameof(ifTrue));
		IfFalse = ifFalse ?? throw new ArgumentNullException(nameof(ifFalse));
	}

	[NotNull]
	public IEvaluate<bool> Condition { get; }

	[NotNull]
	public IEvaluate<TResult> IfTrue { get; }

	[NotNull]
	public IEvaluate<TResult> IfFalse { get; }

	[return: NotNull]
	protected string ToStringInternal(object condition, object ifTrue, object ifFalse)
		=> $"{condition} ? {ifTrue} : {ifFalse}";

	[return: NotNull]
	public override string ToString(object context)
		=> ToStringInternal(
			Condition.Evaluate(context),
			IfTrue.Evaluate(context)!,
			IfFalse.Evaluate(context)!);

	[return: NotNull]
	protected override string ToStringRepresentationInternal()
		=> ToStringInternal(
			Condition.ToStringRepresentation(),
			IfTrue.ToStringRepresentation(),
			IfFalse.ToStringRepresentation());

	[return: NotNull]
	protected override TResult EvaluateInternal(object context)
		=> Condition.Evaluate(context)
			? IfTrue.Evaluate(context)
			: IfFalse.Evaluate(context);

	[return: NotNull]
	internal static Conditional<TResult> Create(
		ICatalog<IEvaluate<TResult>> catalog,
		(IEvaluate<bool>, IEvaluate<TResult>, IEvaluate<TResult>) param)
	{
		if (catalog is null) throw new ArgumentNullException(nameof(catalog));
		Contract.EndContractBlock();

		return catalog.Register(
			new Conditional<TResult>(
				param.Item1,
				param.Item2,
				param.Item3));
	}

	public IEvaluate<TResult> NewUsing(
		ICatalog<IEvaluate<TResult>> catalog,
		(IEvaluate<bool>, IEvaluate<TResult>, IEvaluate<TResult>) param)
		=> Create(catalog, param);
}

public static class Conditional
{
	public const char SYMBOL = '?';
	public const string SEPARATOR = " ? ";
}

public static class ConditionalExtensions
{
	public static IEvaluate<TResult> Conditional<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		(IEvaluate<bool>, IEvaluate<TResult>, IEvaluate<TResult>) param)
		=> Boolean.Conditional<TResult>.Create(catalog, param);
}
