/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;

public interface IConstant: IEvaluate
{
	[NotNull]
	object Value { get; }
}

public interface IConstant<T> : IEvaluate<T>, IConstant
	where T : notnull, IEquatable<T>, IComparable<T>
{
	[NotNull]
	new T Value { get; }

	object IConstant.Value => Value;
}
