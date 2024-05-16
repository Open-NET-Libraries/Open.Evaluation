using Open.Evaluation;
using Open.Evaluation.Catalogs;
using static Spectre.Console.AnsiConsole;

while (true)
{
	var expression = Ask<string>("Enter an expression:");
	if(string.IsNullOrWhiteSpace(expression)) break;
	expression = expression.Replace('x', '*');
	expression = expression.Replace('×', '*');
	//expression = expression.Replace('÷', '/');

	var catalog = new EvaluationCatalog<double>();
	try
	{
		var result = catalog.Parse('('+expression+')');
		var reduced = catalog.GetReduced(result);

		WriteLine(reduced.ToStringRepresentation());
	}
	catch (Exception ex)
	{
		WriteException(ex);
	}
}