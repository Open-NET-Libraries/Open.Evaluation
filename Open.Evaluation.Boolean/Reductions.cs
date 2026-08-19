/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Collections;
using Open.Disposable;
using Open.Evaluation.Core;
using Open.Hierarchy;

namespace Open.Evaluation.Boolean;

// Boolean reductions. Every rule below is an exact propositional identity -- unlike the real
// arithmetic, there is no domain to be careful about -- so a reduction is always equivalent to
// its source and the reduced form is a genuine canonical, smaller spelling of the same function.
// That gives consumers two things at once: algebraic duplicates share one reduced identity, and a
// reduced variant with identical fitness and fewer nodes wins on size -- parsimony without a
// separate mechanism. Reductions never throw.
//
// Not:  !!x → x;  !true → false;  !false → true
// And:  flatten;  x & x → x;  x & !x → false;  false & … → false;  true dropped;
//       a & (a | b) → a (absorption);  () → true;  (x) → x
// Or:   the dual:  x | x → x;  x | !x → true;  true | … → true;  false dropped;
//       a | (a & b) → a;  () → false;  (x) → x
// Conditional (any T):  c ? x : x → x;  true ? a : b → a;  false ? a : b → b;
//                       !c ? a : b → c ? b : a
// Conditional (bool):   c ? c : b → c | b;   c ? a : c → c & a;
//                       c ? true : b → c | b;  c ? false : b → !c & b;
//                       c ? a : true → !c | a;  c ? a : false → c & a

public sealed partial class Not
{
	public override IEvaluate<bool> GetReduction()
	{
		IEvaluate<bool> child = Catalog.GetReduced(Children[0]);
		return child switch
		{
			Not inner => inner.Children[0],
			IConstant<bool> k => Catalog.GetConstant(!k.Value),
			_ => child == Children[0] ? this : Catalog.Not(child),
		};
	}
}

public sealed partial class And
{
	public override IEvaluate<bool> GetReduction()
	{
		using var lease = ListPool<IEvaluate<bool>>.Shared.Rent();
		List<IEvaluate<bool>> children = lease.Item;
		bool changed = false;
		foreach (IEvaluate<bool> c in Catalog.Flatten(Children, static p => p is And))
		{
			if (c is IConstant<bool> k)
			{
				if (!k.Value) return Catalog.GetConstant(false); // false & … → false
				changed = true; // true is the identity: drop it
				continue;
			}

			if (children.Contains(c)) { changed = true; continue; } // x & x → x
			children.Add(c);
		}

		if (children.Count != Children.Length) changed = true;
		else for (int i = 0; i < children.Count && !changed; i++) changed = children[i] != Children[i];

		// x & !x → false
		foreach (IEvaluate<bool> c in children)
		{
			if (c is Not n && children.Contains(n.Children[0]))
				return Catalog.GetConstant(false);
		}

		// Absorption: a & (a | b) → a -- drop any Or that contains another operand.
		for (int i = children.Count - 1; i >= 0; i--)
		{
			if (children[i] is not Or or) continue;
			foreach (IEvaluate<bool> other in children)
			{
				if (other != or && or.Children.Contains(other))
				{
					children.RemoveAt(i);
					changed = true;
					break;
				}
			}
		}

		return children.Count switch
		{
			0 => Catalog.GetConstant(true),
			1 => children[0],
			_ => changed ? Catalog.And(children.ToArray()) : this,
		};
	}
}

public sealed partial class Or
{
	public override IEvaluate<bool> GetReduction()
	{
		using var lease = ListPool<IEvaluate<bool>>.Shared.Rent();
		List<IEvaluate<bool>> children = lease.Item;
		bool changed = false;
		foreach (IEvaluate<bool> c in Catalog.Flatten(Children, static p => p is Or))
		{
			if (c is IConstant<bool> k)
			{
				if (k.Value) return Catalog.GetConstant(true); // true | … → true
				changed = true; // false is the identity: drop it
				continue;
			}

			if (children.Contains(c)) { changed = true; continue; } // x | x → x
			children.Add(c);
		}

		if (children.Count != Children.Length) changed = true;
		else for (int i = 0; i < children.Count && !changed; i++) changed = children[i] != Children[i];

		// x | !x → true
		foreach (IEvaluate<bool> c in children)
		{
			if (c is Not n && children.Contains(n.Children[0]))
				return Catalog.GetConstant(true);
		}

		// Absorption: a | (a & b) → a -- drop any And that contains another operand.
		for (int i = children.Count - 1; i >= 0; i--)
		{
			if (children[i] is not And and) continue;
			foreach (IEvaluate<bool> other in children)
			{
				if (other != and && and.Children.Contains(other))
				{
					children.RemoveAt(i);
					changed = true;
					break;
				}
			}
		}

		return children.Count switch
		{
			0 => Catalog.GetConstant(false),
			1 => children[0],
			_ => changed ? Catalog.Or(children.ToArray()) : this,
		};
	}
}

public sealed partial class Conditional<T>
{
	public override IEvaluate<T> GetReduction()
	{
		IEvaluate<T> ifTrue = Catalog.GetReduced(IfTrue);
		IEvaluate<T> ifFalse = Catalog.GetReduced(IfFalse);
		// The condition lives in a bool catalog (the same catalog when T is bool); reduce it there.
		IEvaluate<bool> condition = Condition is IReducibleEvaluation<IEvaluate<bool>> rc && rc.TryGetReduced(out IEvaluate<bool>? reducedCondition)
			? reducedCondition
			: Condition;

		// c ? x : x → x
		if (ifTrue == ifFalse) return ifTrue;

		// Constant condition selects a branch.
		if (condition is IConstant<bool> k) return k.Value ? ifTrue : ifFalse;

		// !c ? a : b → c ? b : a
		if (condition is Not not)
		{
			condition = not.Children[0];
			(ifTrue, ifFalse) = (ifFalse, ifTrue);
		}

		// Boolean-valued conditionals collapse to And/Or where a branch repeats the condition
		// or is a constant. Pattern-matched, so this is a no-op for any other T.
		if (Catalog is ICatalog<IEvaluate<bool>> bools
			&& ifTrue is IEvaluate<bool> t
			&& ifFalse is IEvaluate<bool> f)
		{
			IEvaluate<bool>? collapsed = null;
			if (t == condition) collapsed = bools.Or([condition, f]);                            // c ? c : b → c | b
			else if (f == condition) collapsed = bools.And([condition, t]);                       // c ? a : c → c & a
			else if (t is IConstant<bool> kt) collapsed = kt.Value ? bools.Or([condition, f]) : bools.And([bools.Not(condition), f]);
			else if (f is IConstant<bool> kf) collapsed = kf.Value ? bools.Or([bools.Not(condition), t]) : bools.And([condition, t]);

			if (collapsed is IEvaluate<T> result)
				return Catalog.GetReduced(result);
		}

		return condition == Condition && ifTrue == IfTrue && ifFalse == IfFalse
			? this
			: Catalog.Conditional((condition, ifTrue, ifFalse));
	}
}
