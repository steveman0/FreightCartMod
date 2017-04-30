using System.Collections.Generic;
using UnityEngine;
using System.Reflection;


public class FreightCartMod : FortressCraftMod
{
    public static int UpdateCounter;
    public static int LiveCarts = 0;
    public static int LiveUpdateTime = -1000;
    public static bool CartCheckin = false;
    public static FreightSystemMonitor monitor;
    public static MPB_Instancer TrackInstances = new MPB_Instancer();
    public static ushort TrackJunctionType = ModManager.mModMappings.CubesByKey["steveman0.TrackJunction"].CubeType;
    public static ushort StationType = ModManager.mModMappings.CubesByKey["steveman0.FreightCartStation"].CubeType;
    public static ushort SystemMonitorType = ModManager.mModMappings.CubesByKey["steveman0.FreightSystemMonitor"].CubeType;
    public static ushort FactoryType = ModManager.mModMappings.CubesByKey["steveman0.FreightCartFactory"].CubeType;
    public static ushort TourStationType = ModManager.mModMappings.CubesByKey["steveman0.TourCartStation"].CubeType;
    public static ushort ScrapTrackType = ModManager.mModMappings.CubesByKey["steveman0.ScrapTrack"].CubeType;

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
        modRegistrationData.RegisterMobHandler("steveman0.OreFreighter_T1");
        modRegistrationData.RegisterMobHandler("steveman0.OreFreighter_T2");
        modRegistrationData.RegisterMobHandler("steveman0.OreFreighter_T3");
        modRegistrationData.RegisterMobHandler("steveman0.OreFreighter_T4");
        modRegistrationData.RegisterMobHandler("steveman0.ScrapCartMK1");
        modRegistrationData.RegisterMobHandler("steveman0.ScrapOreFreighterMK1");
        modRegistrationData.RegisterEntityHandler("steveman0.FreightSystemMonitor");
        modRegistrationData.RegisterEntityHandler("steveman0.FreightCartFactory");
        modRegistrationData.RegisterEntityHandler("steveman0.TrackJunction");
        modRegistrationData.RegisterEntityHandler("steveman0.TourCartStation");
        modRegistrationData.RegisterEntityHandler("steveman0.ScrapTrack");
        modRegistrationData.RegisterEntityUI("steveman0.FreightCartStation", new FreightCartWindow());
        modRegistrationData.RegisterEntityUI("steveman0.FreightSystemMonitor", new SystemMonitorWindow());
        modRegistrationData.RegisterServerComms("steveman0.NetworkStatus", NetworkSync.SendNetworkStatus, NetworkSync.ReadNetworkStatus);

        UIManager.NetworkCommandFunctions.Add("steveman0.FreightCartWindow", new UIManager.HandleNetworkCommand(FreightCartWindow.HandleNetworkCommand));
        UIManager.NetworkCommandFunctions.Add("steveman0.TrackJunctionWindow", new UIManager.HandleNetworkCommand(TrackJunctionWindow.HandleNetworkCommand));
        UIManager.NetworkCommandFunctions.Add(TourStationWindow.InterfaceName, new UIManager.HandleNetworkCommand(TourStationWindow.HandleNetworkCommand));
        UIManager.NetworkCommandFunctions.Add(SystemMonitorWindow.InterfaceName, new UIManager.HandleNetworkCommand(SystemMonitorWindow.HandleNetworkCommand));

        //new FreightCartManager();

        //For generating the sync class on the Unity thread after moving freight cart manager to LFU
        GameObject Sync = new GameObject("ManagerSync");
        Sync.AddComponent<ManagerSync>();
        Sync.SetActive(true);
        Sync.GetComponent<ManagerSync>().enabled = true;

        Debug.Log("Freight Cart Mod V9 registered");

