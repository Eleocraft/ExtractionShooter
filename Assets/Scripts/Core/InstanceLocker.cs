using System.Collections.Generic;
using UnityEngine;

public class InstanceLocker
{
    private readonly HashSet<int> lockIDs = new();
    public bool Locked => lockIDs.Count != 0;

    // function that returns true if Locked but not by given Object
    public bool AlreadyLocked(Object lockObj)
    {
        if (!Locked)
            return false;
        if (lockIDs.Contains(lockObj.GetInstanceID()))
            return false;
        return true;
    }
    public bool LockedByObj(Object lockObj) => lockIDs.Contains(lockObj.GetInstanceID());
    public void AddLock(Object lockObj) => lockIDs.Add(lockObj.GetInstanceID());
    public void RemoveLock(Object lockObj) => lockIDs.Remove(lockObj.GetInstanceID());
}
