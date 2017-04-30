using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FortressCraft.Community.Utilities;
using System.Text;

public class SystemMonitorWindow : BaseMachineWindow
{
    public const string InterfaceName = "steveman0.SystemMonitorWindow";
    public const string InterfaceNetStatus = "RequestNetStatus";

    private bool dirty;
    public static bool networkredraw;
    private WindowTypes CurrentWindow;
    private int SelectedNetwork = 0;
    private int SelectedStorage = 0;
    private int TrackNetworkDisplay = -1;
    private int StationDisplay = -1;
    private int CartDisplay = -1;
    private int InventoryCount;
    private FreightCartStation CurrentStation;
    private List<FreightRegistry> Registries;
    public static FreightCartManager fcm = FreightCartManager.instance;
    private Dictionary<ItemBase, int> FullInventory;
    //private int RefreshCounter;  //Use this someday to refresh the stock window?
    private Vector3 StartPos;
    private bool firstopen;
    private bool OrderByName = true;
    private bool CompactLayout = true;

    //Window position offsets
    const int globalxoffset = 75; //For window scaling adjustments
    const int buttonoffset = 95;
    const int buttonspacing = 140;
    const int buttonx1 = globalxoffset + buttonoffset - 25;
    const int buttonx2 = globalxoffset + buttonoffset + buttonspacing;
    const int buttonx3 = globalxoffset + buttonoffset + 2 * buttonspacing;
    const int buttonx4 = globalxoffset + buttonoffset + 3 * buttonspacing;
    const int buttonx5 = globalxoffset + buttonoffset + 4 * buttonspacing;
    const int buttonx6 = globalxoffset + buttonoffset + 5 * buttonspacing + 25;

    private enum WindowTypes
    {
        NetworkStatus,
        NetworkSelection,
        TrackNetworks,
        GlobalInventory,
        StorageInventory,
    }

    #region SpawnWindow
    public override void SpawnWindow(SegmentEntity targetEntity)
    {
        FreightSystemMonitor monitor = targetEntity as FreightSystemMonitor;
        //Catch for when the window is called on an inappropriate machine
        if (monitor == null)
        {
            //GenericMachinePanelScript.instance.Hide();
            UIManager.RemoveUIRules("Machine");

            return;
        }

        //Debug.LogWarning("first area test fsm");
        if (fcm == null)
            Debug.LogWarning("Freight cart manager instance is null in System Monitor Window SpawnWindow!!");
        if (fcm != null && this.SelectedNetwork >= fcm.Networks.Count)
            this.SelectedNetwork = 0;

        if (!firstopen)
            this.InitializeWindow();

        switch (this.CurrentWindow)
        {
            case WindowTypes.NetworkStatus:
                this.SpawnNetworkStatus(monitor);
                break;
            case WindowTypes.NetworkSelection:
                this.SpawnNetworkSelection();
                break;
            case WindowTypes.TrackNetworks:
                this.SpawnTrackNetworks();
                break;
            case WindowTypes.GlobalInventory:
                this.SpawnGlobalInventory();
                break;
            case WindowTypes.StorageInventory:
                this.SpawnStorageInventory();
                break;
        }
        this.dirty = true;
        networkredraw = false;
    }

    private void InitializeWindow()
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

