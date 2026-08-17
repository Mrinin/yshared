using UnityEngine;

namespace YShared.Singleton
{   
    [System.Obsolete("Use YSingleton instead.", false)]
    public class Singleton<T> : YSingleton<T> where T : MonoBehaviour
    {
        
    } 

    /// <summary>
    /// Singeton.
    /// </summary>
    /// <typeparam name="T">This class</typeparam>
    public abstract class YSingleton<T> : MonoBehaviour where T : MonoBehaviour
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

        protected virtual void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }

    public abstract class YSceneSingleton<T> : YSingleton<T> where T : MonoBehaviour
    {
        public override bool CallDontDestroyOnLoad => false;

        public static new T Instance {
            get
            {
                return YSingleton<T>.Instance != null
                ? YSingleton<T>.Instance
                : null;
            }
        }

    }
}