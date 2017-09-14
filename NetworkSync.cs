using UnityEngine;
using Lidgren.Network;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class NetworkSync
{
    // For use by clients/local host
    public static NetworkStatusWrapper NetworkStatus;
    public static TrackNetworksWrapper TrackNetworks;

    public static void GetNetworkStatus(int networkindex, Player player)
    {
        if (networkindex == -1 || FreightCartManager.instance == null || FreightCartManager.instance.Networks == null)
            return;

        int count = FreightCartManager.instance.Networks.Count;
        if (networkindex >= count && count > 0)
            networkindex = 0;
        else if (networkindex >= count)
            return;

        string networkid = FreightCartManager.instance.Networks[networkindex];
        NetworkStatusWrapper wrapper = new NetworkStatusWrapper(FreightCartManager.instance.GetNetworkRegistries(networkid), networkid);

        // Server is local player making request
        if (player == WorldScript.mLocalPlayer)
        {
            NetworkStatus = wrapper;
            return;
        }
        ModManager.ModSendServerCommToClient("steveman0.NetworkStatus", player, wrapper);
    }

    public static void GetTrackNetworks(int tracknetwork, int station, int cart, Player player)
    {
        if (FreightCartManager.instance == null || FreightCartManager.instance.GlobalTrackNetworks == null)
            return;

        TrackNetworksWrapper wrapper = new TrackNetworksWrapper(tracknetwork, station, cart);

        // Server is local player making request
        if (player == WorldScript.mLocalPlayer)
        {
            TrackNetworks = wrapper;
            return;
        }
        ModManager.ModSendServerCommToClient("steveman0.TrackNetworks", player, wrapper);
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

    public static void SendTrackNetworks(BinaryWriter writer, Player player, object data)
    {
        (data as TrackNetworksWrapper).Write(writer);
    }

    public static void ReadTrackNetworks(NetIncomingMessage message)
    {
        TrackNetworksWrapper wrapper = new TrackNetworksWrapper();
        wrapper.Read(message);
        TrackNetworks = wrapper;
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

public class TrackNetworksWrapper
{
    // Track Networks
    public int NetworkCount;
    public List<TrackNetworkStats> NetworkStats;

    // Freight Cart Stations
    public int StationCount;
    public List<StationStats> Stations;

    // Station details - these need to be item names and number only for serialization... change object to a struct?
    public List<FreightRegistry> StationDeficits;
    public List<FreightRegistry> StationSurplus;

    //Carts 
    public int CartCount;
    public List<CartStats> Carts;

    //Displayed Cart
    public List<ItemBase> CartInventory;

    public TrackNetworksWrapper(int tracknetwork, int station, int cart)
    {
        // This shouldn't happen but just in case...
        if (FreightCartManager.instance == null)
            return;

        // Track Networks
        this.NetworkCount = FreightCartManager.instance.GlobalTrackNetworks.Count;
        this.NetworkStats = new List<TrackNetworkStats>(this.NetworkCount);
        for (int n = 0; n < this.NetworkCount; n++)
            this.NetworkStats.Add(new TrackNetworkStats(n));

        // Freight Cart Stations
        if (tracknetwork != -1 && tracknetwork < this.NetworkCount)
        {
            List<FreightCartStation> stations = FreightCartManager.instance.GlobalTrackNetworks[tracknetwork].GetNetworkStations();
            stations = stations.OrderBy(x => x.StationName).ToList();
            this.StationCount = stations.Count;
            this.Stations = new List<StationStats>(this.StationCount);
            foreach (FreightCartStation sta in stations)
                this.Stations.Add(new StationStats(sta));

            if (station != -1 && station < this.StationCount)
            {
                FreightCartStation Station = stations[station];

                // Station details
                this.GetStationGoods(Station);

                // Carts
                this.CartCount = Station.CartList.Count;
                this.Carts = new List<CartStats>(this.CartCount);
                foreach (FreightCartMob mob in Station.CartList)
                    this.Carts.Add(new CartStats(mob));

                // Displayed Cart
                if (cart != -1 && cart < this.CartCount)
                {
                    FreightCartMob Cart = Station.CartList[cart];
                    if (!string.IsNullOrEmpty(Station.NetworkID) && Cart.LocalInventory.ContainsKey(Station.NetworkID))
                        CartInventory = Cart.LocalInventory[Station.NetworkID].Inventory;
                }
            }
            else
                this.CartCount = 0;
        }
        else
        {
            this.StationCount = 0;
            this.CartCount = 0;
        }
    }

    public TrackNetworksWrapper() { }

    public void Write(BinaryWriter writer)
    {
        // Track Networks
        writer.Write(NetworkCount);
        if (NetworkCount > 0)
        {
            foreach (TrackNetworkStats stats in this.NetworkStats)
                stats.Write(writer);
        }

        //Stations
        writer.Write(StationCount);
        if (StationCount > 0)
        {
            foreach (StationStats stats in this.Stations)
                stats.Write(writer);
        }

        WriteRegistries(StationDeficits, writer, true);
        WriteRegistries(StationSurplus, writer, false);

        // Carts
        writer.Write(CartCount);
        if (CartCount > 0)
        {
            foreach (CartStats stats in Carts)
                stats.Write(writer);
        }

        // Displayed Cart
        if (CartInventory != null)
        {
            int count = CartInventory.Count;
            writer.Write(count);
            for (int n = 0; n < count; n++)
                ItemWriter(CartInventory[n], writer);
        }
        else
            writer.Write(0);
    }

    public void Read(NetIncomingMessage message)
    {
        // Track Networks
        this.NetworkCount = message.ReadInt32();
        this.NetworkStats = new List<TrackNetworkStats>(this.NetworkCount);
        for (int n = 0; n < NetworkCount; n++)
        {
            TrackNetworkStats stats = new TrackNetworkStats();
            stats.Read(message);
            this.NetworkStats.Add(stats);
        }

        // Stations
        this.StationCount = message.ReadInt32();
        if (this.StationCount > 0)
        {
            this.Stations = new List<StationStats>(this.StationCount);
            for (int n = 0; n < StationCount; n++)
            {
                StationStats stats = new StationStats();
                stats.Read(message);
                this.Stations.Add(stats);
            }
        }

        ReadRegistries(message, true);
        ReadRegistries(message, false);

        // Carts
        this.CartCount = message.ReadInt32();
        if (this.CartCount > 0)
        {
            this.Carts = new List<CartStats>(this.CartCount);
            for (int n = 0; n < CartCount; n++)
            {
                CartStats stats = new CartStats();
                stats.Read(message);
                this.Carts.Add(stats);
            }
        }

        // Displayed Cart
        int count = message.ReadInt32();
        CartInventory = new List<ItemBase>(count);
        for (int n = 0; n < count; n++)
        {
            ItemBase item;
            item = ItemReader(message);
            if (item != null)
                CartInventory.Add(item);
        }
    }

    private void ItemWriter(ItemBase item, BinaryWriter writer)
    {
        if (item == null)
        {
            Debug.LogWarning("Freight Cart NetworkSync tried to write a null cart inventory item!");
            writer.Write(-2);
        }
        writer.Write(item.mnItemID);
        if (item.mnItemID == -1)
        {
            // Cube!
            ItemCubeStack cube = (item as ItemCubeStack);
            writer.Write(cube.mCubeType);
            writer.Write(cube.mCubeValue);
            writer.Write(cube.mnAmount);
        }
        else
        {
            if (item.mType == ItemType.ItemStack)
                writer.Write((item as ItemStack).mnAmount);
        }
    }

    private ItemBase ItemReader(NetIncomingMessage message)
    {
        ItemBase item;
        int itemid = message.ReadInt32();
        if (itemid == -2)
        {
            Debug.LogWarning("Freight Cart NetworkSync read in a cart null inventory item!");
            return null;
        }
        else if (itemid == -1)
            return ItemManager.SpawnCubeStack(message.ReadUInt16(), message.ReadUInt16(), message.ReadInt32());
        else
        {
            item = ItemManager.SpawnItem(itemid);
            if (item.mType == ItemType.ItemStack)
                (item as ItemStack).mnAmount = message.ReadInt32();
            return item;
        }
    }

    private void GetStationGoods(FreightCartStation station)
    {
        List<FreightRegistry> LocalDeficits = new List<FreightRegistry>();
        List<FreightRegistry> LocalSurplus = new List<FreightRegistry>();
        if (station.massStorageCrate != null)
        {
            LocalDeficits = FreightCartManager.instance.GetLocalDeficit(station.NetworkID, station.massStorageCrate);
            LocalSurplus = FreightCartManager.instance.GetLocalSurplus(station.NetworkID, station.massStorageCrate);
        }
        else if (station.HopperInterface != null)
        {
            LocalDeficits = this.FreightListingConversion(station.HopperInterface.FreightRequests.OrderByDescending(x => x.Quantity).Take(3).ToList());
            LocalSurplus = this.FreightListingConversion(station.HopperInterface.FreightOfferings.OrderByDescending(x => x.Quantity).Take(3).ToList());
        }
        else if (station.AttachedInterface != null)
        {
            LocalDeficits = this.FreightListingConversion(station.AttachedInterface.FreightRequests.OrderByDescending(x => x.Quantity).Take(3).ToList());
            LocalSurplus = this.FreightListingConversion(station.AttachedInterface.FreightOfferings.OrderByDescending(x => x.Quantity).Take(3).ToList());
        }
        this.StationDeficits = LocalDeficits;
        this.StationSurplus = LocalSurplus;
    }

    private List<FreightRegistry> FreightListingConversion(List<FreightListing> items)
    {
        List<FreightRegistry> regs = new List<FreightRegistry>();
        foreach (FreightListing item in items)
        {
            FreightRegistry reg = new FreightRegistry(null, null, item.Item, 0, 0, FreightRegistry.RegistryType.Registry);
            reg.Deficit = item.Quantity;
            reg.Surplus = item.Quantity;
            regs.Add(reg);
        }
        return regs;
    }

    private void WriteRegistries(List<FreightRegistry> regs, BinaryWriter writer, bool IsDef)
    {
        if (regs == null)
        {
            writer.Write(0);
            return;
        }
        int count = regs.Count;
        writer.Write(count);
        for (int n = 0; n < count; n++)
        {
            ItemWriter(regs[n].FreightItem, writer);
            if (IsDef)
                writer.Write(regs[n].Deficit);
            else
                writer.Write(regs[n].Surplus);
        }
    }

    private void ReadRegistries(NetIncomingMessage message, bool IsDef)
    {
        int count = message.ReadInt32();
        List<FreightRegistry> regs = new List<FreightRegistry>(count);
        FreightRegistry reg;
        for (int n = 0; n < count; n++)
        {
            reg = new FreightRegistry(null, null, ItemReader(message), 0, 0, FreightRegistry.RegistryType.Registry);
            if (IsDef)
                reg.Deficit = message.ReadInt32();
            else
                reg.Surplus = message.ReadInt32();
            regs.Add(reg);
        }
        if (IsDef)
            this.StationDeficits = regs;
        else
            this.StationSurplus = regs;
    }
}

public class TrackNetworkStats
{
    public int JunctionCount;
    public int NetworkID;
    public bool NetworkClosed;
    public int AssignedCarts;
    public int AvailableCarts;

    public TrackNetworkStats(int junccount, int netid, bool netclosed, int assigned, int avail)
    {
        JunctionCount = junccount;
        NetworkID = netid;
        NetworkClosed = netclosed;
        AssignedCarts = assigned;
        AvailableCarts = avail;
    }

    public TrackNetworkStats(int networkindex)
    {
        FreightTrackNetwork network = FreightCartManager.instance.GlobalTrackNetworks[networkindex];
        JunctionCount = network.TrackJunctions.Count;
        NetworkID = network.NetworkID;
        network.GetNetworkStats(out AssignedCarts, out AvailableCarts, out NetworkClosed);
    }

    public TrackNetworkStats() { }

    public void Write(BinaryWriter writer)
    {
        writer.Write(JunctionCount);
        writer.Write(NetworkID);
        writer.Write(NetworkClosed);
        writer.Write(AssignedCarts);
        writer.Write(AvailableCarts);
    }

    public void Read(NetIncomingMessage message)
    {
        JunctionCount = message.ReadInt32();
        NetworkID = message.ReadInt32();
        NetworkClosed = message.ReadByte() == 1;
        AssignedCarts = message.ReadInt32();
        AvailableCarts = message.ReadInt32();
    }
}

public class StationStats
{
    public string StationName;
    public string NetworkID;
    public float StationFull;
    public int AssignedCarts;
    public int AvailableCarts;

    public StationStats(string name, string netid, float full, int assigned, int avail)
    {
        StationName = name;
        NetworkID = netid;
        StationFull = full;
        AssignedCarts = assigned;
        AvailableCarts = avail;
    }

    public StationStats(FreightCartStation station)
    {
        StationName = station.StationName;
        NetworkID = station.NetworkID;
        StationFull = station.StationFull;
        AssignedCarts = station.AssignedCarts;
        AvailableCarts = station.AvailableCarts;
    }

    public StationStats() { }

    public void Write(BinaryWriter writer)
    {
        writer.Write(StationName);
        writer.Write(NetworkID);
        writer.Write(StationFull);
        writer.Write(AssignedCarts);
        writer.Write(AvailableCarts);
    }

    public void Read(NetIncomingMessage message)
    {
        StationName = message.ReadString();
        NetworkID = message.ReadString();
        StationFull = message.ReadFloat();
        AssignedCarts = message.ReadInt32();
        AvailableCarts = message.ReadInt32();
    }
}

public class CartStats
{
    public FreightCartMob.eMinecartType CartType;
    public int UsedStorage;
    public int MaxStorage;

    public CartStats(FreightCartMob mob)
    {
        CartType = mob.meType;
        UsedStorage = mob.mnUsedStorage;
        MaxStorage = mob.mnMaxStorage;
    }

    public CartStats() { }

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)CartType);
        writer.Write(UsedStorage);
        writer.Write(MaxStorage);
    }

    public void Read(NetIncomingMessage message)
    {
        CartType = (FreightCartMob.eMinecartType)message.ReadByte();
        UsedStorage = message.ReadInt32();
        MaxStorage = message.ReadInt32();
    }
}
