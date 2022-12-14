/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */
 
using System.Collections;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace MMKiwi.Collections;

/// <summary>
/// The abstract base class for an immutable collection whose keys are embedded in the values.
/// </summary>
/// <remarks>
/// This class operates similarly to <see cref="KeyedCollection{TKey, TItem}" />. It is backed by an 
/// <see cref="ImmutableArray{T}" /> and an <see cref="ImmutableDictionary{TKey, TValue}" />, but unlike those classes,
/// you can not create a new instance using methods such as <see cref="ImmutableArray{T}.Add(T)" />. To create a new
/// instance, use the methods on the <see cref="Items" /> property and create a new instance of the subclass.
/// </remarks>
/// <typeparam name="TKey">The type of keys in the collection.</typeparam>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
public abstract class ImmutableKeyedCollection<TKey, TItem> : IEquatable<ImmutableKeyedCollection<TKey, TItem>>,
    ICollection<TItem>, IEnumerable<TItem>, IList<TItem>, IReadOnlyCollection<TItem>, IReadOnlyList<TItem>, IList
    where TKey : notnull
{
    /// <summary>
    ///   Initializes a new instance of the <see cref="ImmutableKeyedCollection{TKey, TItem}" /> class.
    /// </summary>
    /// <param name="baseCollection">
    ///   The collection to create the keyed collection off of. No copy of this collection is made
    /// </param>
    /// <param name="comparer">
    ///   The implementation of the <see cref="IEqualityComparer{T}" /> generic interface to usewhen comparing keys, or
    ///   null to use the default equality comparer for the type of the key, obtained from Default.
    /// </param>
    /// <param name="dictionaryCreationThreshold">
    ///   The number of elements the collection can hold without creating a lookup dictionary (0 creates the lookup
    ///   dictionary when the first item is added), or -1 to specify that a lookup dictionary is never created.
    /// </param>
    /// <throws cref="ArgumentOutOfRangeException">dictionaryCreationThreshold is less than -1.</throws>
    protected ImmutableKeyedCollection(ImmutableArray<TItem> baseCollection, IEqualityComparer<TKey>? comparer = null, int dictionaryCreationThreshold = DefaultThreshold)
    // Be explicit about the use of List<T> so we can foreach over
    // Items internally without enumerator allocations.
    {
        if (dictionaryCreationThreshold < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(dictionaryCreationThreshold), "The specified threshold for creating dictionary must be -1 or greater.");
        }

        Items = baseCollection;

        Comparer = comparer ?? EqualityComparer<TKey>.Default;
        Threshold = dictionaryCreationThreshold == -1 ? int.MaxValue : dictionaryCreationThreshold;

        if (baseCollection.Length >= Threshold)
            Dictionary = CreateDictionary();
    }
    /// <summary>
    ///   Gets the generic equality comparer that is used to determine equality of keys in the collection.
    /// </summary>
    /// <value>
    ///   The implementation of the <see cref="IEqualityComparer{T}" /> generic interface that is used to determine
    ///   equality of keys in the collection.
    /// </value>
    public IEqualityComparer<TKey> Comparer { get; }

    /// <summary>
    /// The backing <see cref="ImmutableArray{T}" /> for the collection.
    /// </summary>
    public ImmutableArray<TItem> Items { get; }

    private const int DefaultThreshold = 0;

    /// <summary>
    /// The treshold for generating a backing dictionary. 0 by default.
    /// </summary>
    private int Threshold { get; }

    /// <summary>
    /// Gets the lookup dictionary of the <see cref="ImmutableKeyedCollection{TKey, TItem}" />
    /// </summary>
    /// <value>
    ///   The lookup dictionary of the <see cref="ImmutableKeyedCollection{TKey, TItem}" />, if it exists;
    ///   otherwise, null.
    /// </value>
    protected ImmutableDictionary<TKey, TItem>? Dictionary { get; }

    /// <summary>
    /// Gets the element with the specified key.
    /// </summary>
    /// <param name="key">The key of the element to get.</param>
    /// <value>
    ///   The element with the specified key. If an element with the specified key is not found, an exception is thrown.
    /// </value>
    public TItem this[TKey key]
    {
        get
        {
            TItem item;
            if (TryGetValue(key, out item!))
            {
                return item;
            }

            throw new KeyNotFoundException($"The given key '{key}' was not present in the dictionary.");
        }
    }

    /// <summary>
    /// Gets the element with the specified key.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <value>
    ///   The element at the specified index.
    /// </value>
    public TItem this[int index] => Items[index];

    /// <summary>
    /// Determines whether the collection contains an element with the specified key.
    /// </summary>
    /// <param name="key">The key to locate in the <see cref="ImmutableKeyedCollection{TKey, TItem}" /></param>
    /// <returns>
    ///   true if the <see cref="ImmutableKeyedCollection{TKey, TItem}" /> contains an element with the specified key;
    ///   otherwise, false.
    /// </returns>
    public bool Contains(TKey key)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (Dictionary != null)
        {
            return Dictionary.ContainsKey(key);
        }

        foreach (TItem item in Items)
        {
            if (Comparer.Equals(GetKeyForItem(item), key))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Determines whether the collection contains the specified item.
    /// </summary>
    /// <param name="item">The item to locate in the <see cref="ImmutableKeyedCollection{TKey, TItem}" /></param>
    /// <returns>
    ///   true if the <see cref="ImmutableKeyedCollection{TKey, TItem}" /> contains the specified item;
    ///   otherwise, false.
    /// </returns>
    public bool Contains(TItem item)
    {
        TKey key;
        if ((Dictionary == null) || ((key = GetKeyForItem(item)) == null))
        {
            return Items.Contains(item);
        }

        TItem itemInDict;
        if (Dictionary.TryGetValue(key, out itemInDict!))
        {
            return EqualityComparer<TItem>.Default.Equals(itemInDict, item);
        }

        return false;
    }

    /// <summary>
    /// Gets the number of elements in the collection.
    /// </summary>
    public int Count => Items.Length;

    /// <summary>
    /// Tries to get an item from the collection using the specified key.
    /// </summary>
    /// <param name="key">The key of the item to search in the collection.</param>
    /// <param name="item">
    ///  When this method returns <c>true</c>, the item from the collection that matches the provided key; when this
    ///   method returns <c>false</c>, the default value for the type of the collection.
    /// </param>
    /// <returns>
    ///   <c>true</c> if an item for the specified key was found in the collection; otherwise, <c>false.</c>
    /// </returns>
    /// <throws cref="ArgumentNullException"><c>key</c> is <c>null</c></throws>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TItem item)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (Dictionary != null)
        {
            return Dictionary.TryGetValue(key, out item!);
        }

        foreach (TItem currItem in Items)
        {
            TKey itemKey = GetKeyForItem(currItem);
            if (itemKey != null && Comparer.Equals(key, itemKey))
            {
                item = currItem;
                return true;
            }
        }

        item = default;
        return false;
    }

    /// <summary>
    /// When implemented in a derived class, extracts the key from the specified element.
    /// </summary>
    /// <param name="item">The element from which to extract the key.</param>
    /// <returns>The key for the specified element.</returns>
    protected abstract TKey GetKeyForItem(TItem item);

    private ImmutableDictionary<TKey, TItem> CreateDictionary()
    {
        var dictBuilder = ImmutableDictionary.CreateBuilder<TKey, TItem>(Comparer);
        foreach (TItem item in Items)
        {
            TKey key = GetKeyForItem(item);
            if (key != null)
            {
                dictBuilder.Add(key, item);
            }
        }
        return dictBuilder.ToImmutable();
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to this instance.
    /// </summary>
    /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
    /// <returns>
    ///   <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is ImmutableKeyedCollection<TKey, TItem> other && this.Items == other.Items;
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
    /// </returns>
    public override int GetHashCode() => Items.GetHashCode();

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
    /// </returns>
    public bool Equals(ImmutableKeyedCollection<TKey, TItem>? other)
    {
        return other != null && Items == other.Items;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the contents of the array.
    /// </summary>
    /// <returns>An enumerator</returns>
    public IEnumerator<TItem> GetEnumerator() => ((IEnumerable<TItem>)Items).GetEnumerator();

    /// <summary>
    /// Searches the array for the specified item.
    /// </summary>
    /// <param name="item">The item to search for.</param>
    /// <returns>The zero-based index position of the item if it is found, or -1 if it is not.</returns>
    public int IndexOf(TItem item) => Items.IndexOf(item);

    #region Explicit Implementations
    bool ICollection<TItem>.Remove(TItem item) => throw new NotSupportedException();

    void ICollection<TItem>.Add(TItem item) => throw new NotSupportedException();

    void ICollection<TItem>.Clear() => throw new NotSupportedException();
    bool ICollection<TItem>.IsReadOnly => true;

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Items).GetEnumerator();
    void IList<TItem>.Insert(int index, TItem item) => throw new NotSupportedException();

    void IList<TItem>.RemoveAt(int index) => throw new NotSupportedException();

    int IList.Add(object? value) => throw new NotSupportedException();

    void IList.Clear() => throw new NotSupportedException();

    bool IList.Contains(object? value) => value is TItem item && Contains(item);

    int IList.IndexOf(object? value) => ((IList)Items).IndexOf(value);

    void IList.Insert(int index, object? value) => throw new NotSupportedException();

    void IList.Remove(object? value) => throw new NotSupportedException();

    void IList.RemoveAt(int index) => throw new NotSupportedException();

    void ICollection.CopyTo(Array array, int index) => ((ICollection)Items).CopyTo(array, index);

    void ICollection<TItem>.CopyTo(TItem[] array, int arrayIndex) => Items.CopyTo(array, arrayIndex);

    bool IList.IsReadOnly => true;

    int ICollection<TItem>.Count => throw new NotImplementedException();

    bool IList.IsFixedSize => throw new NotImplementedException();

    bool ICollection.IsSynchronized => throw new NotImplementedException();

    object ICollection.SyncRoot => throw new NotImplementedException();

    TItem IList<TItem>.this[int index] { get => this[index]; set => throw new NotSupportedException(); }
    object? IList.this[int index] { get => this[index]; set => throw new NotSupportedException(); }
    #endregion
}