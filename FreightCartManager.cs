using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using FortressCraft.Community.Utilities;
using System.Threading;

public class FreightCartManager
{
    public static FreightCartManager instance = null;
    private List<FreightRegistry> MasterRegistry; // Handle this all internally to ensure consistent locking
    private List<FreightRegistry> InterfaceMaster; // As with the master handle internally
    public List<FreightRegistry> CopiedFreight;
    public FreightCartStation CopiedFreightStation;
    public List<MassInventory> StationInventories;
    public List<FreightInterfaceContainer> FreightInterfaces;
    public List<MassInventory> OldDeficits;
    public List<MassInventory> NetworkStock;
    public List<MassInventory> NetworkDeficit;
    public List<KeyValuePair<ItemBase, int>> GlobalInventory;
    public List<FreightTrackNetwork> GlobalTrackNetworks = new List<FreightTrackNetwork>();
    public List<string> Networks;
    private object RegistryLock = new object();
    private object InterfaceLock = new object();

    public FreightCartManager()
    {
        //Debug.Log("Freight Cart Manager created on thread id: " + Thread.CurrentThread.ManagedThreadId + " LFG thread ID: " + LowFrequencyThread.mnThreadId);
        instance = this;
        SystemMonitorWindow.fcm = this;
        this.MasterRegistry = new List<FreightRegistry>();
        this.InterfaceMaster = new List<FreightRegistry>();
        this.NetworkStock = new List<MassInventory>();
        this.NetworkDeficit = new List<MassInventory>();
        this.StationInventories = new List<MassInventory>();
        this.FreightInterfaces = new List<FreightInterfaceContainer>();
        this.OldDeficits = new List<MassInventory>();
        this.Networks = new List<string>();
        this.GlobalInventory = new List<KeyValuePair<ItemBase, int>>();
        //GameObject Sync = new GameObject("ManagerSync");
        //Sync.AddComponent<ManagerSync>();
        //Sync.SetActive(true);
        //Sync.GetComponent<ManagerSync>().enabled = true;
    }

    public void DebugFreight()
    {
        lock (RegistryLock)
        {
            Debug.Log("-----------------FREIGHT DEBUG------------------");
            Debug.Log("freight count: " + FreightCartManager.instance.MasterRegistry.Count);
            foreach (FreightRegistry reg in MasterRegistry)
            {
                if (reg.MassStorage == null)
                    Debug.Log("Following entry is for network data: " + reg.NetworkID);
                Debug.Log("Freight registry item: " + reg.FreightItem.ToString() + ", network: " + reg.NetworkID + ", LowStock: " + reg.LowStock.ToString() + ", HighStock: " + reg.HighStock.ToString()
                    + ", Inventory: " + reg.Inventory.ToString() + ", Deficit: " + reg.Deficit.ToString() + ", Surplus: " + reg.Surplus.ToString() + ", Data type: " + reg.DataType.ToString());
            }
            Debug.Log("-------------------Interface Freight------------------");
            foreach (FreightRegistry reg in this.InterfaceMaster)
            {
                Debug.Log("Freight registry item: " + reg.FreightItem.ToString() + ", network: " + reg.NetworkID + ", LowStock: " + reg.LowStock.ToString() + ", HighStock: " + reg.HighStock.ToString()
                    + ", Inventory: " + reg.Inventory.ToString() + ", Deficit: " + reg.Deficit.ToString() + ", Surplus: " + reg.Surplus.ToString() + ", Data type: " + reg.DataType.ToString());
            }
        }
    }

    public void DebugStations()
    {
        foreach (MassInventory x in this.StationInventories)
        {
            foreach (FreightCartStation y in x.ConnectedStations)
            {
                Debug.Log("Station network: " + y.NetworkID);
            }
        }
    }

    public void DebugInv(int count)
    {
        foreach (KeyValuePair<ItemBase, int> x in this.StationInventories[count].Inventory)
        {
            Debug.Log("Inventory key: " + x.Key + " Value: " + x.Value);
        }
    }

    public void DebugNetworks()
    {
        Debug.Log("Total network count: " + this.Networks.Count);
        foreach (string x in this.Networks)
            Debug.Log("Network entry with ID: " + x);
    }

    public void DebugTrackNetworks()
    {
        Debug.Log("---------------Track Network Debug-------------------");
        int count = this.GlobalTrackNetworks.Count;
        for (int n = 0; n < count; n++)
        {
            FreightTrackNetwork x = this.GlobalTrackNetworks[n];
            Debug.Log("Network Entry with ID: " + x.NetworkID.ToString() + " and junction count: " + x.TrackJunctions.Count.ToString());
        }
        Debug.Log("--------------------End Debug----------------------------");
    }

    public bool RegistryInitialized()
    {
        return this.MasterRegistry != null;
    }

    public void AddNetwork(string networkid)
    {
        if (this.Networks.Contains(networkid))
            return;
        this.Networks.Add(networkid);
    }

    public bool ContainsNetwork(string networkid)
    {
        return this.Networks.Contains(networkid);
    }

    /// <summary>
    ///     Remove extra networks from global list if they are not in use
    /// </summary>
    /// <param name="networkid"></param>
    public void RemoveExtraNetwork(string networkid)
    {
        if (string.IsNullOrEmpty(networkid))
            return;
        if (this.StationInventories.SelectMany(x => x.ConnectedStations.Where(y => y.NetworkID == networkid)).ToList().Count == 0)
        {
            this.Networks.Remove(networkid);
        }
    }

