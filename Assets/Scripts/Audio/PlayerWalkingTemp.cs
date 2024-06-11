using System.Collections;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public class PlayerWalkingTemp : MonoBehaviour
    {
        [SerializeField] private AudioSource LocalSource;
        [SerializeField] private AudioClip[] WalkingSounds;
        [SerializeField] private AudioClip[] RunningSounds;
        [SerializeField] private float WalkingSoundInterval;
        [SerializeField] private float RunningSoundInterval;
        private Coroutine coroutine;
        private bool moving;
        private bool running;
        private int lastSound = 0;
        void Start() {
            GetComponent<FirstPersonController>().TransformStateChanged += TransformStateChanged;
        }
        void TransformStateChanged(NetworkTransformState state){
            if (state.Velocity.XZ().magnitude > 1 && Mathf.Abs(state.Velocity.y) < 0.01 && !moving) {
                moving = true;
                coroutine = StartCoroutine("PlayWalkingSound");
            }
            else if ((state.Velocity.XZ().magnitude <= 1 || Mathf.Abs(state.Velocity.y) > 0.01) && moving) {
                moving = false;
                StopCoroutine(coroutine);
            }

            if (state.Velocity.XZ().magnitude > 5)
                running = true;
            else
                running = false;
        }
        IEnumerator PlayWalkingSound() {
            while (true) {
                lastSound = running ?
                    Utility.RandomExcept(0, RunningSounds.Length, lastSound) :
                    Utility.RandomExcept(0, RunningSounds.Length, lastSound);

                LocalSource.PlayOneShot(running ?
                    RunningSounds[lastSound] :
                    WalkingSounds[lastSound]);

                yield return new WaitForSeconds(running ? RunningSoundInterval : WalkingSoundInterval);
            }
        }
    }
}
