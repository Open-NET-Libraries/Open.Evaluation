/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */


namespace Open.Evaluation.Core
{
	public sealed class Parameter : Parameter<double>
	{

		Parameter(in ushort id) : base(in id)
		{
		}

		internal new static Parameter Create(in ICatalog<IEvaluate<double>> catalog, in ushort id)
		{
			var i = id;
			return catalog.Register(ToStringRepresentation(i), k => new Parameter(i));
		}

		public override IEvaluate NewUsing(in ICatalog<IEvaluate> catalog, in ushort id)
		{
			var i = id;
			return catalog.Register(ToStringRepresentation(i), k => new Parameter(i));
		}

	}

	public static partial class ParameterExtensions
	{
		public static Parameter GetParameter(
			this ICatalog<IEvaluate<double>> catalog, in ushort id)
		{
			return Parameter.Create(in catalog, in id);
		}
	}

}