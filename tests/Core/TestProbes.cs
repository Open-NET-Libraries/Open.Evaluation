namespace Open.Evaluation.Tests.Core;

/// <summary>
/// A minimal externally-defined <see cref="EvaluationBase{T}"/> subclass.
/// <see cref="EvaluationBase{T}"/>'s constructor is <c>protected</c>, which means any assembly can
/// define new node types - this is exploited here to directly COUNT how many times the library
/// actually invokes a node's evaluation logic for a given <see cref="Context"/>, which is a much
/// stronger proof of population-scoped memoization than re-checking the resulting value.
/// </summary>
internal sealed class CountingProbe : EvaluationBase<double>
{
	private readonly double _value;
	private readonly string _id;
	private int _evaluationCount;

	/// <summary>Total number of times <see cref="EvaluateInternal"/> has actually run.</summary>
	public int EvaluationCount => _evaluationCount;

	private CountingProbe(ICatalog<IEvaluate<double>> catalog, string id, double value)
		: base(catalog)
	{
		_id = id;
		_value = value;
	}

	protected override string Describe() => _id;

	protected override EvaluationResult<double> EvaluateInternal(Context context)
	{
		Interlocked.Increment(ref _evaluationCount);
		return EvaluationResult.Create(_value, _id);
	}

	/// <summary>Registers via the 3-arg (id, param, factory) <c>Catalog{T}.Register</c> overload.</summary>
	public static CountingProbe Create(ICatalog<IEvaluate<double>> catalog, string id, double value)
		=> catalog.Register(id, value, (regId, c, v) => new CountingProbe(c, regId, v));

	/// <summary>Registers via the 2-arg (id, factory) <c>Catalog{T}.Register</c> overload (no TParam).</summary>
	public static CountingProbe CreateViaTwoArgFactory(ICatalog<IEvaluate<double>> catalog, string id, double value)
		=> catalog.Register(id, (regId, c) => new CountingProbe(c, regId, value));
}

/// <summary>
/// A minimal <see cref="OperationBase{T}"/> subclass that does NOT override
/// <see cref="OperationBase{T}.GetReduction"/>, used to pin the base class's default
/// "no reduction available" contract (<c>GetReduction() =&gt; this</c>).
/// </summary>
internal sealed class NonReducibleOperation : OperationBase<double>
{
	private readonly string _id;

	public NonReducibleOperation(ICatalog<IEvaluate<double>> catalog, string id)
		: base(catalog, new Symbol('~', false))
		=> _id = id;

	protected override string Describe() => _id;

	protected override EvaluationResult<double> EvaluateInternal(Context context)
		=> EvaluationResult.Create(42d, _id);
}
