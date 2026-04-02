using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Ayla;

public static class OrderedDictionary
{
    public const int kSelectorIndex_NewElement = -2;
}

[Serializable]
public class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [Serializable]
    private struct KeyValuePair
    {
        public TKey Key;
        public TValue Value;

        public KeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public static implicit operator KeyValuePair<TKey, TValue>(KeyValuePair value)
        {
            return new KeyValuePair<TKey, TValue>(value.Key, value.Value);
        }
    }

    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private readonly List<KeyValuePair> m_List;
        private int m_Index;

        public Enumerator(object list)
        {
            m_List = (List<KeyValuePair>)list;
            m_Index = -1;
        }

        public readonly KeyValuePair<TKey, TValue> Current => m_List[m_Index];

        readonly object? IEnumerator.Current => Current;

        public readonly void Dispose()
        {
        }

        public bool MoveNext()
        {
            return ++m_Index < m_List.Count;
        }

        public void Reset()
        {
            m_Index = -1;
        }
    }

    public readonly struct KeyCollection : ICollection<TKey>
    {
        public struct Enumerator : IEnumerator<TKey>
        {
            private readonly List<KeyValuePair<TKey, TValue>> m_Rows;
            private int m_Index;

            public Enumerator(List<KeyValuePair<TKey, TValue>> rows)
            {
                m_Rows = rows;
                m_Index = -1;
            }

            public readonly TKey Current => m_Rows[m_Index].Key;

            readonly object? IEnumerator.Current => Current;

            public readonly void Dispose()
            {
            }

            public bool MoveNext()
            {
                return ++m_Index < m_Rows.Count;
            }

            public void Reset()
            {
                m_Index = -1;
            }
        }

        private readonly List<KeyValuePair<TKey, TValue>> m_Rows;

        public KeyCollection(List<KeyValuePair<TKey, TValue>> list)
        {
            m_Rows = list;
        }

        public int Count => m_Rows.Count;

        public bool IsReadOnly => true;

        public void Add(TKey item)
        {
            throw new InvalidOperationException();
        }

        public void Clear()
        {
            throw new InvalidOperationException();
        }

        public bool Contains(TKey item)
        {
            for (int i = 0; i < m_Rows.Count; ++i)
            {
                if (m_Rows[i].Key?.Equals(item) == true)
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(TKey[] array, int arrayIndex)
        {
            for (int i = 0; i < m_Rows.Count; ++i)
            {
                if (arrayIndex + i >= array.Length)
                {
                    throw new ArgumentException("Array is not large enough to hold the keys.");
                }
                array[arrayIndex + i] = m_Rows[i].Key;
            }
        }

        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => GetEnumerator();

        public Enumerator GetEnumerator() => new(m_Rows);

        public bool Remove(TKey item)
        {
            throw new InvalidOperationException();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public readonly struct ValueCollection : ICollection<TValue>
    {
        public struct Enumerator : IEnumerator<TValue>
        {
            private readonly List<KeyValuePair<TKey, TValue>> m_Rows;
            private int m_Index;

            public Enumerator(List<KeyValuePair<TKey, TValue>> rows)
            {
                m_Rows = rows;
                m_Index = -1;
            }

            public readonly TValue Current => m_Rows[m_Index].Value;

            readonly object? IEnumerator.Current => Current;

            public readonly void Dispose()
            {
            }

            public bool MoveNext()
            {
                return ++m_Index < m_Rows.Count;
            }

            public void Reset()
            {
                m_Index = -1;
            }
        }

        private readonly List<KeyValuePair<TKey, TValue>> m_Rows;

        public ValueCollection(List<KeyValuePair<TKey, TValue>> list)
        {
            m_Rows = list;
        }

        public int Count => m_Rows.Count;

        public bool IsReadOnly => true;

        public void Add(TValue item)
        {
            throw new InvalidOperationException();
        }

        public void Clear()
        {
            throw new InvalidOperationException();
        }

        public bool Contains(TValue item)
        {
            for (int i = 0; i < m_Rows.Count; ++i)
            {
                if (m_Rows[i].Value?.Equals(item) == true)
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            for (int i = 0; i < m_Rows.Count; ++i)
            {
                if (arrayIndex + i >= array.Length)
                {
                    throw new ArgumentException("Array is not large enough to hold the values.");
                }
                array[arrayIndex + i] = m_Rows[i].Value;
            }
        }

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();

        public Enumerator GetEnumerator() => new(m_Rows);

        public bool Remove(TValue item)
        {
            throw new InvalidOperationException();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [SerializeField]
    private List<KeyValuePair> m_Rows = new();
#if UNITY_EDITOR
#pragma warning disable CS0414
    [SerializeField]
    private int m_Selector = -1;
#pragma warning restore CS0414
#endif

    private readonly Dictionary<TKey, int> m_Index = new();

    public TValue this[TKey key]
    {
        get => m_Rows[m_Index[key]].Value;
        set
        {
            if (m_Index.TryGetValue(key, out int index))
            {
                var kv = m_Rows[index];
                kv = new KeyValuePair(key, value);
                m_Rows[index] = kv;
            }
            else
            {
                m_Index[key] = m_Rows.Count;
                m_Rows.Add(new KeyValuePair(key, value));
            }
        }
    }

    public ICollection<TKey> Keys => m_Index.Keys;

    public ICollection<TValue> Values => throw new NotImplementedException();

    public int Count => throw new NotImplementedException();

    public bool IsReadOnly => throw new NotImplementedException();

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    public void Add(TKey key, TValue value)
    {
        m_Index.Add(key, m_Rows.Count);
        m_Rows.Add(new KeyValuePair(key, value));
    }

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    public void Clear()
    {
        m_Index.Clear();
        m_Rows.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        if (m_Index.TryGetValue(item.Key, out int index))
        {
            return m_Rows[index].Value?.Equals(item.Value) == true;
        }
        else
        {
            return false;
        }
    }

    public bool ContainsKey(TKey key)
    {
        return m_Index.ContainsKey(key);
    }

    public bool ContainsValue(TValue value)
    {
        for (int i = 0; i < m_Rows.Count; ++i)
        {
            if (m_Rows[i].Value?.Equals(value) == true)
            {
                return true;
            }
        }

        return false;
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        for (int i = 0; i < array.Length - arrayIndex; ++i)
        {
            array[arrayIndex + i] = m_Rows[i];
        }
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();

    public Enumerator GetEnumerator()
    {
        return new Enumerator(m_Rows);
    }

    public void OnAfterDeserialize()
    {
        m_Index.Clear();
        for (int i = 0; i < m_Rows.Count; ++i)
        {
            m_Index.Add(m_Rows[i].Key, i);
        }
    }

    public void OnBeforeSerialize()
    {
    }

    public bool Remove(TKey key)
    {
        if (m_Index.Remove(key, out int index))
        {
            m_Rows.RemoveAt(index);
            for (int i = index; i < m_Rows.Count; ++i)
            {
                m_Index[m_Rows[i].Key] = i;
            }

            return true;
        }

        return false;
    }

    public bool Remove(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (m_Index.Remove(key, out int index))
        {
            value = m_Rows[index].Value;
            m_Rows.RemoveAt(index);
            for (int i = index; i < m_Rows.Count; ++i)
            {
                m_Index[m_Rows[i].Key] = i;
            }

            return true;
        }

        value = default;
        return false;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        if (m_Index.TryGetValue(item.Key, out int index) && m_Rows[index].Value?.Equals(item.Value) == true)
        {
            m_Rows.RemoveAt(index);
            m_Index.Remove(item.Key);
            for (int i = index; i < m_Rows.Count; ++i)
            {
                m_Index[m_Rows[i].Key] = i;
            }

            return true;
        }

        return false;
    }

    public bool TryGetValue(TKey key, out TValue value) => TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
