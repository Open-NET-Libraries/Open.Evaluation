/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation
{
	public interface ICloneable
	{
		object Clone();
	}

	public interface ICloneable<out T> : ICloneable
	{
		new T Clone();
	}
}