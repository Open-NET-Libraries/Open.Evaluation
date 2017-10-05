/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;

namespace Open.Evaluation
{
    public class Constant<TResult>
		: EvaluationBase<TResult>, IConstant<TResult>, ICloneable
		where TResult : IComparable
	{

		public Constant(TResult value) : base()
		{
			Value = value;
		}

		public TResult Value
		{
			get;
			private set;
		}

		protected override string ToStringRepresentationInternal()
		{
			return string.Empty + Value;
		}

		public Constant<TResult> Clone()
		{
			return new Constant<TResult>(Value);
		}


		object ICloneable.Clone()
		{
			return this.Clone();
		}

		protected override TResult EvaluateInternal(object context)
		{
			return Value;
		}

		protected override string ToStringInternal(object context)
		{
			return ToStringRepresentation();
		}

		public static Constant<TResult> operator +(Constant<TResult> a, Constant<TResult> b)
		{
			dynamic value = 0;
			value += a.Value;
			value += b.Value;
			return new Constant<TResult>(value);
		}

		public static Constant<TResult> operator *(Constant<TResult> a, Constant<TResult> b)
		{
			dynamic value = 1;
			value *= a.Value;
			value *= b.Value;
			return new Constant<TResult>(value);
		}

	}

	public sealed class Constant : Constant<double>
	{
		public Constant(double value) : base(value)
		{
		}
	}

}