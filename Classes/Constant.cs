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
			return Clone();
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
			return new Constant<TResult>((dynamic)a.Value + b.Value);
		}

		public static Constant<TResult> operator +(Constant<TResult> a, TResult b)
		{
			return new Constant<TResult>(a.Value + (dynamic)b);
		}

		public static Constant<TResult> operator +(TResult a, Constant<TResult> b)
		{
			return new Constant<TResult>((dynamic)a + b.Value);
		}

		public static Constant<TResult> operator *(Constant<TResult> a, Constant<TResult> b)
		{
			return new Constant<TResult>((dynamic)a.Value * b.Value);
		}

		public static Constant<TResult> operator *(Constant<TResult> a, TResult b)
		{
			return new Constant<TResult>(a.Value * (dynamic)b);
		}

		public static Constant<TResult> operator *(TResult a, Constant<TResult> b)
		{
			return new Constant<TResult>((dynamic)a * b.Value);
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
		public static T GetConstant<T, TValue>(this ICatalog<IEvaluate<TValue>> catalog, TValue value, Func<TValue, T> factory)
			where TValue : IComparable
			where T : IConstant<TValue>
		{
			return catalog.Register(value.ToString(), k => factory(value));
		}

		public static Constant<TValue> GetConstant<TValue>(this ICatalog<IEvaluate<TValue>> catalog, TValue value)
			where TValue : IComparable
		{
			return GetConstant(catalog, value, v=> new Constant<TValue>(v));
		}

		public static Constant GetConstant(this ICatalog<IEvaluate<double>> catalog, double value, Func<double, Constant> factory)
		{
			return GetConstant<Constant, double>(catalog, value, factory);
		}

		public static Constant GetConstant(this ICatalog<IEvaluate<double>> catalog, double value)
		{
			return GetConstant(catalog, value, i => new Constant(value));
		}
	}

}