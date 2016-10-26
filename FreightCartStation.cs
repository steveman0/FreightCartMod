using System.Reflection;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using FortressCraft.Community.Utilities;
using System.IO;

public class FreightCartStation : MachineEntity
{
    public int StationID;
    public string StationName;
    public static int FreightStationIDs = 0;
    public float mrMaxPower = 100f;
    public float mrMaxTransferRate = 100f;
    public const int BOOST_VALUE = 2;
    public const int UNLOAD_VALUE = 3;
    public const int LOAD_VALUE = 4;
    private int mnDepthScanned;
    private bool mbLinkedToGO;
    private Animation mAnimation;
    private Vector3 mForwards;
    private Vector3 mUnityPosition;
    public AudioSource mAudioSource;
    public bool mbWaitForFullLoad;
    private GameObject LoaderBase;
    private MaterialPropertyBlock mMPB;
    private float mrGlow;
    private FreightCartStation.eDoorStatus mDoorStatus;
    private float mrSpawnDelay;
    public float mrCartOnUs;
    public int mnLastCartQuantity;
    public int mnCartsSeen;
    public float mrLastCartLeavTimer;
    private int mnUpdates;
    private bool mbPlayLoadAnim;
    public float mrCurrentPower;

    public long cratex;
    public long cratey;
    public long cratez;
    public List<FreightRegistry> localregistry;
    private bool RegWriteRequired = false;
    private string CachedInvName;

    public MassStorageCrate LocalCrate;
    public MassStorageCrate massStorageCrate;
    public MassInventory ConnectedInventory;
    public string NetworkID;
    //FreightCartWindow machineWindow = new FreightCartWindow();
    public int UIdelay = 0;
    public bool UILock = false;
    private float popuptimer = 0f;
    public List<FreightRegistry> LocalDeficits = new List<FreightRegistry>();
    public List<FreightRegistry> LocalSurplus = new List<FreightRegistry>();
    public bool OfferAll = false;

    public FreightTrackJunction ClosestJunction;
    public int JunctionDirection = -1;
    public int AssignedCarts;
    public int AvailableCarts;
    public int CartTier = 0;
    public List<FreightCartMob> CartList = new List<FreightCartMob>();

    //public MobEntity SpawnedCart;

    public FreightCartStation(ModCreateSegmentEntityParameters parameters)
        : base(parameters)
    {
        this.mbNeedsLowFrequencyUpdate = true;
        this.mbNeedsUnityUpdate = true;
        this.mForwards = SegmentCustomRenderer.GetRotationQuaternion(parameters.Flags) * Vector3.forward;
        this.mForwards.Normalize();
        this.mbWaitForFullLoad = false;
        this.StationID = FreightStationIDs;
        FreightStationIDs++;
        //this.maAttachedHoppers = new StorageHopper[6];
    }

    public override void SpawnGameObject()
    {
        SpawnableObjectEnum modelType = SpawnableObjectEnum.Num;
        modelType = SpawnableObjectEnum.Minecart_Track_LoadStation;

        //if ((int)this.mValue == 2)
        //    modelType = SpawnableObjectEnum.Minecart_Track_Boost;
        //if ((int)this.mValue == 3)
        //    modelType = SpawnableObjectEnum.Minecart_Track_UnloadStation;
        //if ((int)this.mValue == 4)
        //    modelType = SpawnableObjectEnum.Minecart_Track_LoadStation;
        if (modelType == SpawnableObjectEnum.Num)
            Debug.LogError((object)"What type of station is this!?");
        this.mWrapper = SpawnableObjectManagerScript.instance.SpawnObject(eGameObjectWrapperType.Entity, modelType, this.mnX, this.mnY, this.mnZ, this.mFlags, (object)null);
    }

    public override void DropGameObject()
    {
        base.DropGameObject();
        this.mbLinkedToGO = false;
        this.mAnimation = (Animation)null;
        this.LoaderBase = (GameObject)null;
        this.mAudioSource = (AudioSource)null;
    }

