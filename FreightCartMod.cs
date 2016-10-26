using System.Collections.Generic;
using UnityEngine;
using System.Reflection;


public class FreightCartMod : FortressCraftMod
{
    public static int Update;
    public static int LiveCarts = 0;
    public static int LiveUpdateTime = -1000;
    public static bool CartCheckin = false;
    public static FreightSystemMonitor monitor;

    public override ModRegistrationData Register()
    {
        ModRegistrationData modRegistrationData = new ModRegistrationData();
        modRegistrationData.RegisterEntityHandler("steveman0.FreightCartStation");
        modRegistrationData.RegisterMobHandler("steveman0.FreightCart_T1");
        modRegistrationData.RegisterMobHandler("steveman0.FreightCart_T2");
        modRegistrationData.RegisterMobHandler("steveman0.FreightCart_T3");
        modRegistrationData.RegisterMobHandler("steveman0.FreightCart_T4");
        modRegistrationData.RegisterMobHandler("steveman0.FreightCartMK1");
        modRegistrationData.RegisterMobHandler("steveman0.FreightCart_Tour");
        modRegistrationData.RegisterMobHandler("steveman0.FreightCart_TourBasic");
        modRegistrationData.RegisterEntityHandler("steveman0.FreightSystemMonitor");
        modRegistrationData.RegisterEntityHandler("steveman0.FreightCartFactory");
        modRegistrationData.RegisterEntityHandler("steveman0.TrackJunction");
        modRegistrationData.RegisterEntityHandler("steveman0.TourCartStation");
        modRegistrationData.RegisterEntityUI("steveman0.FreightCartStation", new FreightCartWindow());
        modRegistrationData.RegisterEntityUI("steveman0.FreightSystemMonitor", new SystemMonitorWindow());

        UIManager.NetworkCommandFunctions.Add("steveman0.FreightCartWindow", new UIManager.HandleNetworkCommand(FreightCartWindow.HandleNetworkCommand));
        UIManager.NetworkCommandFunctions.Add("steveman0.TrackJunctionWindow", new UIManager.HandleNetworkCommand(TrackJunctionWindow.HandleNetworkCommand));

        //new FreightCartManager();

        //For generating the sync class on the Unity thread after moving freight cart manager to LFU
        GameObject Sync = new GameObject("ManagerSync");
        Sync.AddComponent<ManagerSync>();
        Sync.SetActive(true);
        Sync.GetComponent<ManagerSync>().enabled = true;

        Debug.Log("Freight Cart Mod V7 registered");

        return modRegistrationData;
    }

    public override ModCreateSegmentEntityResults CreateSegmentEntity(ModCreateSegmentEntityParameters parameters)
    {
        ModCreateSegmentEntityResults result = new ModCreateSegmentEntityResults();


        if (parameters.Cube == ModManager.mModMappings.CubesByKey["steveman0.TrackJunction"].CubeType)
        {
            parameters.ObjectType = SpawnableObjectEnum.Minecart_Track_Straight;
            result.Entity = new FreightTrackJunction(parameters);
        }
        foreach (ModCubeMap cubeMap in ModManager.mModMappings.CubeTypes)
        {
            if (cubeMap.CubeType == parameters.Cube)
            {
                if (cubeMap.Key.Equals("steveman0.FreightCartStation"))
                {
                    parameters.ObjectType = SpawnableObjectEnum.Minecart_Track_LoadStation;
                    result.Entity = new FreightCartStation(parameters);
                }
                else if (cubeMap.Key.Equals("steveman0.FreightSystemMonitor"))
                {
                    parameters.ObjectType = SpawnableObjectEnum.AutoBuilder;
                    result.Entity = new FreightSystemMonitor(parameters);
                }
                else if (cubeMap.Key.Equals("steveman0.FreightCartFactory"))
                {
                    parameters.ObjectType = SpawnableObjectEnum.Minecart_Track_Factory;
                    result.Entity = new FreightCartFactory(parameters);
                }
                else if (cubeMap.Key.Equals("steveman0.TourCartStation"))
                {
                    parameters.ObjectType = SpawnableObjectEnum.Minecart_Track_Factory;
                    result.Entity = new TourCartStation(parameters);
                }
            }
        }
        return result;
    }

    public override void CreateMobEntity(ModCreateMobParameters parameters, ModCreateMobResults results)
    {
        if (parameters.MobKey == "steveman0.FreightCart_T1")
            results.Mob = new FreightCartMob(FreightCartMob.eMinecartType.FreightCart_T1, parameters);
        if (parameters.MobKey == "steveman0.FreightCart_T2")
            results.Mob = new FreightCartMob(FreightCartMob.eMinecartType.FreightCart_T2, parameters);
        if (parameters.MobKey == "steveman0.FreightCart_T3")
            results.Mob = new FreightCartMob(FreightCartMob.eMinecartType.FreightCart_T3, parameters);
        if (parameters.MobKey == "steveman0.FreightCart_T4")
            results.Mob = new FreightCartMob(FreightCartMob.eMinecartType.FreightCart_T4, parameters);
        if (parameters.MobKey == "steveman0.FreightCart_Tour")
            results.Mob = new FreightCartMob(FreightCartMob.eMinecartType.FreightCartTour, parameters);
        if (parameters.MobKey == "steveman0.FreightCart_TourBasic")
            results.Mob = new FreightCartMob(FreightCartMob.eMinecartType.FreightCartTourBasic, parameters);

        if (parameters.MobKey == "steveman0.FreightCartMK1")
            results.Mob = new FreightCartMob(FreightCartMob.eMinecartType.FreightCartMK1, parameters);

        base.CreateMobEntity(parameters, results);
    }

    public override void LowFrequencyUpdate()
    {
        Update++;
        if (FreightCartManager.instance == null)
            new FreightCartManager();
        else
            FreightCartManager.instance.UpdateMassInventory();
        if (Update - LiveUpdateTime < 10)
            CartCheckin = true;
        else if (CartCheckin)
        {
            Debug.LogWarning("---------FREIGHT CART DEBUG------------\nLive carts checked in: " + LiveCarts.ToString());
            FloatingCombatTextManager.instance.QueueText(monitor.mnX, monitor.mnY + 1L, monitor.mnZ, 1f, "Total Freight Carts Active: " + ManagerSync.instance.CartCounter.ToString(), Color.yellow, 2f, 64f);
            CartCheckin = false;
            LiveCarts = 0;
        }
    }
}