    public void CopyFreightEntries(FreightCartStation station)
    {
        //Search for all entries for this station and save to this.CopiedFreight 
        if (station == null || station.massStorageCrate == null || string.IsNullOrEmpty(station.NetworkID))
        {
            Debug.LogWarning("CopyFreightEntries tried to copy from null crate or station");
            return;
        }
        this.CopiedFreightStation = station;
        lock (RegistryLock)
        {
            this.CopiedFreight = MasterRegistry.FindAll(x => x.MassStorage == station.massStorageCrate && x.NetworkID == station.NetworkID).ToList();
        }
    }

    public void PasteFreightEntries(FreightCartStation destination, string networkid, MassStorageCrate crate)
    {
        //Remove all old entries for the station before adding new ones
        lock (RegistryLock)
        {
            if (!string.IsNullOrEmpty(networkid))
                MasterRegistry.RemoveAll(x => x.MassStorage == crate && x.NetworkID == networkid);
            else if (this.CopiedFreightStation != null && !string.IsNullOrEmpty(this.CopiedFreightStation.NetworkID))
            {
                FreightCartWindow.SetNetwork(destination, this.CopiedFreightStation.NetworkID);
                networkid = this.CopiedFreightStation.NetworkID;
            }
            else
                return;

            for (int index = 0; index < this.CopiedFreight.Count; index++)
            {
                FreightRegistry freight = this.CopiedFreight[index];
                FreightRegistry newentry = new FreightRegistry(networkid, crate, freight.FreightItem, freight.LowStock, freight.HighStock, FreightRegistry.RegistryType.Registry);
                this.MasterRegistry.Add(newentry);
            }
        }
    }

    public List<FreightRegistry> GetFreightEntries(string networkid, MassStorageCrate crate)
    {
        //Search for all entries for this station and save to this.CopiedFreight 
        if (crate == null)
        {
            Debug.LogWarning("GetFreightEntries tried to retrieve from null crate");
            return new List<FreightRegistry>();
        }
        lock (RegistryLock)
        {
            return MasterRegistry.FindAll(x => x.MassStorage == crate && x.NetworkID == networkid).ToList();
        }
    }

    public void RemoveRegistry(string networkid, MassStorageCrate crate, ItemBase item)
    {
        if (crate == null || item == null)
        {
            Debug.LogWarning("RemoveRegistry tried to remove for null crate or item");
            return;
        }
        lock (RegistryLock)
        {
            MasterRegistry.RemoveAll(x => x.MassStorage == crate && x.FreightItem.Compare(item) && x.NetworkID == networkid);
        }
    }

    public void ZeroInterfaceRegistry(string networkid, ItemBase item)
    {
        if (string.IsNullOrEmpty(networkid) || item == null)
            return;
        lock(InterfaceLock)
        {
            FreightRegistry reg = this.InterfaceMaster.Where(x => x.NetworkID == networkid && x.FreightItem.Compare(item)).FirstOrDefault();
            if (reg != null)
            {
                if (reg.Stock == 0)
                    this.InterfaceMaster.Remove(reg);
                else // Hack solution to prevent maintaining false deficit/surplus on the network -> removed if the stock later hits 0
                {
                    reg.Deficit = 0;
                    reg.Surplus = 0;
                }
            }
            
        }
    }

    public List<FreightRegistry> GetNetworkRegistries(int networkindex)
    {
        IEnumerable<FreightRegistry> registries = new List<FreightRegistry>();
        IEnumerable<FreightRegistry> Interreg = new List<FreightRegistry>();
        lock (RegistryLock)
        {
            registries = this.MasterRegistry.FindAll(x => x.NetworkID == this.Networks[networkindex] && x.DataType == FreightRegistry.RegistryType.NetworkData);
        }
        lock (InterfaceLock)
        {
            Interreg = this.InterfaceMaster.FindAll(x => x.NetworkID == this.Networks[networkindex] && x.DataType == FreightRegistry.RegistryType.NetworkData);
        }
        // Might be horrifically slow but this method is only ever called in the freight system monitor UI
        List<FreightRegistry> newregs = new List<FreightRegistry>();
        foreach (FreightRegistry reg in Interreg)
        {
            FreightRegistry regedit = null;
            regedit = registries.FirstOrDefault(x => x.FreightItem.Compare(reg.FreightItem));
            if (regedit != null)
            {
                // We can't update the registry!  It points to the same entry as in the master!  Create a new one and copy changes.
                FreightRegistry updatedreg = new FreightRegistry(regedit.NetworkID, null, regedit.FreightItem, regedit.LowStock, regedit.HighStock, FreightRegistry.RegistryType.NetworkData);
                updatedreg.Deficit = regedit.Deficit + reg.Deficit;
                updatedreg.Inventory = regedit.Inventory + reg.Inventory;
                updatedreg.Stock = regedit.Stock + reg.Stock;
                updatedreg.Surplus = regedit.Surplus + reg.Surplus;
                newregs.Add(updatedreg);
            }
            else
            {
                newregs.Add(reg);
            }
        }
        foreach (FreightRegistry reg in registries)
        {
            if (!newregs.Exists(x => x.FreightItem.Compare(reg.FreightItem)))
                newregs.Add(reg);
        }
        Debug.Log("NetworkRegistries for network " + this.Networks[networkindex] + " total count is: " + newregs.Count);
        return newregs;
    }

    public bool IsRegistered(FreightCartStation station)
    {
        if (station != null && station.massStorageCrate != null && this.StationInventories != null)
            return this.StationInventories.Exists(x => x.MassStorage == station.massStorageCrate && x.ConnectedStations.Contains(station));
        return false;
    }

