namespace EvaluationEngine
{

	public interface IClonable<out T>
	{
		T Clone();
	}
}