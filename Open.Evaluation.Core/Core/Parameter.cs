/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;

[DebuggerDisplay(@"\{{ID}\}")]
public class Parameter<T>
	: EvaluationBase<T>,
		IParameter<T>,
		IReproducable<ushort,
		IEvaluate<T>>
	where T : notnull, IEquatable<T>, IComparable<T>
{
	protected Parameter(ushort id, Func<object, ushort, T>? evaluator = null)
	{
		_evaluator = evaluator ?? GetParamValueFrom;
		ID = id;
	}

	readonly Func<object, ushort, T> _evaluator;

	static readonly Func<object, ushort, T> GetParamValueFrom
		= (object source, ushort id) => source switch
		{
			IReadOnlyList<T> list => list[id],
			IDictionary<ushort, T> d => d[id],
			T v => v,

			_ => throw new ArgumentException("Unknown type.", nameof(source)),
		};

	public ushort ID { get; }

	protected static string ToStringRepresentation(ushort id) => $"{{{id}}}";

	protected override string Describe()
		=> ToStringRepresentation(ID);

	protected override EvaluationResult<T> EvaluateInternal(object context)
	{
		var value = _evaluator(context is Context p ? p.Context : context, ID);
		Debug.Assert(value is not null);

		return new(value, v =>
		{
			var text = v.ToString();
			Debug.Assert(text is not null);
			return text;
		});
	}

	internal static Parameter<T> Create(ICatalog<IEvaluate<T>> catalog, ushort id)
		=> catalog.Register(ToStringRepresentation(id), id, (_, id) => new Parameter<T>(id));

	public virtual IEvaluate<T> NewUsing(ICatalog<IEvaluate<T>> catalog, ushort param)
		=> catalog.Register(ToStringRepresentation(param), param, (_, id) => new Parameter<T>(id));
}

public static partial class ParameterExtensions
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
