using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;
public interface IDescribe
{
	[NotNull]
	string Description { get; }
}
