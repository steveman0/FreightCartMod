using UnityEngine;

public class FreightCartFactory : MachineEntity
{
    private int mnDepthScanned;
    private bool mbLinkedToGO;
    private Animation mAnimation;
    private Vector3 mForwards;
    public float mrSpawnDelay;
    private StorageMachineInterface[] maAttachedHoppers;
    public int mnNumAttachedValidHoppers;
    public int mnTotalHoppers;
    public bool mbCannotFindResource;

    public static int orefreighterT1 = ModManager.mModMappings.ItemsByKey["steveman0.OreFreighterT1"].ItemId;
    public static int orefreighterT2 = ModManager.mModMappings.ItemsByKey["steveman0.OreFreighterT2"].ItemId;
    public static int orefreighterT3 = ModManager.mModMappings.ItemsByKey["steveman0.OreFreighterT3"].ItemId;
    public static int orefreighterT4 = ModManager.mModMappings.ItemsByKey["steveman0.OreFreighterT4"].ItemId;
    public static int scrapcart = ModManager.mModMappings.ItemsByKey["steveman0.ScrapCartMK1"].ItemId;
    public static int scraporefreighter = ModManager.mModMappings.ItemsByKey["steveman0.ScrapOreFreighterMK1"].ItemId;



    public FreightCartFactory(ModCreateSegmentEntityParameters parameters)
      : base(parameters)
    {
        this.mbNeedsLowFrequencyUpdate = true;
        this.mbNeedsUnityUpdate = false;
        this.mForwards = SegmentCustomRenderer.GetRotationQuaternion(parameters.Flags) * Vector3.forward;
        this.mForwards.Normalize();
        this.maAttachedHoppers = new StorageMachineInterface[6];
    }

    public override void DropGameObject()
    {
        base.DropGameObject();
        this.mbLinkedToGO = false;
    }

    public override void UnityUpdate()
    {
        if (this.mbLinkedToGO || this.mWrapper == null || !this.mWrapper.mbHasGameObject)
            return;
        if (this.mWrapper.mGameObjectList == null)
            Debug.LogError((object)"MF missing game object #0?");
        if ((Object)this.mWrapper.mGameObjectList[0].gameObject == (Object)null)
            Debug.LogError((object)"MF missing game object #0 (GO)?");
        this.mAnimation = this.mWrapper.mGameObjectList[0].GetComponentInChildren<Animation>();
    }

    public override void LowFrequencyUpdate()
    {
        if (!WorldScript.mbIsServer)
            return;
        if ((double)this.mrSpawnDelay > 0.0)
        {
            this.mrSpawnDelay -= LowFrequencyThread.mrPreviousUpdateTimeStep;
        }
        else
        {
            if (WorldScript.meGameMode == eGameMode.eCreative)
            {
                this.mrSpawnDelay = 120f;
                //MobManager.instance.SpawnMob(MobType.Minecart, this.mSegment, this.mnX + (long)(int)this.mForwards.x, this.mnY + (long)(int)this.mForwards.y + 1L, this.mnZ + (long)(int)this.mForwards.z, new Vector3(0.0f, 0.0f, 0.0f), this.mForwards);
                MobManager.instance.SpawnMob(MobType.Mod, "steveman0.FreightCartMK1", this.mSegment, this.mnX, this.mnY + (long)this.mForwards.y + 1L, this.mnZ, new Vector3(0.0f, 0.0f, 0.0f), this.mForwards);
            }
            if (WorldScript.meGameMode != eGameMode.eSurvival || !WorldScript.mbIsServer)
                return;
            this.UpdateAttachedHoppers(false);
            this.mrSpawnDelay = 1f;
            int lnHopper = 0;
            while (lnHopper < this.mnNumAttachedValidHoppers && 
                !this.SearchAndSpawn(lnHopper, ItemEntries.MineCartT1, "steveman0.FreightCart_T1") && 
                !this.SearchAndSpawn(lnHopper, ItemEntries.MineCartT2, "steveman0.FreightCart_T2") && 
                !this.SearchAndSpawn(lnHopper, ItemEntries.MineCartT3, "steveman0.FreightCart_T3") && 
                !this.SearchAndSpawn(lnHopper, ItemEntries.MineCartT4, "steveman0.FreightCart_T4") &&
                !this.SearchAndSpawn(lnHopper, orefreighterT1, "steveman0.OreFreighter_T1") &&
                !this.SearchAndSpawn(lnHopper, orefreighterT2, "steveman0.OreFreighter_T2") &&
                !this.SearchAndSpawn(lnHopper, orefreighterT3, "steveman0.OreFreighter_T3") &&
                !this.SearchAndSpawn(lnHopper, orefreighterT4, "steveman0.OreFreighter_T4") &&
                !this.SearchAndSpawn(lnHopper, scrapcart, "steveman0.ScrapCartMK1") &&
                !this.SearchAndSpawn(lnHopper, scraporefreighter, "steveman0.ScrapOreFreighterMK1")) ++lnHopper;
        }
    }

