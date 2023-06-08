using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class IndicatorBar : MonoBehaviour
    {
        [SerializeField] private RectTransform progressBar;
        [SerializeField] private RectTransform animationBar;
        [SerializeField] private float animationSpeed;
        [SerializeField] private float timeToUpdate;
        private float maxProgress;
        private float lastProgress;
        private float animationProgress;
        private float actualProgress;
        private float timer;
        private void Start()
        {
            maxProgress = progressBar.sizeDelta.x;
            actualProgress = maxProgress;
            animationProgress = maxProgress;
        }
        private void Update()
        {
            if (timer > 0)
                timer -= Time.deltaTime;
            if (animationProgress > actualProgress && timer <= 0)
            {
                animationProgress = Mathf.MoveTowards(animationProgress, actualProgress, animationSpeed * Time.deltaTime * Mathf.Clamp01((animationProgress - actualProgress) / (lastProgress - actualProgress)));
                animationBar.sizeDelta = new Vector2(animationProgress, animationBar.sizeDelta.y);
            }
        }
        public void AnimateProgress(float relProgress)
        {
            relProgress = Mathf.Clamp01(relProgress);
            if (relProgress * maxProgress > actualProgress)
            {
                SetProgress(relProgress);
                return;
            }
            lastProgress = actualProgress;
            actualProgress = relProgress * maxProgress;
            progressBar.sizeDelta = new Vector2(actualProgress, progressBar.sizeDelta.y);
            timer = timeToUpdate;
        }
        public void SetProgress(float relProgress)
        {
            timer = 0;
            relProgress = Mathf.Clamp01(relProgress);
            actualProgress = relProgress * maxProgress;
            animationProgress = actualProgress;
            lastProgress = actualProgress;
            progressBar.sizeDelta = new Vector2(actualProgress, progressBar.sizeDelta.y);
            animationBar.sizeDelta = new Vector2(animationProgress, animationBar.sizeDelta.y);
        }
    }
}