# Boolean Evaluations

## Operators

Operators take any number of sub/child evaluations and produce a modified result from them.

### Standard

#### ```Not<TContext>```

The ```Not``` operator simply negates the value of the provided evaluation.

#### ```And<TContext>```

The ```And``` operator takes any number of evaluations
and *returns **```true```** if the all the child evaluation values are true*.

#### ```Or<TContext>```

The ```Or``` operator takes any number of evaluations
and *returns **```true```** if any one of the child evaluation values are true*.

#### ```Conditional<TContext>```

The ```Conditional``` operator takes the provided condition and
and *returns the result of other ```true``` or ```false``` evaluations depending on the condition value*.


### Counting (Fuzzy Logic)

#### ```Exactly<TContext>```

The ```Exactly``` operator takes any number of evaluations
and *returns **```true```** if the total number of ```true``` evaluations matches the specified count*.

**NOTE:** This is one means to expressiong an XOR operator.

#### ```AtLeast<TContext>```

The ```AtLeast``` operator *returns **```true```** if the total number of ```true``` evaluations is greater than or equal to the specified count*.

#### ```AtMost<TContext>```

The ```AtMost``` operator *returns **```true```** if the total number of ```true``` evaluations is less than or equal to the specified count*.