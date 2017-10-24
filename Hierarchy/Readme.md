# Evaluation Hierarchy

## ```Node<T>```
The intention of ```Hierarchy.Node<T>``` and its supporting classes is to allow for creating and modifying tree structures that only have a parent to child relationship. All the evaluation classes are effectively immutable and cannot be changed after construction. The reason for all this is to guarantee the safe reuse of any evaluation tree within any other.  By guaranteeing only one instance of an evaluation tree (or branch per-se) within a ```Catalog<T>```, memory usage becomes signficantly less of an issue as well as preventing any bi-directional or circular references to evaluations.  Hence allowing for easier cleanup by the garbage collector.

## ```Node<T>.Factory```

### Mapping
Calling ```.Map(root)``` generates node hierarchy map based upon if the root or any of its children implement ```IParent```. 

### Cloning
Calling ```.Clone(node)``` creates a copy of the node map.

### Recycling
```Node<T>``` instances can be recycled by calling the ```.Recycle(node)``` method.  The node itself and its children are torn down and recycled to an object pool.
