using TMPro;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class ScoreboardItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text Name;
        [SerializeField] private TMP_Text Kills;
        [SerializeField] private TMP_Text Deaths;

        private int kills = 0;
        private int deaths = 0;
        public void AddKill()
        {
            kills++;
            Kills.text = kills.ToString();
        }
        public void AddDeath()
        {
            deaths++;
            Deaths.text = deaths.ToString();
        }
        public void SetName(string name) => Name.text = name;
    }
}
