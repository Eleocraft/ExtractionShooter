using Unity.Netcode;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public static class NetworkRNG
    {
        private static int Tick => NetworkManager.Singleton.LocalTime.Tick;

        public static float Value() => Value(Tick);
        public static float Value(int tick, int seed = 1) {
            tick *= seed;
            Debug.Log(Mathf.Abs((float)((tick*tick << 11) ^ tick >> 8 ^ tick >> 19) / int.MaxValue));
            return Mathf.Abs((float)((tick*tick << 11) ^ tick >> 8 ^ tick >> 19) / int.MaxValue);
        }
        public static float Range(float start, float end) => Range(start, end, Tick);
        public static float Range(float start, float end, int tick, int seed = 0) {
            return Value(tick) * (end - start) + start;
        }
    }
}
