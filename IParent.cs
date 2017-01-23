using System.Collections.Generic;

namespace EvaluationEngine
{
	public interface IParent<TChild>
	{
		ICollection<TChild> Children { get; }
	}
}