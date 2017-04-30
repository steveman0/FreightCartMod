using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FortressCraft.Community.Utilities;

public class FreightInterfaceContainer
{
    public FreightCartStation Station;
    public FreightSystemInterface Interface;
    public List<FreightRegistry> Registries;
    public object offerlock = new object();
    public object requestlock = new object();


    public FreightInterfaceContainer(FreightCartStation station, FreightSystemInterface inter)
    {
        this.Station = station;
        this.Interface = inter;
        this.Registries = new List<FreightRegistry>();
    }

    public void UpdateRegistries()
    {
        if (string.IsNullOrEmpty(Station.NetworkID))
            return;
        List<FreightRegistry> registries = new List<FreightRegistry>();
        lock (offerlock)
        {
            foreach (FreightListing entry in Interface.FreightOfferings)
            {
                if (entry.Item == null)
                    continue;
                FreightRegistry reg = new FreightRegistry(Station.NetworkID, null, entry.Item, 0, entry.Quantity, FreightRegistry.RegistryType.InterfaceData);
                reg.Surplus = entry.Quantity;
                registries.Add(reg);
            }
        }

        lock (requestlock)
        {
            foreach (FreightListing entry in Interface.FreightRequests)
            {
                if (entry.Item == null)
                    continue;
                FreightRegistry reg = registries.Where(x => x.FreightItem.Compare(entry.Item)).FirstOrDefault();
                if (reg != null)
                {
                    reg.Deficit = entry.Quantity;
                    reg.LowStock = entry.Quantity;
                }
                else
                {
                    reg = new FreightRegistry(Station.NetworkID, null, entry.Item, entry.Quantity, int.MaxValue, FreightRegistry.RegistryType.InterfaceData);
                    reg.Deficit = entry.Quantity;
                    registries.Add(reg);
                }
            }
        }
        foreach (FreightRegistry reg in registries)
        {
            //Debug.Log("FreightInterfaceContainer updating registry with reg item: " + reg.FreightItem.ToString() + " lowstock: " + reg.LowStock + "highstock: " + reg.HighStock + " deficit: " + reg.Deficit + " surplus: " + reg.Surplus);
        }

        // Check existing registries -> if the new update doesn't have the entry then zero it in the network to remove later
        foreach (FreightRegistry reg in this.Registries)
        {
            if (!registries.Exists(x => x.FreightItem.Compare(reg.FreightItem)))
            {
                FreightCartManager.instance.ZeroInterfaceRegistry(Station.NetworkID, reg.FreightItem);
                //Debug.Log("FreightInterfaceContainer had to zero an interface registry for " + reg.FreightItem.ToString());
            }
        }

        this.Registries = registries;
    }
}

