using UnityEngine;

public class FreightRegistry
{
    public string NetworkID;
    public RegistryType DataType;
    public MassStorageCrate MassStorage;
    public ItemBase FreightItem;
    public int LowStock;
    public int HighStock;
    public int Inventory;
    public int Deficit;
    public int Surplus;
    public int Stock;


    public FreightRegistry(string networkID, MassStorageCrate crate, ItemBase item, int lowstock, int highstock, RegistryType datatype)
    {
        this.NetworkID = networkID;
        this.MassStorage = crate;
        this.FreightItem = item;
        this.LowStock = lowstock;
        this.HighStock = highstock;
        this.DataType = datatype;
    }

    public void RecalculateParams()
    {
        this.Deficit = this.LowStock > this.Inventory ? this.LowStock - this.Inventory : 0;
        this.Surplus = this.Inventory > this.HighStock ? this.Inventory - this.HighStock : 0;
        //Debug.Log("Recalculating deficit at: " + this.Deficit + ", Surplus as: " + this.Surplus + ", with Inventory: " + this.Inventory + ", at lowstock of: " + this.LowStock + ", and highstock of: " + this.HighStock);
    }

    public enum RegistryType
    {
        Registry,
        NetworkData,
        InterfaceData
    }
}

