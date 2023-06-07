using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ExoplanetStudios
{
    public enum MusicState
    {
        Ambient = 0,
        Fighting = 1
    }
    [CreateAssetMenu(fileName = "New Music State List", menuName = "CustomObjects/Audio/Main")]
    public class MusicStateList : SoundStateObject
    {
        [SerializeField] private EnumDictionary<MusicState, SoundStateObject> states;
        [SerializeField] [ReadOnly] private MusicState musicState;

        private Dictionary<MusicState, InstanceLocker> activations;

        public override AudioClip GetClip()
        {
            return states[musicState].GetClip();
        }
        public override float GetPauseTime()
        {
            return states[musicState].GetPauseTime();
        }
        public override bool StateChanged()
        {
            IEnumerable<MusicState> LockedStates = activations.Where(a => a.Value.Locked == true)
                                .Select(k => k.Key);

            MusicState HighestActiveState = (LockedStates.Count() == 0) ? 0 : LockedStates.Max();
            
            if (musicState != HighestActiveState) // The state has changed
            {
                musicState = HighestActiveState;
                return true;
            }

            return states[musicState].StateChanged(); // The state hasn't changed
        }

        public void SetMusicState(MusicState newState, Object lockObj)
        {
            activations[newState].AddLock(lockObj);
        }
        public void ResetMusicState(MusicState disabledState, Object lockObj)
        {
            activations[disabledState].RemoveLock(lockObj);
        }
        // Automation
        private void OnValidate()
        {
            states.Update();
        }
        private void OnEnable()
        {
            // Populating activations dict
            states.Update();
            activations = new();
            foreach (MusicState state in Utility.GetEnumValues<MusicState>())
                activations.Add(state, new());
        }
    }
}