    public MassInventory TryRegisterStation(FreightCartStation station)
    {
        if (station.massStorageCrate == null || string.IsNullOrEmpty(station.NetworkID))
            return null;
        if (!this.IsRegistered(station))
        {
            if (!string.IsNullOrEmpty(station.NetworkID) && !this.ContainsNetwork(station.NetworkID))
                this.AddNetwork(station.NetworkID);
            MassInventory currentinv = this.StationInventories.Where(x => x.MassStorage == station.massStorageCrate).FirstOrDefault();
            MassInventory inv = null;
            if (station.massStorageCrate != null && !string.IsNullOrEmpty(station.NetworkID))
                 inv = new MassInventory(station.massStorageCrate, station.NetworkID);
            if (currentinv == null)
            {
                inv.ConnectedStations.Add(station);
                this.StationInventories.Add(inv);
                return inv;
            }
            else if (station != null && currentinv.ConnectedStations != null)
            {
                currentinv.ConnectedStations.Add(station);
                return currentinv;
            }
            else
            {
                Debug.LogWarning("TryRegister failed to register freight cart station.");
                return null;
            }
        }
        return this.StationInventories.Where(x => x.MassStorage == station.massStorageCrate && x.ConnectedStations.Contains(station)).FirstOrDefault();
    }

    public void TryRegisterInterface(FreightCartStation station, FreightSystemInterface freightinterface)
    {
        if (freightinterface == null)
            return;
        lock (InterfaceLock)
        {
            if (!this.FreightInterfaces.Exists(x => x.Interface == freightinterface))
                this.FreightInterfaces.Add(new FreightInterfaceContainer(station, freightinterface));
        }
        if (!string.IsNullOrEmpty(station.NetworkID) && !this.ContainsNetwork(station.NetworkID))
            this.AddNetwork(station.NetworkID);
    }

    public void RemoveStationReg(FreightCartStation station)
    {
        if (station.massStorageCrate == null || station.NetworkID == null)
            return;
        MassStorageCrate crate = station.massStorageCrate;
        string networkid = station.NetworkID;
        if (crate != null && !string.IsNullOrEmpty(networkid))
        {
            lock (RegistryLock)
            {
                if (!StationInventories.Exists(x => x.MassStorage == crate && x.ConnectedStations.Exists(y => y.NetworkID == networkid && y != station)))
                    MasterRegistry.RemoveAll(x => x.MassStorage == crate && x.NetworkID == networkid);
            }
        }
        station.ConnectedInventory.ConnectedStations.Remove(station);
        //this.StationInventories.ForEach(x => x.ConnectedStations.RemoveAll(y => y == station));
        this.RemoveExtraNetwork(networkid);
    }

    public bool AddRegistry(string networkid, MassStorageCrate crate, ItemBase item, int lowstock, int highstock)
    {
        lock (RegistryLock)
        {
            if (MasterRegistry.Exists(x => x.MassStorage == crate && x.FreightItem.Compare(item) && x.NetworkID == networkid))
                return false;
            if (!MasterRegistry.Exists(x => x.MassStorage == null && x.FreightItem.Compare(item) && x.NetworkID == networkid))
                MasterRegistry.Add(new FreightRegistry(networkid, null, item, 0, int.MaxValue, FreightRegistry.RegistryType.NetworkData));
            MasterRegistry.Add(new FreightRegistry(networkid, crate, item, lowstock, highstock, FreightRegistry.RegistryType.Registry));
            return true;
        }
    }

    /// <summary>
    ///     Update the low/high stock limits for a freight entry
    /// </summary>
    /// <param name="networkid">Freight network ID</param>
    /// <param name="crate">Mass storage crate of the freight entry</param>
    /// <param name="item">Freight Item</param>
    /// <param name="lowstock">Low stock limit (-1 will skip writing the entry)</param>
    /// <param name="highstock">High stock limit (-1 will skip writing the entry)</param>
    /// <returns>True if it completes the write without error</returns>
    public bool UpdateRegistry(string networkid, MassStorageCrate crate, ItemBase item, int lowstock, int highstock)
    {
        lock (RegistryLock)
        {
            if (!MasterRegistry.Exists(x => x.MassStorage == crate && x.FreightItem.Compare(item) && x.NetworkID == networkid))
                return false;
            int index = MasterRegistry.FindIndex(x => x.MassStorage == crate && x.FreightItem.Compare(item) && x.NetworkID == networkid);
            if (index == -1)
            {
                Debug.LogWarning("UpdateRegistry tried to update a missing registry");
                return false;
            }
            if (lowstock != -1)
                MasterRegistry[index].LowStock = lowstock;
            if (highstock != -1)
                MasterRegistry[index].HighStock = highstock;
            return true;
        }
    }

    /// <summary>
    ///     Returns the high stock setting of the associated freight registry
    /// </summary>
    /// <param name="networkid">the network id</param>
    /// <param name="crate">associated mass storage system</param>
    /// <param name="item">the item</param>
    /// <returns></returns>
    public int GetHighStock(string networkid, MassStorageCrate crate, ItemBase item)
    {
        FreightRegistry reg = null;
        lock (RegistryLock)
        {
            reg = FreightCartManager.instance.MasterRegistry.First(x => x.NetworkID == networkid && x.MassStorage == crate && x.FreightItem.Compare(item));
        }
        if (reg == null)
            return -1;
        else
            return reg.HighStock;
    }

    /// <summary>
    ///     Returns the low stock setting of the associated freight registry
    /// </summary>
    /// <param name="networkid">the network id</param>
    /// <param name="crate">associated mass storage system</param>
    /// <param name="item">the item</param>
    /// <returns></returns>
    public int GetLowStock(string networkid, MassStorageCrate crate, ItemBase item)
    {
        FreightRegistry reg = null;
        lock (RegistryLock)
        {
            reg = FreightCartManager.instance.MasterRegistry.First(x => x.NetworkID == networkid && x.MassStorage == crate && x.FreightItem.Compare(item));
        }
        if (reg == null)
            return -1;
        else
            return reg.LowStock;
    }


