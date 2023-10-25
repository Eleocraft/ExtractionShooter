using TMPro;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Playercard : MonoBehaviour
    {
        [SerializeField] private TMP_Text NameText;
        public void SetName(string name)
        {
            NameText.text = name;
        }
    }
}
