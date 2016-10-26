using UnityEngine;
using System.Collections.Generic;
using FortressCraft.Community.Utilities;
using System.Linq;

public class FreightCartWindow : BaseMachineWindow
{
    public const string InterfaceName = "steveman0.FreightCartWindow";
    public const string InterfaceAddReg = "AddRegistry";
    public const string InterfaceRemoveReg = "RemoveReg";
    public const string InterfaceSetLowStock = "SetLowStock";
    public const string InterfaceSetHighStock = "SetHighStock";
    public const string InterfaceSetNetwork = "SetNetwork";
    public const string InterfaceSetName = "SetName";
    public const string InterfaceSetInventoryName = "SetInventoryName";
    public const string InterfaceCopyFreight = "CopyFreight";
    public const string InterfacePasteFreight = "PasteFreight";
    public const string InterfaceAssignedCarts = "AssignCart";
    public const string InterfaceToggleLoad = "ToggleLoad";
    public const string InterfaceToggleOffer = "ToggleOffer";
    public const string InterfaceCartTier = "CartTier";


    private static bool dirty;
    private static bool networkredraw;
    private bool ChooseLowStock = false;
    private bool ItemSearchWindow = false;
    private List<ItemBase> SearchResults;
    private string EntryString;
    private int Counter;
    private bool SetNetworkID = false;
    private bool SetName = false;
    private bool SetInventoryName = false;
    private bool SetFreightItems = false;

