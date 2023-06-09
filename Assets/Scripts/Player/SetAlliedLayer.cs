using Unity.Netcode;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class SetAlliedLayer : NetworkBehaviour
    {
        [SerializeField] private int layer;
        void Start()
        {
            if (IsOwner)
                transform.SetLayerOfAllChilds(layer);
        }
    }
}