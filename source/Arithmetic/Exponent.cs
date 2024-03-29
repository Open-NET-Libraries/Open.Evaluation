﻿/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace Open.Evaluation.Arithmetic;

// ReSharper disable once PossibleInfiniteInheritance
public class Exponent<TResult> : OperatorBase<TResult>,
	IReproducable<(IEvaluate<TResult>, IEvaluate<TResult>), IEvaluate<TResult>>
	where TResult : notnull, IComparable<TResult>, IComparable
{
	protected Exponent(
		IEvaluate<TResult> @base,
		IEvaluate<TResult> power)
		: base(
			  Exponent.SYMBOL,
			  Exponent.SEPARATOR,
			  // Need to provide to children so a node tree can be built.
			  new[] { @base, power }
		)
	{
		Base = @base ?? throw new ArgumentNullException(nameof(@base));
		Power = power ?? throw new ArgumentNullException(nameof(power));
	}

	public IEvaluate<TResult> Base { get; }

	public IEvaluate<TResult> Power { get; }

	protected static double ConvertToDouble(in dynamic value) => (double)value;

	protected override TResult EvaluateInternal(object context)
	{
		var evaluation = ConvertToDouble(Base.Evaluate(context));
		var power = ConvertToDouble(Power.Evaluate(context));

		return (TResult)(dynamic)Math.Pow(evaluation, power);
	}

	protected override IEvaluate<TResult> Reduction(ICatalog<IEvaluate<TResult>> catalog)
	{
		Debug.Assert(catalog is not null);
		var pow = catalog!.GetReduced(Power);
		IEvaluate<TResult> bas;
		if (pow is Constant<TResult> cPow)
		{
			dynamic p = cPow.Value;
			if (p == 0)
				return GetConstant(catalog, (dynamic)1);

			if (p == 1)
				return catalog.GetReduced(Base);

			bas = catalog.GetReduced(Base);
			if (bas is Exponent<TResult> basExp && basExp.Power is Constant<TResult> basePow)
			{
				// Multiply cascaded exponents.
				return catalog.Register(NewUsing(catalog, (basExp.Base, GetConstant(catalog, p * basePow.Value))));
			}
		}
		else
		{
			bas = catalog.GetReduced(Base);
		}

		return catalog.Register(NewUsing(catalog, (bas, pow)));
	}

	internal static Exponent<TResult> Create(
		ICatalog<IEvaluate<TResult>> catalog,
		IEvaluate<TResult> @base,
		IEvaluate<TResult> power)
	{
		if (catalog is null) throw new ArgumentNullException(nameof(catalog));
		if (@base is null) throw new ArgumentNullException(nameof(@base));
		if (power is null) throw new ArgumentNullException(nameof(power));
		Contract.EndContractBlock();

		// ReSharper disable once SuspiciousTypeConversion.Global
		return catalog is ICatalog<IEvaluate<double>> dCat
				&& @base is IEvaluate<double> b
				&& power is IEvaluate<double> p
			? (Exponent<TResult>)(dynamic)Exponent.Create(dCat, b, p)
			: catalog.Register(new Exponent<TResult>(@base, power));
	}

	public virtual IEvaluate<TResult> NewUsing(
		ICatalog<IEvaluate<TResult>> catalog,
		(IEvaluate<TResult>, IEvaluate<TResult>) param)
		=> Create(catalog, param.Item1, param.Item2);
}

public static partial class ExponentExtensions
{
	public static Exponent<TResult> GetExponent<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		IEvaluate<TResult> @base,
		IEvaluate<TResult> power)
		where TResult : notnull, IComparable<TResult>, IComparable
		=> Exponent<TResult>.Create(catalog, @base, power);

	public static bool IsPowerOf(this Exponent<double> exponent, in double power)
	{
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		Debug.Assert(exponent is not null);
		return exponent!.Power is Constant<double> p && p.Value == power;
	}

	public static bool IsSquareRoot(this Exponent<double> exponent)
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		=> exponent.IsPowerOf(0.5);
}
