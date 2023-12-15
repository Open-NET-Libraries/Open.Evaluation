/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;

/// <summary>
/// An evaluation that it's value never changes.
/// </summary>
public interface IConstant : IEvaluate
{
	[NotNull]
	object Value { get; }
}

/// <inheritdoc cref="IConstant"/>
public interface IConstant<T> : IEvaluate<T>, IConstant
	where T : notnull, IEquatable<T>, IComparable<T>
{
	new T Value { get; }

	object IConstant.Value => Value;
}
