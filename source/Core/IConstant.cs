/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;
using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;

public interface IConstant : IEvaluate
{
	[NotNull]
	IComparable Value { get; }
}

public interface IConstant<out TResult> : IEvaluate<TResult>, IConstant
	where TResult : notnull, IComparable<TResult>, IComparable
{
	[NotNull]
	new TResult Value { get; }
}