    private void SpawnNetworkStatus(FreightSystemMonitor monitor)
    {
        this.manager.SetTitle("Freight System Status");

        this.manager.AddButton("prevnetwork", "Previous Network", buttonx1, 0);
        this.manager.AddButton("allnetworks", "Global Inventory", buttonx2, 0);
        this.manager.AddButton("selnetwork", "Select Network", buttonx3, 0);
        this.manager.AddButton("tracknetworks", "Track Systems", buttonx4, 0);
        this.manager.AddButton("viewinventory", "View Inventory", buttonx5, 0);
        this.manager.AddButton("nextnetwork", "Next Network", buttonx6, 0);

        if (NetworkSync.NetworkStatus == null)
            SystemMonitorWindow.RequestNetworkStatus(this.SelectedNetwork, WorldScript.mLocalPlayer, monitor);

        if (NetworkSync.NetworkStatus != null)
        {
            int spacing = 60; //Spacing between each registry line
            int count = 0;
            int yoffset = 100; //Offset below button row
            int col2xoffset = 350 + globalxoffset; //Col 2 offset from the left
            int col3xoffset = 125 + col2xoffset; //Col 3 offset from the left
            int col4xoffset = 125 + col3xoffset; //Col 3 offset from the left
            int col5xoffset = 125 + col4xoffset; //Col 4 offset from the left

            //this.Registries = new List<FreightRegistry>();
            //this.FullInventory = new Dictionary<ItemBase, int>(new ItemBaseComparer());
            //this.Registries = fcm.GetNetworkRegistries(this.SelectedNetwork);
            count = NetworkSync.NetworkStatus.Registries.Count;
            //Debug.Log("After registeries counting " + count);
            this.manager.AddBigLabel("nameofnetworktitle", "Viewing status of freight network: ", Color.cyan, 350, 50);
            this.manager.AddBigLabel("nameofnetwork", NetworkSync.NetworkStatus.NetworkID, Color.cyan, 650, 50);
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
        else if (WorldScript.mbIsServer)
        {
            this.manager.AddBigLabel("loadingnetworks", "No Freight Networks found...", Color.red, 225, 150);
        }
        else
        {
            this.manager.AddBigLabel("loadingnetworks", "Waiting for server...", Color.red, 225, 150);
        }
    }

    private void SpawnNetworkSelection()
    {
        this.manager.SetTitle("Select Network");
        this.manager.AddButton("allnetworks", "Global Inventory", buttonx2, 0);
        this.manager.AddButton("selnetwork", "Network Status", buttonx3, 0);
        this.manager.AddButton("tracknetworks", "Track Systems", buttonx4, 0);
        this.manager.AddButton("viewinventory", "View Inventory", buttonx5, 0);

        int spacing = 50; //Spacing between each registry line
        int yoffset = 65; //Offset below button row

        for (int n = 0; n < fcm.Networks.Count; n++)
        {
            this.manager.AddButton("networknum" + n, fcm.Networks[n], buttonx3 + 70, yoffset + (n * spacing));
        }
    }

    private void SpawnTrackNetworks()
    {
        this.manager.SetTitle("Track Systems");
        this.manager.AddButton("allnetworks", "Global Inventory", buttonx2, 0);
        this.manager.AddButton("selnetwork", "Select Network", buttonx3, 0);
        this.manager.AddButton("tracknetworks", "Network Status", buttonx4, 0);
        this.manager.AddButton("viewinventory", "View Inventory", buttonx5, 0);

        int ycursor = 65;
        int trackiconx = globalxoffset + 175;
        int trackxoffset = trackiconx + 65;
        int tracklabel = trackxoffset + 250;
        int stationxicon = trackiconx + 50;
        int stationlabel = stationxicon + 65;
        int cartxicon = stationxicon + 50;
        int cartlabel = cartxicon + 65;
        int invxicon = cartxicon + 50;
        int invlabel = invxicon + 65;

        int networkcount = FreightCartManager.instance.GlobalTrackNetworks.Count;

        if (networkcount == 0)
            this.manager.AddBigLabel("notracknetworks", "No track networks found...", Color.red, 225, 150);
        else
        {
            string trackicon = "Track Straight";
            string stationicon = "Minecart Load";
            for (int n = 0; n < networkcount; n++)
            {
                FreightTrackNetwork network = FreightCartManager.instance.GlobalTrackNetworks[n];
                if (network == null)
                    continue;
                int junctioncount = network.TrackJunctions.Count;
                int networkID = network.NetworkID;
                int assignedcarts;
                int availcarts;
                network.GetNetworkStats(out assignedcarts, out availcarts);

                this.manager.AddIcon("trackicon" + n, trackicon, Color.white, trackiconx, ycursor);
                this.manager.AddBigLabel("trackjunctions" + n, "ID: " + network.NetworkID.ToString() + "   " + junctioncount.ToString() + " Junctions   Carts: ", Color.white, trackxoffset, ycursor);
                this.manager.AddBigLabel("trackcarts" + n, availcarts.ToString() + " / " + assignedcarts.ToString(), availcarts > assignedcarts ? Color.green : availcarts == assignedcarts ? Color.white : Color.red, tracklabel, ycursor);
                ycursor += 60;
                if (this.TrackNetworkDisplay == n)
                {
                    List<FreightCartStation> stations = network.GetNetworkStations();
                    stations = stations.OrderBy(x => x.StationName).ToList();
                    int stationcount = stations.Count;
                    Debug.LogWarning("FSM Station Count: " + stationcount.ToString());
                    for (int m = 0; m < stationcount; m++)
                    {
                        FreightCartStation station = stations[m];
                        int stationavail = station.AvailableCarts;
                        int stationassigned = station.AssignedCarts;
                        this.manager.AddIcon("stationicon" + m, stationicon, Color.white, stationxicon, ycursor);
                        this.manager.AddBigLabel("stationnetwork" + m, (!string.IsNullOrEmpty(station.StationName) ? station.StationName : "UNNAMED") + " - " + station.NetworkID, station.StationFull <= 0 ? Color.white : Color.red, stationlabel, ycursor);
                        this.manager.AddBigLabel("stationcarts" + m, "Carts: " + stationavail.ToString() + " / " + stationassigned.ToString(), stationavail > stationassigned ? Color.green : stationavail == stationassigned ? Color.white : Color.red, stationlabel + 350, ycursor);
                        ycursor += 60;
                        if (this.StationDisplay == m)
                        {
                            this.manager.AddButton("addcart", "Add Cart", stationlabel + 475, ycursor - 60);
                            this.manager.AddButton("removecart", "Remove Cart", stationlabel + 475, ycursor - 10);
                            this.CurrentStation = station;


                            List<FreightRegistry> LocalDeficits = new List<FreightRegistry>();
                            List<FreightRegistry> LocalSurplus = new List<FreightRegistry>();
                            if (station.massStorageCrate != null)
                            {
                                LocalDeficits = FreightCartManager.instance.GetLocalDeficit(station.NetworkID, station.massStorageCrate);
                                LocalSurplus = FreightCartManager.instance.GetLocalSurplus(station.NetworkID, station.massStorageCrate);
                            }
                            else if (station.HopperInterface != null)
                            {
                                LocalDeficits = this.FreightListingConversion(station.HopperInterface.FreightRequests.OrderByDescending(x => x.Quantity).Take(3).ToList());
                                LocalSurplus = this.FreightListingConversion(station.HopperInterface.FreightOfferings.OrderByDescending(x => x.Quantity).Take(3).ToList());
                            }
                            else if (station.AttachedInterface != null)
                            {
                                LocalDeficits = this.FreightListingConversion(station.AttachedInterface.FreightRequests.OrderByDescending(x => x.Quantity).Take(3).ToList());
                                LocalSurplus = this.FreightListingConversion(station.AttachedInterface.FreightOfferings.OrderByDescending(x => x.Quantity).Take(3).ToList());
                            }
                            ycursor -= 20;
                            string str = "";
                            int shifter = 1;
                            int ind2 = 0;
                            if (LocalDeficits.Count <= 0)
                                this.manager.AddLabel(GenericMachineManager.LabelType.OneLineFullWidth, "localdef", "This storage is fully stocked!", Color.white, false, stationlabel, ycursor);
                            else
                                this.manager.AddLabel(GenericMachineManager.LabelType.OneLineFullWidth, "localdef", "Top requests for this storage:", Color.white, false, stationlabel, ycursor);
                            ycursor += 20;
                            for (int index = 0; index < LocalDeficits.Count; index++)
                            {
                                if (LocalDeficits[index].Deficit != 0)
                                {
                                    str = (index + 1).ToString() + ") " + LocalDeficits[index].Deficit.ToString("N0") + "x " + ItemManager.GetItemName(LocalDeficits[index].FreightItem) + "\n";
                                    this.manager.AddLabel(GenericMachineManager.LabelType.OneLineFullWidth, "localdef" + index, str, Color.white, false, stationlabel, ycursor);
                                    ycursor += 20;
                                }
                                shifter++;
                            }
                            ycursor -= 20 * shifter;
                            if (LocalSurplus.Count <= 0)
                                this.manager.AddLabel(GenericMachineManager.LabelType.OneLineFullWidth, "localsur", "This storage has nothing to offer!", Color.white, false, stationlabel + 250, ycursor);
                            else
                                this.manager.AddLabel(GenericMachineManager.LabelType.OneLineFullWidth, "localsur", "Top offerings for this storage:", Color.white, false, stationlabel + 250, ycursor);
                            ycursor += 20;
                            for (int index = 0; index < LocalSurplus.Count; index++)
                            {
                                if (LocalSurplus[index].Surplus != 0)
                                {
                                    str = (index + 1).ToString() + ") " + LocalSurplus[index].Surplus.ToString("N0") + "x " + ItemManager.GetItemName(LocalSurplus[index].FreightItem) + "\n";
                                    this.manager.AddLabel(GenericMachineManager.LabelType.OneLineFullWidth, "localsur" + index, str, Color.white, false, stationlabel + 250, ycursor);
                                    ycursor += 20;
                                }
                                ind2 = index;
                            }
                            if (ind2 > (shifter - 2))
                                ycursor += 20;
                            else
                                ycursor += (shifter - 1 - ind2) * 20 + 20;

                            int cartcount = station.CartList.Count;
                            for (int p = 0; p < cartcount; p++)
                            {
                                FreightCartMob cart = station.CartList[p];

                                int itemID = ItemEntries.MineCartT1;
                                if (cart.meType == FreightCartMob.eMinecartType.FreightCartMK1)
                                    itemID = ModManager.mModMappings.ItemsByKey["steveman0.FreightCartMK1"].ItemId;
                                else if (cart.meType == FreightCartMob.eMinecartType.FreightCart_T1 || cart.meType == FreightCartMob.eMinecartType.OreFreighter_T1)
                                    itemID = ItemEntries.MineCartT1;
                                else if (cart.meType == FreightCartMob.eMinecartType.FreightCart_T2 || cart.meType == FreightCartMob.eMinecartType.OreFreighter_T2)
                                    itemID = ItemEntries.MineCartT2;
                                else if (cart.meType == FreightCartMob.eMinecartType.FreightCart_T3 || cart.meType == FreightCartMob.eMinecartType.OreFreighter_T3)
                                    itemID = ItemEntries.MineCartT3;
                                else if (cart.meType == FreightCartMob.eMinecartType.FreightCart_T4 || cart.meType == FreightCartMob.eMinecartType.OreFreighter_T4)
                                    itemID = ItemEntries.MineCartT4;
                                else if (cart.meType == FreightCartMob.eMinecartType.FreightCartTour)
                                    itemID = ItemEntries.TourCart;
                                string carticon = ItemManager.GetItemIcon(itemID);

                                this.manager.AddIcon("carticon" + p, carticon, Color.white, cartxicon, ycursor);
                                this.manager.AddBigLabel("cartlabel" + p, "Inventory: " + cart.mnUsedStorage.ToString() + "/" + cart.mnMaxStorage.ToString(), Color.white, cartlabel, ycursor);
                                ycursor += 60;
                                if (p == CartDisplay)
                                {
                                    MachineInventory inv = null;
                                    if (!string.IsNullOrEmpty(station.NetworkID) && cart.LocalInventory.ContainsKey(station.NetworkID))
                                        inv = cart.LocalInventory[station.NetworkID];
                                    if (inv == null || inv.ItemCount() == 0)
                                    {
                                        this.manager.AddBigLabel("invlabelempty", "No goods from this station", Color.white, invxicon + 15, ycursor);
                                        ycursor += 60;
                                    }
                                    else
                                    {
                                        int invcount = inv.Inventory.Count;
                                        for (int q = 0; q < invcount; q++)
                                        {
                                            ItemBase item = inv.Inventory[q];
                                            string invicon = ItemManager.GetItemIcon(item);
                                            this.manager.AddIcon("invicon" + q, invicon, Color.white, invxicon, ycursor);
                                            this.manager.AddBigLabel("invlabel" + q, item.ToString(), Color.white, invlabel, ycursor);
                                            ycursor += 60;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //Insert Tour cart staion listing here
                }
            }
        }
    }

    private void SpawnGlobalInventory()
    {
        this.manager.SetTitle("Global Inventory");
        if (this.OrderByName)
            this.manager.AddButton("ordercount", "Order by Count", buttonx1, 0);
        else
            this.manager.AddButton("ordername", "Order by Name", buttonx1, 0);
        this.manager.AddButton("allnetworks", "Network Status", buttonx2, 0);
        this.manager.AddButton("selnetwork", "Select Network", buttonx3, 0);
        this.manager.AddButton("tracknetworks", "Track Systems", buttonx4, 0);
        this.manager.AddButton("viewinventory", "View Inventory", buttonx5, 0);

        //Need to restore the buttons for different layout and ordering here!

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

    private void SpawnStorageInventory()
    {
        this.manager.SetTitle("Mass Storage Inventory");

        this.manager.AddButton("prevstorage", "Previous Inventory", buttonx1, 0);
        this.manager.AddButton("allnetworks", "Global Inventory", buttonx2, 0);
        this.manager.AddButton("selnetwork", "Select Network", buttonx3, 0);
        this.manager.AddButton("tracknetworks", "Track Systems", buttonx4, 0);
        this.manager.AddButton("viewinventory", "Network Status", buttonx5, 0);
        this.manager.AddButton("nextstorage", "Next Inventory", buttonx6, 0);

        if (fcm.StationInventories != null && fcm.StationInventories.Count > this.SelectedStorage && fcm.StationInventories.Count > 0)
        {

            this.InventoryCount = fcm.StationInventories[this.SelectedStorage].Inventory.Count;

            int ItemRowSpacing = 60;
            int ItemColSpacing = 60;
            int yoffset = 100;

            this.manager.AddBigLabel("nameofnetworktitle", "Viewing inventory of mass storage: ", Color.cyan, 335, 50);
            this.manager.AddBigLabel("nameofnetwork", fcm.StationInventories[this.SelectedStorage].Name, Color.cyan, 650, 50);

            for (int n = 0; n < this.InventoryCount; n++)
            {
                int row = n / 15;
                int col = n % 15;
                this.manager.AddIcon("itemicon" + n, "empty", Color.white, globalxoffset + col * ItemColSpacing + 35, row * ItemRowSpacing + yoffset);
                this.manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth, "StackSize" + n, "", Color.white, false, globalxoffset + col * ItemColSpacing + 27 + 25, row * ItemRowSpacing + 22 + yoffset);
            }
        }
    }
    #endregion

    #region UpdateMachine
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

        if (networkredraw)
            this.manager.RedrawWindow();

        GenericMachinePanelScript.instance.Scroll_Bar.GetComponent<UIScrollBar>().scrollValue -= Input.GetAxis("Mouse ScrollWheel");

        switch (this.CurrentWindow)
        {
            case WindowTypes.NetworkStatus:
                this.UpdateNetworkStatus();
                break;
            case WindowTypes.GlobalInventory:
                this.UpdateGlobalInventory();
                break;
            case WindowTypes.StorageInventory:
                this.UpdateStorageInventory();
                break;
        }
    }

    private void UpdateNetworkStatus()
    {
        if (NetworkSync.NetworkStatus != null)
        {
            //string networkid = fcm.Networks[this.SelectedNetwork];

            int count;
            count = NetworkSync.NetworkStatus.Registries.Count;

            for (int n = 0; n < count; n++)
            {
                ItemBase item = NetworkSync.NetworkStatus.Registries[n].FreightItem;
                string iconname = ItemManager.GetItemIcon(item);
                string itemname = ItemManager.GetItemName(item);
                int def = NetworkSync.NetworkStatus.Registries[n].Deficit;
                int stock = NetworkSync.NetworkStatus.Registries[n].Stock;
                int inventory = NetworkSync.NetworkStatus.Registries[n].Inventory;
                int surplus = NetworkSync.NetworkStatus.Registries[n].Surplus;

                this.manager.UpdateIcon("item" + n, iconname, Color.white);
                this.manager.UpdateLabel("registrytitle" + n, itemname, Color.white);
                this.manager.UpdateLabel("deficit" + n, def.ToString("N0"), def <= surplus ? Color.white : Color.red);
                this.manager.UpdateLabel("surplus" + n, surplus.ToString("N0"), surplus > def ? Color.green : Color.white);
                this.manager.UpdateLabel("stock" + n, stock.ToString("N0"), stock < def ? Color.red : Color.green);
                this.manager.UpdateLabel("totalinv" + n, inventory.ToString("N0"), Color.white);
            }
            // The server references update regularly but clients only get a snapshot
            if (!WorldScript.mbIsServer)
                dirty = false;
        }
    }

    private void UpdateGlobalInventory()
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

    private void UpdateStorageInventory()
    {
        if (fcm.StationInventories != null && fcm.StationInventories.Count > this.SelectedStorage)
        {
            int count = fcm.StationInventories[this.SelectedStorage].Inventory.Count;
            if (count != this.InventoryCount)
                this.manager.RedrawWindow();

            for (int n = 0; n < this.InventoryCount; n++)
            {
                ItemBase item = fcm.StationInventories[this.SelectedStorage].Inventory[n].Key;
                string iconname = ItemManager.GetItemIcon(item);
                string itemname = ItemManager.GetItemName(item);
                int itemcount = fcm.StationInventories[this.SelectedStorage].Inventory[n].Value;
                string iteminfo = (itemcount.ToString("N0") + "x " + itemname);
                iteminfo = iteminfo.Substring(0, 35 > iteminfo.Length ? iteminfo.Length : 35);

                this.manager.UpdateIcon("itemicon" + n, iconname, Color.white);
                if (this.CompactLayout)
                    this.manager.UpdateLabel("StackSize" + n, FormatStackText(itemcount), Color.white);
                else
                    this.manager.UpdateLabel("iteminfo" + n, iteminfo, Color.white);
            }
        }
        else
            this.manager.RedrawWindow();
    }
    #endregion

    private List<FreightRegistry> FreightListingConversion(List<FreightListing> items)
    {
        List<FreightRegistry> regs = new List<FreightRegistry>();
        foreach (FreightListing item in items)
        {
            FreightRegistry reg = new FreightRegistry(null, null, item.Item, 0, 0, FreightRegistry.RegistryType.Registry);
            reg.Deficit = item.Quantity;
            reg.Surplus = item.Quantity;
            regs.Add(reg);
        }
        return regs;
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

        if (name == "nextnetwork") // Increment network count
        {
            this.SelectedNetwork++;
            if (this.SelectedNetwork >= fcm.Networks.Count)
                this.SelectedNetwork = 0;
            NetworkSync.NetworkStatus = null;
            this.manager.RedrawWindow();
            return true;
        }
        else if (name == "prevnetwork") // Decrement network count
        {
            this.SelectedNetwork--;
            if (this.SelectedNetwork < 0)
                this.SelectedNetwork = fcm.Networks.Count - 1;
            NetworkSync.NetworkStatus = null;
            this.manager.RedrawWindow();
            return true;
        }
        else if (name == "nextstorage") // Increment storage count
        {
            this.SelectedStorage++;
            if (this.SelectedStorage >= fcm.StationInventories.Count)
                this.SelectedStorage = 0;
            this.manager.RedrawWindow();
            return true;
        }
        else if (name == "prevstorage") // Decrement storage count
        {
            this.SelectedStorage--;
            if (this.SelectedStorage < 0)
                this.SelectedStorage = fcm.StationInventories.Count - 1;
            this.manager.RedrawWindow();
            return true;
        }
        else if (name == "allnetworks")
        {
            if (this.CurrentWindow == WindowTypes.GlobalInventory)
                this.CurrentWindow = WindowTypes.NetworkStatus;
            else
                this.CurrentWindow = WindowTypes.GlobalInventory;
            this.manager.RedrawWindow();
            return true;
        }
        else if (name == "tracknetworks")
        {
            if (this.CurrentWindow == WindowTypes.TrackNetworks)
                this.CurrentWindow = WindowTypes.NetworkStatus;
            else
                this.CurrentWindow = WindowTypes.TrackNetworks;
            this.manager.RedrawWindow();
            return true;
        }
        else if (name == "selnetwork")
        {
            if (this.CurrentWindow == WindowTypes.NetworkSelection)
                this.CurrentWindow = WindowTypes.NetworkStatus;
            else
                this.CurrentWindow = WindowTypes.NetworkSelection;
            this.manager.RedrawWindow();
            return true;
        }
        else if (name == "viewinventory")
        {
            if (this.CurrentWindow == WindowTypes.StorageInventory)
                this.CurrentWindow = WindowTypes.NetworkStatus;
            else
                this.CurrentWindow = WindowTypes.StorageInventory;
            this.manager.RedrawWindow();
            return true;
        }
        else if (name.Contains("networknum"))
        {
            int slotNum = -1;
            int.TryParse(name.Replace("networknum", ""), out slotNum); //Get slot name as number

            if (slotNum > -1) // valid slot
            {
                this.SelectedNetwork = slotNum;
                NetworkSync.NetworkStatus = null;
                this.CurrentWindow = WindowTypes.NetworkStatus;
                this.manager.RedrawWindow();
                return true;
            }
        }
        else if (name.Contains("trackicon"))
        {
            int slotNum = -1;
            int.TryParse(name.Replace("trackicon", ""), out slotNum); //Get slot name as number

            if (slotNum > -1) // valid slot
            {
                if (this.TrackNetworkDisplay == slotNum)
                    this.TrackNetworkDisplay = -1;
                else
                    this.TrackNetworkDisplay = slotNum;
                this.StationDisplay = -1;
                this.CartDisplay = -1;
                this.manager.RedrawWindow();
                return true;
            }
        }
        else if (name.Contains("stationicon"))
        {
            int slotNum = -1;
            int.TryParse(name.Replace("stationicon", ""), out slotNum); //Get slot name as number

            if (slotNum > -1) // valid slot
            {
                if (this.StationDisplay == slotNum)
                    this.StationDisplay = -1;
                else
                    this.StationDisplay = slotNum;
                this.CartDisplay = -1;
                this.manager.ClearWindow();
                this.manager.RedrawWindow();
                return true;
            }
        }
        else if (name.Contains("carticon"))
        {
            int slotNum = -1;
            int.TryParse(name.Replace("carticon", ""), out slotNum); //Get slot name as number

            if (slotNum > -1) // valid slot
            {
                if (this.CartDisplay == slotNum)
                    this.CartDisplay = -1;
                else
                    this.CartDisplay = slotNum;
                this.manager.RedrawWindow();
                return true;
            }
        }
        else if (name == "addcart")
        {
            int amount = 1;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                amount = 10;
            FreightCartWindow.SetCartAssignment(this.CurrentStation, this.CurrentStation.AssignedCarts + amount);
            this.manager.UpdateLabel("stationcarts" + this.StationDisplay.ToString(), "Carts: " + this.CurrentStation.AvailableCarts.ToString() + " / " + this.CurrentStation.AssignedCarts.ToString(), this.CurrentStation.AvailableCarts > this.CurrentStation.AssignedCarts ? Color.green : this.CurrentStation.AvailableCarts == this.CurrentStation.AssignedCarts ? Color.white : Color.red);
            return true;
        }
        else if (name == "removecart")
        {
            int amount = 1;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                amount = 10;
            FreightCartWindow.SetCartAssignment(this.CurrentStation, this.CurrentStation.AssignedCarts - amount < 0 ? 0 : this.CurrentStation.AssignedCarts - amount);
            this.manager.UpdateLabel("stationcarts" + this.StationDisplay.ToString(), "Carts: " + this.CurrentStation.AvailableCarts.ToString() + " / " + this.CurrentStation.AssignedCarts.ToString(), this.CurrentStation.AvailableCarts > this.CurrentStation.AssignedCarts ? Color.green : this.CurrentStation.AvailableCarts == this.CurrentStation.AssignedCarts ? Color.white : Color.red);
            return true;
        }
        else if (name == "ordername")
        {
            this.OrderByName = true;
            this.manager.RedrawWindow();
        }
        else if (name == "ordercount")
        {
            this.OrderByName = false;
            this.manager.RedrawWindow();
        }
        else if (name == "togglelayout")
        {
            this.CompactLayout = !this.CompactLayout;
            this.manager.RedrawWindow();
        }

        return false;
    }

    public override void ButtonEnter(string name, SegmentEntity targetEntity)
    {
        FreightSystemMonitor monitor = targetEntity as FreightSystemMonitor;

        if (this.CurrentWindow != WindowTypes.GlobalInventory)
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
        if (slotNum > -1 && slotNum < fcm.GlobalInventory.Count)
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

        this.TrackNetworkDisplay = -1;
        this.StationDisplay = -1;
        this.CartDisplay = -1;

        base.OnClose(targetEntity);
    }

    public override void HandleItemDrag(string name, ItemBase draggedItem, DragAndDropManager.DragRemoveItem dragDelegate, SegmentEntity targetEntity)
    {
        //FreightSystemMonitor monitor = targetEntity as FreightSystemMonitor;
        return;
    }

    public static void RequestNetworkStatus(int netindex, Player player, FreightSystemMonitor monitor)
    {
        if (WorldScript.mbIsServer)
            NetworkSync.GetNetworkStatus(netindex, player);
        else
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfaceNetStatus, netindex.ToString(), null, monitor, 0f);
    }

    public static NetworkInterfaceResponse HandleNetworkCommand(Player player, NetworkInterfaceCommand nic)
    {
        FreightSystemMonitor monitor = nic.target as FreightSystemMonitor;
        string command = nic.command;

        if (command != null)
        {
            if (command == InterfaceNetStatus)
            {
                int Netindex = 0;
                int.TryParse(nic.payload ?? "-1", out Netindex);
                SystemMonitorWindow.RequestNetworkStatus(Netindex, player, monitor);
            }
        }

        return new NetworkInterfaceResponse
        {
            entity = monitor,
            inventory = player.mInventory
        };
    }
}

