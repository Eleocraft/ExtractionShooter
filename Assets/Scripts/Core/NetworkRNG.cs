using Unity.Netcode;

namespace ExoplanetStudios.ExtractionShooter
{
    public static class NetworkRNG
    {
        private static int Tick => NetworkManager.Singleton.LocalTime.Tick;

        public static float Value => 0.5f; //Implementation pending

        public static float Range(float start, float end) {
            return Value * (end - start) + start;
        }
    }
}
