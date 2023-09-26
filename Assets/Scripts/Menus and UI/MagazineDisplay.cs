using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class MagazineDisplay : NetworkSingleton<MagazineDisplay>
    {
        [SerializeField] private GameObject Display;
        [SerializeField] private TMP_Text RemainingBulletsDisplay;
        [SerializeField] private TMP_Text MaxBulletsDisplay;

        [ClientRpc]
        private void UpdateDisplayClientRpc(ulong id, int bulletsLeft, int magSize, bool active)
        {
            Instance.RemainingBulletsDisplay.text = bulletsLeft.ToString();
            Instance.MaxBulletsDisplay.text = magSize.ToString();
            Display.SetActive(active);
        }
        public static void SetMagazineInfo(ulong ownerId, int bullets, int magSize, bool active)
        {
            if (!Instance.IsServer) return;

            Instance.UpdateDisplayClientRpc(ownerId, bullets, magSize, active);
        }
        public static void SetMagazineInfo(ulong ownerId, int bullets, int magSize)
        {
            if (!Instance.IsServer) return;

            Instance.UpdateDisplayClientRpc(ownerId, bullets, magSize, Instance.Display.activeSelf);
        }
    }
}
