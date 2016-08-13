using UnityEngine;
using System.Collections.Generic;
using FortressCraft.Community.Utilities;
using System.Linq;

public class FreightCartWindow : BaseMachineWindow
{
    public const string InterfaceName = "FreightCartStation";

    private static bool dirty;
    private bool ChooseLowStock = false;
    private bool ItemSearchWindow = false;
    private List<ItemBase> SearchResults;
    private string EntryString;
    private int Counter;
    private bool SetNetworkID = false;

    public override void SpawnWindow(SegmentEntity targetEntity)
    {
        FreightCartStation station = targetEntity as FreightCartStation;
        //Catch for when the window is called on an inappropriate machine
        if (station == null)
        {
            GenericMachinePanelScript.instance.Hide();
            UIManager.RemoveUIRules("Machine");
            return;
        }
        UIUtil.UIdelay = 0;
        UIUtil.UILock = true;


        if (!ItemSearchWindow && !SetNetworkID && !string.IsNullOrEmpty(station.NetworkID))
        {
            this.manager.SetTitle("Freight Cart Station - Register Freight");
            this.manager.AddButton("namenetwork", "Change Network", 15, 0);
            this.manager.AddBigLabel("networkid", station.NetworkID, Color.white, 165, 0);
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
        else if (SetNetworkID || string.IsNullOrEmpty(station.NetworkID))
        {
            this.manager.SetTitle("Freight Cart Station - Set Network");
            UIManager.mbEditingTextField = true;
            UIManager.AddUIRules("TextEntry", UIRules.RestrictMovement | UIRules.RestrictLooking | UIRules.RestrictBuilding | UIRules.RestrictInteracting | UIRules.SetUIUpdateRate);
            GenericMachinePanelScript.instance.Scroll_Bar.GetComponent<UIScrollBar>().scrollValue = 0.0f;

            this.manager.AddButton("networkidcancel", "Cancel", 100, 0);
            this.manager.AddBigLabel("networktitle", "Enter Network ID", Color.white, 50, 40);
            this.manager.AddBigLabel("networkentry", "_", Color.cyan, 50, 65);
        }
        dirty = true;
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
        UIUtil.UIdelay = 0;

        if (!ItemSearchWindow && !SetNetworkID && station.NetworkID != null)
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
                                    this.SearchResults.Add(ItemManager.SpawnCubeStack(TerrainData.mEntries[n].CubeType, 0, 1));
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
        else if (SetNetworkID || string.IsNullOrEmpty(station.NetworkID))
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
                    string oldid = station.NetworkID;
                    if (!string.IsNullOrEmpty(this.EntryString))
                    {
                        if (station.massStorageCrate != null)
                            FreightCartManager.instance.CopyFreightEntries(oldid, station.massStorageCrate);
                        if (!string.IsNullOrEmpty(oldid))
                            FreightCartManager.instance.RemoveStationReg(station);
                        station.NetworkID = this.EntryString;
                        FreightCartManager.instance.TryRegisterStation(station);
                        if (station.massStorageCrate != null)
                            FreightCartManager.instance.PasteFreightEntries(station.NetworkID, station.massStorageCrate);
                    }
                    FreightCartManager.instance.AddNetwork(station.NetworkID);
                    this.SetNetworkID = false;
                    this.EntryString = "";

                    UIManager.mbEditingTextField = false;
                    UIManager.RemoveUIRules("TextEntry");

                    this.manager.RedrawWindow();
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
            {
                //clear registry
                FreightCartManager.instance.RemoveRegistry(station.NetworkID, station.massStorageCrate, registries[slotNum].FreightItem);
                this.manager.RedrawWindow();
            }

            return true;
        }
        else if (name.Contains("switchlowstock"))
        {
            this.ChooseLowStock = true;
            this.manager.RedrawWindow();
        }
        else if (name.Contains("switchhighstock"))
        {
            this.ChooseLowStock = false;
            this.manager.RedrawWindow();
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
                    FreightCartManager.instance.UpdateRegistry(station.NetworkID, station.massStorageCrate, registries[slotNum].FreightItem, stock, registries[slotNum].HighStock);
                    this.manager.UpdateLabel("lowstock" + slotNum, stock.ToString(), Color.white);
                }
                else
                {
                    stock = registries[slotNum].HighStock - amount;
                    if (stock < 0)
                        stock = 0;
                    FreightCartManager.instance.UpdateRegistry(station.NetworkID, station.massStorageCrate, registries[slotNum].FreightItem, registries[slotNum].LowStock, stock);
                    this.manager.UpdateLabel("highstock" + slotNum, stock.ToString(), Color.white);
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
                    FreightCartManager.instance.UpdateRegistry(station.NetworkID, station.massStorageCrate, registries[slotNum].FreightItem, stock, registries[slotNum].HighStock);
                    this.manager.UpdateLabel("lowstock" + slotNum, stock.ToString(), Color.white);
                }
                else
                {
                    stock = registries[slotNum].HighStock + amount;
                    FreightCartManager.instance.UpdateRegistry(station.NetworkID, station.massStorageCrate, registries[slotNum].FreightItem, registries[slotNum].LowStock, stock);
                    this.manager.UpdateLabel("highstock" + slotNum, stock.ToString(), Color.white);
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
            this.manager.RedrawWindow();
        }
        else if (name.Contains("itemicon"))
        {
            int slotNum = -1;
            int.TryParse(name.Replace("itemicon", ""), out slotNum); //Get slot name as number
            if (slotNum > -1)
            {
                FreightCartManager.instance.AddRegistry(station.NetworkID, station.massStorageCrate, this.SearchResults[slotNum], 0, 0);
                this.SearchResults = null;
                this.ItemSearchWindow = false;
                this.EntryString = "";
                this.manager.RedrawWindow();
            }
        }
        else if (name == "namenetwork")
        {
            this.SetNetworkID = true;
            this.Redraw(targetEntity);
            return true;
        }
        else if (name == "networkidcancel")
        {
            this.SetNetworkID = false;
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
                {
                    //Debug.Log("We're trying to drop in item");
                    FreightCartManager.instance.AddRegistry(station.NetworkID, station.massStorageCrate, draggedItem, 0, 0);
                    this.manager.RedrawWindow();
                }
            }
        }

        return;
    }

    public override void OnClose(SegmentEntity targetEntity)
    {
        this.SearchResults = null;
        this.ItemSearchWindow = false;
        this.SetNetworkID = false;
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
            if (command == "test")
            {
                // do whatever
            }
        }

        return new NetworkInterfaceResponse
        {
            entity = station,
            inventory = player.mInventory
        };
    }
}

