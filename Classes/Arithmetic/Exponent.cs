/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation.ArithmeticOperators
{
    public class Exponent<TResult, TPower> : FunctionBase<TResult>
		where TResult : struct, IComparable
		where TPower : struct, IComparable
	{
		public Exponent(
			IEvaluate<TResult> evaluation,
			IEvaluate<TPower> power)
			: base(Exponent.SYMBOL, Exponent.SEPARATOR, evaluation)
		{
			Power = power;
			ChildrenInternal.Add(power);
		}

		public IEvaluate<TPower> Power
		{
			get;
			private set;
		}

		protected static double ConvertToDouble(dynamic value)
		{
			return (double)value;
		}
		protected override TResult EvaluateInternal(object context)
		{
			var evaluation = ConvertToDouble(base.Evaluate(context));
			var power = ConvertToDouble(Power.Evaluate(context));

			return (TResult)(dynamic)Math.Pow(evaluation, power);
		}

		protected override string ToStringRepresentationInternal()
		{
			return ToStringInternal(Evaluation.ToStringRepresentation(), Power.ToStringRepresentation());
		}

		public override IEvaluate<TResult> Reduction()
		{
			var pow = Power.AsReduced();
			var cPow = pow as Constant<TResult>;
			if (cPow != null)
			{
				dynamic p = cPow.Value;
				if (p == 0) return new Constant<TResult>((dynamic)1);
				if (p == 1) return Evaluation.AsReduced();
			}

			var result = new Exponent<TResult, TPower>(Evaluation.AsReduced(), pow);
			return result.ToStringRepresentation() == result.ToStringRepresentation() ? null : result;
		}

		protected string ToStringInternal(object contents, object power)
		{
			return string.Format("({0}^{1})", contents, power);
		}

		public override IEvaluate CreateNewFrom(object param, IEnumerable<IEvaluate> children)
		{
			return new Exponent<TResult, TPower>((IEvaluate<TResult>)children.Single(), (IEvaluate<TPower>)param);
		}
	}

	public class Exponent<TResult> : Exponent<TResult, TResult>
		where TResult : struct, IComparable
	{
		public Exponent(
			IEvaluate<TResult> evaluation,
			IEvaluate<TResult> power) : base(evaluation, power)
		{
		}

		public override IEvaluate CreateNewFrom(object param, IEnumerable<IEvaluate> children)
		{
			return new Exponent<TResult>((IEvaluate<TResult>)children.Single(), (IEvaluate<TResult>)param);
		}
	}


	public class Exponent : Exponent<double>
	{
		public const char SYMBOL = '^';
		public const string SEPARATOR = "^";

		public Exponent(IEvaluate<double> evaluation, IEvaluate<double> power) : base(evaluation, power)
		{
		}

		public Exponent(IEvaluate<double> evaluation, double power) : base(evaluation, new Constant<double>(power))
		{
		}

		public override IEvaluate CreateNewFrom(object param, IEnumerable<IEvaluate> children)
		{
			return new Exponent((IEvaluate<double>)children.Single(), (IEvaluate<double>)param);
		}
	}

	// Can handle better precision operations that are only positive integers.
	// Because any fractional or negative exponents can introduce precision error. 
	public class IntegerExponent<TResult, TPower> : Exponent<TResult, TPower>
		where TResult : struct, IComparable
		where TPower : struct, IComparable
	{
		public IntegerExponent(
			IEvaluate<TResult> evaluation,
			IEvaluate<TPower> power) : base(evaluation, power)
		{
			if (!IsIntergerType(typeof(TPower), out IsSignedPowerType))
				throw new InvalidOperationException("Incompatible power type for IntegerExponent.");
		}

		protected static bool IsIntergerType(Type type, out bool isSigned)
		{
			isSigned
				= type == typeof(long)
				|| type == typeof(int)
				|| type == typeof(short)
				|| type == typeof(sbyte);

			return isSigned
				|| type == typeof(uint)
				|| type == typeof(ushort)
				|| type == typeof(byte);
		}

		protected static bool IsIntergerType(Type type)
		{
			bool isSigned;
			return IsIntergerType(type, out isSigned);
		}

		readonly bool IsSignedPowerType;

		// Why is this good?  Because it avoids precision errors that would occur with the default double precision math.
		// It also avoids any type conversion.  Integers stay integers, floats stay floats and decimals stay decimals.
		// This makes a lot of sense when considering how common a number to the power of a positive integer is.
		protected override TResult EvaluateInternal(object context)
		{
			dynamic value = Evaluation.Evaluate(context);
			if (value == 0 || value == 1) return value;

			dynamic power = Power.Evaluate(context);
			if (value == 1) return value;
			if (value == 0) return (TResult)(dynamic)1;

			var isNegativePower = IsSignedPowerType && power < 0;
			if (isNegativePower)
			{
				if (IsIntergerType(typeof(TResult)))
				{
					// Futile to divide integer 1 by another integer;
					throw new InvalidOperationException("Applying a negative exponent to an integer type will always result in 0.");
				}

				for (long i = 0; i > power; i--) value *= value;

				return 1 / value;
			}
			else
			{
				for (ulong i = 0; i < power; i++) value *= value;

				return value;
			}
		}

		public override IEvaluate CreateNewFrom(object param, IEnumerable<IEvaluate> children)
		{
			return new IntegerExponent<TResult, TPower>((IEvaluate<TResult>)children.Single(), (IEvaluate<TPower>)param);
		}

	}

}