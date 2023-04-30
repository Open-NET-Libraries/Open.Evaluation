/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System.Diagnostics.Contracts;
using Throw;

namespace Open.Evaluation.Arithmetic;

public class Sum : Sum<double>
{
	public const char SYMBOL = '+';
	public const string SEPARATOR = " + ";

	internal Sum(IEnumerable<IEvaluate<double>> children)
		: base(children)
	{ }

	public override IEvaluate<double> NewUsing(
		ICatalog<IEvaluate<double>> catalog,
		IEnumerable<IEvaluate<double>> param)
	{
		catalog.ThrowIfNull();
		if (param is null) throw new ArgumentNullException(nameof(param));
		Contract.EndContractBlock();

		var p = param as IEvaluate<double>[] ?? param.ToArray();
		return p.Length == 1 ? p[0] : Create(catalog, p);
	}
}