    /// <summary>
    ///     Update FreightRegistries when a new crate is promoted to center crate
    /// </summary>
    /// <param name="networkid">Freight network ID of the registry</param>
    /// <param name="oldcrate">Old crate</param>
    /// <param name="newcrate">New crate</param>
    public void ReassignFreightRegistry(string networkid, MassStorageCrate oldcrate, MassStorageCrate newcrate)
    {
        if (oldcrate == null || newcrate == null)
        {
            Debug.LogWarning("ReassignFreightRegistry tried to reassign to/from null crate");
            return;
        }
        List<FreightRegistry> registries = this.GetFreightEntries(networkid, oldcrate);
        for (int index = 0; index < registries.Count; ++index)
            registries[index].MassStorage = newcrate;
    }

    public MassInventory InventoryExists(MassStorageCrate crate)
    {
        MassInventory inventory = this.StationInventories.FirstOrDefault(x => x.MassStorage == crate);
        if (inventory != null)
            return inventory;
        return null;
    }


    //Functions for tracking number of items already in the freight system
    public bool NetworkAdd(string networkid, ItemBase item, int amount)
    {
        //Debug.LogWarning("Adding to network: " + networkid + " item: " + item.ToString() + " amount: " + amount.ToString());
        FreightRegistry reg = null;
        FreightRegistry interreg = null;
        lock (RegistryLock)
        {
            reg = this.MasterRegistry.Where(x => x.NetworkID == networkid && x.FreightItem.Compare(item) && x.DataType == FreightRegistry.RegistryType.NetworkData).FirstOrDefault();
        }
        lock (InterfaceLock)
        {
            interreg = this.InterfaceMaster.Where(x => x.NetworkID == networkid && x.FreightItem.Compare(item) && x.DataType == FreightRegistry.RegistryType.NetworkData).FirstOrDefault();
        }
        int otherstock = 0;
        if (interreg != null)
            otherstock = interreg.Stock;
        if (reg != null)
            reg.Stock += amount;
        else if (interreg != null) // Only add to interface stock if we don't have it tracked in main storage
            interreg.Stock += amount;
        else
        {
            //Debug logging removed now that carts will try to add to registry on load and will likely not fird it
            //Debug.LogWarning("FreightCartManager network tried to add item to stock that doesn't exist in freight registry");
            return false;
        }
        return true;
    }

    /// <summary>
    ///     For removing items from network stock
    /// </summary>
    /// <param name="networkid"></param>
    /// <param name="item"></param>
    /// <param name="amount"></param>
    public void NetworkRemove(string networkid, ItemBase item, int amount)
    {
        FreightRegistry reg = null;
        FreightRegistry interreg = null;
        lock (RegistryLock)
        {
            reg = this.MasterRegistry.Where(x => x.NetworkID == networkid && x.FreightItem.Compare(item) && x.DataType == FreightRegistry.RegistryType.NetworkData).FirstOrDefault();
        }
        lock (InterfaceLock)
        {
            interreg = this.InterfaceMaster.Where(x => x.NetworkID == networkid && x.FreightItem.Compare(item) && x.DataType == FreightRegistry.RegistryType.NetworkData).FirstOrDefault();
        }
        if (reg != null)
        {
            if (reg.Stock > amount)
                reg.Stock -= amount;
            else if (interreg != null)
            {
                // Subtrack from main stock first and remove overflow from the interface stock
                amount -= reg.Stock;
                reg.Stock = 0;
                interreg.Stock -= amount;
                if (interreg.Stock < 0)
                    interreg.Stock = 0;

                // Remove old entries if they don't have meaningful freight data
                if (interreg.Stock == 0 && interreg.Deficit == 0 && interreg.Surplus == 0)
                {
                    lock(InterfaceLock)
                    {
                        this.InterfaceMaster.Remove(interreg);
                    }
                }
            }
        }
        else if (interreg != null) // No registry with storage but we have it in the interfaces
        {
            if (interreg.Stock > amount)
                interreg.Stock -= amount;
            else
                interreg.Stock = 0;
        }
        else
            Debug.LogWarning("Tried to remove network stock from a null freight registry!");
    }


    //For reducing the mass storage quantity as carts draw from it (between refreshes)
    public void StorageRemove(MassInventory inventory, ItemBase item, int amount)
    {
        int loc = inventory.Inventory.FindIndex(x => x.Key.Compare(item));
        if (loc != -1)
        {
            KeyValuePair<ItemBase, int> kvp = inventory.Inventory[loc];
            inventory.Inventory[loc] = new KeyValuePair<ItemBase, int>(kvp.Key, kvp.Value - amount);
        }

        //if (inventory.Inventory.ContainsKey(item))
        //    inventory.Inventory[item] -= amount;
    }

    /// <summary>
    ///     How many items are still needed by the network
    /// </summary>
    /// <param name="item">Item to check</param>
    /// <returns>Number required to satisfy the deficit</returns>
    public int NetworkNeeds(string networkid, ItemBase item)
    {
        FreightRegistry reg = null;
        FreightRegistry interreg = null;
        lock (RegistryLock)
        {
            reg = this.MasterRegistry.Where(x => x.NetworkID == networkid && x.FreightItem.Compare(item) && x.DataType == FreightRegistry.RegistryType.NetworkData).FirstOrDefault();
        }
        lock (InterfaceLock)
        {
            interreg = this.InterfaceMaster.Where(x => x.NetworkID == networkid && x.FreightItem.Compare(item) && x.DataType == FreightRegistry.RegistryType.NetworkData).FirstOrDefault();
        }
        int otherdef = 0;
        int otherstock = 0;
        if (interreg != null)
        {
            otherdef = interreg.Deficit;
            otherstock = interreg.Stock;
        }
        if (reg != null)
            return (reg.Deficit + otherdef) > (reg.Stock + otherstock) ? (reg.Deficit + otherdef) - (reg.Stock + otherstock) : 0;
        else if (otherdef > 0 || otherstock > 0)
            return otherdef > otherstock ? otherdef - otherstock : 0;
        return -1;
    }

