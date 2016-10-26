using System;
using UnityEngine;
using FortressCraft.Community.Utilities;
using System.IO;

public class TourCartStation : MachineEntity
{
    private bool mbLinkedToGO;
    private Animation mAnimation;
    private Vector3 mForwards;
    private GameObject HoloPreview;
    public StorageMachineInterface hopper;
    public string StationName;
    public FreightTrackNetwork TrackNetwork;
    public TourStationWindow MachineWindow = new TourStationWindow();
    public FreightTrackJunction ClosestJunction;
    public int JunctionDirection = -1;
    public static int TourBasicID = ModManager.mModMappings.ItemsByKey["steveman0.FreightTourBasic"].ItemId;
    private bool RotationUpdated;

    public TourCartStation(ModCreateSegmentEntityParameters parameters)
      : base(parameters)
    {
        this.mbNeedsUnityUpdate = true;
        this.mbNeedsLowFrequencyUpdate = true;
        this.mForwards = SegmentCustomRenderer.GetRotationQuaternion(parameters.Flags) * Vector3.forward;
        this.mForwards.Normalize();
    }

    public override string GetPopupText()
    {
        UIUtil.HandleThisMachineWindow(this, this.MachineWindow);

        string str1 = "Tour Cart Station\n";

        if (!string.IsNullOrEmpty(this.StationName))
        {
            str1 += "Location: " + this.StationName + "\n";

            long disX = WorldScript.mLocalPlayer.mnWorldX;
            long disY = WorldScript.mLocalPlayer.mnWorldY;
            long disZ = WorldScript.mLocalPlayer.mnWorldZ;

            Vector3 vector = new Vector3(disX, disY, disZ) - this.mPosition;
            if (vector.sqrMagnitude < 256f && this.TrackNetwork != null)
            {
                str1 += "Press E to access the list of destinations\n";
            }
        }
        else
            str1 += "Press E to set this location's name\n";
        if (this.hopper != null)
            str1 += this.hopper.CountItems(111).ToString() + "x Tour Carts available\n";
        if (this.TrackNetwork != null)
            str1 += "Track Network ID: " + this.TrackNetwork.NetworkID.ToString() + "\n";

        return str1;
    }

    public override void DropGameObject()
    {
        base.DropGameObject();
        this.mbLinkedToGO = false;
    }

    public override void LowFrequencyUpdate()
    {
        if (this.hopper == null)
            this.UpdateAttachedHoppers();
        if (this.ClosestJunction != null && this.TrackNetwork != null && this.ClosestJunction.TrackNetwork != this.TrackNetwork)
        {
            if (this.TrackNetwork.TourCartStations.ContainsKey(this.StationName))
                this.TrackNetwork.TourCartStations.Remove(this.StationName);
            this.TrackNetwork = null;
            this.ClosestJunction.ConnectedSegments[this.JunctionDirection] = null;
            this.ClosestJunction.ConnectedJunctions[this.JunctionDirection] = null;
            this.ClosestJunction = null;
            this.JunctionDirection = -1;
        }
    }

    public void TravelTo(TourCartStation station, FreightTrackJunction start)
    {
        if (station != null && station.JunctionDirection != -1 && station.ClosestJunction != null)
        {
            string mobtype = this.GetSpawnedCartType();
            if (string.IsNullOrEmpty(mobtype))
                return;
            FreightCartMob tourcart = MobManager.instance.SpawnMob(MobType.Mod, mobtype, this.mSegment, this.mnX, this.mnY + (long)this.mForwards.y + 1L, this.mnZ, new Vector3(0.0f, 0.0f, 0.0f), this.mForwards) as FreightCartMob;
            ManagerSync.TourCart = tourcart;
            tourcart.DestinationJunction = station.ClosestJunction;
            tourcart.DestinationDirection = station.JunctionDirection;
            tourcart.JunctionRoute = station.TrackNetwork.RouteFind(start, station.ClosestJunction);
            tourcart.NextJunction = start;
        }
    }

    private string GetSpawnedCartType()
    {
        if ((WorldScript.mLocalPlayer.mInventory.GetItemCount(111) > 0 && WorldScript.mLocalPlayer.mInventory.RemoveItem(111, 1) == 1) || (this.hopper != null && this.hopper.TryExtractItems((StorageUserInterface)this, ItemEntries.TourCart, 1)))
            return "steveman0.FreightCart_Tour";
        else if ((WorldScript.mLocalPlayer.mInventory.GetItemCount(TourBasicID) > 0 && WorldScript.mLocalPlayer.mInventory.RemoveItem(TourBasicID, 1) == 1) || (this.hopper != null && this.hopper.TryExtractItems((StorageUserInterface)this, TourBasicID, 1)))
            return "steveman0.FreightCart_TourBasic";
        return string.Empty;
    }

