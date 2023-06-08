using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PlayerLifeBar : MonoSingleton<PlayerLifeBar>
    {
        private IndicatorBar _lifeBar;
        private void Start()
        {
            _lifeBar = GetComponent<IndicatorBar>();
        }
        public static void AnimateProgress(float t) => Instance._lifeBar.AnimateProgress(t);
        public static void SetProgress(float t) => Instance._lifeBar.SetProgress(t);
    }
}
