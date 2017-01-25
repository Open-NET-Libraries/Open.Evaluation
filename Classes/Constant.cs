namespace EvaluationFramework
{
	public sealed class Constant<TResult>
		: EvaluationBase<object, TResult>, IConstant<TResult>, IClonable<Constant<TResult>>
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

		public override TResult Evaluate(object context)
		{
			return Value;
		}

		public override string ToString(object context)
		{
			return ToStringRepresentation();
		}
	}
}