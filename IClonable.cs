namespace EvaluationFramework
{

	public interface IClonable<out T>
	{
		T Clone();
	}
}