    public override string GetPopupText()
    {
        //if (this.chatwindowclose)
        //{
        //    UIManager.instance.mChatPanel.StopEdit();
        //    this.chatwindowclose = false;
        //}

        //UIUtil.HandleThisMachineWindow(this, this.machineWindow);

        string str1 = "Freight Cart Station (" + (!string.IsNullOrEmpty(this.StationName) ? this.StationName : ("ID: " + this.StationID.ToString())) + ")";
        if (string.IsNullOrEmpty(this.NetworkID))
            str1 += " - NO NETWORK\nPress E to configure this station\n";
        else
            str1 += " - " + this.NetworkID + "\n";

        string str2 = "";
        string str3 = "";
        if (!string.IsNullOrEmpty(this.NetworkID))
        {
            if (this.popuptimer < 0 && this.massStorageCrate != null)
            {
                this.LocalDeficits = FreightCartManager.instance.GetLocalDeficit(this.NetworkID, this.massStorageCrate);
                this.LocalSurplus = FreightCartManager.instance.GetLocalSurplus(this.NetworkID, this.massStorageCrate);
                this.popuptimer = 1.5f;
            }
            else
                this.popuptimer -= Time.deltaTime;

            //Debug.Log("Deficit count: " + this.LocalDeficits.Count);
            if (this.LocalDeficits.Count <= 0)
                str2 = "This storage is fully stocked!\n";
            else
                str2 = "Top requests for this storage:\n";
            for (int index = 0; index < this.LocalDeficits.Count; index++)
            {
                //Debug.Log("Are we trying to print deficits? : " + index + " the actual deficit: " + this.LocalDeficits[index].Deficit);
                if (this.LocalDeficits[index].Deficit != 0)
                    str2 += (index + 1).ToString() + ") " + this.LocalDeficits[index].Deficit.ToString("N0") + "x " + ItemManager.GetItemName(this.LocalDeficits[index].FreightItem) + "\n";
            }

            if(this.LocalSurplus.Count <= 0)
                str3 = "This storage has nothing to offer!\n";
            else
                str3 = "Top offerings for this storage:\n";
            for (int index = 0; index < this.LocalSurplus.Count; index++)
            {
                //Debug.Log("Are we trying to print surplus? : " + index);
                if (this.LocalSurplus[index].Surplus != 0)
                    str3 += (index + 1).ToString() + ") " + this.LocalSurplus[index].Surplus.ToString("N0") + "x " + ItemManager.GetItemName(this.LocalSurplus[index].FreightItem) + "\n";
            }
        }


        //Freight registry copy+paste
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetButtonDown("Extract") && this.massStorageCrate != null && !string.IsNullOrEmpty(this.NetworkID))
            FreightCartWindow.CopyFreight(this);
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetButtonDown("Store") && this.massStorageCrate != null && !string.IsNullOrEmpty(this.NetworkID) && FreightCartManager.instance.CopiedFreightStation != null)
            FreightCartWindow.PasteFreight(this);

        string str4 = "";
        string str5 = "\n";
        string str6 = "\n";

        //Handle text entry for item searching
        //if (this.TextEntryMode)
        //{
        //    str2 = "";
        //    str3 = "";
        //    this.CursorTimer += Time.deltaTime;
        //    foreach (char c in Input.inputString)
        //    {
        //        if (c == "\b"[0])  //Backspace
        //        {
        //            if (this.EntryString.Length != 0)
        //                this.EntryString = this.EntryString.Substring(0, this.EntryString.Length - 1);
        //        }
        //        else if (c == "\n"[0] || c == "\r"[0]) //Enter or Return
        //        {
        //            string oldid = this.NetworkID;
        //            if (!string.IsNullOrEmpty(this.EntryString))
        //            {
        //                if (this.massStorageCrate != null)
        //                    FreightCartManager.instance.CopyFreightEntries(oldid, this.massStorageCrate);
        //                if (!string.IsNullOrEmpty(oldid))
        //                    FreightCartManager.instance.RemoveStationReg(this);
        //                this.NetworkID = this.EntryString;
        //                FreightCartManager.instance.TryRegisterStation(this);
        //                if (this.massStorageCrate != null)
        //                    FreightCartManager.instance.PasteFreightEntries(this.NetworkID, this.massStorageCrate);
        //                //if (this.ConnectedInventory != null)
        //                //{
        //                //    this.ConnectedInventory.RemoveNetwork(this, oldid);
        //                //    this.ConnectedInventory.ConnectedStations.Add(this);
        //                //}
        //                //else
        //                //    FreightCartManager.instance.RemoveExtraNetwork(oldid);
        //            }
        //            FreightCartManager.instance.AddNetwork(this.NetworkID);
        //            this.TextEntryMode = false;
        //            this.EntryString = "";
        //            UIManager.RemoveUIRules("TextEntry");
        //            this.chatwindowclose = true;
        //        }
        //        else
        //            this.EntryString += c;
        //        if (c == "p"[0] || c == "P"[0])
        //        {

        //        }
        //    }
        //    str3 = "Network naming - enter freight network name.\n";
        //    str4 = "Press Enter to submit.\n";
        //    str5 = "Press ESC to cancel.\n";
        //    str6 = "Network: " + this.EntryString;

        //    if ((int)this.CursorTimer % 2 == 1)
        //        str6 += "_";

        //    str6 += "\n";

        //    //Hide unwanted UI when typing
        //    if (Input.GetKeyDown(KeyCode.P))
        //        UIManager.instance.UnpauseGame();
        //    if (Input.GetKeyDown(KeyCode.H))
        //        UIManager.instance.mHelpPanel.Hide();

        //    if (Input.GetKeyDown(KeyCode.Escape))
        //    {
        //        this.TextEntryMode = false;
        //        this.EntryString = "";
        //        UIManager.RemoveUIRules("TextEntry");
        //        UIManager.instance.UnpauseGame();
        //    }

        //}


        //Activate text entry mode
        //if (Input.GetButtonDown("Store") && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && !this.TextEntryMode)
        //{
        //    this.TextEntryMode = true;
        //    this.EntryString = "";
        //    this.CursorTimer = 0.0f;
        //    UIManager.AddUIRules("TextEntry", UIRules.RestrictMovement | UIRules.RestrictLooking | UIRules.RestrictBuilding | UIRules.RestrictInteracting | UIRules.SetUIUpdateRate);
        //}

        //if (Input.GetKeyDown(KeyCode.LeftAlt))
        //{
        //    //Dictionary<string, MobHandlerRegistration> x = new Dictionary<string, MobHandlerRegistration>();
        //    //x = (Dictionary<string, MobHandlerRegistration>)x.GetType().GetField("mMobHandlersByKey", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(x);
        //    ////x.Add("steveman0.FreightCartMK1", new MobHandlerRegistration("steveman0.FreightCartMK1", this));
        //    //foreach (KeyValuePair<string, >)
        //    //List<string> keys = ModManager.mModMappings.MobsByKey.Keys.ToList();
        //    //foreach (string mobkeys in keys)
        //    //{
        //    //    Debug.Log("Mob key: " + mobkeys);
        //    //}
        //    //List<string> keys2 = ModManager.mModMappings.CubesByKey.Keys.ToList();
        //    //foreach (string cubekeys in keys2)
        //    //{
        //    //    Debug.Log("Cube key: " + cubekeys);
        //    //}

        //    MobManager.instance.SpawnMob(MobType.Mod, "steveman0.FreightCartMK1", this.mSegment, this.mnX, this.mnY + (long)this.mForwards.y + 1L, this.mnZ, new Vector3(0.0f, 0.0f, 0.0f), this.mForwards);
        //}

        return str1 + str2 + str3 + str4 + str5 + str6;
    }

    public override void UnityUpdate()
    {
        //UIUtil.DisconnectUI(this);
        //if (this.SpawnedCart != null && SpawnedCart.mWrapper != null && SpawnedCart.mWrapper.mGameObjectList.Count != 0)
        //{
        //    //Component[] obj = SpawnedCart.mWrapper.mGameObjectList[0].gameObject.GetComponentsInChildren(typeof(Component));
        //    //foreach (Component x in obj)
        //    //{
        //    //    //Debug.Log("mWrapper Object: " + x + " name: " + x.name);
        //    //    if (x.name == "Minecart_Unity")
        //    //        x.gameObject.SetActive(false);
        //    //}

        //    //GameObject unscript = Extensions.Search(SpawnedCart.mWrapper.mGameObjectList[0].gameObject.transform, "Minecart_Unity").gameObject;
        //    //unscript.SetActive(false);
        //    SpawnedCart.mWrapper.mGameObjectList[0].AddComponent<FreightCartUnity>();
        //    SpawnedCart.mWrapper.mGameObjectList[0].gameObject.SetActive(true);
        //    SpawnedCart = null;
        //}
        if (!this.mbLinkedToGO)
        {
            if (this.mWrapper == null || !this.mWrapper.mbHasGameObject)
                return;
            if (this.mWrapper.mGameObjectList == null)
                Debug.LogError((object)"Minecart Station missing game object #0?");
            if (this.mWrapper.mGameObjectList[0].gameObject == null)
                Debug.LogError((object)"Minecart Station missing game object #0 (GO)?");
            this.mUnityPosition = this.mWrapper.mGameObjectList[0].gameObject.transform.position;
            this.mMPB = new MaterialPropertyBlock();
            this.mbPlayLoadAnim = false;
            //if ((int)this.mValue == 4)
                this.LoaderBase = Extensions.Search(this.mWrapper.mGameObjectList[0].gameObject.transform, "Loader Base").gameObject;
            //if ((int)this.mValue == 3)
            //    this.mAudioSource = Extensions.Search(this.mWrapper.mGameObjectList[0].gameObject.transform, "AudioSource").GetComponent<AudioSource>();
            this.mAnimation = this.mWrapper.mGameObjectList[0].GetComponentInChildren<Animation>();
            this.mbLinkedToGO = true;
        }

        this.mrGlow += (this.mrCartOnUs - this.mrGlow) * Time.deltaTime;
        if ((double)this.mrGlow < -0.00999999977648258)
            this.mrGlow = -0.01f;
        this.mMPB.SetFloat("_GlowMult", this.mrGlow * 8f);
        this.LoaderBase.GetComponent<Renderer>().SetPropertyBlock(this.mMPB);
        if (this.mbPlayLoadAnim && !this.mAnimation.isPlaying)
        {
            this.mAnimation["Hoisting"].speed = 4f;
            this.mAnimation.Play("Hoisting");
            this.mbPlayLoadAnim = false;
            AudioSoundEffectManager.instance.PlayPositionEffect(AudioSoundEffectManager.instance.MinecartStationLoadAddBlock, this.mUnityPosition, 1f, 8f);
        }
        //if ((int)this.mValue != 3)
        //    return;
        //if ((double)this.mrCartOnUs <= 0.0)
        //{
        //    if (this.mDoorStatus == FreightCartStation.eDoorStatus.Closed)
        //        return;
        //    this.mAnimation.Play("Close Doors");
        //    this.mDoorStatus = FreightCartStation.eDoorStatus.Closed;
        //    AudioSoundEffectManager.instance.PlayPositionEffect(AudioSoundEffectManager.instance.MinecartStationUnLoadFinish, this.mUnityPosition, 1f, 16f);
        //}
        //else
        //{
        //    if (this.mDoorStatus == FreightCartStation.eDoorStatus.Open)
        //        return;
        //    this.mAnimation.Play("Open Doors");
        //    this.mDoorStatus = FreightCartStation.eDoorStatus.Open;
        //    this.mAudioSource.Play();
        //    AudioSoundEffectManager.instance.PlayPositionEffect(AudioSoundEffectManager.instance.MinecartStationUnloadStart, this.mUnityPosition, 1f, 16f);
        //}
    }

    //private void UpdateAttachedHoppers(bool lbInput)
    //{
    //    int index1 = 0;
    //    this.mnTotalHoppers = 0;
    //    this.mnNumAttachedValidHoppers = 0;
    //    for (int index2 = 0; index2 < 6; ++index2)
    //    {
    //        this.maAttachedHoppers[index2] = (StorageHopper)null;
    //        long x = this.mnX;
    //        long y = this.mnY;
    //        long z = this.mnZ;
    //        if (index2 == 0)
    //            --x;
    //        if (index2 == 1)
    //            ++x;
    //        if (index2 == 2)
    //            --y;
    //        if (index2 == 3)
    //            ++y;
    //        if (index2 == 4)
    //            --z;
    //        if (index2 == 5)
    //            ++z;
    //        Segment segment = this.AttemptGetSegment(x, y, z);
    //        if (segment != null && (int)segment.GetCube(x, y, z) == 505)
    //        {
    //            ++this.mnTotalHoppers;
    //            StorageHopper storageHopper = segment.FetchEntity(eSegmentEntity.StorageHopper, x, y, z) as StorageHopper;
    //            if (storageHopper != null && storageHopper.mPermissions != eHopperPermissions.Locked && (lbInput || storageHopper.mPermissions != eHopperPermissions.AddOnly) && ((!lbInput || storageHopper.mPermissions != eHopperPermissions.RemoveOnly) && (!lbInput || storageHopper.mnStorageFree > 0)) && (lbInput || storageHopper.mnStorageUsed != 0))
    //            {
    //                this.maAttachedHoppers[index1] = storageHopper;
    //                ++index1;
    //            }
    //        }
    //    }
    //    this.mnNumAttachedValidHoppers = index1;
    //}

    public override void LowFrequencyUpdate()
    {
        //++this.mnUpdates;
        this.MassStorageChecks();
        this.mrCartOnUs -= LowFrequencyThread.mrPreviousUpdateTimeStep;
        //if (this.mnNumAttachedValidHoppers == 0 && this.mnUpdates % 10 == 0 && (double)this.mDistanceToPlayer < 64.0)
        //{
        //    //if ((int)this.mValue == 4)
        //    //    this.UpdateAttachedHoppers(false);
        //    //if ((int)this.mValue == 3)
        //    //    this.UpdateAttachedHoppers(true);
        //}
        //if (this.mrCartOnUs <= 0.0)
        //    return;
        //if (this.mProferredItem == null && (int)this.mValue == 4)
        //{
        //    //this.UpdateAttachedHoppers(false);
        //    if (this.mnNumAttachedValidHoppers > 0)
        //    {
        //        this.mProferredItem = this.maAttachedHoppers[0].RemoveFirstInventoryItemOrDecrementStack();
        //        this.mbPlayLoadAnim = true;
        //        this.maAttachedHoppers[0].RequestImmediateNetworkUpdate();
        //    }
        //}
        //if (this.mProferredItem == null || (int)this.mValue != 3)
        //    return;
        //this.UpdateAttachedHoppers(true);
        //if (this.mnNumAttachedValidHoppers <= 0)
        //    return;
        //this.maAttachedHoppers[0].AddItem(this.mProferredItem);
        //this.maAttachedHoppers[0].RequestImmediateNetworkUpdate();
        //this.mProferredItem = (ItemBase)null;

        //if (this.UIdelay < 0 && UIManager.AllowMovement)
        //{
        //    GenericMachinePanelScript panel = GenericMachinePanelScript.instance;
        //    GenericMachineManager manager2 = typeof(GenericMachinePanelScript).GetField("manager", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GenericMachinePanelScript.instance) as GenericMachineManager;
        //    manager2.windows[eSegmentEntity.Mod] = null;
        //    panel.currentWindow = null;
        //    panel.targetEntity = null;
        //    panel.gameObject.SetActive(false);
        //    panel.Background_Panel.SetActive(false);
        //}
    }

    public void MassStorageChecks()
    {
        if (FreightCartManager.instance == null)
            return;
        //No center or local crate -> most likely only to occur on start 
        if (this.massStorageCrate == null && this.LocalCrate == null)
        {
            this.SearchForCrateNeighbours(this.mnX, this.mnY, this.mnZ);
            this.CheckNetworkReg();
            return;
        }

        //No center crate but we have a local crate?  Registry probably lost...
        if ((this.massStorageCrate == null) && this.LocalCrate != null)
        {
            this.massStorageCrate = this.LocalCrate.GetCenter();
            this.CheckNetworkReg();
            return;
        }

        //Center crate is gone but still have local crate
        if (this.massStorageCrate != null && this.massStorageCrate.mbDelete && this.LocalCrate != null && this.massStorageCrate != this.LocalCrate)
        {
            //Not switched to a new center crate yet
            if (this.LocalCrate.GetCenter() == this.massStorageCrate)
                return;
            else if (FreightCartManager.instance.InventoryExists(this.LocalCrate.GetCenter()) != null) //linked to a new center that already exists
            {
                this.ConnectedInventory = null;
                this.massStorageCrate = this.LocalCrate.GetCenter();
                return;
            }
            else if (!string.IsNullOrEmpty(this.NetworkID)) //new center isn't registered so copy old to new
            {
                FreightCartManager.instance.ReassignFreightRegistry(this.NetworkID, this.massStorageCrate, this.LocalCrate.GetCenter());
                this.massStorageCrate = this.LocalCrate.GetCenter();
                return;
            }
        }

        //Deal with split/merged mass storage cases
        if (this.massStorageCrate != null && this.LocalCrate != null && this.massStorageCrate != this.LocalCrate.GetCenter())
        {
            if (FreightCartManager.instance.InventoryExists(this.LocalCrate.GetCenter()) != null) //linked to a new center that already exists
            {
                this.ConnectedInventory = null;
                this.massStorageCrate = this.LocalCrate.GetCenter();
                return;
            }
            else //new center isn't registered so copy old to new
            {
                if (this.massStorageCrate != null && this.LocalCrate != null && !string.IsNullOrEmpty(this.NetworkID))
                {
                    FreightCartManager.instance.ReassignFreightRegistry(this.NetworkID, this.massStorageCrate, this.LocalCrate.GetCenter());
                    this.massStorageCrate = this.LocalCrate.GetCenter();
                    return;
                }
            }
        }
        if (this.massStorageCrate != null)
        {
            if (this.massStorageCrate.mnX == this.cratex && this.massStorageCrate.mnY == this.cratey && this.massStorageCrate.mnZ == this.cratez)
                this.WriteLocalRegToMaster();
        }
        //Debug.LogWarning("FCS just before checking if mass inventory is null Station ID: " + this.StationID.ToString());
        if (this.ConnectedInventory == null)
            this.ConnectedInventory = FreightCartManager.instance.TryRegisterStation(this);
        //else
        //{
        //    Debug.LogWarning("Connected inventory station count: " + this.ConnectedInventory.ConnectedStations.Count);
        //    if (this.ConnectedInventory.ConnectedStations.Count > 0)
        //        Debug.LogWarning("Connected inventory station id: " + this.ConnectedInventory.ConnectedStations[0].StationID.ToString());
        //    else
        //    {
        //        Debug.LogWarning("Conencted inventory had no stations so resetting the stations inventory to reregister");
        //        this.ConnectedInventory = null;
        //    }
        //}
    }

    public void CheckNetworkReg()
    {
        //Debug.LogWarning("Station ID: " + this.StationID.ToString() + " is checking is network reg");
        if (!FreightCartManager.instance.IsRegistered(this))
        {
            //Debug.LogWarning("Station is not registered");
            this.ConnectedInventory = FreightCartManager.instance.TryRegisterStation(this);
            if (this.ConnectedInventory != null && !string.IsNullOrEmpty(this.CachedInvName))
            {
                this.ConnectedInventory.Name = this.CachedInvName;
                this.CachedInvName = null;
            }
        }
    }

    public int DepositItem(ItemBase item)
    {
        int remainder = item.GetAmount();
        if (this.massStorageCrate == null || this.massStorageCrate.mbDelete)
            return remainder;

        if (item.IsStack())
        {
            //Try to find a matching crate before switching a crate
            for (int index = 0; index <= this.massStorageCrate.mConnectedCrates.Count; index++)
            {
                MassStorageCrate crate;
                if (index == this.massStorageCrate.mConnectedCrates.Count)
                    crate = this.massStorageCrate;
                else
                    crate = this.massStorageCrate.mConnectedCrates[index];

                if (crate.mrInputLockTimer <= 0 && crate.mMode == MassStorageCrate.CrateMode.SingleStack)
                {
                    int freespace = crate.mnLocalFreeStorage;
                    bool match = false;
                    if (crate.mItem != null)
                        match = crate.mItem.Compare(item);
                    if (freespace > 0 && (crate.mnLocalUsedStorage == 0 || match))
                    {
                        if (remainder > freespace)
                        {
                            if (crate.AddItem(ItemManager.CloneItem(item).SetAmount(freespace)))
                                remainder -= freespace;
                            else
                                Debug.LogWarning("Freight cart station tried to deposit partial stack to crate but failed!");
                        }
                        else
                        {
                            if (crate.AddItem(ItemManager.CloneItem(item).SetAmount(remainder)))
                                return 0;
                            else
                                Debug.LogWarning("Freight cart station tried to deposit remaining stack to crate but failed!");
                        }
                    }
                }
            }
            //Repeat trial with crate switching allowed
            for (int index = 0; index <= this.massStorageCrate.mConnectedCrates.Count; index++)
            {
                MassStorageCrate crate;
                if (index == this.massStorageCrate.mConnectedCrates.Count)
                    crate = this.massStorageCrate;
                else
                    crate = this.massStorageCrate.mConnectedCrates[index];

                if (crate.mrInputLockTimer <= 0 && crate.mMode != MassStorageCrate.CrateMode.SingleStack && crate.SwitchMode(MassStorageCrate.CrateMode.SingleStack))
                {
                    int freespace = crate.mnLocalFreeStorage;
                    bool match = false;
                    if (crate.mItem != null)
                        match = crate.mItem.Compare(item);
                    if (freespace > 0 && (crate.mnLocalUsedStorage == 0 || match))
                    {
                        if (remainder > freespace)
                        {
                            if (crate.AddItem(ItemManager.CloneItem(item).SetAmount(freespace)))
                                remainder -= freespace;
                            else
                                Debug.LogWarning("Freight cart station tried to deposit partial stack to crate but failed!");
                        }
                        else
                        {
                            if (crate.AddItem(ItemManager.CloneItem(item).SetAmount(remainder)))
                                return 0;
                            else
                                Debug.LogWarning("Freight cart station tried to deposit remaining stack to crate but failed!");
                        }
                    }
                }
            }
        }
        else
        {
            //Try to find a matching crate before switching a crate
            for (int index = 0; index <= this.massStorageCrate.mConnectedCrates.Count; index++)
            {
                MassStorageCrate crate;
                if (index == this.massStorageCrate.mConnectedCrates.Count)
                    crate = this.massStorageCrate;
                else
                    crate = this.massStorageCrate.mConnectedCrates[index];

                if (crate.mrInputLockTimer <= 0 && crate.mMode == MassStorageCrate.CrateMode.Items)
                {
                    if (crate.mnLocalFreeStorage > 0)
                    {
                        if (crate.AddItem(ItemManager.CloneItem(item)))
                            return 0;
                        else
                            Debug.LogWarning("Freight cart station tried to deposit single item to crate but failed!");
                    }
                }
            }
            //Repeat trial with crate switching allowed
            for (int index = 0; index <= this.massStorageCrate.mConnectedCrates.Count; index++)
            {
                MassStorageCrate crate;
                if (index == this.massStorageCrate.mConnectedCrates.Count)
                    crate = this.massStorageCrate;
                else
                    crate = this.massStorageCrate.mConnectedCrates[index];

                if (crate.mrInputLockTimer <= 0 && crate.mMode != MassStorageCrate.CrateMode.Items && crate.SwitchMode(MassStorageCrate.CrateMode.Items))
                {
                    if (crate.mnLocalFreeStorage > 0)
                    {
                        if (crate.AddItem(ItemManager.CloneItem(item)))
                            return 0;
                        else
                            Debug.LogWarning("Freight cart station tried to deposit single item to crate but failed!");
                    }
                }
            }
        }
        return remainder;
    }

    public int WithdrawItem(ItemBase item, out ItemBase itemout)
    {
        int remainder = item.GetAmount();
        itemout = null;
        if (this.massStorageCrate == null || this.massStorageCrate.mbDelete)
            return remainder;

        if (item.IsStack())
        {
            for (int index = 0; index <= this.massStorageCrate.mConnectedCrates.Count; index++)
            {
                MassStorageCrate crate;
                if (index == this.massStorageCrate.mConnectedCrates.Count)
                    crate = this.massStorageCrate;
                else
                    crate = this.massStorageCrate.mConnectedCrates[index];

                if (crate.mrOutputLockTimer <= 0 && crate.mMode == MassStorageCrate.CrateMode.SingleStack)
                {
                    int available = crate.mnLocalUsedStorage;
                    bool match = false;
                    if (crate.mItem != null)
                        match = crate.mItem.Compare(item);
                    if (available > 0 && match)
                    {
                        if (remainder >= available)
                        {
                            remainder -= available;
                            crate.mItem = null;
                            itemout = ItemManager.CloneItem(item).SetAmount(item.GetAmount() - remainder);
                            crate.CountUpFreeStorage(false);
                        }
                        else
                        {
                            crate.mItem.DecrementStack(remainder);
                            itemout = ItemManager.CloneItem(item);
                            crate.CountUpFreeStorage(false);
                            return 0;
                        }
                    }
                }
            }
        }
        else
        {
            for (int index = 0; index <= this.massStorageCrate.mConnectedCrates.Count; index++)
            {
                MassStorageCrate crate;
                if (index == this.massStorageCrate.mConnectedCrates.Count)
                    crate = this.massStorageCrate;
                else
                    crate = this.massStorageCrate.mConnectedCrates[index];

                if (crate.mrOutputLockTimer <= 0 && crate.mMode == MassStorageCrate.CrateMode.Items && crate.mnLocalUsedStorage > 0)
                {
                    for (int index2 = 0; index2 < crate.STORAGE_CRATE_SIZE; index2++)
                    {
                        bool match = false;
                        if (crate.mItems[index2] != null)
                            match = crate.mItems[index2].Compare(item);
                        if (match)
                        {
                            itemout = ItemManager.CloneItem(crate.mItems[index2]);
                            crate.mItems[index2] = null;
                            crate.CountUpFreeStorage(false);
                            return 0;
                        }
                    }
                }
            }
        }
        return remainder;
    }


    private void SearchForCrateNeighbours(long x, long y, long z)
    {
        for (int index = 0; index < 8; ++index)
        {
            //Debug.Log("Searching for crate");
            //Get adjacent crate or 1 block higher (for recessing the tracks)
            long x1 = x;
            long y1 = y;
            long z1 = z;
            if (index == 0 || index == 4)
                --x1;
            if (index == 1 || index == 5)
                ++x1;
            if (index == 2 || index == 6)
                --z1;
            if (index == 3 || index == 7)
                ++z1;
            if (index >= 4)
                ++y1;

            Segment segment = this.AttemptGetSegment(x1, y1, z1);
            if (segment == null)
            {
                segment = WorldScript.instance.GetSegment(x1, y1, z1);
                if (segment == null)
                {
                    Debug.Log((object)"SearchForCrateNeighbours did not find segment");
                    continue;
                }
            }
            if ((int)segment.GetCube(x1, y1, z1) == 527)
            {
                MassStorageCrate massStorageCrate = segment.FetchEntity(eSegmentEntity.MassStorageCrate, x1, y1, z1) as MassStorageCrate;
                if (massStorageCrate == null)
                    return;
                this.LocalCrate = massStorageCrate;
                this.massStorageCrate = massStorageCrate.GetCenter();
            }
        }
    }

    public void WriteLocalRegToMaster()
    {
        if (this.localregistry == null || FreightCartManager.instance.MasterRegistry == null || !this.RegWriteRequired || string.IsNullOrEmpty(this.NetworkID))
            return;
        for (int index = 0; index < this.localregistry.Count; index++)
        {
            FreightRegistry reg = this.localregistry[index];
            FreightCartManager.instance.AddRegistry(this.NetworkID, this.massStorageCrate, reg.FreightItem, reg.LowStock, reg.HighStock);
        }
        this.RegWriteRequired = false;
        this.cratex = 0;
        this.cratey = 0;
        this.cratez = 0;
        this.localregistry.Clear();
        //FreightCartManager.instance.DebugFreight();
    }

    public string CartTierLabel()
    {
        switch (this.CartTier)
        {
            case 0:
                return "Any";
            case 1:
                return "T2/T3+";
            case 2:
                return "T4 only";
            default:
                return "Unknown Tier";
        }
    }

    public override void OnDelete()
    {
        FreightCartManager.instance.RemoveStationReg(this);
        base.OnDelete();
    }

    public override int GetVersion()
    {
        return 5;
    }

    public override bool ShouldSave()
    {
        return true;
    }

    public MassStorageCrate GetCrateDirect(long x1, long y1, long z1)
    {
        Segment segment = this.AttemptGetSegment(x1, y1, z1);
        if (segment == null)
        {
            segment = WorldScript.instance.GetSegment(x1, y1, z1);
            if (segment == null)
            {
                Debug.Log((object)"SearchForCrateNeighbours did not find segment");
            }
        }
        if ((int)segment.GetCube(x1, y1, z1) == 527)
        {
            MassStorageCrate massStorageCrate = segment.FetchEntity(eSegmentEntity.MassStorageCrate, x1, y1, z1) as MassStorageCrate;
            return massStorageCrate;
        }
        return null;
    }

    public override bool ShouldNetworkUpdate()
    {
        return true;
    }

    public override void WriteNetworkUpdate(BinaryWriter writer)
    {
        if (!string.IsNullOrEmpty(this.NetworkID))
            writer.Write(this.NetworkID);
        else
            writer.Write(string.Empty);

        List<FreightRegistry> registries = new List<FreightRegistry>();
        if (this.massStorageCrate != null && this.NetworkID != null)
            registries = FreightCartManager.instance.GetFreightEntries(this.NetworkID, this.massStorageCrate);
        writer.Write(registries.Count);
        for (int index = 0; index < registries.Count; index++)
        {
            ItemFile.SerialiseItem(registries[index].FreightItem, writer);
            writer.Write(registries[index].LowStock);
            writer.Write(registries[index].HighStock);
        }
    }

    public override void ReadNetworkUpdate(BinaryReader reader)
    {
        this.NetworkID = reader.ReadString();
        if (this.NetworkID == string.Empty)
            this.NetworkID = null;

        List<FreightRegistry> registries = new List<FreightRegistry>();
        if (this.massStorageCrate != null && this.NetworkID != null)
            registries = FreightCartManager.instance.GetFreightEntries(this.NetworkID, this.massStorageCrate);

        this.localregistry = new List<FreightRegistry>();
        bool safereg = true;
        int count = reader.ReadInt32();

        if (count != 0 && string.IsNullOrEmpty(this.NetworkID))
        {
            Debug.LogWarning("Found registry entries but no network ID!");
            safereg = false;
        }
        for (int index = 0; index < count; index++)
        {
            ItemBase item = ItemFile.DeserialiseItem(reader);
            int LowStock = reader.ReadInt32();
            int HighStock = reader.ReadInt32();
            if (safereg)
                this.localregistry.Add(new FreightRegistry(this.NetworkID, null, item, LowStock, HighStock));
            else
                this.localregistry.Add(new FreightRegistry(null, null, item, LowStock, HighStock));
        }
        if (this.localregistry != null && registries.Count != this.localregistry.Count)
            this.RegWriteRequired = true;
        this.ConnectedInventory = FreightCartManager.instance.TryRegisterStation(this);
    }

    public override void Write(BinaryWriter writer)
    {
        if (!string.IsNullOrEmpty(this.NetworkID))
            writer.Write(this.NetworkID);
        else
        {
            writer.Write(string.Empty);
        }
        if (this.massStorageCrate != null)
        {
            writer.Write(true);
            writer.Write(this.massStorageCrate.mnX);
            writer.Write(this.massStorageCrate.mnY);
            writer.Write(this.massStorageCrate.mnZ);
        }
        else
            writer.Write(false);

        if (this.LocalCrate != null)
        {
            writer.Write(true);
            writer.Write(this.LocalCrate.mnX);
            writer.Write(this.LocalCrate.mnY);
            writer.Write(this.LocalCrate.mnZ);
        }
        else
            writer.Write(false);
        
        List<FreightRegistry> registries = new List<FreightRegistry>();
        if (this.massStorageCrate != null && this.NetworkID != null)
             registries = FreightCartManager.instance.GetFreightEntries(this.NetworkID, this.massStorageCrate);
        writer.Write(registries.Count);
        for (int index = 0; index < registries.Count; index++)
        {
            ItemFile.SerialiseItem(registries[index].FreightItem, writer);
            writer.Write(registries[index].LowStock);
            writer.Write(registries[index].HighStock);
        }

        writer.Write(this.AssignedCarts);
        writer.Write(this.mbWaitForFullLoad);
        writer.Write(this.OfferAll);
        if (!string.IsNullOrEmpty(this.StationName))
            writer.Write(this.StationName);
        else
            writer.Write(string.Empty);
        if (this.ConnectedInventory != null && !string.IsNullOrEmpty(this.ConnectedInventory.Name))
            writer.Write(this.ConnectedInventory.Name);
        else
            writer.Write(string.Empty);
        writer.Write(this.CartTier);
    }

    public override void Read(BinaryReader reader, int entityVersion)
    {
        long x;
        long y;
        long z;
        this.NetworkID = reader.ReadString();
        if (this.NetworkID == string.Empty)
            this.NetworkID = null;
        if (reader.ReadBoolean())
        {
            x = reader.ReadInt64();
            y = reader.ReadInt64();
            z = reader.ReadInt64();
            this.cratex = x;
            this.cratey = y;
            this.cratez = z;
            this.massStorageCrate = this.GetCrateDirect(x, y, z);
        }
        if (reader.ReadBoolean())
        {
            x = reader.ReadInt64();
            y = reader.ReadInt64();
            z = reader.ReadInt64();
            this.LocalCrate = this.GetCrateDirect(x, y, z);
        }

        //Read out registries and write them to the Master if it isn't there
        int count = reader.ReadInt32();
        this.localregistry = new List<FreightRegistry>();
        bool safereg = true;
        if (count != 0 && string.IsNullOrEmpty(this.NetworkID))
        {
            Debug.LogWarning("Found registry entries but no network ID!");
            safereg = false;
        }
        for (int index = 0; index < count; index++)
        {
            ItemBase item = ItemFile.DeserialiseItem(reader);
            int LowStock = reader.ReadInt32();
            int HighStock = reader.ReadInt32();
            if (safereg)
                this.localregistry.Add(new FreightRegistry(this.NetworkID, null, item, LowStock, HighStock));
            else
                this.localregistry.Add(new FreightRegistry(null, null, item, LowStock, HighStock));
        }
        if (this.localregistry != null)
            this.RegWriteRequired = true;

        //Check if the storage has been registered and add it if missing
        this.ConnectedInventory = FreightCartManager.instance.TryRegisterStation(this);

        if (entityVersion >= 1)
        {
            this.AssignedCarts = reader.ReadInt32();
            this.mbWaitForFullLoad = reader.ReadBoolean();
        }
        if (entityVersion >= 2)
            this.OfferAll = reader.ReadBoolean();
        if (entityVersion >= 3)
            this.StationName = reader.ReadString();
        if (entityVersion >= 4)
        {
            if (this.ConnectedInventory != null)
                this.ConnectedInventory.Name = reader.ReadString();
            else
                this.CachedInvName = reader.ReadString();
        }
        if (entityVersion >= 5)
            this.CartTier = reader.ReadInt32();

    }

    public override HoloMachineEntity CreateHolobaseEntity(Holobase holobase)
    {
        HolobaseEntityCreationParameters parameters = new HolobaseEntityCreationParameters((SegmentEntity)this);
        parameters.AddVisualisation(holobase.mPreviewCube).Color = Color.green;
        return holobase.CreateHolobaseEntity(parameters);
    }

    private enum eDoorStatus
    {
        Unknown,
        Open,
        Closed,
    }
}
