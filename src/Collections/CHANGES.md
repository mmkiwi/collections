# Changelog

## 0.2.1

* Added changelog

### Bug fixes

* Fixed bug NotImplementedException for ICollection&lt;TItem&gt;.Count, IList.IsFixedSize, and ICollection.IsSynchronized.

### Backwards-incompatible changes (Minor)

* ICollection.SyncRoot now throws NotSupportedException instead of NotImplementedException()

## 0.2.0

### Backwards-incompatible changes

* Updated assembly name to MMKiwi.Collections

## 0.1.1

### New features

* Switched to Polysharp polyfill for .NET standard (removed the dependency on Nullable)

## 0.1.0

### New features

* Added CLSCompliant attribute

## 0.1.0-beta

* Initial release implementing ImmutableKeyedCollection<TKey,TValue>