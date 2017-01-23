# Arithmetic Evaluations

## Operators

Operators take any number of sub/child evaluations and produce a modified result from them.

### Basic

#### ```Sum<TContext>```

The ```Sum``` operator simply adds the values of the provided evaluations.

#### ```Product<TContext>```

The ```Product``` operator simply multiplies the values of the provided evaluations.

#### ```Exponent<TContext>```

The ```Exponent``` operator multiplies the value of the provided evaluation to the provided power value.

##### NOTE

Exponent also serves as a means for ***division*** since ```x^-1``` is the same as ```1/x```.

It is also easy to reduce exponents since ```x^+1 * x^-1 == 1```.  Exponents can easily cancel.

And lastly, serves a means for roots like a square root ( ```9^(1/2) == 3``` ).

##### WARNING

Any operations other than positive integer exponents could introduce precision error.