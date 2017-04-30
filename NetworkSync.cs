using UnityEngine;
using Lidgren.Network;
using System.Collections.Generic;
using System.IO;

public static class NetworkSync
{
    // For use by clients/local host
    public static NetworkStatusWrapper NetworkStatus;

    public static void GetNetworkStatus(int networkindex, Player player)
    {
        if (networkindex == -1 || FreightCartManager.instance == null || FreightCartManager.instance.Networks == null)
            return;

        int count = FreightCartManager.instance.Networks.Count;
        if (networkindex >= count && count > 0)
            networkindex = 0;
        else if (networkindex >= count)
            return;

        NetworkStatusWrapper wrapper = new NetworkStatusWrapper(FreightCartManager.instance.GetNetworkRegistries(networkindex), FreightCartManager.instance.Networks[networkindex]);

        // Server is local player making request
        if (player == WorldScript.mLocalPlayer)
        {
            NetworkStatus = wrapper;
            return;
        }
        ModManager.ModSendServerCommToClient("steveman0.NetworkStatus", player, wrapper);
    }

    public static void SendNetworkStatus(BinaryWriter writer, Player player, object data)
    {
        (data as NetworkStatusWrapper).Write(writer);
    }

    public static void ReadNetworkStatus(NetIncomingMessage message)
    {
        NetworkStatusWrapper wrapper = new NetworkStatusWrapper();
        wrapper.Read(message);
        NetworkStatus = wrapper;
        SystemMonitorWindow.networkredraw = true;
    }
}

public class NetworkStatusWrapper
{
    public string NetworkID;
    public List<FreightRegistry> Registries = new List<FreightRegistry>();

    public NetworkStatusWrapper(List<FreightRegistry> reg, string networkid)
    {
        this.NetworkID = networkid;
        this.Registries = reg;
    }

    public NetworkStatusWrapper() { }

    public void Write(BinaryWriter writer)
    {
        if (string.IsNullOrEmpty(NetworkID))
        {
            Debug.LogWarning("FreightCarts NetworkSync tried to write a NetworkStatusWrapper with null/empty networkid");
            writer.Write(string.Empty);
        }
        else
            writer.Write(this.NetworkID);

        int count = this.Registries.Count;
        writer.Write(count);

        for (int n = 0; n < count; n++)
        {
            FreightRegistry reg = Registries[n];
            if (reg.FreightItem.mType == ItemType.ItemCubeStack)
            {
                writer.Write(-1);
                writer.Write((reg.FreightItem as ItemCubeStack).mCubeType);
                writer.Write((reg.FreightItem as ItemCubeStack).mCubeValue);
            }
            else
                writer.Write(reg.FreightItem.mnItemID);
            writer.Write(reg.Deficit);
            writer.Write(reg.Surplus);
            writer.Write(reg.Inventory);
            writer.Write(reg.Stock);
        }
    }

    public void Read(NetIncomingMessage message)
    {
        this.Registries = new List<FreightRegistry>();
        this.NetworkID = message.ReadString();
        int count = message.ReadInt32();
        for (int n = 0; n < count; n++)
        {
            int itemid = message.ReadInt32();
            ushort type;
            ushort val;
            ItemBase item;
            if (itemid == -1)
            {
                type = message.ReadUInt16();
                val = message.ReadUInt16();
                item = ItemManager.SpawnCubeStack(type, val, 1);
            }
            else
                item = ItemManager.SpawnItem(itemid);
            
            FreightRegistry reg = new FreightRegistry(this.NetworkID, null, item, 0, 0, FreightRegistry.RegistryType.Registry);
            reg.Deficit = message.ReadInt32();
            reg.Surplus = message.ReadInt32();
            reg.Inventory = message.ReadInt32();
            reg.Stock = message.ReadInt32();

            this.Registries.Add(reg);
        }
    }
}
