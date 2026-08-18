/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Core;

/// <summary>
/// An expression that is undefined everywhere -- the result of reducing something that
/// constant-folds to an undefined value (e.g. division by a constant zero), or that contains
/// such a sub-expression. Reduction reports invalidity as this value rather than throwing, so
/// reduction doubles as the validity detector; consumers check for it before evaluating.
/// Non-generic so it can be tested for without knowing <c>T</c>, like <see cref="IParameter"/>
/// and <see cref="IConstant"/>.
/// </summary>
public interface IUndefined : IEvaluate;

/// <inheritdoc cref="IUndefined"/>
public interface IUndefined<T> : IEvaluate<T>, IUndefined
	where T : notnull, IEquatable<T>, IComparable<T>;
