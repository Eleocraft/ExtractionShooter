using UnityEngine;
using System;

namespace ExoplanetStudios
{
    public abstract class FadeController : MonoBehaviour
    {
        private float _decayTime;
        private float _fadeTime;
        private float _totalFadeTime;
        private bool _timerStarted;
        private Action _callback;
        public void SetVisible()
        {
            _timerStarted = false;
            gameObject.SetActive(true);
            SetOpacity(1);
        }
        public void UnlockTimer()
        {
            _timerStarted = true;
        }
        public void SetTimer(float decayTime, float fadeTime, Action callback = default)
        {
            _callback = callback;
            gameObject.SetActive(true);
            SetOpacity(1);

            _decayTime = decayTime;
            _fadeTime = fadeTime;
            _totalFadeTime = fadeTime;
        }
        public void StartTimer(float decayTime, float fadeTime, Action callback = default)
        {
            SetTimer(decayTime, fadeTime, callback);
            UnlockTimer();
        }
        private void Update()
        {
            if (!_timerStarted)
                return;

            if (_decayTime > 0f)
                _decayTime -= Time.deltaTime;
            else if (_fadeTime > 0f)
            {
                _fadeTime -= Time.deltaTime;
                SetOpacity(_fadeTime/_totalFadeTime);
            }
            else
            {
                gameObject.SetActive(false);
                _callback?.Invoke();
            }
        }
        protected abstract void SetOpacity(float opacity);
    }
}