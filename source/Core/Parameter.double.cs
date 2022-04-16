/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Core;

public sealed class Parameter : Parameter<double>
{
	Parameter(ushort id) : base(id)
	{
	}

	internal new static Parameter Create(ICatalog<IEvaluate<double>> catalog, ushort id)
		=> catalog.Register(ToStringRepresentation(id), id, (_,id) => new Parameter(id));

	public override IEvaluate<double> NewUsing(ICatalog<IEvaluate<double>> catalog, ushort id)
		=> catalog.Register(ToStringRepresentation(id), id, (_, id) => new Parameter(id));
}

public static partial class ParameterExtensions
{
	public static Parameter GetParameter(this ICatalog<IEvaluate<double>> catalog, ushort id)
		=> Parameter.Create(catalog, id);

	public static Parameter GetParameter(this ICatalog<IEvaluate<double>> catalog, in int id)
		=> id is < 0 or > ushort.MaxValue
			? throw new System.ArgumentOutOfRangeException(nameof(id))
			: Parameter.Create(catalog, (ushort)id);
}