    private bool SearchAndSpawn(int lnHopper, int lnItemID, string lMobType)
    {
        if (!this.maAttachedHoppers[lnHopper].TryExtractItems((StorageUserInterface)this, lnItemID, 1))
            return false;
        //Achievements.UnlockAchievementDelayed(Achievements.eAchievements.eNotaRollercoaster);
        this.mrSpawnDelay = 15f;
        //MobManager.instance.SpawnMob(lMobType, this.mSegment, this.mnX, this.mnY + (long)(int)this.mForwards.y + 1L, this.mnZ, new Vector3(0.0f, 0.0f, 0.0f), this.mForwards);
        MobManager.instance.SpawnMob(MobType.Mod, lMobType, this.mSegment, this.mnX, this.mnY + (long)this.mForwards.y + 1L, this.mnZ, new Vector3(0.0f, 0.0f, 0.0f), this.mForwards);

        if (!FloatingCombatTextManager.Initialised)
            ;
        return true;
    }

    private void UpdateAttachedHoppers(bool lbInput)
    {
        int index1 = 0;
        this.mnTotalHoppers = 0;
        this.mnNumAttachedValidHoppers = 0;
        for (int index2 = 0; index2 < 6; ++index2)
        {
            this.maAttachedHoppers[index2] = (StorageMachineInterface)null;
            long x = this.mnX;
            long y = this.mnY;
            long z = this.mnZ;
            if (index2 == 0)
                --x;
            if (index2 == 1)
                ++x;
            if (index2 == 2)
                --y;
            if (index2 == 3)
                ++y;
            if (index2 == 4)
                --z;
            if (index2 == 5)
                ++z;
            Segment segment = this.AttemptGetSegment(x, y, z);
            if (segment != null)
            {
                StorageMachineInterface machineInterface = segment.SearchEntity(x, y, z) as StorageMachineInterface;
                if (machineInterface != null)
                {
                    ++this.mnTotalHoppers;
                    eHopperPermissions permissions = machineInterface.GetPermissions();
                    if (permissions != eHopperPermissions.Locked && (lbInput || permissions != eHopperPermissions.AddOnly) && ((!lbInput || permissions != eHopperPermissions.RemoveOnly) && (!lbInput || !machineInterface.IsFull())) && (lbInput || !machineInterface.IsEmpty()))
                    {
                        this.maAttachedHoppers[index1] = machineInterface;
                        ++index1;
                    }
                }
            }
        }
        this.mnNumAttachedValidHoppers = index1;
    }

    public override void OnUpdateRotation(byte newFlags)
    {
        base.OnUpdateRotation(newFlags);
        this.mFlags = newFlags;
        this.mForwards = SegmentCustomRenderer.GetRotationQuaternion(this.mFlags) * Vector3.forward;
        this.mForwards.Normalize();
    }

    //private bool IsTrackPiece(long checkX, long checkY, long checkZ)
    //{
    //    Segment segment;
    //    if (this.mFrustrum != null)
    //    {
    //        segment = this.AttemptGetSegment(checkX, checkY, checkZ);
    //        if (segment == null)
    //            return false;
    //    }
    //    else
    //    {
    //        segment = WorldScript.instance.GetSegment(checkX, checkY, checkZ);
    //        if (segment == null || !segment.mbInitialGenerationComplete || segment.mbDestroyed)
    //        {
    //            this.mrSpawnDelay = 10f;
    //            return false;
    //        }
    //    }
    //    segment.GetCube(checkX, checkY, checkZ);
    //    return true;
    //}

    public override HoloMachineEntity CreateHolobaseEntity(Holobase holobase)
    {
        HolobaseEntityCreationParameters parameters = new HolobaseEntityCreationParameters((SegmentEntity)this);
        parameters.AddVisualisation(holobase.mPreviewCube).Color = Color.green;
        return holobase.CreateHolobaseEntity(parameters);
    }
}
