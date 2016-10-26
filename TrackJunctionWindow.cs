using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class TrackJunctionWindow
{
    public const string InterfaceName = "steveman0.TrackJunctionWindow";
    public const string InterfaceResetJunction = "ResetJunction";

    public static void ResetJunction(FreightTrackJunction junction)
    {
        junction.ResetJunction();
        if (!WorldScript.mbIsServer)
            NetworkManager.instance.SendInterfaceCommand(InterfaceName, InterfaceResetJunction, null, null, junction, 0f);
    }

    public static NetworkInterfaceResponse HandleNetworkCommand(Player player, NetworkInterfaceCommand nic)
    {
        FreightTrackJunction junction = nic.target as FreightTrackJunction;

        string command = nic.command;
        if (command != null)
        {
            if (command == InterfaceResetJunction)
                TrackJunctionWindow.ResetJunction(junction);
        }

        return new NetworkInterfaceResponse
        {
            entity = junction,
            inventory = player.mInventory
        };
    }
}

