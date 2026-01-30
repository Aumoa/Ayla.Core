using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ayla
{
    public class DataTable : ScriptableObject
    {
    }

    public class DataTable<TKey, TValue> : DataTable, IDictionary<TKey, TValue>
    {
        [SerializeField]
        private OrderedDictionary<TKey, TValue> m_Dict = new();

        public TValue this[TKey key] { get => ((IDictionary<TKey, TValue>)m_Dict)[key]; set => ((IDictionary<TKey, TValue>)m_Dict)[key] = value; }

        public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)m_Dict).Keys;

        public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)m_Dict).Values;

        public int Count => ((ICollection<KeyValuePair<TKey, TValue>>)m_Dict).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)m_Dict).IsReadOnly;

        public void Add(TKey key, TValue value)
        {
            ((IDictionary<TKey, TValue>)m_Dict).Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)m_Dict).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)m_Dict).Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)m_Dict).Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return ((IDictionary<TKey, TValue>)m_Dict).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)m_Dict).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)m_Dict).GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            return ((IDictionary<TKey, TValue>)m_Dict).Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)m_Dict).Remove(item);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return ((IDictionary<TKey, TValue>)m_Dict).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)m_Dict).GetEnumerator();
        }
    }
}
