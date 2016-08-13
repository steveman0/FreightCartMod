using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using FortressCraft.Community.Utilities;
using System.Threading;

public class FreightCartManager
{
    public static FreightCartManager instance = null;
    public List<FreightRegistry> MasterRegistry;
    public List<FreightRegistry> CopiedFreight;
    public List<MassInventory> StationInventories;
    public List<MassInventory> OldDeficits;
    public List<MassInventory> NetworkStock;
    public List<MassInventory> NetworkDeficit;
    public List<KeyValuePair<ItemBase, int>> GlobalInventory;
    public List<string> Networks;

    public FreightCartManager()
    {
        Debug.Log("Freight Cart Manager created on thread id: " + Thread.CurrentThread.ManagedThreadId + " LFG thread ID: " + LowFrequencyThread.mnThreadId);
        instance = this;
        this.MasterRegistry = new List<FreightRegistry>();
        this.NetworkStock = new List<MassInventory>();
        this.NetworkDeficit = new List<MassInventory>();
        this.StationInventories = new List<MassInventory>();
        this.OldDeficits = new List<MassInventory>();
        this.Networks = new List<string>();
        this.GlobalInventory = new List<KeyValuePair<ItemBase, int>>();
        GameObject Sync = new GameObject("ManagerSync");
        Sync.AddComponent<ManagerSync>();
        Sync.SetActive(true);
        Sync.GetComponent<ManagerSync>().enabled = true;
    }

