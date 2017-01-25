using System.Collections.Generic;

namespace EvaluationFramework
{
	public interface IParent<TChild>
	{
		IReadOnlyList<TChild> Children { get; }
	}
}