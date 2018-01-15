using System.Collections.Generic;
using FortressCraft.Community.Utilities;
using System;

public interface FreightSystemInterface
{
    /// <summary>
    /// List of all items that the interface has available to provide to the freight system
    /// </summary>
    List<FreightListing> FreightOfferings
    {
        get;
    }

    /// <summary>
    /// List of all items that the interface wants delivered to it by the freight system
    /// </summary>
    List<FreightListing> FreightRequests
    {
        get;
    }

    /// <summary>
    /// Checked each tick by the connected freight station.  If true the station will query
    /// the master freight registry for the freight data associated with the stations network.
    /// This request is costly, avoid making frequent requests!  The registry fully updates 
    /// every 3 seconds so requests more frequent than this are unnecessary.  Cache locally!
    /// </summary>
    bool FreightDataRequest
    {
        get;
    }

    /// <summary>
    /// Queried freight network data is passed to the interface here
    /// </summary>
    /// <param name="data">Current freight status for the connected station's network.</param>
    void FreightNetworkData(List<FreightData> data);

    /// <summary>
    /// The freight system calls this when it has an item to offer to the interface 
    /// </summary>
    /// <param name="item">The item being offered to the interface</param>
    /// <returns>True if the item is accepted</returns>
    bool ReceiveFreight(ItemBase item);

    /// <summary>
    /// The freight system calls this when requesting an item from the interface
    /// </summary>
    /// <param name="item">The item the freight system wants the interface to provide</param>
    /// <returns>True if the interface successfully provides the item</returns>
    bool ProvideFreight(ItemBase item);
}

/// <summary>
/// Class representing a freight item and offered/requested quantity
/// </summary>
public class FreightListing
{
    public ItemBase Item { get; }
    public int Quantity { get; set; }

    public FreightListing(ItemBase item, int quantity)
    {
        Item = item;
        Quantity = quantity;
    }

    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;
        Type type = obj.GetType();
        if (type == typeof(FreightListing))
            return Item.Compare((obj as FreightListing).Item);
        else if (type == typeof(ItemBase))
            return Item.Compare(obj as ItemBase);
        else
            return false;
    }

    public static bool operator ==(FreightListing a, FreightListing b)
    {
        if ((object)a != null && (object)b != null)
            return a.Item.Compare(b.Item);
        if ((object)a == null && (object)b == null)
            return true;
        return false;
    }

    public static bool operator !=(FreightListing a, FreightListing b)
    {
        if ((object)a != null && (object)b != null)
            return !(a.Item.Compare(b.Item));
        if (((object)a != null && (object)b == null) || ((object)a == null && (object)b != null))
            return true;

        return false;
    }

    public static explicit operator ItemBase(FreightListing freight)
    {
        return freight.Item;
    }

    public override int GetHashCode()
    {
        return Item.GetHashCode();
    }
}

/// <summary>
/// Class containing basic freight system status information
/// </summary>
public class FreightData
{
    /// <summary>
    /// The Freight Item traded on the network
    /// </summary>
    public ItemBase FreightItem { get; }

    /// <summary>
    /// Total quantity of this freight item among all mass storage with networked stations
    /// Hoppers do not contribute to network inventory.
    /// </summary>
    public int Inventory { get; }

    /// <summary>
    /// The sum of all demand across the network for this item
    /// </summary>
    public int Deficit { get; }

    /// <summary>
    /// The sum of all excess offered available for delivery to requesting stations
    /// </summary>
    public int Surplus { get; }

    /// <summary>
    /// Current stock of this item in transit in the freight system
    /// </summary>
    public int Stock { get; }

    public FreightData(ItemBase item, int inv, int def, int sur, int stock)
    {
        FreightItem = item;
        Inventory = inv;
        Deficit = def;
        Surplus = sur;
        Stock = stock;
    }
}

