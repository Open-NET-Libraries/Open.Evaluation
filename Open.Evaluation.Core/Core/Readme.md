# Open.Evaluation.Core

Classes should not be constructable directly from outside this namespace.
(All constructors should be internal or protected.)

Generating instances occurs through a ```Catalog``` and extensions defined within each core class file.

By ensuring this contract, immutibily is guaranteed, and instances are effectively 'owned' by a catalog.