    /// <summary>
    ///     Returns the deficit for an item
    /// </summary>
    /// <param name="networkid">The freight network id</param>
    /// <param name="item">The item</param>
    /// <returns>The deficit of the item on the network</returns>
    public int RegistryDeficit(string networkid, ItemBase item)
    {
        FreightRegistry reg = null;
        FreightRegistry interreg = null;
        lock (RegistryLock)
        {
            reg = this.MasterRegistry.Where(x => x.NetworkID == networkid && x.FreightItem.Compare(item) && x.DataType == FreightRegistry.RegistryType.NetworkData).FirstOrDefault();
        }
        lock (InterfaceLock)
        {
            interreg = this.InterfaceMaster.Where(x => x.NetworkID == networkid && x.FreightItem.Compare(item)).FirstOrDefault();
        }
        int interfacedef = 0;
        if (interreg != null)
            interfacedef = interreg.Deficit;
        if (reg != null)
            return reg.Deficit + interfacedef;
        else if (interreg != null)
            return interfacedef;
        return -1;
    }

    public int InterfaceDeficit(string networkid, ItemBase item)
    {
        FreightRegistry interreg = null;
        lock (InterfaceLock)
        {
            interreg = this.InterfaceMaster.Where(x => x.NetworkID == networkid && x.FreightItem.Compare(item)).FirstOrDefault();
        }
        if (interreg != null)
            return interreg.Deficit;
        return 0;
    }

    public List<KeyValuePair<ItemBase, int>> GetStationNeeds(FreightCartStation station)
    {
        if (station == null || string.IsNullOrEmpty(station.NetworkID) || this.NoConnectedProvider(station))
        {
            if (station != null && !string.IsNullOrEmpty(station.NetworkID))
                Debug.LogWarning("FreightCartManager Tried to get station needs for null provider - probably mass storage?");
            else
                Debug.LogWarning("FreightCartManager Tried to get station needs for null station or networkid?");
            return new List<KeyValuePair<ItemBase, int>>();
        }
        if (station.AttachedInterface != null)
        {
            return ListingToKvPList(station.AttachedInterface.FreightRequests);
        }
        else if (station.HopperInterface != null)
        {
            return ListingToKvPList(station.HopperInterface.FreightRequests);
        }
        List<FreightRegistry> registries = this.GetFreightEntries(station.NetworkID, station.ConnectedInventory.MassStorage);
        List<KeyValuePair<ItemBase, int>> needs = new List<KeyValuePair<ItemBase, int>>();

        IEnumerable<FreightRegistry> offers = registries.Where(x => x.MassStorage == station.massStorageCrate && x.LowStock - x.Inventory > 0);
        foreach (FreightRegistry reg in offers)
            needs.Add(new KeyValuePair<ItemBase, int>(reg.FreightItem, reg.LowStock - reg.Inventory));
        return needs;
    }

    private bool NoConnectedProvider(FreightCartStation station)
    {
        return (station.ConnectedInventory == null || station.ConnectedInventory.MassStorage == null) && station.AttachedInterface == null && station.HopperInterface == null;
    }

    private List<KeyValuePair<ItemBase, int>> ListingToKvPList(List<FreightListing> listing)
    {
        List<KeyValuePair<ItemBase, int>> outputdata = new List<KeyValuePair<ItemBase, int>>();
        foreach (FreightListing entry in listing)
        {
            outputdata.Add(new KeyValuePair<ItemBase, int>(entry.Item, entry.Quantity));
        }
        return outputdata;
    }
    
        /// <summary>
        ///     Get Items that this station offers that the network needs
        /// </summary>
        /// <param name="inventory">Inventory to check</param>
        /// <returns>List of items and their quantities available</returns>
    public List<KeyValuePair<ItemBase, int>> GetStationOfferings(FreightCartStation station)
    {
        if (station == null || string.IsNullOrEmpty(station.NetworkID) || NoConnectedProvider(station))
            return new List<KeyValuePair<ItemBase, int>>();
        if (station.AttachedInterface != null)
        {
            return ListingToKvPList(station.AttachedInterface.FreightOfferings);
        }
        else if (station.HopperInterface != null)
        {
            return ListingToKvPList(station.HopperInterface.FreightOfferings);
        }
        if (station.OfferAll)
            return station.ConnectedInventory.Inventory;
        List<FreightRegistry> registries = this.GetFreightEntries(station.NetworkID, station.ConnectedInventory.MassStorage);
        List<KeyValuePair<ItemBase, int>> available = new List<KeyValuePair<ItemBase, int>>();

        IEnumerable<FreightRegistry> offers = registries.Where(x => x.MassStorage == station.massStorageCrate && x.Inventory - x.HighStock > 0);
        foreach (FreightRegistry reg in offers)
            available.Add(new KeyValuePair<ItemBase, int>(reg.FreightItem, reg.Inventory - reg.HighStock));
        return available;
    }

