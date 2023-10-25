using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ExoplanetStudios.ExtractionShooter
{
    public class Friendcard : MonoBehaviour
    {
        [SerializeField] private TMP_Text NameText;
        [SerializeField] private Button B;
        public void Initialize(string name, UnityAction callback)
        {
            NameText.text = name;
            B.onClick.AddListener(callback);
        }
    }
}
