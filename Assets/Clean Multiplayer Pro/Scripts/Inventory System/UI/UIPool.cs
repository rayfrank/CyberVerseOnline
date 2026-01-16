#if CMPSETUP_COMPLETE
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class UIPool<T> where T : MonoBehaviour
{
    public T prefab;
    private List<T> _pool = new List<T>();
    private readonly Transform _poolContainer = null;
    private List<bool> _activeFlags = new List<bool>();

    public UIPool(T prefab)
    {
        this.prefab = prefab;
    }

    public UIPool(T prefab, Transform parent)
    {
        this.prefab = prefab;
        _poolContainer = parent;
    }

    public T RentObject()
    {
        for (var i = 0; i < _pool.Count; i++)
        {
            if (_activeFlags[i]) continue;
            var item = _pool[i];
            item.gameObject.SetActive(true);
            _activeFlags[i] = true;
            return item;
        }

        var newItem = _poolContainer == null
            ? UnityEngine.Object.Instantiate(prefab)
            : UnityEngine.Object.Instantiate(prefab, _poolContainer);

        _pool.Add(newItem);
        _activeFlags.Add(true);
        return newItem;
    }

    public void ReturnObject(T item)
    {
        var index = _pool.IndexOf(item);
        if (index < 0) return;
        item.gameObject.SetActive(false);
        _activeFlags[index] = false;
    }

    public void ReturnAllObjects()
    {
        for (var i = 0; i < _pool.Count; i++)
        {
            _pool[i].gameObject.SetActive(false);
            _activeFlags[i] = false;
        }
    }
}
#endif