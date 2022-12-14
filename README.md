# MMKiwi Collection Collection

This package is miscellaneous collections for .NET

## ImmutableKeyedCollection<TKey,TValue>

This collection is similar to the [KeyedCollection](https://learn.microsoft.com/en-us/dotnet/api/system.collections.objectmodel.keyedcollection-2)
class in the BCL, but it is immutable and is backed by an [ImmutableArray](https://learn.microsoft.com/en-us/dotnet/api/system.collections.immutable.immutablearray-1)
and [ImmutableDictionary](https://learn.microsoft.com/en-us/dotnet/api/system.collections.immutable.immutabledictionary-2).

This is an abstract class that needs to be overriden to be used. See the Keyed Collection documentation for an example.