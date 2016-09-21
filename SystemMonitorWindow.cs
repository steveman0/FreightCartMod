using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FortressCraft.Community.Utilities;
using System.Text;

public class SystemMonitorWindow : BaseMachineWindow
{
    public const string InterfaceName = "FreightSystemMonitor";
    private bool dirty;
    private bool DisplayAll = false;
    private bool SelNetwork = false;
    private int SelectedNetwork = 0;
    private List<FreightRegistry> Registries;
    public static FreightCartManager fcm = FreightCartManager.instance;
    private Dictionary<ItemBase, int> FullInventory;
    private int RefreshCounter;  //Use this someday to refresh the stock window?
    private Vector3 StartPos;
    private bool firstopen;
    private bool OrderByName = true;
    private bool CompactLayout = true;

    public override void SpawnWindow(SegmentEntity targetEntity)
    {
        FreightSystemMonitor monitor = targetEntity as FreightSystemMonitor;
        //Catch for when the window is called on an inappropriate machine
        if (monitor == null)
        {
            GenericMachinePanelScript.instance.Hide();
            UIManager.RemoveUIRules("Machine");

            return;
        }
        UIUtil.UIdelay = 0;
        UIUtil.UILock = true;
        Debug.LogWarning("first area test fsm");
        if (fcm == null)
            Debug.LogWarning("Freight cart manager instance is null!!");
        if (fcm != null && this.SelectedNetwork >= fcm.Networks.Count)
            this.SelectedNetwork = 0;
        if (!firstopen)
        {
            this.StartPos = GenericMachinePanelScript.instance.gameObject.transform.localPosition;
            GenericMachinePanelScript.instance.gameObject.transform.localScale = new Vector3(3.0f, 1.0f, 1.0f);
            GenericMachinePanelScript.instance.gameObject.transform.localPosition = this.StartPos + new Vector3(-420f, 0f, 0f);
            GenericMachinePanelScript.instance.Background_Panel.transform.localScale = new Vector3(3.0f, 1.0f, 1.0f);
            GenericMachinePanelScript.instance.Label_Holder.transform.localScale = new Vector3(0.33f, 1.0f, 1.0f);
            GenericMachinePanelScript.instance.Icon_Holder.transform.localScale = new Vector3(0.33f, 1.0f, 1.0f);
            GenericMachinePanelScript.instance.Content_Holder.transform.localScale = new Vector3(0.33f, 1.0f, 1.0f);
            GenericMachinePanelScript.instance.Content_Icon_Holder.transform.localScale = new Vector3(0.33f, 1.0f, 1.0f);
            GenericMachinePanelScript.instance.Scroll_Bar.transform.localScale = new Vector3(0.33f, 1.0f, 1.0f);
            GenericMachinePanelScript.instance.Source_Holder.transform.localScale = new Vector3(0.33f, 1.0f, 1.0f);
            GenericMachinePanelScript.instance.Generic_Machine_Title_Label.transform.localScale = new Vector3(0.33f, 1.0f, 1.0f);

            //Bring labels in front of icons
            float x = GenericMachinePanelScript.instance.Label_Holder.transform.position.x;
            float y = GenericMachinePanelScript.instance.Label_Holder.transform.position.y;
            GenericMachinePanelScript.instance.Label_Holder.transform.position = new Vector3(x, y, 69.3f);


            this.firstopen = true;
        }
        if (!this.DisplayAll && !this.SelNetwork)
        {
            this.manager.SetTitle("Freight System Status");
            int globalxoffset = 75; //For window scaling adjustments
            int buttonoffset = 175;
            int buttonspacing = 175;

            this.manager.AddButton("allnetworks", "Global Inventory", globalxoffset + buttonoffset, 0);
            this.manager.AddButton("prevnetwork", "Previous Network", globalxoffset + buttonoffset + buttonspacing, 0);
            this.manager.AddButton("nextnetwork", "Next Network", globalxoffset + buttonoffset + 2*buttonspacing, 0);
            this.manager.AddButton("selnetwork", "Select Network", globalxoffset + buttonoffset + 3*buttonspacing, 0);

            int spacing = 60; //Spacing between each registry line
            int count = 0;
            int yoffset = 100; //Offset below button row
            int col2xoffset = 350 + globalxoffset; //Col 2 offset from the left
            int col3xoffset = 125 + col2xoffset; //Col 3 offset from the left
            int col4xoffset = 125 + col3xoffset; //Col 3 offset from the left
            int col5xoffset = 125 + col4xoffset; //Col 4 offset from the left

            this.Registries = new List<FreightRegistry>();
            //this.FullInventory = new Dictionary<ItemBase, int>(new ItemBaseComparer());
            if (fcm.Networks != null && fcm.Networks.Count > this.SelectedNetwork)
            {
                this.Registries = fcm.MasterRegistry.FindAll(x => x.NetworkID == fcm.Networks[this.SelectedNetwork] && x.MassStorage == null).ToList();
                count = Registries.Count;
                //Debug.Log("After registeries counting " + count);
                this.manager.AddBigLabel("nameofnetworktitle", "Viewing status of freight network: ", Color.cyan, 350, 50);
                this.manager.AddBigLabel("nameofnetwork", fcm.Networks[this.SelectedNetwork], Color.cyan, 650, 50);
                //Debug.Log("Spawning window with network: " + fcm.Networks[this.SelectedNetwork]);
                for (int n = 0; n < count; n++)
                {
                    //Debug.Log("Catch error at n = " + n);
                    this.manager.AddIcon("item" + n, "empty", Color.white, globalxoffset + 40, yoffset + (spacing * n));
                    this.manager.AddBigLabel("registrytitle" + n, "Registry Item", Color.white, 100 + globalxoffset, yoffset + (spacing * n));
                    this.manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth, "deficittitle" + n, "Network Deficit", Color.white, false, col2xoffset, yoffset + (spacing * n));
                    this.manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth, "surptitle" + n, "Network Surplus", Color.white, false, col3xoffset, yoffset + (spacing * n));
                    this.manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth, "stocktitle" + n, "In Transit", Color.white, false, col4xoffset, yoffset + (spacing * n));
                    this.manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth, "invtitle" + n, "Network Inventory", Color.white, false, col5xoffset, yoffset + (spacing * n));
                    this.manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth, "deficit" + n, "0", Color.white, false, col2xoffset, yoffset + (spacing * n + 20));
                    this.manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth, "surplus" + n, "0", Color.white, false, col3xoffset, yoffset + (spacing * n + 20));
                    this.manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth, "stock" + n, "0", Color.white, false, col4xoffset, yoffset + (spacing * n + 20));
                    this.manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth, "totalinv" + n, "0", Color.white, false, col5xoffset, yoffset + (spacing * n + 20));
                }
            }
            else
            {
                this.manager.AddBigLabel("loadingnetworks", "No Freight Networks found...", Color.white, 225, 250);
            }
        }
        else if (this.SelNetwork)
        {
            this.manager.SetTitle("Select Network");
            this.manager.AddButton("cancel", "Cancel", 500, 0);

            int spacing = 50; //Spacing between each registry line
            int yoffset = 65; //Offset below button row

            for (int n = 0; n < fcm.Networks.Count; n++)
            {
                this.manager.AddButton("networknum" + n, fcm.Networks[n], 500, yoffset + (n * spacing));
            }
        }
        else //Display all
        {
            int globalxoffset = 75; //For window scaling adjustments
            int buttonoffset = 175;
            int buttonspacing = 175;

            this.manager.SetTitle("Freight System Status");
            this.manager.AddButton("allnetworks", "Single Network", globalxoffset + buttonoffset, 0);
            this.manager.AddButton("ordername", "Order By Name", globalxoffset + buttonoffset + buttonspacing, 0);
            this.manager.AddButton("ordercount", "Order By Count", globalxoffset + buttonoffset + 2*buttonspacing, 0);
            this.manager.AddButton("togglelayout", "Toggle Layout", globalxoffset + buttonoffset + 3*buttonspacing, 0);

            fcm.CalculateGlobalInventory();
            int count = fcm.GlobalInventory.Count;

            if (CompactLayout)
            {
                int ItemRowSpacing = 60;
                int ItemColSpacing = 60;
                int yoffset = 60;

                for (int n = 0; n < count; n++)
                {
                    int row = n / 15;
                    int col = n % 15;
                    this.manager.AddIcon("itemicon" + n, "empty", Color.white, globalxoffset + col * ItemColSpacing + 35, row * ItemRowSpacing + yoffset);
                    this.manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth, "StackSize" + n, "", Color.white, false, globalxoffset + col * ItemColSpacing + 27 + 25, row * ItemRowSpacing + 22 + yoffset);
                }
            }
            else
            {
                int spacing = 60; //Spacing between each registry line
                int yoffset = 60; //Offset below button row
                int labeloffset = 50 + globalxoffset; //x offset for label from icon
                                                      //int count1offset = 120 + globalxoffset; //Inventory count offset
                int col2xoffset = 300 + labeloffset; //Col 3 offset from the left

                for (int n = 0; n < count; n++)
                {
                    //Debug.Log("Catch error at n = " + n);
                    int height = n / 2; // int division rounds down
                    int shift = n % 2 == 1 ? col2xoffset : 0; // add shift for odd entries
                    int shift2 = n % 2 == 1 ? 50 : 0; // extra shift required for icons due to different scaling
                    this.manager.AddIcon("itemicon" + n, "empty", Color.white, globalxoffset + shift + 35, yoffset + (spacing * height));
                    this.manager.AddBigLabel("iteminfo" + n, "Inventory Item", Color.white, labeloffset + shift + 35, yoffset + (spacing * height));
                }
            }
        }


        this.dirty = true;
    }

    public override void UpdateMachine(SegmentEntity targetEntity)
    {
        FreightSystemMonitor monitor = targetEntity as FreightSystemMonitor;
        //Catch for when the window is called on an inappropriate machine
        if (monitor == null)
        {
            GenericMachinePanelScript.instance.Hide();
            UIManager.RemoveUIRules("Machine");
            return;
        }
        UIUtil.UIdelay = 0;

        if (!this.DisplayAll && !this.SelNetwork)
        {
            if (fcm.Networks != null && fcm.Networks.Count >= this.SelectedNetwork)
            {
                string networkid = fcm.Networks[this.SelectedNetwork];

                int count;
                count = this.Registries.Count;

                for (int n = 0; n < count; n++)
                {
                    ItemBase item = Registries[n].FreightItem;
                    string iconname = ItemManager.GetItemIcon(item);
                    string itemname = ItemManager.GetItemName(item);
                    int def = Registries[n].Deficit;
                    int stock = Registries[n].Stock;
                    int inventory = Registries[n].Inventory;
                    int surplus = Registries[n].Surplus;

                    this.manager.UpdateIcon("item" + n, iconname, Color.white);
                    this.manager.UpdateLabel("registrytitle" + n, itemname, Color.white);
                    this.manager.UpdateLabel("deficit" + n, def.ToString("N0"), def <= surplus ? Color.white : Color.red);
                    this.manager.UpdateLabel("surplus" + n, surplus.ToString("N0"), surplus > def ? Color.green : Color.white);
                    this.manager.UpdateLabel("stock" + n, stock.ToString("N0"), stock < def ? Color.red : Color.green);
                    this.manager.UpdateLabel("totalinv" + n, inventory.ToString("N0"), Color.white);
                }
            }
        }
        else if (this.DisplayAll)
        {
            int count = fcm.GlobalInventory.Count;
            if (this.OrderByName)
                fcm.OrderyInvByName();
            else
                fcm.OrderInvByCount();

            for (int n = 0; n < count; n++)
            {
                ItemBase item = fcm.GlobalInventory[n].Key;
                string iconname = ItemManager.GetItemIcon(item);
                string itemname = ItemManager.GetItemName(item);
                int itemcount = fcm.GlobalInventory[n].Value;
                string iteminfo = (itemcount.ToString("N0") + "x " + itemname);
                iteminfo = iteminfo.Substring(0, 35 > iteminfo.Length ? iteminfo.Length : 35);

                this.manager.UpdateIcon("itemicon" + n, iconname, Color.white);
                if (this.CompactLayout)
                    this.manager.UpdateLabel("StackSize" + n, FormatStackText(itemcount), Color.white);
                else
                    this.manager.UpdateLabel("iteminfo" + n, iteminfo, Color.white);
            }
        }
    }

    private string FormatStackText(int count)
    {
        string label = "";
        if (count < 10)
            label = "     " + count.ToString();
        else if (count < 100)
            label = "   " + count.ToString();
        else if (count < 1000)
            label = "  " + count.ToString();
        else if (count < 10000)
            label = " " + ((double)count / 1000f).ToString("N1") + "k";
        else if (count < 100000)
            label = ((double)count / 1000f).ToString("N1") + "k";
        else if (count < 1000000)
            label = " "+ (count / 1000).ToString() + "k";
        return label;
    }

    private void CountInventory(List<MassInventory> inv)
    {
        for (int index = 0; index < inv.Count; index++)
        {
            this.FullInventory =    (from e in inv[index].Inventory.Concat(this.FullInventory)
                                    group e by e.Key into g
                                    select new { Name = g.Key, Count = g.Sum(kvp => kvp.Value) })
                                    .ToDictionary(item => item.Name, item => item.Count);
        }
    }


    public override bool ButtonClicked(string name, SegmentEntity targetEntity)
    {

        if (name.Contains("nextnetwork")) // Increment network count
        {
            this.SelectedNetwork++;
            if (this.SelectedNetwork >= fcm.Networks.Count)
                this.SelectedNetwork = 0;
            this.manager.RedrawWindow();
            return true;
        }
        if (name.Contains("prevnetwork")) // Decrement network count
        {
            this.SelectedNetwork--;
            if (this.SelectedNetwork < 0)
                this.SelectedNetwork = fcm.Networks.Count - 1;
            this.manager.RedrawWindow();
            return true;
        }
        if (name.Contains("allnetworks"))
        {
            this.DisplayAll = !this.DisplayAll;
            this.manager.RedrawWindow();
            return true;
        }
        if (name.Contains("selnetwork"))
        {
            this.SelNetwork = true;
            this.manager.RedrawWindow();
            return true;
        }
        if (name.Contains("cancel"))
        {
            this.SelNetwork = false;
            this.manager.RedrawWindow();
            return true;
        }
        if (name.Contains("networknum")) // drag drop to a slot
        {
            int slotNum = -1;
            int.TryParse(name.Replace("networknum", ""), out slotNum); //Get slot name as number

            if (slotNum > -1) // valid slot
            {
                this.SelectedNetwork = slotNum;
                this.SelNetwork = false;
                this.manager.RedrawWindow();
                return true;
            }
        }
        if (name.Contains("ordername"))
        {
            this.OrderByName = true;
            this.manager.RedrawWindow();
        }
        if (name.Contains("ordercount"))
        {
            this.OrderByName = false;
            this.manager.RedrawWindow();
        }
        if (name.Contains("togglelayout"))
        {
            this.CompactLayout = !this.CompactLayout;
            this.manager.RedrawWindow();
        }

        return false;
    }

    public override void ButtonEnter(string name, SegmentEntity targetEntity)
    {
        FreightSystemMonitor monitor = targetEntity as FreightSystemMonitor;

        if (!this.DisplayAll)
            return;
        string str = string.Empty;
        int count;
        ItemBase itemForSlot = this.GetItemForSlot(name, out count);
        if (itemForSlot == null)
            return;
        if (HotBarManager.mbInited)
        {
            HotBarManager.SetCurrentBlockLabel(ItemManager.GetItemName(itemForSlot));
        }
        else
        {
            if (!SurvivalHotBarManager.mbInited)
                return;
            string name1 = !WorldScript.mLocalPlayer.mResearch.IsKnown(itemForSlot) ? "Unknown Material" : ItemManager.GetItemName(itemForSlot);
            if (count > 1)
                SurvivalHotBarManager.SetCurrentBlockLabel(string.Format("{0} {1}", count, name1));
            else
                SurvivalHotBarManager.SetCurrentBlockLabel(name1);
        }
    }

    private ItemBase GetItemForSlot(string name, out int count)
    {
        ItemBase itemForSlot = null;
        count = 0;
        int slotNum = -1;
        int.TryParse(name.Replace("itemicon", ""), out slotNum); //Get slot name as number
        if (slotNum > -1)
        {
            itemForSlot = fcm.GlobalInventory[slotNum].Key;
            count = fcm.GlobalInventory[slotNum].Value;
        }
        return itemForSlot;
    }

    public override void OnClose(SegmentEntity targetEntity)
    {
        GenericMachinePanelScript.instance.gameObject.transform.localPosition = this.StartPos;
        GenericMachinePanelScript.instance.gameObject.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        GenericMachinePanelScript.instance.Background_Panel.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        GenericMachinePanelScript.instance.Label_Holder.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        GenericMachinePanelScript.instance.Icon_Holder.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        GenericMachinePanelScript.instance.Content_Holder.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        GenericMachinePanelScript.instance.Content_Icon_Holder.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        GenericMachinePanelScript.instance.Scroll_Bar.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        GenericMachinePanelScript.instance.Source_Holder.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        GenericMachinePanelScript.instance.Generic_Machine_Title_Label.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        this.firstopen = false;

        base.OnClose(targetEntity);
    }

    public override void HandleItemDrag(string name, ItemBase draggedItem, DragAndDropManager.DragRemoveItem dragDelegate, SegmentEntity targetEntity)
    {
        //FreightSystemMonitor monitor = targetEntity as FreightSystemMonitor;
        return;
    }

    //public static NetworkInterfaceResponse HandleNetworkCommand(Player player, NetworkInterfaceCommand nic)
    //{

    //    string command = nic.command;
    //    if (command != null)
    //    {
    //        if (command == "test")
    //        {
    //            // do whatever
    //        }
    //    }

    //    return new NetworkInterfaceResponse
    //    {
    //        entity = station,
    //        inventory = player.mInventory
    //    };
    //}
}

