using System.Collections.Generic;
using UnityEngine;
using System.Reflection;


public class FreightCartMod : FortressCraftMod
{
    public override ModRegistrationData Register()
    {
        ModRegistrationData modRegistrationData = new ModRegistrationData();
        modRegistrationData.RegisterEntityHandler("steveman0.FreightCartStation");
        modRegistrationData.RegisterMobHandler("steveman0.FreightCart_T1");
        modRegistrationData.RegisterMobHandler("steveman0.FreightCart_T2");
        modRegistrationData.RegisterMobHandler("steveman0.FreightCart_T3");
        modRegistrationData.RegisterMobHandler("steveman0.FreightCart_T4");
        modRegistrationData.RegisterMobHandler("steveman0.FreightCartMK1");
        modRegistrationData.RegisterEntityHandler("steveman0.FreightSystemMonitor");
        modRegistrationData.RegisterEntityHandler("steveman0.FreightCartFactory");

        //I have to use this to register the mob because the mob registration above doesn't work.  See the forum with details.
        //ModMobMap modmap = new ModMobMap();
        //modmap.Key = "steveman0.FreightCartMK1";
        //modmap.Value = ModManager.mModMappings.MobsByKey.Count + 1;
        //ModManager.mModMappings.MobsByKey.Add(modmap.Key, modmap);
        //ModManager.mModMappings.MobsByNumber.Add(modmap.Value, modmap);

        //Dictionary<string, MobHandlerRegistration> x = new Dictionary<string, MobHandlerRegistration>();
        //x = (Dictionary<string, MobHandlerRegistration>)x.GetType().GetField("mMobHandlersByKey", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(x);
        //x.Add("steveman0.FreightCartMK1", new MobHandlerRegistration("steveman0.FreightCartMK1", this));

        new FreightCartManager();
        Debug.Log("Freight Cart Mod V3 registered");

        return modRegistrationData;
    }

    public override ModCreateSegmentEntityResults CreateSegmentEntity(ModCreateSegmentEntityParameters parameters)
    {
        ModCreateSegmentEntityResults result = new ModCreateSegmentEntityResults();

        foreach (ModCubeMap cubeMap in ModManager.mModMappings.CubeTypes)
        {
            if (cubeMap.CubeType == parameters.Cube)
            {
                if (cubeMap.Key.Equals("steveman0.FreightCartStation"))
                    result.Entity = new FreightCartStation(parameters.Segment, parameters.X, parameters.Y, parameters.Z, parameters.Cube, parameters.Flags, parameters.Value, parameters.LoadFromDisk);
                if (cubeMap.Key.Equals("steveman0.FreightSystemMonitor"))
                    result.Entity = new FreightSystemMonitor(parameters.Segment, parameters.X, parameters.Y, parameters.Z, parameters.Cube, parameters.Flags, parameters.Value, parameters.LoadFromDisk);
                if (cubeMap.Key.Equals("steveman0.FreightCartFactory"))
                    result.Entity = new FreightCartFactory(parameters);
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

        if (parameters.MobKey == "steveman0.FreightCartMK1")
            results.Mob = new FreightCartMob(FreightCartMob.eMinecartType.FreightCartMK1, parameters);

        base.CreateMobEntity(parameters, results);
    }
}