    public void DebugFreight()
    {
        Debug.Log("-----------------FREIGHT DEBUG------------------");
        Debug.Log("freight count: " + FreightCartManager.instance.MasterRegistry.Count);
        foreach (FreightRegistry reg in MasterRegistry)
        {
            if (reg.MassStorage == null)
                Debug.Log("Following entry is for network data: " + reg.NetworkID);
            Debug.Log("Freight registry item: " + reg.FreightItem.ToString() + ", network: " + reg.NetworkID + ", LowStock: " + reg.LowStock.ToString() + ", HighStock: " + reg.HighStock.ToString()
                + ", Inventory: " + reg.Inventory.ToString() + ", Deficit: " + reg.Deficit.ToString() + ", Surplus: " + reg.Surplus.ToString());
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

    public void CopyFreightEntries(string networkid, MassStorageCrate crate)
    {
        //Search for all entries for this station and save to this.CopiedFreight 
        if (crate == null)
        {
            Debug.LogWarning("CopyFreightEntries tried to copy from null crate");
            return;
        }
        this.CopiedFreight = MasterRegistry.FindAll(x => x.MassStorage == crate && x.NetworkID == networkid).ToList();
    }

    public void PasteFreightEntries(string networkid, MassStorageCrate crate)
    {
        //Remove all old entries for the station before adding new ones
        MasterRegistry.RemoveAll(x => x.MassStorage == crate && x.NetworkID == networkid);

        for (int index = 0; index < this.CopiedFreight.Count; index++)
        {
            FreightRegistry freight = this.CopiedFreight[index];
            FreightRegistry newentry = new FreightRegistry(networkid, crate, freight.FreightItem, freight.LowStock, freight.HighStock);
            this.MasterRegistry.Add(newentry);
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
        return MasterRegistry.FindAll(x => x.MassStorage == crate && x.NetworkID == networkid).ToList();
    }

    public void RemoveRegistry(string networkid, MassStorageCrate crate, ItemBase item)
    {
        if (crate == null || item == null)
        {
            Debug.LogWarning("RemoveRegistry tried to remove for null crate or item");
            return;
        }
        MasterRegistry.RemoveAll(x => x.MassStorage == crate && x.FreightItem.Compare(item) && x.NetworkID == networkid);
        return;
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
            }
        }
        return this.StationInventories.Where(x => x.MassStorage == station.massStorageCrate).FirstOrDefault();
    }

    public void RemoveStationReg(FreightCartStation station)
    {
        MassStorageCrate crate = station.massStorageCrate;
        string networkid = station.NetworkID;
        if (crate != null && !string.IsNullOrEmpty(networkid))
        {
            MasterRegistry.RemoveAll(x => x.MassStorage == crate && x.NetworkID == networkid);
        }
        this.StationInventories.ForEach(x => x.ConnectedStations.RemoveAll(y => y == station));
        this.RemoveExtraNetwork(networkid);
    }

    public bool AddRegistry(string networkid, MassStorageCrate crate, ItemBase item, int lowstock, int highstock)
    {
        if (MasterRegistry.Exists(x => x.MassStorage == crate && x.FreightItem.Compare(item) && x.NetworkID == networkid))
            return false;
        if (!MasterRegistry.Exists(x => x.MassStorage == null && x.FreightItem.Compare(item) && x.NetworkID == networkid))
            MasterRegistry.Add(new FreightRegistry(networkid, null, item, 0, int.MaxValue));
        MasterRegistry.Add(new FreightRegistry(networkid, crate, item, lowstock, highstock));
        return true;
    }

    public bool UpdateRegistry(string networkid, MassStorageCrate crate, ItemBase item, int lowstock, int highstock)
    {
        if (!MasterRegistry.Exists(x => x.MassStorage == crate && x.FreightItem.Compare(item) && x.NetworkID == networkid))
            return false;
        int index = MasterRegistry.FindIndex(x => x.MassStorage == crate && x.FreightItem.Compare(item) && x.NetworkID == networkid);
        if (index == -1)
        {
            Debug.LogWarning("UpdateRegistry tried to update a missing registry");
            return false;
        }
        MasterRegistry[index].LowStock = lowstock;
        MasterRegistry[index].HighStock = highstock;
        return true;
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
    public void NetworkAdd(string networkid, ItemBase item, int amount)
    {
        FreightRegistry reg = this.MasterRegistry.Where(x => x.NetworkID == networkid && x.FreightItem.Compare(item) && x.MassStorage == null).FirstOrDefault();
        if (reg != null)
            reg.Stock += amount;
        else
            Debug.LogWarning("FreightCartManager network tried to add item to stock that doesn't exist in freight registry");
    }

    /// <summary>
    ///     For removing items from network stock
    /// </summary>
    /// <param name="networkid"></param>
    /// <param name="item"></param>
    /// <param name="amount"></param>
    public void NetworkRemove(string networkid, ItemBase item, int amount)
    {
        FreightRegistry reg = this.MasterRegistry.Where(x => x.NetworkID == networkid && x.FreightItem.Compare(item) && x.MassStorage == null).FirstOrDefault();
        if (reg != null)
        {
            if (reg.Stock > amount)
                reg.Stock -= amount;
            else
                reg.Stock = 0;
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
        FreightRegistry reg = this.MasterRegistry.Where(x => x.NetworkID == networkid && x.FreightItem.Compare(item) && x.MassStorage == null).FirstOrDefault();
        if (reg != null)
            return reg.Deficit > reg.Stock ? reg.Deficit - reg.Stock : 0;
        return 0;
    }

    public List<KeyValuePair<ItemBase, int>> GetStationNeeds(FreightCartStation station)
    {
        List<FreightRegistry> registries = this.GetFreightEntries(station.NetworkID, station.ConnectedInventory.MassStorage);
        List<KeyValuePair<ItemBase, int>> needs = new List<KeyValuePair<ItemBase, int>>();

        IEnumerable<FreightRegistry> offers = registries.Where(x => x.MassStorage == station.massStorageCrate && x.LowStock - x.Inventory > 0);
        foreach (FreightRegistry reg in offers)
            needs.Add(new KeyValuePair<ItemBase, int>(reg.FreightItem, reg.LowStock - reg.Inventory));
        return needs;
    }


        /// <summary>
        ///     Get Items that this station offers that the network needs
        /// </summary>
        /// <param name="inventory">Inventory to check</param>
        /// <returns>List of items and their quantities available</returns>
    public List<KeyValuePair<ItemBase, int>> GetStationOfferings(FreightCartStation station)
    {
        List<FreightRegistry> registries = this.GetFreightEntries(station.NetworkID, station.ConnectedInventory.MassStorage);
        List<KeyValuePair<ItemBase, int>> available = new List<KeyValuePair<ItemBase, int>>();

        IEnumerable<FreightRegistry> offers = registries.Where(x => x.MassStorage == station.massStorageCrate && x.Inventory - x.HighStock > 0);
        foreach (FreightRegistry reg in offers)
            available.Add(new KeyValuePair<ItemBase, int>(reg.FreightItem, reg.Inventory - reg.HighStock));
        return available;
    }

    public int GetDeficitFromRegistry(string networkid, MassStorageCrate crate, ItemBase item)
    {
        return this.MasterRegistry.Where(x => x.MassStorage == crate && x.FreightItem.Compare(item) && x.NetworkID == networkid).FirstOrDefault().Deficit;
    }

    /// <summary>
    ///     Returns the top three items in deficit
    /// </summary>
    /// <param name="crate">For this storage</param>
    /// <returns></returns>
    public List<FreightRegistry> GetLocalDeficit(string networkid, MassStorageCrate crate)
    {
        return this.MasterRegistry.Where(x => x.NetworkID == networkid && x.MassStorage == crate && x.Deficit > 0).OrderByDescending(x => x.Deficit).Take(3).ToList();
    }

    public List<FreightRegistry> GetLocalSurplus(string networkid, MassStorageCrate crate)
    {
        return this.MasterRegistry.Where(x => x.NetworkID == networkid && x.MassStorage == crate && x.Surplus > 0).OrderByDescending(x => x.Surplus).Take(3).ToList();
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
            if (index % 15 == ManagerSync.instance.Update % 15)
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
                var groups = this.MasterRegistry.Where(x => x.MassStorage == null
                        && this.MasterRegistry.Where(y => y.MassStorage == crate).Select(z => z.NetworkID == x.NetworkID).Any(a => a.Equals(true)))
                        .GroupBy(z => z.FreightItem, new ItemBaseComparer());

                foreach (var group in groups)
                {
                    ItemBase item = group.Key;
                    foreach (FreightRegistry x in group)
                    {
                        x.Deficit = this.MasterRegistry.Where(y => y.NetworkID == x.NetworkID && y.FreightItem.Compare(item) && y.MassStorage != null).Sum(z => z.Deficit);
                        x.Surplus = this.MasterRegistry.Where(y => y.NetworkID == x.NetworkID && y.FreightItem.Compare(item) && y.MassStorage != null).Sum(z => z.Surplus);
                        x.Inventory = this.MasterRegistry.Where(y => y.NetworkID == x.NetworkID && y.FreightItem.Compare(item) && y.MassStorage != null).Sum(z => z.Inventory);
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
    private int Counter;
    public int Update;
    public static ManagerSync instance = null;
    public static Queue<FreightCartMob> CartLoader = new Queue<FreightCartMob>();

    void Start()
    {
        this.Counter = 0;
        instance = this;
        Debug.Log("Freight Cart Manager synchronizing counter started.");
    }

    void FixedUpdate()
    {
        this.Counter++;
        if (this.Counter % 10 == 0)
        {
            this.Update++;
            FreightCartManager.instance.UpdateMassInventory();
        }

        FreightCartMob mob = null;
        if (CartLoader.Count > 0)
            mob = CartLoader.Peek();
        if (mob != null)
        {
            if (mob.mWrapper.mGameObjectList != null && mob.mWrapper.mGameObjectList.Count != 0)
            {
                mob.mWrapper.mGameObjectList[0].AddComponent<FreightCartUnity>();
                mob.mWrapper.mGameObjectList[0].gameObject.SetActive(true);
                CartLoader.Dequeue();
            }
        }
    } 
}