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
        void Start() {
            GetComponent<FirstPersonController>().TransformStateChanged += TransformStateChanged;
        }
        void TransformStateChanged(NetworkTransformState state){
            if (state.Velocity.magnitude > 1 && !moving) {
                moving = true;
                coroutine = StartCoroutine("PlayWalkingSound");
            }
            else if (state.Velocity.magnitude <= 1 && moving) {
                moving = false;
                StopCoroutine(coroutine);
            }

            if (state.Velocity.magnitude > 3)
                running = true;
            else
                running = false;
        }
        IEnumerator PlayWalkingSound() {
            while (true) {
                LocalSource.PlayOneShot(running ?
                    RunningSounds[Random.Range(0, RunningSounds.Length)] :
                    WalkingSounds[Random.Range(0, WalkingSounds.Length)]);

                yield return new WaitForSeconds(running ? RunningSoundInterval : WalkingSoundInterval);
            }
        }
    }
}