    /// <summary>
    ///     Gets the network with the highest rating for deficit/surplus that a cart can help satisfy
    /// </summary>
    /// <returns>The network ID</returns>
    public string GetNeedyNetwork()
    {
        List<FreightRegistry> reg = new List<FreightRegistry>();
        lock (RegistryLock)
        {
            reg = this.MasterRegistry.Where(x => x.MassStorage == null).ToList();
        }
        int count = reg.Count;
        float maxval = 0;
        string network = "";
        for (int n = 0; n < count; n++)
        {
            int surplus = reg[n].Surplus;
            int deficit = reg[n].Deficit;
            float testval = (float)(deficit + surplus) / System.Math.Abs(deficit - surplus);
            if (testval > maxval && surplus > 0)
            {
                maxval = testval;
                network = reg[n].NetworkID;
            }
        }
        return network;
    }

    public FreightCartStation GetNeedyStation()
    {
        string networkid = this.GetNeedyNetwork();
        return this.GetStation(networkid, null, false);
    }

    /// <summary>
    ///     Gets a freight cart station that is offering/requesting the item on the network
    /// </summary>
    /// <param name="networkid">On this network</param>
    /// <param name="item">This item type for delivery - search for any offer if item is null</param>
    /// <param name="offloadingexcess">true if we're trying to get rid of the item</param>
    /// <param name="wantsoffers">true if we're looking for stations offering the item</param>
    /// <returns>The station</returns>
    public FreightCartStation GetStation(string networkid, ItemBase item, bool offloadingexcess, bool wantsoffers = false)
    {
        //OMG please come up with something better!!

        //Get all item registries for this network on all storages
        //Debug.Log("FCM GetStation running with network '" + networkid + "' and item: " + item.ToString() + " with wantsoffers: " + wantsoffers.ToString());
        List<FreightRegistry> reg = new List<FreightRegistry>();
        lock (RegistryLock)
        {
            if (item == null)
                reg = this.MasterRegistry.Where(x => x.NetworkID == networkid && x.DataType == FreightRegistry.RegistryType.Registry).ToList();
            else
                reg = this.MasterRegistry.Where(x => x.NetworkID == networkid && x.DataType == FreightRegistry.RegistryType.Registry && x.FreightItem.Compare(item)).ToList();
        }
        int count = reg.Count;
        for (int n = 0; n < count; n++)
        {
            //Get the crate associated with the entry
            FreightRegistry regentry = reg[n];
            MassStorageCrate crate = regentry.MassStorage;
            int count2 = this.StationInventories.Count;
            //Debug.LogWarning("Registry entry: " + regentry.FreightItem.ToString() + " lowstock: " + regentry.LowStock.ToString() + " highstock: " + regentry.HighStock.ToString());
            for (int m = 0; m < count2; m++)
            {
                //Alternate?
                //this.StationInventories.Where(x => x.MassStorage == crate).SelectMany(x => x.ConnectedStations.Where(y => y.NetworkID == x.NetworkID)).ToList();
                //Get the mass inventory that matches the crate 
                MassInventory inv = this.StationInventories[m];
                //if (inv.ConnectedStations.Count > 0)
                //    Debug.LogWarning("Looping over inventories m: " + m.ToString() + " inventory station ID: " + inv.ConnectedStations[0].StationID.ToString());
                //else
                //    Debug.LogWarning("Station inventory doesn't have a connected station?");
                if (inv.MassStorage == crate)
                {
                    int count3 = inv.ConnectedStations.Count;
                    for (int p = 0; p < count3; p++)
                    {
                        //Check all of the connected stations
                        FreightCartStation station = inv.ConnectedStations[p];
                        //Debug.LogWarning("Current station being checked.  ID: " + station.StationID.ToString());
                        if (station.NetworkID == networkid)
                        {
                            //Debug.LogWarning("FCM GetStation found a station with matching network id - station ID: " + station.StationID.ToString());
                            //Debug.LogWarning("Total registry count: " + count.ToString() + " total inventories count: " + count2.ToString() + " total connected stations count: " + count3.ToString());
                            //For the station that matches the network ID
                            if (item == null)
                            {
                                List<KeyValuePair<ItemBase, int>> offers = this.GetStationOfferings(station);
                                if (offers.Count > 0 && offers.Sum(x => x.Value) > 5)
                                    return station;
                            }
                            else
                            {
                                if (!offloadingexcess)
                                {
                                    if (!wantsoffers)
                                    {
                                        List<KeyValuePair<ItemBase, int>> needs = this.GetStationNeeds(station);
                                        int count4 = needs.Count;
                                        //.LogWarning("Station needs count: " + count4);
                                        //List<FreightRegistry> regdeb = this.GetLocalDeficit(networkid, crate);
                                        //if (regdeb.Count > 0)
                                        //{
                                        //    FreightRegistry regdebug = this.GetLocalDeficit(networkid, crate)[0];
                                        //    //Debug.LogWarning("Station first local deficit: " + regdebug.Deficit.ToString() + " " + regdebug.FreightItem.ToString());
                                        //}
                                        for (int q = 0; q < count4; q++)
                                        {
                                            //Debug.LogWarning("Station needs in FCM GetStation: " + needs[q].Key.ToString());
                                            if (needs[q].Key.Compare(item))
                                                return station;
                                        }
                                    }
                                    else
                                    {
                                        List<KeyValuePair<ItemBase, int>> offers = this.GetStationOfferings(station);
                                        int count4 = offers.Count;
                                        for (int q = 0; q < count4; q++)
                                        {
                                            //Debug.LogWarning("Station needs in FCM GetStation: " + offers[q].Key.ToString());
                                            if (offers[q].Key.Compare(item))
                                                return station;
                                        }
                                    }
                                }
                                else
                                    return station;
                            }
                        }
                    }
                }
            }
        }

        // Look into this to see how this would/wouldn't work before turning back on...
        if (offloadingexcess)
            return null;
        lock (InterfaceLock)
        {
            //Check Freight Interfaces for requesting/offering station
            List<FreightInterfaceContainer> containers = this.FreightInterfaces;
            foreach (FreightInterfaceContainer container in containers)
            {
                if (!wantsoffers)
                {
                    KeyValuePair<ItemBase, int> kvp = this.GetStationNeeds(container.Station).Where(x => x.Key.Compare(item) && x.Value > 0).FirstOrDefault();
                    if (!kvp.Equals(default(KeyValuePair<ItemBase, int>)))
                        return container.Station;
                }
                else
                {
                    KeyValuePair<ItemBase, int> kvp = this.GetStationOfferings(container.Station).Where(x => x.Key.Compare(item) && x.Value > 0).FirstOrDefault();
                    if (!kvp.Equals(default(KeyValuePair<ItemBase, int>)))
                        return container.Station;
                }
            }
        }
        
        return null;
    }