    public override void SpawnWindow(SegmentEntity targetEntity)
    {
        FreightCartStation station = targetEntity as FreightCartStation;
        //Catch for when the window is called on an inappropriate machine
        if (station == null)
        {
            //GenericMachinePanelScript.instance.Hide();
            UIManager.RemoveUIRules("Machine");
            return;
        }


        if (!SetFreightItems && !ItemSearchWindow && !SetNetworkID && !SetName && !SetInventoryName && !string.IsNullOrEmpty(station.NetworkID))
        {
            this.manager.SetTitle("Freight Cart Station");
            this.manager.AddButton("stationname", "Set Name", 15, 0);
            this.manager.AddBigLabel("namelabel", string.IsNullOrEmpty(station.StationName) ? "UNNAMED" : station.StationName, Color.white, 165, 0);
            this.manager.AddButton("namenetwork", "Change Network", 15, 55);
            this.manager.AddBigLabel("networkid", station.NetworkID, Color.white, 165, 55);
            this.manager.AddButton("namestorage", "Name Storage", 15, 110);
            this.manager.AddBigLabel("storagename", station.ConnectedInventory != null ? station.ConnectedInventory.Name : "Connect Mass Storage", Color.white, 165, 110);
            this.manager.AddButton("setfreight", "Set Freight Goods", 100, 165);
            this.manager.AddBigLabel("assignedcarts", "Assigned Carts: " + station.AssignedCarts.ToString(), Color.white, 15, 215);
            this.manager.AddButton("decreasecarts", "Remove Cart", 25, 260);
            this.manager.AddButton("increasecarts", "Add Cart", 175, 260);
            this.manager.AddButton("toggleload", "Toggle Load", 25, 315);
            this.manager.AddButton("toggleoffer", "Toggle Offers", 25, 370);
            this.manager.AddButton("togglecarttier", "Toggle Tier", 25, 425);
            this.manager.AddBigLabel("loadstatus", "Wait for " + (station.mbWaitForFullLoad ? "Full" : "Any"), Color.white, 165, 315);
            this.manager.AddBigLabel("offerlabel", (station.OfferAll ? "Offer All" : "Use Offer List"), Color.white, 165, 370);
            this.manager.AddBigLabel("carttier", "Cart Tier: " + station.CartTierLabel(), Color.white, 165, 425);

        }
        else if (SetFreightItems && !ItemSearchWindow && !SetNetworkID && !SetName && !SetInventoryName && !string.IsNullOrEmpty(station.NetworkID))
        {
            this.manager.SetTitle("Freight Cart Station - Register Freight");
            this.manager.AddButton("freightdone", "Done", 100, 0);
            this.manager.AddTabButton("switchlowstock", "Edit Requests", !this.ChooseLowStock, 25, 50);
            this.manager.AddTabButton("switchhighstock", "Edit Offers", this.ChooseLowStock, 175, 50);

            int spacing = 175;
            int count = 0;
            int offset = 50 + 50;
            if (station.massStorageCrate != null)
                count = FreightCartManager.instance.GetFreightEntries(station.NetworkID, station.massStorageCrate).Count;
            for (int n = 0; n < count + 1; n++)
            {
                int suffix = n;
                if (n == count)
                    suffix = -1;
                this.manager.AddIcon("registry" + suffix, "empty", Color.white, 0, offset + (spacing * n));
                this.manager.AddBigLabel("registrytitle" + suffix, "Add New Freight", Color.white, 60, offset + (spacing * n));
                if (suffix != -1)
                {
                    this.manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth, "lowstocktitle" + n, "Request if below", this.ChooseLowStock == true ? Color.white : Color.gray, false, 0, offset + (spacing * n + 40));
                    this.manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth, "highstocktitle" + n, "Offer if above", this.ChooseLowStock == false ? Color.white : Color.gray, false, 150, offset + (spacing * n + 40));
                    this.manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth, "lowstock" + n, "Request if below", this.ChooseLowStock == true ? Color.white : Color.gray, false, 0, offset + (spacing * n + 60));
                    this.manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth, "highstock" + n, "Offer if above", this.ChooseLowStock == false ? Color.white : Color.gray, false, 150, offset + (spacing * n + 60));
                    this.manager.AddButton("decreasestock" + n, "Decrease Stock", 25, offset + (spacing * n + 100));
                    this.manager.AddButton("increasestock" + n, "Increase Stock", 175, offset + (spacing * n + 100));
                }
            }
        }
        else if (ItemSearchWindow)
        {
            this.manager.SetTitle("Freight Cart Station - Item Search");
            this.manager.AddButton("searchcancel", "Cancel", 100, 0);
            this.manager.AddBigLabel("searchtitle", "Enter Item Search Term", Color.white, 50, 40);
            this.manager.AddBigLabel("searchtext", "_", Color.cyan, 50, 65);
            if (this.SearchResults != null)
            {
                int count = this.SearchResults.Count;
                int spacing = 60; //Spacing between each registry line
                int yoffset = 100; //Offset below button row
                int labeloffset = 60; //x offset for label from icon

                for (int n = 0; n < count; n++)
                {
                    this.manager.AddIcon("itemicon" + n, "empty", Color.white, 0, yoffset + (spacing * n));
                    this.manager.AddBigLabel("iteminfo" + n, "Inventory Item", Color.white, labeloffset, yoffset + (spacing * n));
                }
            }
        }
        else if (SetNetworkID || string.IsNullOrEmpty(station.NetworkID) || SetName || SetInventoryName)
        {
            if (SetName)
            {
                this.manager.SetTitle("Freight Cart Station - Set Name");
                this.manager.AddBigLabel("networktitle", "Enter Station Name", Color.white, 50, 40);
            }
            else if (SetInventoryName)
            {
                this.manager.SetTitle("Set Mass Storage Name");
                this.manager.AddBigLabel("networktitle", "Enter Storage Name", Color.white, 50, 40);
            }
            else
            {
                this.manager.SetTitle("Freight Cart Station - Set Network");
                this.manager.AddBigLabel("networktitle", "Enter Network ID", Color.white, 50, 40);
            }
            UIManager.mbEditingTextField = true;
            UIManager.AddUIRules("TextEntry", UIRules.RestrictMovement | UIRules.RestrictLooking | UIRules.RestrictBuilding | UIRules.RestrictInteracting | UIRules.SetUIUpdateRate);
            GenericMachinePanelScript.instance.Scroll_Bar.GetComponent<UIScrollBar>().scrollValue = 0.0f;

            this.manager.AddButton("networkidcancel", "Cancel", 100, 0);
            this.manager.AddBigLabel("networkentry", "_", Color.cyan, 50, 65);
        }
        dirty = true;
        networkredraw = false;
    }

    public override void UpdateMachine(SegmentEntity targetEntity)
    {
        FreightCartStation station = targetEntity as FreightCartStation;
        //Catch for when the window is called on an inappropriate machine
        if (station == null)
        {
            GenericMachinePanelScript.instance.Hide();
            UIManager.RemoveUIRules("Machine");
            return;
        }

        GenericMachinePanelScript.instance.Scroll_Bar.GetComponent<UIScrollBar>().scrollValue -= Input.GetAxis("Mouse ScrollWheel");


        if (networkredraw)
            this.manager.RedrawWindow();
        if (!dirty)
            return;

        if (!SetFreightItems && !ItemSearchWindow && !SetNetworkID && !SetName && !SetInventoryName && !string.IsNullOrEmpty(station.NetworkID))
        {
            this.manager.UpdateLabel("assignedcarts", "Assigned Carts: " + station.AssignedCarts.ToString(), Color.white);
            this.manager.UpdateLabel("loadstatus", "Wait for " + (station.mbWaitForFullLoad ? "Full" : "Any"), Color.white);
            this.manager.UpdateLabel("offerlabel", (station.OfferAll ? "Offer All" : "Use Offer List"), Color.white);
            this.manager.UpdateLabel("carttier", "Cart Tier: " + station.CartTierLabel(), Color.white);
        }
        else if (SetFreightItems && !ItemSearchWindow && !SetNetworkID && !SetName && !SetInventoryName && station.NetworkID != null)
        {
            List<FreightRegistry> registries = new List<FreightRegistry>();
            if (station.massStorageCrate != null)
                registries = FreightCartManager.instance.GetFreightEntries(station.NetworkID, station.massStorageCrate);
            else
            {
                this.manager.UpdateLabel("registrytitle-1", "Connect to Mass Storage", Color.red);
                return;
            }

            for (int index = 0; index < registries.Count; index++)
            {
                ItemBase item = registries[index].FreightItem;
                int lowstock = registries[index].LowStock;
                int highstock = registries[index].HighStock;

                string itemname = ItemManager.GetItemName(item);
                string iconname = ItemManager.GetItemIcon(item);

                this.manager.UpdateIcon("registry" + index, iconname, Color.white);
                this.manager.UpdateLabel("registrytitle" + index, itemname, Color.white);
                this.manager.UpdateLabel("lowstock" + index, registries[index].LowStock.ToString(), this.ChooseLowStock == true ? Color.white : Color.gray);
                this.manager.UpdateLabel("highstock" + index, registries[index].HighStock.ToString(), this.ChooseLowStock == false ? Color.white : Color.gray);
                this.manager.UpdateLabel("lowstocktitle" + index, "Request if below", this.ChooseLowStock == true ? Color.white : Color.gray);
                this.manager.UpdateLabel("highstocktitle" + index, "Offer if above", this.ChooseLowStock == false ? Color.white : Color.gray);
            }
        }
        else if (ItemSearchWindow)
        {
            if (this.SearchResults == null)
            {
                this.Counter++;
                foreach (char c in Input.inputString)
                {
                    if (c == "\b"[0])  //Backspace
                    {
                        if (this.EntryString.Length != 0)
                            this.EntryString = this.EntryString.Substring(0, this.EntryString.Length - 1);
                    }
                    else if (c == "\n"[0] || c == "\r"[0]) //Enter or Return
                    {
                        this.SearchResults = new List<ItemBase>();

                        for (int n = 0; n < ItemEntry.mEntries.Length; n++)
                        {
                            if (ItemEntry.mEntries[n] == null) continue;
                            if (ItemEntry.mEntries[n].Name.ToLower().Contains(this.EntryString.ToLower()))
                                this.SearchResults.Add(ItemManager.SpawnItem(ItemEntry.mEntries[n].ItemID));
                        }
                        for (int n = 0; n < TerrainData.mEntries.Length; n++)
                        {
                            bool foundvalue = false;
                            if (TerrainData.mEntries[n] == null) continue;
                            if (TerrainData.mEntries[n].Name.ToLower().Contains(this.EntryString.ToLower()))
                            {
                                int count = TerrainData.mEntries[n].Values.Count;
                                for (int m = 0; m < count; m++)
                                {
                                    if (TerrainData.mEntries[n].Values[m].Name.ToLower().Contains(this.EntryString.ToLower()))
                                    {
                                        this.SearchResults.Add(ItemManager.SpawnCubeStack(TerrainData.mEntries[n].CubeType, TerrainData.mEntries[n].Values[m].Value, 1));
                                        foundvalue = true;
                                    }
                                }
                                if (!foundvalue)
                                    this.SearchResults.Add(ItemManager.SpawnCubeStack(TerrainData.mEntries[n].CubeType, TerrainData.mEntries[n].DefaultValue, 1));
                            }
                            if ((this.EntryString.ToLower().Contains("component") || this.EntryString.ToLower().Contains("placement") || this.EntryString.ToLower().Contains("multi")) && TerrainData.mEntries[n].CubeType == 600)
                            {
                                int count = TerrainData.mEntries[n].Values.Count;
                                for (int m = 0; m < count; m++)
                                {
                                    this.SearchResults.Add(ItemManager.SpawnCubeStack(600, TerrainData.mEntries[n].Values[m].Value, 1));
                                }
                            }
                        }
                        if (this.SearchResults.Count == 0)
                            this.SearchResults = null;

                        UIManager.mbEditingTextField = false;
                        UIManager.RemoveUIRules("TextEntry");

                        this.manager.RedrawWindow();
                        return;
                    }
                    else
                        this.EntryString += c;
                }
                this.manager.UpdateLabel("searchtext", this.EntryString + (this.Counter % 20 > 10 ? "_" : ""), Color.cyan);
                dirty = true;
                return;
            }
            else
            {
                this.manager.UpdateLabel("searchtitle", "Searching for:", Color.white);
                this.manager.UpdateLabel("searchtext", this.EntryString, Color.cyan);
                int count = this.SearchResults.Count;
                for (int n = 0; n < count; n++)
                {
                    ItemBase item = this.SearchResults[n];
                    string itemname = ItemManager.GetItemName(item);
                    string iconname = ItemManager.GetItemIcon(item);

                    this.manager.UpdateIcon("itemicon" + n, iconname, Color.white);
                    this.manager.UpdateLabel("iteminfo" + n, itemname, Color.white);
                }
            }
        }
        else if (SetNetworkID || string.IsNullOrEmpty(station.NetworkID) || SetName || SetInventoryName)
        {
            this.Counter++;
            foreach (char c in Input.inputString)
            {
                if (c == "\b"[0])  //Backspace
                {
                    if (this.EntryString.Length != 0)
                        this.EntryString = this.EntryString.Substring(0, this.EntryString.Length - 1);
                }
                else if (c == "\n"[0] || c == "\r"[0]) //Enter or Return
                {
                    if (SetName)
                    {
                        FreightCartWindow.SetStationName(station, this.EntryString);
                        this.SetName = false;
                    }
                    else if (SetInventoryName)
                    {
                        FreightCartWindow.NameInventory(station, this.EntryString);
                        this.SetInventoryName = false;
                    }
                    else
                    {
                        FreightCartWindow.SetNetwork(station, this.EntryString);
                        this.SetNetworkID = false;
                    }
                    this.EntryString = "";
                    UIManager.mbEditingTextField = false;
                    UIManager.RemoveUIRules("TextEntry");
                    return;
                }
                else
                    this.EntryString += c;
            }
            this.manager.UpdateLabel("networkentry", this.EntryString + (this.Counter % 20 > 10 ? "_" : ""), Color.cyan);
            dirty = true;
            return;
        }
    dirty = false;
    }

    public override bool ButtonRightClicked(string name, SegmentEntity targetEntity)
    {
        if (name.Contains("registry" + -1))
        {
            this.ItemSearchWindow = true;
            UIManager.mbEditingTextField = true;
            UIManager.AddUIRules("TextEntry", UIRules.RestrictMovement | UIRules.RestrictLooking | UIRules.RestrictBuilding | UIRules.RestrictInteracting | UIRules.SetUIUpdateRate);
            this.Redraw(targetEntity);
            GenericMachinePanelScript.instance.Scroll_Bar.GetComponent<UIScrollBar>().scrollValue = 0.0f;
            return true;
        }
        else
            return base.ButtonRightClicked(name, targetEntity);
    }

    public override bool ButtonClicked(string name, SegmentEntity targetEntity)
    {
        FreightCartStation station = targetEntity as FreightCartStation;

        if (name.Contains("registry")) // drag drop to a slot
        {
            int slotNum = -1;
            int.TryParse(name.Replace("registry", ""), out slotNum); //Get slot name as number
            List<FreightRegistry> registries = FreightCartManager.instance.GetFreightEntries(station.NetworkID, station.massStorageCrate);

            if (slotNum > -1) // valid slot
                FreightCartWindow.RemoveRegistry(station, registries[slotNum].FreightItem);
            return true;
        }
        else if (name.Contains("switchlowstock"))
        {
            this.ChooseLowStock = true;
            this.manager.RedrawWindow();
            return true;
        }
        else if (name.Contains("switchhighstock"))
        {
            this.ChooseLowStock = false;
            this.manager.RedrawWindow();
            return true;
        }
        else if (name.Contains("decreasestock"))
        {
            int slotNum = -1;
            int.TryParse(name.Replace("decreasestock", ""), out slotNum); //Get slot name as number
            List<FreightRegistry> registries = FreightCartManager.instance.GetFreightEntries(station.NetworkID, station.massStorageCrate);

            if (slotNum > -1) // valid slot
            {
                int amount = 100;
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    amount = 10;
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    amount = 1;
                if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    amount = 1000;

                int stock;
                if (this.ChooseLowStock)
                {
                    stock = registries[slotNum].LowStock - amount;
                    if (stock < 0)
                        stock = 0;
                    FreightCartWindow.SetLowStock(station, registries[slotNum].FreightItem, stock);
                    return true;
                }
                else
                {
                    stock = registries[slotNum].HighStock - amount;
                    if (stock < 0)
                        stock = 0;
                    FreightCartWindow.SetHighStock(station, registries[slotNum].FreightItem, stock);
                    return true;
                }
            }
        }
        else if (name.Contains("increasestock"))
        {
            int slotNum = -1;
            int.TryParse(name.Replace("increasestock", ""), out slotNum); //Get slot name as number
            List<FreightRegistry> registries = FreightCartManager.instance.GetFreightEntries(station.NetworkID, station.massStorageCrate);

            if (slotNum > -1) // valid slot
            {
                int amount = 100;
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    amount = 10;
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    amount = 1;
                if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    amount = 1000;

                int stock;
                if (this.ChooseLowStock)
                {
                    stock = registries[slotNum].LowStock + amount;
                    FreightCartWindow.SetLowStock(station, registries[slotNum].FreightItem, stock);
                    return true;
                }
                else
                {
                    stock = registries[slotNum].HighStock + amount;
                    FreightCartWindow.SetHighStock(station, registries[slotNum].FreightItem, stock);
                    return true;
                }
            }
        }
        else if (name.Contains("searchcancel"))
        {
            this.ItemSearchWindow = false;
            this.SearchResults = null;
            UIManager.mbEditingTextField = false;
            UIManager.RemoveUIRules("TextEntry");
            this.EntryString = "";
            GenericMachinePanelScript.instance.Scroll_Bar.GetComponent<UIScrollBar>().scrollValue = 0.0f;
            this.manager.RedrawWindow();
            return true;
        }
        else if (name.Contains("itemicon"))
        {
            int slotNum = -1;
            int.TryParse(name.Replace("itemicon", ""), out slotNum); //Get slot name as number
            if (slotNum > -1)
            {
                FreightCartWindow.AddRegistry(station, this.SearchResults[slotNum]);
                this.SearchResults = null;
                this.ItemSearchWindow = false;
                this.EntryString = "";
                GenericMachinePanelScript.instance.Scroll_Bar.GetComponent<UIScrollBar>().scrollValue = 0.0f;
                return true;
            }
        }
        else if (name == "namenetwork")
        {
            this.SetNetworkID = true;
            this.Redraw(targetEntity);
            return true;
        }
        else if (name == "stationname")
        {
            this.SetName = true;
            this.Redraw(targetEntity);
            return true;
        }
        else if (name == "namestorage")
        {
            this.SetInventoryName = true;
            this.Redraw(targetEntity);
            return true;
        }
        else if (name == "networkidcancel")
        {
            this.SetNetworkID = false;
            this.SetName = false;
            this.SetInventoryName = false;
            UIManager.mbEditingTextField = false;
            UIManager.RemoveUIRules("TextEntry");
            this.EntryString = "";
            if (string.IsNullOrEmpty(station.NetworkID))
            {
                GenericMachinePanelScript.instance.Hide();
                return true;
            }
            this.manager.RedrawWindow();
        }
        else if (name == "setfreight")
        {
            this.SetFreightItems = true;
            this.manager.RedrawWindow();
            return true;
        }
        else if (name == "freightdone")
        {
            this.SetFreightItems = false;
            this.manager.RedrawWindow();
            return true;
        }
        else if (name == "increasecarts")
        {
            int amount = 1;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                amount = 10;
            FreightCartWindow.SetCartAssignment(station, station.AssignedCarts + amount);
            return true;
        }
        else if (name == "decreasecarts")
        {
            int amount = 1;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                amount = 10;
            FreightCartWindow.SetCartAssignment(station, station.AssignedCarts - amount < 0 ? 0 : station.AssignedCarts - amount);
            return true;
        }
        else if (name == "toggleload")
        {
            FreightCartWindow.ToggleLoadStatus(station, !station.mbWaitForFullLoad ? "Full" : "Any");
            return true;
        }
        else if (name == "toggleoffer")
        {
            FreightCartWindow.ToggleOfferAll(station, !station.OfferAll ? "All" : "registry");
            return true;
        }
        else if (name == "togglecarttier")
        {
            FreightCartWindow.ToggleCartTier(station, station.CartTier);
            return true;
        }

        return false;
    }

    public override void HandleItemDrag(string name, ItemBase draggedItem, DragAndDropManager.DragRemoveItem dragDelegate, SegmentEntity targetEntity)
    {
        FreightCartStation station = targetEntity as FreightCartStation;
        if (station.massStorageCrate == null)
            return;

        if (name.Contains("registry")) // drag drop to a slot
        {
            int slotNum = -1;
            int.TryParse(name.Replace("registry", ""), out slotNum); //Get slot name as number

            if (slotNum == -1) // valid slot
            {
                if (this.manager.mWindowLookup[name + "_icon"].GetComponent<UISprite>().spriteName == "empty")
                    FreightCartWindow.AddRegistry(station, draggedItem);
            }
        }

        return;
    }

    public static void RemoveRegistry(FreightCartStation station, ItemBase item)
    {
        if (item == null)
            Debug.LogWarning("Freight cart window trying to remove registry for null item!");
        FreightCartManager.instance.RemoveRegistry(station.NetworkID, station.massStorageCrate, item);
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfaceRemoveReg, null, item, station, 0f);
        FreightCartWindow.networkredraw = true;
        station.MarkDirtyDelayed();
    }

    public static void SetLowStock(FreightCartStation station, ItemBase item, int stock)
    {
        if (item == null)
            Debug.LogWarning("Freight cart window trying to set low stock for null item!");
        FreightRegistry reg = FreightCartManager.instance.MasterRegistry.First(x => x.NetworkID == station.NetworkID && x.MassStorage == station.massStorageCrate && x.FreightItem.Compare(item));
        int highstock = -1;
        if (reg != null)
            highstock = reg.HighStock;
        FreightCartManager.instance.UpdateRegistry(station.NetworkID, station.massStorageCrate, item, stock, stock > highstock ? stock : -1);
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfaceSetLowStock, stock.ToString(), item, station, 0f);
        FreightCartWindow.dirty = true;
        station.MarkDirtyDelayed();
    }

    public static void SetHighStock(FreightCartStation station, ItemBase item, int stock)
    {
        if (item == null)
            Debug.LogWarning("Freight cart window trying to set high stock for null item!");
        FreightCartManager.instance.UpdateRegistry(station.NetworkID, station.massStorageCrate, item, -1, stock);
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfaceSetHighStock, stock.ToString(), item, station, 0f);
        FreightCartWindow.dirty = true;
        station.MarkDirtyDelayed();
    }

    public static void SetCartAssignment(FreightCartStation station, int assignedcarts)
    {
        station.AssignedCarts = assignedcarts;
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfaceAssignedCarts, assignedcarts.ToString(), null, station, 0f);
        FreightCartWindow.dirty = true;
        station.MarkDirtyDelayed();
    }

    public static void ToggleLoadStatus(FreightCartStation station, string loadwhenfull)
    {
        station.mbWaitForFullLoad = loadwhenfull == "Full" ? true : false;
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfaceToggleLoad, loadwhenfull, null, station, 0f);
        FreightCartWindow.dirty = true;
        station.MarkDirtyDelayed();
    }

    public static void ToggleOfferAll(FreightCartStation station, string offerall)
    {
        station.OfferAll = offerall == "All" ? true : false;
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfaceToggleOffer, offerall, null, station, 0f);
        FreightCartWindow.dirty = true;
        station.MarkDirtyDelayed();
    }

    public static void ToggleCartTier(FreightCartStation station, int carttier)
    {
        station.CartTier = carttier + 1 <= 2 ? carttier + 1 : 0;
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfaceCartTier, carttier.ToString(), null, station, 0f);
        FreightCartWindow.dirty = true;
        station.MarkDirtyDelayed();
    }

    public static void AddRegistry(FreightCartStation station, ItemBase item)
    {
        if (item == null)
            Debug.LogWarning("Freight cart window trying to add registry for null item!");
        FreightCartManager.instance.AddRegistry(station.NetworkID, station.massStorageCrate, item, 0, 0);
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfaceAddReg, null, item, station, 0f);
        FreightCartWindow.networkredraw = true;
        station.MarkDirtyDelayed();
    }

    public static void SetNetwork(FreightCartStation station, string networkid)
    {
        string oldid = station.NetworkID;
        if (!string.IsNullOrEmpty(networkid))
        {
            if (!string.IsNullOrEmpty(oldid))
                FreightCartManager.instance.RemoveStationReg(station);
            station.NetworkID = networkid;
            FreightCartManager.instance.TryRegisterStation(station);
        }
        FreightCartManager.instance.AddNetwork(station.NetworkID);
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfaceSetNetwork, networkid, null, station, 0f);
        FreightCartWindow.networkredraw = true;
        station.MarkDirtyDelayed();
    }

    public static void SetStationName(FreightCartStation station, string stationname)
    {
        station.StationName = stationname;
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfaceSetName, stationname, null, station, 0f);
        FreightCartWindow.networkredraw = true;
        station.MarkDirtyDelayed();
    }

    public static void NameInventory(FreightCartStation station, string inventoryname)
    {
        station.ConnectedInventory.Name = inventoryname;
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfaceSetInventoryName, inventoryname, null, station, 0f);
        FreightCartWindow.networkredraw = true;
        station.MarkDirtyDelayed();
    }

    //Copy and paste is currently global for all players - one players copy will overwrite the last
    //This may lead to unintuitive results where two players try to copy and paste freight entries
    //In the future I could build a dictionary in FreightCartManager to store the copied freight for each player so they have their own instance that is also network sync'd

    public static void CopyFreight(FreightCartStation source)
    {
        FreightCartManager.instance.CopyFreightEntries(source);
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfaceCopyFreight, null, null, source, 0f);
        FreightCartWindow.networkredraw = true;
    }

    public static void PasteFreight(FreightCartStation destination)
    {
        FreightCartManager.instance.PasteFreightEntries(destination.NetworkID, destination.massStorageCrate);
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfacePasteFreight, null, null, destination, 0f);
        FreightCartWindow.networkredraw = true;
        destination.MarkDirtyDelayed();
    }

    public override void OnClose(SegmentEntity targetEntity)
    {
        this.SearchResults = null;
        this.ItemSearchWindow = false;
        this.SetNetworkID = false;
        this.SetFreightItems = false;
        this.SetName = false;
        this.SetInventoryName = false;
        this.EntryString = "";
        UIManager.mbEditingTextField = false;
        UIManager.RemoveUIRules("TextEntry");
        base.OnClose(targetEntity);
    }

    public static NetworkInterfaceResponse HandleNetworkCommand(Player player, NetworkInterfaceCommand nic)
    {
        FreightCartStation station = nic.target as FreightCartStation;

        string command = nic.command;
        if (command != null)
        {
            if (command == InterfaceRemoveReg)
                FreightCartWindow.RemoveRegistry(station, nic.itemContext);
            else if (command == InterfaceSetLowStock)
            {
                int stock = -1;
                int.TryParse(nic.payload ?? "-1", out stock);
                FreightCartWindow.SetLowStock(station, nic.itemContext, stock);
            }
            else if (command == InterfaceSetHighStock)
            {
                int stock = -1;
                int.TryParse(nic.payload ?? "-1", out stock);
                FreightCartWindow.SetHighStock(station, nic.itemContext, stock);
            }
            else if (command == InterfaceAssignedCarts)
            {
                int carts = 0;
                int.TryParse(nic.payload ?? "0", out carts);
                FreightCartWindow.SetCartAssignment(station, carts);
            }
            else if (command == InterfaceCartTier)
            {
                int carttier = 0;
                int.TryParse(nic.payload ?? "2", out carttier);
                FreightCartWindow.ToggleCartTier(station, carttier);
            }
            else if (command == InterfaceToggleLoad)
                FreightCartWindow.ToggleLoadStatus(station, nic.payload);
            else if (command == InterfaceToggleOffer)
                FreightCartWindow.ToggleOfferAll(station, nic.payload);
            else if (command == InterfaceAddReg)
                FreightCartWindow.AddRegistry(station, nic.itemContext);
            else if (command == InterfaceSetNetwork)
                FreightCartWindow.SetNetwork(station, nic.payload);
            else if (command == InterfaceSetName)
                FreightCartWindow.SetStationName(station, nic.payload);
            else if (command == InterfaceSetInventoryName)
                FreightCartWindow.NameInventory(station, nic.payload);
            else if (command == InterfaceCopyFreight)
                FreightCartWindow.CopyFreight(station);
            else if (command == InterfacePasteFreight)
                FreightCartWindow.PasteFreight(station);
        }

        return new NetworkInterfaceResponse
        {
            entity = station,
            inventory = player.mInventory
        };
    }
}

