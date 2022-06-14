using System;
using UnityEngine;

/// <summary>
/// Serializable wrapper for System.Guid.
/// Can be implicitly converted to/from System.Guid.
///
/// Author: Searous
/// </summary>
[Serializable]
public struct SerializableGuid : ISerializationCallbackReceiver
{
    private Guid _guid;
    [SerializeField] private string guid;

    public SerializableGuid(Guid guid)
    {
        _guid = guid;
        this.guid = null;
    }

    public override bool Equals(object obj)
    {
        return obj is SerializableGuid guid && _guid.Equals(guid._guid);
    }

    public override int GetHashCode()
    {
        return -1324198676 + _guid.GetHashCode();
    }

    public void OnAfterDeserialize()
    {
        try
        {
            _guid = Guid.Parse(guid);
        }
        catch
        {
            _guid = Guid.Empty;
            Debug.LogWarning($"Attempted to parse invalid GUID string '{guid}'. GUID will set to System.Guid.Empty");
        }
    }

    public void OnBeforeSerialize()
    {
        guid = _guid.ToString();
    }

    public override string ToString() => _guid.ToString();

    public static bool operator ==(SerializableGuid a, SerializableGuid b) => a._guid == b._guid;
    public static bool operator !=(SerializableGuid a, SerializableGuid b) => a._guid != b._guid;
    public static implicit operator SerializableGuid(Guid guid) => new SerializableGuid(guid);
    public static implicit operator Guid(SerializableGuid serializable) => serializable._guid;
    public static implicit operator SerializableGuid(string serializedGuid) => new SerializableGuid(Guid.Parse(serializedGuid));
    public static implicit operator string(SerializableGuid serializedGuid) => serializedGuid.ToString();
}

