using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;

/// <summary>
/// Anything that can be described.
/// </summary>
public interface IDescribe
{
	[NotNull]
	Lazy<string> Description { get; }
}
