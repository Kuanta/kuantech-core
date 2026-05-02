
#if NETWORKING_FISHNET
using FishNet.Serializing;
using FishNet.Object;
#endif

using Kuantech.Core;
using UnityEngine;

public static class ActionCastDataSerializer
{
    #if NETWORKING_FISHNET
    public static void WriteActionCastData(this Writer writer, ActionCastData data)
    {
        writer.WriteVector3(data.StartPosition);
        writer.WriteVector3(data.Direction);
        writer.WriteVector3(data.TargetPosition);
        NetworkObject targetNob = data.Target != null ? data.Target.GetComponent<NetworkObject>() : null;
        writer.WriteNetworkObject(targetNob);
    }

    public static ActionCastData ReadActionCastData(this Reader reader)
    {
        ActionCastData data = new ActionCastData();
        data.StartPosition = reader.ReadVector3();
        data.Direction = reader.ReadVector3();
        data.TargetPosition = reader.ReadVector3();
        NetworkObject nob = reader.ReadNetworkObject();
        data.Target = nob != null ? nob.GetComponent<Actor>() : null;
        return data;
    }
    #endif
}
