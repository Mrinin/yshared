using UnityEngine;

namespace YShared.Singleton
{    
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }
        public virtual bool CallDontDestroyOnLoad { get => true; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this as T;

            if (CallDontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }
    }
}