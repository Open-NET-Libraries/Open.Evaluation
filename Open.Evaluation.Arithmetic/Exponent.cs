/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System.Diagnostics.Contracts;
using System.Numerics;
using Throw;

namespace Open.Evaluation.Arithmetic;

// ReSharper disable once PossibleInfiniteInheritance
public class Exponent<TResult> : OperatorBase<TResult>,
	IReproducable<(IEvaluate<TResult>, IEvaluate<TResult>), IEvaluate<TResult>>
	where TResult : notnull, INumber<TResult>
{
	protected Exponent(
		IEvaluate<TResult> @base,
		IEvaluate<TResult> power)
		: base(Symbols.Exponent,
			  // Need to provide to children so a node tree can be built.
			  new[] { @base, power })
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

	protected override IEvaluate<TResult> Reduction(
		ICatalog<IEvaluate<TResult>> catalog)
	{
		catalog.ThrowIfNull().OnlyInDebug();

		var pow = catalog.GetReduced(Power);
		IEvaluate<TResult> bas;
		if (pow is Constant<TResult> cPow)
		{
			var p = cPow.Value;
			if (p == TResult.Zero)
				return catalog.GetConstant(TResult.One);

			if (p == TResult.One)
				return catalog.GetReduced(Base);

			bas = catalog.GetReduced(Base);
			if (bas is Exponent<TResult> basExp && basExp.Power is Constant<TResult> basePow)
			{
				// Multiply cascaded exponents.
				return catalog.Register(NewUsing(catalog, (basExp.Base, catalog.GetConstant(p * basePow.Value))));
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
		catalog.ThrowIfNull();
		@base.ThrowIfNull();
		power.ThrowIfNull();
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
		where TResult : notnull, INumber<TResult>
		=> Exponent<TResult>.Create(catalog, @base, power);

	public static bool IsPowerOf<T>(this Exponent<T> exponent, in T power)
		where T : notnull, INumber<T>
	{
		exponent.ThrowIfNull();
		return exponent.Power is Constant<T> p && p.Value == power;
	}

	public static bool IsSquareRoot(this Exponent<double> exponent)
		=> exponent.IsPowerOf(0.5);

	public static bool IsSquareRoot<T>(this Exponent<T> exponent)
		where T : notnull, INumber<T>, IFloatingPoint<T>, IDivisionOperators<T, T, T>
		=> exponent.IsPowerOf(Value<T>.Half);

	static class Value<T> where T : notnull, INumber<T>, IFloatingPoint<T>, IDivisionOperators<T, T, T>
	{
		public static readonly T Two = T.One + T.One;
		public static readonly T Half = T.One / Two;
	}
}
