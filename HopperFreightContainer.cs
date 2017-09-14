using System;
using System.Collections.Generic;
using FortressCraft.Community.Utilities;
using System.Text;

public class HopperFreightContainer : FreightSystemInterface
{
    public FreightCartStation Station;
    public StorageMachineInterface Machine;
    public ItemBase OfferItem;
    public ItemBase RequestItem;
    public int RequestLimit;
    public int OfferLimit;


    public HopperFreightContainer(StorageMachineInterface machine, FreightCartStation station)
    {
        this.Machine = machine;
        this.Station = station;
    }

    public List<FreightListing> FreightOfferings
    {
        get
        {
            List<FreightListing> offers = new List<FreightListing>(1);
            if (Station.OfferAll)
                Machine.IterateContents(HopperIterator, offers);
            else if (OfferItem != null)
            {
                int count = GetItemCountFromItemBase(OfferItem);
                if (count > OfferLimit)
                    offers.Add(new FreightListing(OfferItem, count - OfferLimit));
                else
                    offers.Add(new FreightListing(OfferItem, 0)); // Add listing of zero to register in system monitor
            }

            return offers;
        }
    }

    bool HopperIterator(ItemBase item, object offers)
    {
        List<FreightListing> offerslist = (List<FreightListing>)offers;
        if (offerslist == null)
            return false;

        bool matchfound = false;
        foreach (FreightListing entry in offers as List<FreightListing>)
        {
            if (entry.Equals(item))
            {
                entry.Quantity += item.GetAmount();
                matchfound = true;
                break;
            }
        }
        if (!matchfound)
            (offers as List<FreightListing>).Add(new FreightListing(item, item.GetAmount()));

        return true;
    }

    public List<FreightListing> FreightRequests
    {
        get
        {
            List<FreightListing> requests = new List<FreightListing>(1);
            if (RequestItem != null)
            {
                int count = GetItemCountFromItemBase(RequestItem);
                if (count < RequestLimit)
                    requests.Add(new FreightListing(RequestItem, RequestLimit - count));
                else
                    requests.Add(new FreightListing(RequestItem, 0));
            }

            return requests;
        }
    }

    private int GetItemCountFromItemBase(ItemBase item)
    {
        if (item.mnItemID == -1)
        {
            ItemCubeStack cubes = item as ItemCubeStack;
            return Machine.CountCubes(cubes.mCubeType, cubes.mCubeValue);
        }
        else
        {
            return Machine.CountItems(item.mnItemID);
        }
    }

    public bool ProvideFreight(ItemBase item)
    {
        if (item.mType == ItemType.ItemCubeStack)
        {
            ItemCubeStack stack = (ItemCubeStack)item;
            return Machine.TryExtractItemsOrCubes(Station, item.mnItemID, stack.mCubeType, stack.mCubeValue, item.GetAmount());
        }
        else
            return Machine.TryExtractItems(null, item.mnItemID, item.GetAmount());
    }

    public bool ReceiveFreight(ItemBase item)
    {
        return Machine.TryInsert(Station, item);
    }

    public bool FreightDataRequest
    {
        get
        {
            return false;
        }
    }

    public void FreightNetworkData(List<FreightData> data)
    {
        return;
    }
}

