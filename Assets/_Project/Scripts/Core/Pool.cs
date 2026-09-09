using System.Collections.Generic;
using UnityEngine;

namespace ScalePunch.Core
{
    /// <summary>
    /// Component pool. Nothing in a run may call Instantiate — see
    /// docs/02-tech-stack.md §5. Grows on demand but pre-warm in Awake.
    /// </summary>
    public class Pool<T> where T : Component
    {
        readonly T _prefab;
        readonly Transform _parent;
        readonly Stack<T> _idle = new();
        int _created;

        public int CreatedCount => _created;

        public Pool(T prefab, Transform parent, int prewarm = 0)
        {
            _prefab = prefab;
            _parent = parent;
            for (int i = 0; i < prewarm; i++)
            {
                T item = CreateNew();
                item.gameObject.SetActive(false);
                _idle.Push(item);
            }
        }

        T CreateNew()
        {
            _created++;
            T item = Object.Instantiate(_prefab, _parent);
            item.name = $"{_prefab.name}_{_created}";
            return item;
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            T item = _idle.Count > 0 ? _idle.Pop() : CreateNew();
            item.transform.SetPositionAndRotation(position, rotation);
            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(T item)
        {
            if (item == null || !item.gameObject.activeSelf) return;
            item.gameObject.SetActive(false);
            item.transform.SetParent(_parent, false);
            _idle.Push(item);
        }
    }
}
