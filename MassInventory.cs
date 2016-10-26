using System.Collections.Generic;
using FortressCraft.Community.Utilities;
using System.Linq;
using UnityEngine;

public class MassInventory
{
    private string name;
    public MassStorageCrate MassStorage;
    public List<KeyValuePair<ItemBase, int>> Inventory;
    public string NetworkID; //Not sure I still need this - can probably strip it out but keeping it to be safe
    public List<FreightCartStation> ConnectedStations; // For use by actual mass storage inventories

    public MassInventory(MassStorageCrate crate, string networkid = null)
    {
        this.MassStorage = crate;
        this.NetworkID = networkid;
        this.Inventory = new List<KeyValuePair<ItemBase, int>>();
        this.ConnectedStations = new List<FreightCartStation>();
    }

    public void BuildInventoryList()
    {
        List<KeyValuePair<ItemBase, int>> items = new List<KeyValuePair<ItemBase, int>>();

        for (int index = 0; index <= this.MassStorage.mConnectedCrates.Count; index++)
        {
            MassStorageCrate crate;
            ItemBase item;
            if (index == this.MassStorage.mConnectedCrates.Count)
                crate = this.MassStorage;
            else
                crate = this.MassStorage.mConnectedCrates[index];

            if (crate.mMode == MassStorageCrate.CrateMode.SingleStack)
            {
                item = crate.mItem;
                if (item != null)
                {
                    int loc = items.FindIndex(x => x.Key.Compare(item));
                    if (loc != -1)
                        items[loc] = new KeyValuePair<ItemBase, int>(items[loc].Key, items[loc].Value + item.GetAmount());
                    else
                    {
                        items.Add(new KeyValuePair<ItemBase, int>(ItemBaseUtil.NewInstance(item), item.GetAmount()));
                    }
                }
            }
            else
            {
                for (int n = 0; n < crate.STORAGE_CRATE_SIZE; n++)
                {
                    item = crate.mItems[n];
                    if (item != null)
                    {
                        int loc = items.FindIndex(x => x.Key.Compare(item));
                        if (loc != -1)
                            items[loc] = new KeyValuePair<ItemBase, int>(items[loc].Key, items[loc].Value + item.GetAmount());
                        else
                        {
                            items.Add(new KeyValuePair<ItemBase, int>(ItemBaseUtil.NewInstance(item), item.GetAmount()));
                        }
                        item = null;
                    }
                }
            }
        }
        this.Inventory = items;
    }

    public string Name
    {
        get { return string.IsNullOrEmpty(this.name) ? "UNNAMED" : this.name; }
        set { this.name = value; }
    }

    public void RemoveNetwork(FreightCartStation station, string networkid)
    {
        this.ConnectedStations.Remove(station);
        if (!this.ConnectedStations.Exists(x => x.NetworkID == networkid))
            FreightCartManager.instance.RemoveExtraNetwork(networkid);
    }

    public bool HasNetwork(string networkid)
    {
        return this.ConnectedStations.Exists(x => x.NetworkID == networkid);
    }
}