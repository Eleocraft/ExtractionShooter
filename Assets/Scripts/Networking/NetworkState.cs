
using System.Collections.Generic;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class NetworkState
    {
        public int Tick;
        public abstract NetworkState GetStateWithTick(int tick);
        public static bool operator==(NetworkState s1, NetworkState s2)
        {
            if (s1 is null)
                return s2 is null;
            return s1.Equals(s2);
        }
        public static bool operator!=(NetworkState s1, NetworkState s2)
        {
            if (s1 is null)
                return !(s2 is null);
            return !s1.Equals(s2);
        }
        public override abstract bool Equals(object obj);
        public override abstract int GetHashCode();
    }
    public abstract class NetworkStateList<T> where T : NetworkState
    {
        protected int _ticksSaved;
        protected int _lastReceivedTick;
        public int LastTick => _lastReceivedTick;
        public List<T> States = new();

        public T this[int tick]
        {
            get
            {
                if (_lastReceivedTick - _ticksSaved > tick) // Tick is to old
                {
                    Debug.Log(_lastReceivedTick + " tick to old " + tick);
                    return null;
                }
                
                for (int i = 0; i < States.Count; i++)
                    if (States[i].Tick <= tick)
                        return (T)States[i].GetStateWithTick(tick);
                
                Debug.Log("No state found " + States.Count + " " + _lastReceivedTick);
                return null;
            }
        }
        
        public void Add(T newState)
        {
            if (_lastReceivedTick - _ticksSaved > newState.Tick) // new state is too old
                return;

            for (int i = 0; i < States.Count; i++)
            {
                if (States[i].Tick <= newState.Tick)
                {
                    if (States[i].Tick == newState.Tick)
                        States[i] = newState;
                    else if (States[i] != newState)
                        States.Insert(i, newState);
                        
                    if (_lastReceivedTick < newState.Tick)
                    {
                        _lastReceivedTick = newState.Tick;
                        RemoveOutdated();
                    }

                    return;
                }
            }
            States.Add(newState);
            if (_lastReceivedTick < newState.Tick)
            {
                _lastReceivedTick = newState.Tick;
                RemoveOutdated();
            }
        }
        public void Insert(NetworkStateList<T> newNetworkStateList, int insertAfterTick) // insert another NetworkStateList into this networkStateList
        {
            if (_lastReceivedTick < newNetworkStateList._lastReceivedTick)
                _lastReceivedTick = newNetworkStateList._lastReceivedTick;
            
            foreach (T state in newNetworkStateList.States)
                if (state.Tick > insertAfterTick)
                    Add(state);

            RemoveOutdated();
        }
        private void RemoveOutdated()
        {
            bool hasEndState = false; // make sure there is always info for the oldest ticks
            for (int i = 0; i < States.Count; i++)
            {
                if (_lastReceivedTick - _ticksSaved < States[i].Tick)
                    continue;
                
                if (!hasEndState)
                    hasEndState = true;
                else
                {
                    States.RemoveAt(i);
                    i--;
                }
            }
        }
        public bool Contains(int tick) => _lastReceivedTick >= tick && tick >= Mathf.Max(_lastReceivedTick + _ticksSaved, States[States.Count-1].Tick);
    }
}