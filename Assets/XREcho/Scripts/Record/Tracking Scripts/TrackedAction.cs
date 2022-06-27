using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TrackedAction : System.IEquatable<TrackedAction>
{
    public string trackedActionName;
    public List<string> parameters;
    public bool Equals(TrackedAction other)
    {
        if (!trackedActionName.Equals(other.trackedActionName))
            return false;

        return true;
    }
};