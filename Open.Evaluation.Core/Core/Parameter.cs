namespace Open.Evaluation.Core;

[DebuggerDisplay(@"\{{Id}\}")]
public class Parameter<T>
	: EvaluationBase<T>,
		IParameter<T>,
		IReproducable<ushort, IEvaluate<T>>
	where T : notnull, IEquatable<T>, IComparable<T>
{
	protected Parameter(ICatalog<IEvaluate<T>> catalog, ushort id)
		: base(catalog)
		=> Id = id;

	public ushort Id { get; }

	[Pure]
	protected static string ToStringRepresentation(ushort id) => $"{{{id}}}";

	[Pure]
	protected override string Describe()
		=> ToStringRepresentation(Id);

	protected override EvaluationResult<T> EvaluateInternal(Context context)
	{
		// Debug-only guard: the sole call path (EvaluationBase<T>.Evaluate) has already
		// validated context, and Parameter evaluation is the per-leaf hot path.
		context.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		return context.TryGetResult(this, out EvaluationResult<T> result) ? result
			: throw new InvalidOperationException($"Parameter {Id} result not found in the context.");
	}

	internal static Parameter<T> Create(ICatalog<IEvaluate<T>> catalog, ushort id)
		=> catalog.Register(ToStringRepresentation(id), id, (_, c, id) => new Parameter<T>(c, id));

	internal static Parameter<T> Create(ICatalog<IEvaluate<T>> catalog, int id)
		=> Create(catalog,
			(ushort)id
				.Throw("The value of 'id' must be within the ushort range.")
				.IfOutOfRange(ushort.MinValue, ushort.MaxValue));

	public virtual Parameter<T> NewUsing(ICatalog<IEvaluate<T>> catalog, ushort param)
	{
		catalog.ThrowIfNull();
		Contract.EndContractBlock();

		return Create(catalog, param);
	}

	public Parameter<T> NewUsing(ushort param)
		=> NewUsing(Catalog, param);

	IEvaluate<T> IReproducable<ushort, IEvaluate<T>>.NewUsing(ICatalog<IEvaluate<T>> catalog, ushort param)
		=> NewUsing(Catalog, param);

	IEvaluate<T> IReproducable<ushort, IEvaluate<T>>.NewUsing(ushort param)
		=> NewUsing(param);
}

public static class ParameterExtensions
{
	public static IParameter<T> GetParameter<T>(
		this ICatalog<IEvaluate<T>> catalog, ushort id)
		where T : notnull, IEquatable<T>, IComparable<T>
	{
		catalog.ThrowIfNull();
		Contract.EndContractBlock();

		return Parameter<T>.Create(catalog, id);
	}

	public static IParameter<T> GetParameter<T>(
	this ICatalog<IEvaluate<T>> catalog, int id)
		where T : notnull, IEquatable<T>, IComparable<T>
	{
		catalog.ThrowIfNull();
		Contract.EndContractBlock();

		return id > ushort.MaxValue
			? throw new ArgumentOutOfRangeException(nameof(id), id, "Cannot exceed an unsigned 16-bit integer.")
			: (IParameter<T>)Parameter<T>.Create(catalog, (ushort)id);
	}
}
