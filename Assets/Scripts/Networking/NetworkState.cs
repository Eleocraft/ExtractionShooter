
using System.Collections.Generic;
using UnityEngine;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class NetworkState
    {
        public int Tick;
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
        protected List<T> States = new();
        public T LastState => States.Count > 0 ? States[0] : null;
        public T OldestState => States.Count > 0 ? States[States.Count-1] : null;
        public T this[int tick]
        {
            get
            {
                if (LastState?.Tick - _ticksSaved > tick) // Tick is to old
                    return null;
                
                for (int i = 0; i < States.Count; i++)
                    if (States[i].Tick <= tick)
                        return States[i];
                
                return null;
            }
            set
            {
                if (LastState?.Tick - _ticksSaved > tick) // Tick is to old
                    return;
                
                for (int i = 0; i < States.Count; i++)
                {
                    if (States[i].Tick == tick)
                    {
                        States[i] = value;
                        break;
                    }
                    else if (States[i].Tick < tick)
                    {
                        if (States[i] != value)
                            States.Insert(i, value);
                        break;
                    }
                }
            }
        }
        
        public void Add(T newState)
        {
            if (newState.Tick <= LastState?.Tick)
                return;

            if (LastState is not null && LastState == newState && States.Count > 1)
                LastState.Tick = newState.Tick;
            else
                States.Insert(0, newState);            
            
            RemoveOutdated();
        }
        public void Insert(NetworkStateList<T> newNetworkStateList, int insertAfterTick) // insert another NetworkStateList into this networkStateList
        {
            int newStatesIndex = newNetworkStateList.States.Count - 1;
            int statesIndex = States.Count - 1;

            if (newStatesIndex < 0) // new states are empty
                return;
            if (statesIndex < 0) // states are empty
            {
                while (newStatesIndex >= 0)
                { // just insert all relevant new entries (tick > insertAfterTick)
                    if (newNetworkStateList.States[newStatesIndex].Tick > insertAfterTick)
                        States.Insert(0, newNetworkStateList.States[newStatesIndex]);
                    newStatesIndex--;
                }
                return;
            }

            while (true)
            {
                if (newNetworkStateList.States[newStatesIndex].Tick <= insertAfterTick)
                    newStatesIndex--; // only do something if tick is new enough
                else if (newNetworkStateList.States[newStatesIndex].Tick < States[statesIndex].Tick)
                { // If state is newer than newState: insert newState, then skip
                    States.Insert(statesIndex + 1, newNetworkStateList.States[newStatesIndex]);
                    newStatesIndex--;
                }
                else if (newNetworkStateList.States[newStatesIndex].Tick > States[statesIndex].Tick)
                    statesIndex--; // If state is older than newState: skip
                else
                { // If both states have the same tick: new overwrites old, then both skip
                    States[statesIndex] = newNetworkStateList.States[newStatesIndex];
                    statesIndex--;
                    newStatesIndex--;
                }

                // If all new states have been inserted: break
                if (newStatesIndex < 0)
                    return;
                
                // all remaining new states are newer than LastState: add all remaining new states to 0, then break
                if (statesIndex < 0)
                {
                    for (int i = newStatesIndex; i >= 0; i--)
                        States.Insert(0, newNetworkStateList.States[i]);
                    return;
                }
            }
        }
        private void RemoveOutdated()
        {
            if (LastState is null)
                return;

            int startTick = LastState.Tick;
            bool hasFirstState = false;
            for (int i = 0; i < States.Count; i++)
            {
                if (States[i].Tick > startTick - _ticksSaved)
                    continue;

                if (!hasFirstState)
                    hasFirstState = true;
                else
                {
                    States.RemoveAt(i);
                    i--;
                }
            }
        }
        public bool Contains(int tick) => LastState?.Tick >= tick && tick >= Mathf.Max(LastState.Tick + _ticksSaved, States[States.Count-1].Tick);
    }
}