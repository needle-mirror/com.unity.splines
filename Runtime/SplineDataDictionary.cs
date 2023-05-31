using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Splines
{
    [Serializable]
    class SplineDataKeyValuePair<T>
    {
        public string Key;
        public SplineData<T> Value;
    }

    [Serializable]
    class SplineDataDictionary<T> : IEnumerable<SplineDataKeyValuePair<T>>
    {
        [SerializeField]
        List<SplineDataKeyValuePair<T>> m_Data = new ();

        public IEnumerable<string> Keys => m_Data.Select(x => x.Key);

        public IEnumerable<SplineData<T>> Values => m_Data.Select(x => x.Value);

        int FindIndex(string key)
        {
            for (int i = 0, c = m_Data.Count; i < c; ++i)
                if (m_Data[i].Key == key)
                    return i;
            return -1;
        }

        public bool TryGetValue(string key, out SplineData<T> value)
        {
            var index = FindIndex(key);
            value = index < 0 ? null : m_Data[index].Value;
            return index > -1;
        }

        public SplineData<T> GetOrCreate(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (!TryGetValue(key, out var data))
                m_Data.Add(new SplineDataKeyValuePair<T>()
                {
                    Key = key,
                    Value = data = new SplineData<T>()
                });
            return data;
        }

        public SplineData<T> this[string key]
        {
            get => TryGetValue(key, out var data) ? data : null;
            set
            {
                int i = FindIndex(key);
                var copy = new SplineData<T>(value);
                if (i < 0)
                    m_Data.Add(new SplineDataKeyValuePair<T>() { Key = key, Value = copy });
                else
                    m_Data[i].Value = copy;
            }
        }

        public bool Contains(string key) => FindIndex(key) > -1;

        public IEnumerator<SplineDataKeyValuePair<T>> GetEnumerator() => m_Data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)m_Data).GetEnumerator();

        public bool Remove(string key)
        {
            var i = FindIndex(key);
            if (i < 0)
                return false;
            m_Data.RemoveAt(i);
            return true;
        }

        public void RemoveEmpty()
        {
            for (int i = m_Data.Count - 1; i > -1; --i)
            {
                if (string.IsNullOrEmpty(m_Data[i].Key) || m_Data[i].Value?.Count < 1)
                {
                    Debug.Log($"{typeof(T)} remove empty key \"{m_Data[i].Key}\"");
                    m_Data.RemoveAt(i);
                }
            }
        }
    }
}
