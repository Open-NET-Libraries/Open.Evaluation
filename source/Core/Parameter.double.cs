/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */


namespace Open.Evaluation.Core
{
	public sealed class Parameter : Parameter<double>
	{

		Parameter(ushort id) : base(id)
		{
		}

		internal new static Parameter Create(ICatalog<IEvaluate<double>> catalog, ushort id)
		{
			var i = id;
			return catalog.Register(ToStringRepresentation(i), k => new Parameter(i));
		}

		public override IEvaluate<double> NewUsing(ICatalog<IEvaluate<double>> catalog, ushort id)
		{
			var i = id;
			return catalog.Register(ToStringRepresentation(i), k => new Parameter(i));
		}

	}

	public static partial class ParameterExtensions
	{
		public static Parameter GetParameter(
			this ICatalog<IEvaluate<double>> catalog, ushort id)
			=> Parameter.Create(catalog, id);

		public static Parameter GetParameter(
			this ICatalog<IEvaluate<double>> catalog, int id)
		{
			if (id < 0 || id > ushort.MaxValue)
				throw new System.ArgumentOutOfRangeException(nameof(id));
			return Parameter.Create(catalog, (ushort)id);
		}
	}

}
