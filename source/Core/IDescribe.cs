using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;
public interface IDescribe
{
	[NotNull]
	Lazy<string> Description { get; }
}
