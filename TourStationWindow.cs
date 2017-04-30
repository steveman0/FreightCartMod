using UnityEngine;
using System.Collections.Generic;
using FortressCraft.Community.Utilities;
using System.Linq;

public class TourStationWindow : BaseMachineWindow
{
    public const string InterfaceName = "TourStationWindow";
    public const string InterfaceStationName = "SetStationName";

    //private static bool dirty;
    private static bool networkredraw;
    private string EntryString;
    private int Counter;
    private bool SetName = false;

    public override void SpawnWindow(SegmentEntity targetEntity)
    {
        TourCartStation station = targetEntity as TourCartStation;
        //Catch for when the window is called on an inappropriate machine
        if (station == null)
        {
            GenericMachinePanelScript.instance.Hide();
            UIManager.RemoveUIRules("Machine");
            return;
        }
        UIUtil.UIdelay = 0;
        UIUtil.UILock = true;

        if (this.SetName || string.IsNullOrEmpty(station.StationName))
        {
            this.manager.SetTitle("Tour Cart Station - Set Name");
            UIManager.mbEditingTextField = true;
            UIManager.AddUIRules("TextEntry", UIRules.RestrictMovement | UIRules.RestrictLooking | UIRules.RestrictBuilding | UIRules.RestrictInteracting | UIRules.SetUIUpdateRate);
            GenericMachinePanelScript.instance.Scroll_Bar.GetComponent<UIScrollBar>().scrollValue = 0.0f;

            this.manager.AddButton("namecancel", "Cancel", 100, 0);
            this.manager.AddBigLabel("nametitle", "Enter Network ID", Color.white, 50, 40);
            this.manager.AddBigLabel("nameentry", "_", Color.cyan, 50, 65);
        }
        else
        {
            // Select destination
            this.manager.SetTitle("Select Tour Cart Destination");
            this.manager.AddButton("namestation", "Change Name", 15, 0);
            this.manager.AddBigLabel("stationname", station.StationName, Color.white, 165, 0);

            int spacing = 50; //Spacing between each registry line
            int yoffset = 65; //Offset below button row
            if (station.TrackNetwork != null && station.TrackNetwork.TourCartStations.Count != 0)
            {
                List<string> keys = station.TrackNetwork.TourCartStations.Keys.ToList();
                keys.Remove(station.StationName);
                for (int n = 0; n < keys.Count; n++)
                    this.manager.AddButton("tourstation" + n, keys[n], 100, yoffset + (n * spacing));
                if (keys.Count == 0)
                {
                    this.manager.AddBigLabel("nostations", "Add Additional Tour Cart Stations", Color.red, 0, yoffset);
                    this.manager.AddBigLabel("nostations1", "to Track Network...", Color.red, 0, yoffset + 22);
                }
            }
            else
            {
                this.manager.AddBigLabel("nostations", "Connect Tour Cart Station to", Color.red, 0, yoffset);
                this.manager.AddBigLabel("nostations1", "a Freight Track Network...", Color.red, 0, yoffset + 22);
            }
        }
        //dirty = true;
        networkredraw = false;
    }


    public override void UpdateMachine(SegmentEntity targetEntity)
    {
        TourCartStation station = targetEntity as TourCartStation;
        //Catch for when the window is called on an inappropriate machine
        if (station == null)
        {
            GenericMachinePanelScript.instance.Hide();
            UIManager.RemoveUIRules("Machine");
            return;
        }
        UIUtil.UIdelay = 0;

        if (networkredraw)
            this.manager.RedrawWindow();

        if (this.SetName || string.IsNullOrEmpty(station.StationName))
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
                    TourStationWindow.SetStationName(station, this.EntryString);
                    this.SetName = false;
                    this.EntryString = "";
                    UIManager.mbEditingTextField = false;
                    UIManager.RemoveUIRules("TextEntry");
                    return;
                }
                else
                    this.EntryString += c;
            }
            this.manager.UpdateLabel("nameentry", this.EntryString + (this.Counter % 20 > 10 ? "_" : ""), Color.cyan);
            //dirty = true;
            return;
        }
    }

    public override bool ButtonClicked(string name, SegmentEntity targetEntity)
    {
        TourCartStation station = targetEntity as TourCartStation;

        if (name == "namestation")
        {
            this.SetName = true;
            this.manager.RedrawWindow();
            return true;
        }
        else if (name == "namecancel")
        {
            this.SetName = false;
            this.manager.RedrawWindow();
        }
        else if (name.Contains("tourstation")) // drag drop to a slot
        {
            if (station.TrackNetwork == null || station.ClosestJunction == null)
            {
                Debug.LogWarning("TourStationWindow attemption to travel on null network or from null junction");
                return false;
            }
            int slotNum = -1;
            int.TryParse(name.Replace("tourstation", ""), out slotNum); //Get slot name as number
            List<string> keys = station.TrackNetwork.TourCartStations.Keys.ToList();
            keys.Remove(station.StationName);

            if (slotNum > -1) // valid slot
                station.TravelTo(station.TrackNetwork.TourCartStations[keys[slotNum]], station.ClosestJunction);
            GenericMachinePanelScript.instance.Hide();
            UIManager.RemoveUIRules("Machine");
            return true;
        }

        return false;
    }

    public static void SetStationName(TourCartStation station, string stationname)
    {
        if (string.IsNullOrEmpty(stationname))
            Debug.LogWarning("TourStationWindow trying to set name for null or empty string!");
        if (station.TrackNetwork != null && station.TrackNetwork.TourCartStations.ContainsKey(stationname))
        {
            Debug.LogWarning("User attempted to duplicate Tour Cart Station names");
            return;
        }
        if (!string.IsNullOrEmpty(station.StationName) && station.TrackNetwork != null && station.TrackNetwork.TourCartStations.ContainsKey(station.StationName))
            station.TrackNetwork.TourCartStations.Remove(station.StationName);
        station.StationName = stationname;
        if (station.TrackNetwork != null)
            station.TrackNetwork.TourCartStations.Add(station.StationName, station);
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfaceStationName, stationname, null, station, 0f);
        TourStationWindow.networkredraw = true;
        station.RequestImmediateNetworkUpdate();
        station.MarkDirtyDelayed();
    }

    public override void OnClose(SegmentEntity targetEntity)
    {
        this.SetName = false;
        this.EntryString = "";
        UIManager.mbEditingTextField = false;
        UIManager.RemoveUIRules("TextEntry");
    }

    public static NetworkInterfaceResponse HandleNetworkCommand(Player player, NetworkInterfaceCommand nic)
    {
        TourCartStation station = nic.target as TourCartStation;

        string command = nic.command;
        if (command != null)
        {
            if (command == InterfaceStationName)
                TourStationWindow.SetStationName(station, nic.payload);
        }

        return new NetworkInterfaceResponse
        {
            entity = station,
            inventory = player.mInventory
        };
    }
}
