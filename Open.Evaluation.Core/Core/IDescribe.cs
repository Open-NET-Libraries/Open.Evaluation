namespace Open.Evaluation.Core;

/// <summary>
/// Anything that can be described.
/// </summary>
public interface IDescribe
{
	Lazy<string> Description { get; }
}
