/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Cloneable;
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

		IComparable IConstant.Value => Value;

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

	public static class ConstantExtensions
	{
		public static T GetConstant<T, TValue>(this Catalog catalog, TValue value, Func<TValue, T> factory)
			where TValue : IComparable
			where T : IConstant<TValue>
		{
			return catalog.Register(value.ToString(), k => factory(value));
		}

		public static Constant GetConstant(this Catalog catalog, double value, Func<double, Constant> factory)
		{
			return GetConstant<Constant, double>(catalog, value, factory);
		}

		public static Constant GetConstant(this Catalog catalog, double value)
		{
			return GetConstant(catalog, value, i => new Constant(value));
		}
	}

}