    public int GetDeficitFromRegistry(string networkid, MassStorageCrate crate, ItemBase item)
    {
        lock (RegistryLock)
        {
            return this.MasterRegistry.Where(x => x.MassStorage == crate && x.FreightItem.Compare(item) && x.NetworkID == networkid).FirstOrDefault().Deficit;
        }
    }

    /// <summary>
    ///     Returns the top three items in deficit
    /// </summary>
    /// <param name="crate">For this storage</param>
    /// <returns></returns>
    public List<FreightRegistry> GetLocalDeficit(string networkid, MassStorageCrate crate)
    {
        lock (RegistryLock)
        {
            return this.MasterRegistry.Where(x => x.NetworkID == networkid && x.MassStorage == crate && x.Deficit > 0).OrderByDescending(x => x.Deficit).Take(3).ToList();
        }
    }

    public List<FreightRegistry> GetLocalSurplus(string networkid, MassStorageCrate crate)
    {
        lock (RegistryLock)
        {
            return this.MasterRegistry.Where(x => x.NetworkID == networkid && x.MassStorage == crate && x.Surplus > 0).OrderByDescending(x => x.Surplus).Take(3).ToList();
        }
    }

    static int Compare2(KeyValuePair<ItemBase, int> a, KeyValuePair<ItemBase, int> b)
    {
        return -a.Value.CompareTo(b.Value);
    }

    public void UpdateMassInventory()
    {
        //this.DebugNetworks();
        //Debug.Log("Station inv count: " + this.StationInventories.Count);
        //Find entry for given mass storage and overwrite the Dictionary with the old.
        //Need to update all old deficits for new... loop over update deficit for the change in each local deficit
        int count = this.StationInventories.Count;
        for (int index = 0; index < count; ++index)
        {
            //Distribute the load of updating each inventory while maintaining a reasonable refresh rate
            //if (index % 15 == ManagerSync.instance.Update % 15)
            if (index % 15 == FreightCartMod.UpdateCounter % 15)
            {
                //New approach outline
                //Crate == null implies the entry is for the network data associated with the networkid of the entry
                //0. Build the new inventory list nothing needs to happen before this
                //1. Use linq to get all freight entries for the crate
                //2. Copy the inventory storage over to the freight registry
                //3. Calculate the new deficit from the lowstock and inventory and store it
                //4. Calculate the network deficit by performing a sum over all inventories by network ID (linq here as well should work)
                //  -> the network deficit is also stored in the MasterRegistry where NetworkID is the ID and Crate is null

                //0. Build the new inventory list nothing needs to happen before this
                this.StationInventories[index].BuildInventoryList();
                MassStorageCrate crate = this.StationInventories[index].MassStorage;
                List <KeyValuePair<ItemBase, int>> items = this.StationInventories[index].Inventory;

                lock (RegistryLock)
                { 
                    //this.DebugFreight();
                    //1. Get all freight entries for the crate
                    this.MasterRegistry.Where(x => x.MassStorage == crate).ToList()
                            //2. Copy the inventory storage over to the freight registry
                            .ForEach(x => x.Inventory = items.FirstOrDefault(y => y.Key.Compare(x.FreightItem)).Value);
                    //3. Calculate the new deficits and surpluses from the lowstock and highstock and inventory and store it
                    this.MasterRegistry.Where(x => x.MassStorage == crate).ToList().ForEach(x => x.RecalculateParams());

                    //4. Calculate the network deficit by performing a sum over all inventories by network ID
                    //Second line is to select only the entries with networks associated with the crate - no need to update non-updating networks
                    //Third line is to group by freight item so that we sum up deficits per item entry
                    var groups = this.MasterRegistry.Where(x => x.DataType == FreightRegistry.RegistryType.NetworkData
                            && this.MasterRegistry.Where(y => y.MassStorage == crate).Select(z => z.NetworkID == x.NetworkID).Any(a => a.Equals(true)))
                            .GroupBy(z => z.FreightItem, new ItemBaseComparer());
                    
                    foreach (var group in groups)
                    {
                        ItemBase item = group.Key;
                        foreach (FreightRegistry x in group)
                        {
                            // Save big by one call of where!!!
                            IEnumerable<FreightRegistry> regs = this.MasterRegistry.Where(y => y.NetworkID == x.NetworkID && y.FreightItem.Compare(item) && y.DataType == FreightRegistry.RegistryType.Registry);
                            x.Deficit = regs.Sum(z => z.Deficit);
                            x.Surplus = regs.Sum(z => z.Surplus);
                            x.Inventory = regs.Sum(z => z.Inventory);

                            //x.Deficit = this.MasterRegistry.Where(y => y.NetworkID == x.NetworkID && y.FreightItem.Compare(item) && y.DataType == FreightRegistry.RegistryType.Registry).Sum(z => z.Deficit);
                            //x.Surplus = this.MasterRegistry.Where(y => y.NetworkID == x.NetworkID && y.FreightItem.Compare(item) && y.DataType == FreightRegistry.RegistryType.Registry).Sum(z => z.Surplus);
                            //x.Inventory = this.MasterRegistry.Where(y => y.NetworkID == x.NetworkID && y.FreightItem.Compare(item) && y.DataType == FreightRegistry.RegistryType.Registry).Sum(z => z.Inventory);
                        }
                    }
                }
                //catch (InvalidOperationException)
                //{
                //    Debug.LogWarning("UpdateMassInventory caught an InvalidOperationException, probably due to threaded access of MasterRegistry.  Aborting!");
                //    break;
                //}
            }
        }

        //Update freight interfaces -> spread over update ticks just like storages
        count = this.FreightInterfaces.Count;
        //Debug.Log("FreightInterface count: " + count);
        for (int n = 0; n < count; n++)
        {
            if (n % 15 == FreightCartMod.UpdateCounter % 15)
            {
                lock (InterfaceLock)
                {
                    // Update the interface's deficits/surplus
                    this.FreightInterfaces[n].UpdateRegistries();
                    //Debug.Log("Updating registries for FreightInterface " + n);
                    List<FreightRegistry> registries = this.FreightInterfaces[n].Registries;
                
                    // This could possible be done with grouping but not sure it'd gain much since then it'd need a foreach on the groups
                    foreach (FreightRegistry reg in registries)
                    {
                        //Debug.Log("FreightReg data: Item: " + reg.FreightItem.ToString() + " Deficit:" + reg.Deficit + " HighStock:" + reg.HighStock + " LowStock:" + reg.LowStock + " NetworkID:" + reg.NetworkID + " Surplus:" + reg.Surplus + " Stock:" + reg.Stock);
                        // if exists update the item based on all interface entries
                        if (this.InterfaceMaster.Exists(x => x.NetworkID == reg.NetworkID && x.FreightItem.Compare(reg.FreightItem)))
                        {
                            FreightRegistry update = this.InterfaceMaster.Where(x => x.NetworkID == reg.NetworkID && x.FreightItem.Compare(reg.FreightItem)).FirstOrDefault();
                            IEnumerable<FreightRegistry> regs = this.FreightInterfaces.Where(x => x.Station.NetworkID == reg.NetworkID).SelectMany(y => y.Registries.Where(z => reg.FreightItem.Compare(z.FreightItem)));
                            update.Deficit = regs.Sum(x => x.Deficit);
                            update.Surplus = regs.Sum(x => x.Surplus);
                            //Debug.Log("Update data: Item: " + reg.FreightItem.ToString() + " Deficit:" + reg.Deficit + " HighStock:" + reg.HighStock + " LowStock:" + reg.LowStock + " NetworkID:" + reg.NetworkID + " Surplus:" + reg.Surplus + " Stock:" + reg.Stock);
                        }
                        else // No registry - add it
                        {
                            //Debug.Log("Added registry");
                            reg.DataType = FreightRegistry.RegistryType.NetworkData;
                            this.InterfaceMaster.Add(reg);
                        }
                    }
                }
            }
        }
    }

