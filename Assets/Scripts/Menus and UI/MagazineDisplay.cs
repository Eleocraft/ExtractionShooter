using TMPro;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class MagazineDisplay : MonoSingleton<MagazineDisplay>
    {
        [SerializeField] private TMP_Text Text;
        public static void SetMagazineInfo(int bullets, int magSize)
        {
            Instance.Text.text = $"{bullets} / {magSize}";
        }
    }
}
