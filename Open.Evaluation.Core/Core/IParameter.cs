/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Core;

/// <summary>
/// A parameter.
/// </summary>
public interface IParameter : IEvaluate
{
	ushort Id { get; }
}

/// <inheritdoc cref="IParameter"/>
public interface IParameter<T> : IEvaluate<T>, IParameter
	where T : notnull, IEquatable<T>, IComparable<T>;