        // Instanced track rendering
        if (!PersistentSettings.mbHeadlessServer)
        {
            Material TrackMaterial = InstanceManager.instance.maSimpleMaterials[(int)InstanceManager.eSimpleInstancerType.eMinecartStraight];
            Mesh TrackMesh = InstanceManager.instance.maSimpleMeshes[(int)InstanceManager.eSimpleInstancerType.eMinecartStraight];
            TrackInstances.Init(TrackMesh, TrackMaterial);
            FreightTrackJunction.TrackMesh = TrackMesh;
            FreightTrackJunction.TrackMaterial = TrackMaterial;
            Material scrapmat = new Material(TrackMaterial);
            scrapmat.color = new Color(200/256f, 117/256f, 51/256f);
            FreightTrackJunction.ScrapTrackMat = scrapmat;
            ScrapTrack.StraightTrackMesh = TrackMesh;
            ScrapTrack.StraightTrackMaterial = scrapmat;
            Mesh ShrinkMesh = SetupMesh(TrackMesh);
            FreightTrackJunction.TrackMesh2 = ShrinkMesh;
        }
        return modRegistrationData;
    }

    Mesh SetupMesh(Mesh mesh)
    {
        Mesh newmesh = new Mesh();
        newmesh.vertices = mesh.vertices;
        newmesh.triangles = mesh.triangles;
        newmesh.uv = mesh.uv;
        newmesh.normals = mesh.normals;
        newmesh.colors = mesh.colors;
        newmesh.tangents = mesh.tangents;

        Vector3[] baseVertices = newmesh.vertices;
        var vertices = new Vector3[baseVertices.Length];
        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = baseVertices[i];
            vertex.x = vertex.x * 0.99f;
            vertex.y = vertex.y * 0.99f;
            vertex.z = vertex.z * 0.99f;
            vertices[i] = vertex;
        }
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return newmesh;
    }

    public override ModCreateSegmentEntityResults CreateSegmentEntity(ModCreateSegmentEntityParameters parameters)
    {
        ModCreateSegmentEntityResults result = new ModCreateSegmentEntityResults();

        if (parameters.Cube == TrackJunctionType)
        {
            parameters.ObjectType = SpawnableObjectEnum.Minecart_Track_Straight;
            result.Entity = new FreightTrackJunction(parameters);
        }
        else if (parameters.Cube == StationType)
        {
            parameters.ObjectType = SpawnableObjectEnum.Minecart_Track_LoadStation;
            result.Entity = new FreightCartStation(parameters);
        }
        else if (parameters.Cube == SystemMonitorType)
        {
            parameters.ObjectType = SpawnableObjectEnum.AutoBuilder;
            result.Entity = new FreightSystemMonitor(parameters);
        }
        else if (parameters.Cube == FactoryType)
        {
            parameters.ObjectType = SpawnableObjectEnum.Minecart_Track_Factory;
            result.Entity = new FreightCartFactory(parameters);
        }
        else if (parameters.Cube == TourStationType)
        {
            parameters.ObjectType = SpawnableObjectEnum.Minecart_Track_Factory;
            result.Entity = new TourCartStation(parameters);
        }
        else if (parameters.Cube == ScrapTrackType)
        {
            // No Object - rendered separately
            parameters.ObjectType = SpawnableObjectEnum.XXX;
            result.Entity = new ScrapTrack(parameters);
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
        if (parameters.MobKey == "steveman0.OreFreighter_T1")
            results.Mob = new FreightCartMob(FreightCartMob.eMinecartType.OreFreighter_T1, parameters);
        if (parameters.MobKey == "steveman0.OreFreighter_T2")
            results.Mob = new FreightCartMob(FreightCartMob.eMinecartType.OreFreighter_T2, parameters);
        if (parameters.MobKey == "steveman0.OreFreighter_T3")
            results.Mob = new FreightCartMob(FreightCartMob.eMinecartType.OreFreighter_T3, parameters);
        if (parameters.MobKey == "steveman0.OreFreighter_T4")
            results.Mob = new FreightCartMob(FreightCartMob.eMinecartType.OreFreighter_T4, parameters);
        if (parameters.MobKey == "steveman0.ScrapCartMK1")
            results.Mob = new FreightCartMob(FreightCartMob.eMinecartType.ScrapCartMK1, parameters);
        if (parameters.MobKey == "steveman0.FreightCartMK1")
            results.Mob = new FreightCartMob(FreightCartMob.eMinecartType.FreightCartMK1, parameters);
        if (parameters.MobKey == "steveman0.ScrapOreFreighterMK1")
            results.Mob = new FreightCartMob(FreightCartMob.eMinecartType.ScrapOreFreighterMK1, parameters);

        base.CreateMobEntity(parameters, results);
    }

    public override void LowFrequencyUpdate()
    {
        UpdateCounter++;
        if (FreightCartManager.instance == null)
            new FreightCartManager();
        else
            FreightCartManager.instance.UpdateMassInventory();
        if (UpdateCounter - LiveUpdateTime < 10)
            CartCheckin = true;
        else if (CartCheckin)
        {
            Debug.LogWarning("---------FREIGHT CART DEBUG------------\nLive carts checked in: " + LiveCarts.ToString());
            FloatingCombatTextManager.instance.QueueText(monitor.mnX, monitor.mnY + 1L, monitor.mnZ, 1f, "Total Freight Carts Active: " + ManagerSync.instance.CartCounter.ToString(), Color.yellow, 2f, 64f);
            CartCheckin = false;
            LiveCarts = 0;
        }
    }

    void LateUpdate()
    {
        if (PersistentSettings.mbHeadlessServer) return;
        TrackInstances.Render();
    }
}

