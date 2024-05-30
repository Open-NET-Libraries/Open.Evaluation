using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;

/*
 * The main idea here is to enforce an immutable class and it's related sub classes.  No changes allowed after construction
 * A clone can only be created by 'recreating' or 'reconstructing'.
 */

public abstract class EvaluationBase<T>
	: IEvaluate<T>
		where T : notnull, IEquatable<T>, IComparable<T>
{
	protected EvaluationBase(ICatalog<IEvaluate<T>> catalog)
	{
		Description = new(Describe);
		Catalog = catalog;
	}

	/// <summary>
	/// The catalog this evaluation is associated with.
	/// </summary>
	public ICatalog<IEvaluate<T>> Catalog { get; }
	object IEvaluate.Catalog => Catalog;

	protected abstract string Describe();

	/// <summary>
	/// Provides the non-paramerterized description of this evaluation.
	/// </summary>
	[NotNull]
	public Lazy<string> Description { get; }

	public override string ToString() => Description.Value;
		//=> $"{GetType()} {Description.Value}";

	protected abstract EvaluationResult<T> EvaluateInternal(Context context); // **

	/// <inheritdoc />
	public EvaluationResult<T> Evaluate(Context context)
	{
		context.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		// Cache results...
		return context.GetOrAdd(this, () => EvaluateInternal(context))!;
	}
}
