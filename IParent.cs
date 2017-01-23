using System.Collections.Generic;

namespace EvaluationEngine
{
	public interface IParent<TChild>
	{
		IReadOnlyList<TChild> Children { get; }
	}
}