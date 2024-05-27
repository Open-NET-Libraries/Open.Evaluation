/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Core;

[DebuggerDisplay(@"\{{ID}\}")]
public class Parameter<T>
	: EvaluationBase<T>,
		IParameter<T>,
		IReproducable<ushort,
		IEvaluate<T>>
	where T : notnull, IEquatable<T>, IComparable<T>
{
	protected Parameter(ushort id)
		=> ID = id;

	public ushort ID { get; }

	protected static string ToStringRepresentation(ushort id) => $"{{{id}}}";

	protected override string Describe()
		=> ToStringRepresentation(ID);

	protected override EvaluationResult<T> EvaluateInternal(Context context)
		=> context.TryGetResult(this, out EvaluationResult<T> result) ? result
			: throw new InvalidOperationException($"Parameter {ID} result not found in the context.");

	internal static Parameter<T> Create(ICatalog<IEvaluate<T>> catalog, ushort id)
		=> catalog.Register(ToStringRepresentation(id), id, (_, id) => new Parameter<T>(id));

	internal static Parameter<T> Create(ICatalog<IEvaluate<T>> catalog, int id)
		=> Create(catalog,
			(ushort)id
				.Throw("The value of 'id' must be within the ushort range.")
				.IfOutOfRange(ushort.MinValue, ushort.MaxValue));

	public virtual IEvaluate<T> NewUsing(ICatalog<IEvaluate<T>> catalog, ushort param)
		=> catalog.Register(ToStringRepresentation(param), param, (_, id) => new Parameter<T>(id));
}

public static class ParameterExtensions
{
	public static IParameter<T> GetParameter<T>(
		this ICatalog<IEvaluate<T>> catalog, ushort id)
		where T : notnull, IEquatable<T>, IComparable<T>
		=> Parameter<T>.Create(catalog, id);

	public static IParameter<T> GetParameter<T>(
	this ICatalog<IEvaluate<T>> catalog, int id)
		where T : notnull, IEquatable<T>, IComparable<T>
		=> id > ushort.MaxValue
			? throw new ArgumentOutOfRangeException(nameof(id), id, "Cannot exceed an unsigned 16-bit integer.")
			: (IParameter<T>)Parameter<T>.Create(catalog, (ushort)id);
}
