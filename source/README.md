# Open.Evaluation

Open.Evaluation is a framework for constructing and evaluating any hierarchal formula or complex structure.  Typically used for evaluating math or algorithms.

For example: Given a specific mathmatical formula (or function), it is possible to not only resolve/evaluate the result given a set of parameters, it is also possible to inspect the formula's structure and see how the values are being used.

## Evaluation Classes

(See Core/EvaluationBase.cs)

### Immutability 

The main idea here is to envorce immutable classes and related sub classes.
No changes allowed after construction.  A clone can only be created by 'recreating' or 'reconstructing'.

It is very important to maintain this since these classes should not change during operation.

### Unidirectional Referencing

To avoid potential memory problems, all heirarchies should be unidirectional: "The parent is aware of the children, but the children are unaware of the parent."
This allows for child sharing and has the potential to signficantly reduce the memory footprint.