using UnityEngine;
using Unity.Netcode;

namespace ExoplanetStudios
{
    public class NetworkSingleton<T> : NetworkBehaviour where T : NetworkBehaviour
    {
        private static T _instance;
        public static T Instance
        {
            get => _instance;
            private set
            {
                if (_instance == null)
                    _instance = value;
                else if (_instance != value)
                {
                    Debug.Log($"{typeof(T)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }
        public static bool Instantiated()
        {
            return Instance;
        }
        private void Awake()
        {
            Instance = this as T;
            if (Instance != this)
                return;
            SingletonAwake();
        }
        protected virtual void SingletonAwake() {}
    }
}