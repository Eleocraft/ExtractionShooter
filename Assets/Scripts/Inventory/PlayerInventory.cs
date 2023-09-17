using Unity.Netcode;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PlayerInventory : NetworkBehaviour
    {
        private Weapon _mainWeapon;
        private Weapon _secondaryWeapon;
    }
}
