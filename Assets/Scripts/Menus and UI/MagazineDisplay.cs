using TMPro;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class MagazineDisplay : MonoSingleton<MagazineDisplay>
    {
        [SerializeField] private TMP_Text Text;
        private void Start()
        {
            Text.gameObject.SetActive(false);
        }
        public static void SetMagazineInfo(int bullets, int magSize)
        {
            Instance.Text.text = $"{bullets} / {magSize}";
        }
        public static void Deactivate()
        {
            Instance.Text.gameObject.SetActive(false);
        }
        public static void Activate()
        {
            Instance.Text.gameObject.SetActive(true);
        }
    }
}