    public override void UnityUpdate()
    {
        UIUtil.DisconnectUI(this);
        if (this.RotationUpdated)
        {
            this.HoloPreview.transform.forward = this.mForwards;
            this.RotationUpdated = false;
        }
        if (this.mbLinkedToGO || this.mWrapper == null || !this.mWrapper.mbHasGameObject)
            return;
        if (this.mWrapper.mGameObjectList == null)
            Debug.LogError((object)"MF missing game object #0?");
        if (this.mWrapper.mGameObjectList[0].gameObject == null)
            Debug.LogError((object)"MF missing game object #0 (GO)?");
        this.mAnimation = this.mWrapper.mGameObjectList[0].GetComponentInChildren<Animation>();
        this.mAnimation.Stop();
        //this.mAnimation.enabled = false;
        //this.mAnimation.gameObject.SetActive(false);


        int index = (int)ItemEntry.mEntries[111].Object;
        this.HoloPreview = (GameObject)UnityEngine.Object.Instantiate(SpawnableObjectManagerScript.instance.maSpawnableObjects[index], this.mWrapper.mGameObjectList[0].gameObject.transform.position + new Vector3(0.0f, 0.75f, 0.0f), Quaternion.identity);
        this.HoloPreview.transform.parent = this.mWrapper.mGameObjectList[0].gameObject.transform;
        if (this.HoloPreview.GetComponent<Renderer>() != null)
        {
            this.HoloPreview.GetComponent<Renderer>().material = PrefabHolder.instance.HoloPreviewMaterial;
            this.HoloPreview.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            this.HoloPreview.GetComponent<Renderer>().receiveShadows = false;
        }
        this.HoloPreview.gameObject.AddComponent<RotateConstantlyScript>();
        this.HoloPreview.gameObject.GetComponent<RotateConstantlyScript>().YRot = 1f;
        this.HoloPreview.gameObject.GetComponent<RotateConstantlyScript>().XRot = 0.35f;
        this.HoloPreview.SetActive(true);
        this.mbLinkedToGO = true;
    }

    private void UpdateAttachedHoppers()
    {
        for (int index2 = 0; index2 < 6; ++index2)
        {
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
                    eHopperPermissions permissions = machineInterface.GetPermissions();
                    if (permissions != eHopperPermissions.Locked && permissions != eHopperPermissions.AddOnly && permissions != eHopperPermissions.RemoveOnly)
                    {
                        this.hopper= machineInterface;
                    }
                }
            }
        }
    }

    public override void OnUpdateRotation(byte newFlags)
    {
        base.OnUpdateRotation(newFlags);
        this.mFlags = newFlags;
        this.mForwards = SegmentCustomRenderer.GetRotationQuaternion(this.mFlags) * Vector3.forward;
        this.mForwards.Normalize();
        this.RotationUpdated = true;
    }

    public override void OnDelete()
    {
        if (this.TrackNetwork != null && !string.IsNullOrEmpty(this.StationName) && this.TrackNetwork.TourCartStations.ContainsKey(this.StationName))
            this.TrackNetwork.TourCartStations.Remove(this.StationName);
        if (this.ClosestJunction != null && this.JunctionDirection != -1)
        {
            this.ClosestJunction.ConnectedJunctions[this.JunctionDirection] = null;
            this.ClosestJunction.ConnectedSegments[this.JunctionDirection] = null;
            this.ClosestJunction.LinkStatusDirty = true;
        }
    }

    public override HoloMachineEntity CreateHolobaseEntity(Holobase holobase)
    {
        HolobaseEntityCreationParameters parameters = new HolobaseEntityCreationParameters((SegmentEntity)this);
        parameters.AddVisualisation(holobase.mPreviewCube).Color = Color.green;
        return holobase.CreateHolobaseEntity(parameters);
    }

    public override bool ShouldSave()
    {
        return true;
    }

    public override int GetVersion()
    {
        return 1;
    }

    public override void Write(BinaryWriter writer)
    {
        if (string.IsNullOrEmpty(this.StationName))
            writer.Write(string.Empty);
        else
            writer.Write(this.StationName);
    }

    public override void Read(BinaryReader reader, int entityVersion)
    {
        this.StationName = reader.ReadString();
    }
}
