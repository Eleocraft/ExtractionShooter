using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public enum PrefabTypes { Projectile, Throwable }
    public class PrefabHolder : MonoSingleton<PrefabHolder>
    {
        [SerializeField] private EnumDictionary<PrefabTypes, GameObject> prefabs;
        public static EnumDictionary<PrefabTypes, GameObject> Prefabs => Instance.prefabs;
        private void OnValidate()
        {
            prefabs.Update();
        }
        private void Start()
        {
            prefabs.Update();
        }
    }
}