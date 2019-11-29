using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticObjectPool<T> where T : new()
{
    private T[] _objects;

    private int _freeHeadIndex = -1;
    private int _count = -1;

    public int FreeCount
    {
        get { return _freeHeadIndex; }
    }

    public int UsedCount
    {
        get { return _count- _freeHeadIndex; }
    }

    public StaticObjectPool(int count)
    {
        if (count > 0)
        {
            _objects     = new T[count];

            for (int a = 0; a < _objects.Length; ++a)
            {
                _objects[a] = new T();
            }

            _freeHeadIndex = _objects.Length - 1;
            _count = count;
        }
        else
        {
            Debug.LogError("Trying to create an object pool of size 0");
        }
    }

    public T GetInstance()
    {
        if(_freeHeadIndex > 0)
        {
            var obj = _objects[_freeHeadIndex];
            _freeHeadIndex--;
            return obj;
        }

        Debug.LogWarning("Object Pool Empty!!!");

        return default(T);
    }

    public void ReleaseInstance(T obj)
    {
        if(obj != null && _freeHeadIndex < _count - 1)
        {
            _freeHeadIndex++;
            _objects[_freeHeadIndex] = obj;
        }
    }

    public override string ToString()
    {
        return UsedCount+" / "+_count;
    }
}