    public void CalculateGlobalInventory()
    {
        this.GlobalInventory = new List<KeyValuePair<ItemBase, int>>();
        var groups = this.StationInventories.SelectMany(x => x.Inventory).GroupBy(y => y.Key, new ItemBaseComparer());

        foreach (var group in groups)
            this.GlobalInventory.Add(new KeyValuePair<ItemBase, int>(group.Key, group.Sum(y => y.Value)));
    }

    public void OrderyInvByName()
    {
        this.GlobalInventory = this.GlobalInventory.OrderBy(x => ItemManager.GetItemName(x.Key)).ToList();
    }

    public void OrderInvByCount()
    {
        this.GlobalInventory = this.GlobalInventory.OrderByDescending(x => x.Value).ToList();
    }
}



public class ItemBaseComparer : IEqualityComparer<ItemBase>
{
    public bool Equals(ItemBase x, ItemBase y)
    {
        return x.Compare(y);
    }

    public int GetHashCode(ItemBase obj)
    {
        return obj.GetHashCode();
    }
}

class ManagerSync : MonoBehaviour
{
    //private int Counter;
    //public int Update;
    public int CartCounter = 0;
    public static ManagerSync instance = null;
    public static Queue<FreightCartMob> CartLoader = new Queue<FreightCartMob>();
    public static FreightCartMob TourCart;
    public static float RebuildTimer;

    void Start()
    {
        //this.Counter = 0;
        instance = this;
        //Debug.Log("Freight Cart Manager Cart Initializer started.");
    }

    void Update()
    {
        //this.Counter++;
        //if (this.Counter % 10 == 0)
        //{
        //    this.Update++;
        //    FreightCartManager.instance.UpdateMassInventory();
        //}
        RebuildTimer -= Time.deltaTime;

        FreightCartMob mob = null;
        if (CartLoader.Count > 0)
            mob = CartLoader.Peek();
        if (mob != null)
        {
            if (mob.mWrapper != null && mob.mWrapper.mGameObjectList != null && mob.mWrapper.mGameObjectList.Count != 0)
            {
                mob.mWrapper.mGameObjectList[0].AddComponent<FreightCartUnity>();
                mob.mWrapper.mGameObjectList[0].gameObject.SetActive(true);
                mob.CartQueuelock = false;
                CartLoader.Dequeue();
                this.CartCounter++;
                if (TourCart != null && mob == TourCart)
                {
                    WorldScript.instance.localPlayerInstance.mRideable = TourCart.mWrapper.mGameObjectList[0].AddComponent<Rideable>();
                    WorldScript.instance.localPlayerInstance.mbRidingCart = true;
                    WorldScript.instance.localPlayerInstance.mbGravity = false;
                }
            }
        }
    } 
}