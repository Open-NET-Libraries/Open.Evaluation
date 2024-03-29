﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.Evaluation.Catalogs;
using Open.Evaluation.Core;

namespace Open.Evaluation.Tests;

public static class ParameterContextTests
{
	[TestMethod]
	public static void GetOrAddTest()
	{
		var catalog = new EvaluationCatalog<double>();
		// Shouldn't throw.
		var context = new ParameterContext(new object());
		var c0 = catalog.GetConstant(0);
		context.GetOrAdd(c0, _ => 0);
		var c1 = catalog.GetConstant(1);
		context.GetOrAdd(c1, _ => 1);
	}
}
