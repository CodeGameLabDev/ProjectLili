using UnityEngine;

/// <summary>
/// Singleton base class for MonoBehaviour classes.
/// Implements the singleton pattern with lazy initialization.
/// </summary>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;
    
    private static T _instance;
    
    /// <summary>
    /// Singleton instance with thread-safe lazy initialization.
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = (T)FindObjectOfType(typeof(T));

                    if (FindObjectsOfType(typeof(T)).Length > 1)
                    {
                        Debug.LogError($"[Singleton] Something went wrong - there should never be more than 1 singleton! Reopening the scene might fix it.");
                        return _instance;
                    }

                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject();
                        _instance = singleton.AddComponent<T>();
                        singleton.name = $"[Singleton] {typeof(T)}";

                        DontDestroyOnLoad(singleton);

                        Debug.Log($"[Singleton] An instance of {typeof(T)} was created with DontDestroyOnLoad.");
                    }
                }

                return _instance;
            }
        }
    }

    /// <summary>
    /// Override this to change the default initialization behavior.
    /// </summary>
    protected virtual void InitSingleton() { }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
            InitSingleton();
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"[Singleton] Instance already exists - destroying duplicate {gameObject.name}");
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }
    
    /// <summary>
    /// Manually reset the singleton instance. Use with caution!
    /// </summary>
    public static void ResetInstance()
    {
        if (_instance != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_instance.gameObject);
            }
            _instance = null;
        }
    }
} 