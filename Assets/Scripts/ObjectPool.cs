using System;
using UnityEngine;
using UnityEngine.Assertions;


public abstract partial class PooledObject : MonoBehaviour
{
    private ObjectPool _pool;
    private PooledObject _nextPooledObject = null;

    protected virtual void OnConstruction()
    {
    }

    protected virtual void Init()
    {
    }

    public void Destroy()
    {
        _pool.Destroy(this);
    }
    
    private PooledObject GetNextPooledObject()
    {
        return _nextPooledObject;
    }

    private void SetNextPooledObject(PooledObject objectReference)
    {
        _nextPooledObject = objectReference;
    }
}

public abstract partial class PooledObject
{
    public abstract class ObjectPool : MonoBehaviour
    {
        public GameObject pooledObjectPrefab;
        public int poolSize = 100;

        private int _numLiveObjects = 0;

        private PooledObject _firstAvailable;
        private GameObject[] _objectInstances;

        protected virtual void Awake() {
            if (pooledObjectPrefab.GetComponent<PooledObject>() == null)
            {
                Debug.LogError("ObjectPool only works on PooledObjects. Please inherit from BaseClass PooledObject in your prefab!");
                return;
            }
    
            _objectInstances = new GameObject[poolSize];
            for (var i = 0; i < poolSize; ++i)
            {
                _objectInstances[i] = Instantiate(pooledObjectPrefab, Vector3.zero, Quaternion.identity);
                _objectInstances[i].SetActive(false);
            }
    
            _firstAvailable = _objectInstances[0].GetComponent<PooledObject>();
    
            for (var i = 0; i < poolSize - 1; i++)
            {
                var current = _objectInstances[i].GetComponent<PooledObject>();
                var next = _objectInstances[i+1].GetComponent<PooledObject>();
                current.SetNextPooledObject(next);

                current.OnConstruction();
            }
    
            var last = _objectInstances[poolSize - 1].GetComponent<PooledObject>();
            last.SetNextPooledObject(null);
        }

        public int LiveSize()
        {
            return _numLiveObjects;
        }

        public PooledObject Create(Vector3 position, Quaternion orientation)
        {
            if (_firstAvailable == null)
            {
                Debug.LogError("ObjectPool overflow! Increase pool size.");
                return null;
            }
    
            _numLiveObjects += 1;

            var available = _firstAvailable;
            _firstAvailable = available.GetNextPooledObject().GetComponent<PooledObject>();

            available.Init();

            var obj = available.gameObject;
            obj.transform.position = position;
            obj.transform.rotation = orientation;
            obj.SetActive(true);
            return available;
        }
    
        public void Destroy(PooledObject pooledObject)
        {
            Assert.IsTrue(pooledObject._pool == this);

            pooledObject.gameObject.SetActive(false);

            pooledObject.SetNextPooledObject(_firstAvailable);
            _firstAvailable = pooledObject;

            _numLiveObjects -= 1;
        }
        
        public void Destroy(GameObject obj)
        {
            var pooledObject = obj.GetComponent<PooledObject>();

            Assert.IsTrue(pooledObject != null);
            Assert.IsTrue(pooledObject._pool == this);

            Destroy(pooledObject);

            _numLiveObjects -= 1;
        }
    }
}

