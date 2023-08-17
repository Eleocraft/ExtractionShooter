
using Unity.Netcode;
using System.Collections.Generic;

namespace ExoplanetStudios.ExtractionShooter
{
    public abstract class NetworkState
    {
        public int Tick;
    }
    public abstract class NetworkStateList<T> where T : NetworkState
    {
        protected int _ticksSaved;
        protected List<T> States = new();
        public T LastState => States.Count > 0 ? States[0] : null;
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
            
            if (LastState is not null && LastState == newState)
                LastState.Tick = newState.Tick;
            else
                States.Insert(0, newState);
            
            RemoveOutdated();
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
        public bool Contains(int tick) => LastState?.Tick >= tick && tick >= LastState.Tick + _ticksSaved;
    }
}