using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FortressCraft.Community.Utilities;

class FreightCartMob : MobEntity
{
    public int mnMaxStorage = 25;
    private float mrSpeedScalar = 1f;
    public float mrSpeed = 1f;
    public List<CubeCoord> mVisitedLocations = new List<CubeCoord>();
    private float mrTargetSpeed;
    public int mnUsedStorage;
    //private ItemBase[] maStoredItems;
    private FreightCartMob.eMinecartType meType;
    private float mrLoadTimer;
    //private MinecartStation mStation;
    private FreightCartStation FCStation;
    private string NetworkID;
    public FreightCartMob.eLoadState meLoadState;
    private System.Random rand;
    private static int MineCartID;
    public float mrVisualLoadTimer;
    public ConcurrentQueue<Vector8> maCartPositions;
    private float lrTimer;
    private int mnUpdatesPerTick;
    private float mrSpeedPerTick;
    private int mnUpdates;
    private int mnCurrentUpdateTick;
    private float mrSpeedBoost;
    private Segment mUnderSegment;
    private Segment mPrevGetSeg;

    public static ushort FreightStationType = ModManager.mModMappings.CubesByKey["steveman0.FreightCartStation"].CubeType;
    public static ushort FreightStationValue = 0;
    public Dictionary<string, MachineInventory> LocalInventory = new Dictionary<string, MachineInventory>();
    public MachineInventory TransferInventory;
    public MassInventory CurrentNetworkStock;
    List<KeyValuePair<ItemBase, int>> StationOfferings;
    public bool LoadCheckIn = false;
    public bool LoadCheckOut = false;

    public FreightCartMob(FreightCartMob.eMinecartType leType, ModCreateMobParameters modmobtype)
    : base(MobType.Mod, modmobtype.MobNumber, SpawnableObjectEnum.Minecart_T3)
  {
        ++FreightCartMob.MineCartID;
        this.rand = new System.Random();
        this.mrSpeed = 0.17345f;
        this.mrSpeedScalar = 1f;
        this.meType = leType;
        //if (this.meType == FreightCartMob.eMinecartType.FreightCartMK1)
            this.mType = MobType.Mod;
        //if (this.meType == FreightCartMob.eMinecartType.Basic)
        //    this.mType = MobType.Minecart;
        //if (this.meType == FreightCartMob.eMinecartType.Fast)
        //    this.mType = MobType.Minecart_T2;
        //if (this.meType == FreightCartMob.eMinecartType.Large)
        //    this.mType = MobType.Minecart_T3;
        //if (this.meType == FreightCartMob.eMinecartType.Bulk)
        //    this.mType = MobType.Minecart_T4;
        //if (this.meType == FreightCartMob.eMinecartType.Tour)
        //    this.mType = MobType.TourCart;
        this.SetStatsFromType();
        this.mrTargetSpeed = (float)this.rand.Next(95, 105) / 100f * this.mrSpeedScalar;
        if (WorldScript.meGameMode == eGameMode.eCreative)
            this.mrTargetSpeed = 5f;
        this.ConfigSpeedTicks();
        this.mnHealth = 1;
        //this.maStoredItems = new ItemBase[100];
        this.meLoadState = FreightCartMob.eLoadState.eTravelling;
        this.mbHostile = false;
        this.maCartPositions = new ConcurrentQueue<Vector8>();
        this.DoThreadedRaycastVis = true;
        this.MaxRayCastVis = 64f;
    }

    public override void SpawnGameObject()
    {
        if (this.meType == FreightCartMob.eMinecartType.FreightCart_T1)
            this.mObjectType = SpawnableObjectEnum.Minecart_T1;
        if (this.meType == FreightCartMob.eMinecartType.FreightCart_T2)
            this.mObjectType = SpawnableObjectEnum.Minecart_T2;
        if (this.meType == FreightCartMob.eMinecartType.FreightCart_T3)
            this.mObjectType = SpawnableObjectEnum.Minecart_T3;
        if (this.meType == FreightCartMob.eMinecartType.FreightCart_T4)
            this.mObjectType = SpawnableObjectEnum.Minecart_T4;

        if (this.meType == FreightCartMob.eMinecartType.FreightCartMK1)
            this.mObjectType = SpawnableObjectEnum.Minecart_T4;

        base.SpawnGameObject();
        if (this.mWrapper.mGameObjectList == null)
        {
            this.mWrapper.mGameObjectList = new List<GameObject>();
            ManagerSync.CartLoader.Enqueue(this);
        }


        //if (this.mWrapper != null && this.mWrapper.mGameObjectList.Count != 0)
        //{
        //    Component[] obj = this.mWrapper.mGameObjectList[0].gameObject.GetComponentsInChildren(typeof(Component));
        //    foreach (Component x in obj)
        //    {
        //        //Debug.Log("mWrapper Object: " + x + " name: " + x.name);
        //        if (x.name == "Minecart_Unity")
        //            x.gameObject.SetActive(false);
        //    }
        //    Debug.Log("Adding script object?");
        //    this.mWrapper.mGameObjectList[0].AddComponent<FreightCartUnity>();
        //    this.mWrapper.mGameObjectList[0].gameObject.SetActive(true);
        //}

        //foreach (GameObject x in this.mWrapper.mGameObjectList)
        //{
        //    Debug.Log("GameObject in wrapper: " + x.ToString());
        //}
        //Debug.Log("Wrapper: " + this.mWrapper.ToString());
    }

    private void SetStatsFromType()
    {
        if (this.meType == FreightCartMob.eMinecartType.FreightCart_T1)
        {
            this.mrSpeedScalar = 1f;
            this.mnMaxStorage = 25;
            this.TransferInventory = new MachineInventory(this, this.mnMaxStorage);
        }
        if (this.meType == FreightCartMob.eMinecartType.FreightCart_T2)
        {
            this.mrSpeedScalar = 2f;
            this.mnMaxStorage = 25;
            this.TransferInventory = new MachineInventory(this, this.mnMaxStorage);
        }
        if (this.meType == FreightCartMob.eMinecartType.FreightCart_T3)
        {
            this.mrSpeedScalar = 1f;
            this.mnMaxStorage = 50;
            this.TransferInventory = new MachineInventory(this, this.mnMaxStorage);
        }
        if (this.meType == FreightCartMob.eMinecartType.FreightCart_T4)
        {
            this.mrSpeedScalar = 2f;
            this.mnMaxStorage = 50;
            this.TransferInventory = new MachineInventory(this, this.mnMaxStorage);
        }

        if (this.meType == FreightCartMob.eMinecartType.FreightCartMK1)
        {
            this.mrSpeedScalar = 2f;
            this.mnMaxStorage = 50;
            this.TransferInventory = new MachineInventory(this, this.mnMaxStorage);
        }

        //if (this.meType == FreightCartMob.eMinecartType.Basic)
        //{
        //    this.mrSpeedScalar = 1f;
        //    this.mnMaxStorage = 25;
        //}
        //if (this.meType == FreightCartMob.eMinecartType.Fast)
        //{
        //    this.mrSpeedScalar = 2f;
        //    this.mnMaxStorage = 25;
        //}
        //if (this.meType == FreightCartMob.eMinecartType.Large)
        //{
        //    this.mrSpeedScalar = 1f;
        //    this.mnMaxStorage = 50;
        //}
        //if (this.meType == FreightCartMob.eMinecartType.Bulk)
        //{
        //    this.mrSpeedScalar = 2f;
        //    this.mnMaxStorage = 50;
        //}
        //if (this.meType != FreightCartMob.eMinecartType.Tour)
        //    return;
        //this.mrSpeedScalar = 4f;
        //this.mnMaxStorage = 0;
    }

    private void ConfigSpeedTicks()
    {
        this.mnUpdatesPerTick = (int)(this.mrSpeed * 0.333000004291534);
        ++this.mnUpdatesPerTick;
        if (this.mnUpdatesPerTick < 1)
            this.mnUpdatesPerTick = 1;
        this.mrSpeedPerTick = this.mrSpeed / (float)this.mnUpdatesPerTick;
    }

    public override int TakeDamage(int lnDamage)
    {
        return -1;
    }

    public override void MobUpdate()
    {
        ++this.mnUpdates;
        this.UpdatePlayerDistanceInfo();
        this.lrTimer -= MobUpdateThread.mrPreviousUpdateTimeStep;
        if (this.mLook.y > 0.0)
        {
            float num = 0.5f * this.mrSpeedScalar;
            if (this.mrSpeed > (double)num)
            {
                this.mrSpeed *= 0.5f;
                if (this.mrSpeed < (double)num)
                    this.mrSpeed = num;
                this.ConfigSpeedTicks();
            }
        }
        if (this.mLook.y < 0.0 && this.mrSpeed < 2.0 * this.mrSpeedScalar)
        {
            this.mrSpeed += MobUpdateThread.mrPreviousUpdateTimeStep * (1.5f * this.mrSpeedScalar);
            this.ConfigSpeedTicks();
        }
        this.UpdateLoadStatus();
        float num1 = this.mrTargetSpeed * (1f + this.mrSpeedBoost);
        float num2 = MobUpdateThread.mrPreviousUpdateTimeStep / 5f;
        if (this.mrSpeedBoost > 0.5)
            num2 = MobUpdateThread.mrPreviousUpdateTimeStep;
        this.mrSpeed += (num1 - this.mrSpeed) * num2;
        this.mrSpeedBoost *= 0.99f;
        if (this.mrTargetSpeed == 0.0)
            this.mrSpeed *= 0.0f;
        this.ConfigSpeedTicks();
        for (this.mnCurrentUpdateTick = 0; this.mnCurrentUpdateTick < this.mnUpdatesPerTick; ++this.mnCurrentUpdateTick)
            this.UpdateMove(this.mrSpeedPerTick);
    }

    private void UpdateLoadStatus()
    {
        this.mrLoadTimer += MobUpdateThread.mrPreviousUpdateTimeStep;
        if (this.FCStation == null && (this.meLoadState == eLoadState.eLoading || this.meLoadState == eLoadState.eUnloading))
        {
            Debug.LogError((object)"Error, our station went null!");
            this.LeaveStation();
        }
        if (this.meLoadState == FreightCartMob.eLoadState.eUnloading)
            this.UpdateCartUnload();
        if (this.meLoadState == FreightCartMob.eLoadState.eLoading)
            this.UpdateCartLoad();
    }

    private void UpdateCartUnload()
    { 
        if (string.IsNullOrEmpty(this.NetworkID))
        {
            this.LeaveStation();
        }
        else
        {
            this.mBlockOffset = Vector3.zero;
            if ((int)this.FCStation.mValue != FreightStationValue)
                Debug.LogError(("Attempted to unload into the wrong type of station! (Expected Freight station val, got " + this.FCStation.mValue));
            //if (!this.FCStation.mbWaitForFullLoad && this.mrLoadTimer > 300.0)
            //    this.LeaveStation();

            if (this.TransferInventory.IsEmpty() && this.LoadCheckIn)
            {
                this.meLoadState = eLoadState.eLoading;
                this.LoadCheckIn = false;
            }
            else
            {
                //Get the network stock to compare and adjust accordingly to unloading etc.
                //if (this.CurrentNetworkStock == null)
                //    this.CurrentNetworkStock = FreightCartManager.instance.GetNetworkStock(this.FCStation);

                if (this.FCStation.ConnectedInventory != null && this.TransferInventory.ItemCount() == 0 && this.LocalInventory.ContainsKey(this.NetworkID) && this.LocalInventory[this.NetworkID].ItemCount() != 0)
                {
                    List<KeyValuePair<ItemBase, int>> needs = FreightCartManager.instance.GetStationNeeds(this.FCStation);
                    for (int index = 0; index < needs.Count; index++)
                    {
                        this.RetrieveItem(needs[index]);
                        this.mrVisualLoadTimer = 1f;
                    }
                }
                if (this.TransferInventory.ItemCount() > 0 && this.FCStation.ConnectedInventory != null)
                {
                    ItemBase deposited = this.TransferInventory.RemoveAnySingle(1);
                    int remainder = this.FCStation.DepositItem(deposited);
                    if (remainder == 0)
                    {
                        FreightCartManager.instance.NetworkRemove(this.NetworkID, deposited, 1);
                        this.mnUsedStorage--;
                    }
                    else
                    {
                        Debug.LogWarning("Station failed to deposit item at FreightCart unload step. Returning to local inventory.");
                        this.LocalInventory[this.NetworkID].AddItem(deposited.SetAmount(remainder));
                    }
                    //this.CurrentNetworkStock.Inventory[deposited]--;
                    this.mrVisualLoadTimer = 1f;
                }
                else if (this.TransferInventory.ItemCount() > 0)
                {
                    Debug.LogWarning("Freight cart still had items to transfer but lost mass storage! Remaining items: " + this.TransferInventory.ItemCount());
                }
                this.FCStation.mrCartOnUs = 1f;
                this.FCStation.mrLastCartLeavTimer = this.mrLoadTimer;
                this.LoadCheckIn = true;
            }
        }
    }

    private void UpdateCartLoad()
    {
        if (string.IsNullOrEmpty(this.FCStation.NetworkID))
        {
            this.LeaveStation();
        }
        else
        {
            this.mBlockOffset = Vector3.zero;
            if ((int)this.FCStation.mValue != FreightStationValue)
                Debug.LogError(("Attempted to unload into the wrong type of station! (Expected FreightStationValue, got " + this.FCStation.mValue));
            if (this.mnUsedStorage == this.mnMaxStorage || this.LoadCheckOut || this.mrLoadTimer > 30.0f)
            {
                this.LeaveStation();
            }
            else
            {
                //if (this.CurrentNetworkStock == null)
                //    this.CurrentNetworkStock = FreightCartManager.instance.GetNetworkStock(this.FCStation);
                this.FCStation.mrCartOnUs = 1f;
                this.FCStation.mrLastCartLeavTimer = this.mrLoadTimer;
                if (this.FCStation.ConnectedInventory != null && this.StationOfferings == null)
                {
                    this.StationOfferings = FreightCartManager.instance.GetStationOfferings(this.FCStation);
                    if (this.StationOfferings == null)
                        Debug.LogWarning("Tried to get station offerings but they were null?");
                }
                if (this.StationOfferings != null && this.StationOfferings.Count > 0)
                {
                    ItemBase item = this.GetNextOffer();
                    ItemBase itemout = null;
                    if (item != null)
                    {
                        this.FCStation.WithdrawItem(item, out itemout);
                        if (itemout != null)
                        {
                            if (!this.LocalInventory.ContainsKey(this.NetworkID))
                                this.LocalInventory.Add(this.NetworkID, new MachineInventory(this, this.mnMaxStorage));
                            ItemBase remainder = this.LocalInventory[FCStation.NetworkID].AddItem(itemout);
                            if (remainder == null)
                            {
                                //this.CurrentNetworkStock.Inventory[item]++;
                                FreightCartManager.instance.NetworkAdd(this.NetworkID, item, 1);
                                this.mnUsedStorage++;
                                this.mrVisualLoadTimer = 1f;
                                this.MarkDirtyDelayed();
                            }
                            else
                            {
                                Debug.LogWarning("Freight cart withdrawal failed? ");
                                this.FCStation.DepositItem(remainder);
                            }
                        }
                        else
                            this.LoadCheckOut = true;
                    }
                    else
                        this.LoadCheckOut = true;


                }
                else
                    this.LoadCheckOut = true;

                //if (this.StoreItem(this.FCStation.mProferredItem))
                //{
                //    this.mrVisualLoadTimer = 1f;
                //    this.FCStation.mProferredItem = (ItemBase)null;
                //    if (this.mnUsedStorage == this.mnMaxStorage)
                //        this.LeaveStation();
                //    this.mrLoadTimer = 0.0f;
                //}
                //else
                //    this.LeaveStation();
            }
        }
    }

    private ItemBase GetNextOffer()
    {
        if (!WorldScript.mbIsServer)
            return null;
        //Start on a random item to help balance input
        int getitem = this.rand.Next(0, this.StationOfferings.Count);
        ItemBase item = this.StationOfferings[getitem].Key;
        if (FreightCartManager.instance.NetworkNeeds(this.NetworkID, item) > 0)
        {
            return item.SetAmount(1);
        }
        else
        {
            int check;
            for (int index = getitem + 1; index < this.StationOfferings.Count + getitem; ++index)
            {
                check = index;
                if (index >= this.StationOfferings.Count)
                    check -= this.StationOfferings.Count;
                item = this.StationOfferings[check].Key;
                if (FreightCartManager.instance.NetworkNeeds(this.NetworkID, item) > 0)
                    return item.SetAmount(1);
            }
        }
        return null;
    }

    private void RetrieveItem(KeyValuePair<ItemBase, int> keypair)
    {
        ItemBase item = keypair.Key;
        int amount = keypair.Value;
        this.MarkDirtyDelayed();
        if (this.LocalInventory.ContainsKey(this.NetworkID))
        {
            if (this.LocalInventory[this.NetworkID].IsEmpty())
                Debug.LogError("Error, can't retrieve item from Minecart as it's empty.");
            this.LocalInventory[this.NetworkID].RemoveWhiteList(ref this.TransferInventory.Inventory, item, this.TransferInventory.StorageCapacity, amount);
            if (this.LocalInventory[this.NetworkID].IsEmpty())
                this.LocalInventory.Remove(this.NetworkID);
        }
        else
            Debug.LogWarning("FreightCart tried to access inventory for missing network id?");
    }

    private string GetStorageString()
    {
        int num1 = 0;
        string str = string.Empty;
        int num2 = -1;
        if (string.IsNullOrEmpty(this.NetworkID) || !this.LocalInventory.ContainsKey(this.NetworkID))
            return "";
        for (int index = 0; index < this.LocalInventory[this.NetworkID].Inventory.Count; ++index)
        {
            if (this.LocalInventory[this.NetworkID].Inventory[index] != null)
            {
                if (str == string.Empty)
                {
                    str = ItemManager.GetItemName(this.LocalInventory[this.NetworkID].Inventory[index]);
                    num2 = this.LocalInventory[this.NetworkID].Inventory[index].mType != ItemType.ItemCubeStack ? this.LocalInventory[this.NetworkID].Inventory[index].mnItemID : (this.LocalInventory[this.NetworkID].Inventory[index] as ItemCubeStack).mCubeType;
                    num1 = this.LocalInventory[this.NetworkID].Inventory[index].GetAmount();
                }
            }
        }

        //for (int index = 0; index < this.mnMaxStorage; ++index)
        //{
        //    if (this.maStoredItems[index] != null)
        //    {
        //        if (str == string.Empty)
        //        {
        //            str = ItemManager.GetItemName(this.maStoredItems[index]);
        //            num2 = this.maStoredItems[index].mType != ItemType.ItemCubeStack ? this.maStoredItems[index].mnItemID : (int)(this.maStoredItems[index] as ItemCubeStack).mCubeType;
        //        }
        //        if (this.maStoredItems[index].mType == ItemType.ItemCubeStack)
        //        {
        //            if ((int)(this.maStoredItems[index] as ItemCubeStack).mCubeType == num2)
        //                ++num1;
        //        }
        //        else if (this.maStoredItems[index].mnItemID == num2)
        //            ++num1;
        //    }
        //}
        if (num2 == -1)
            return "Nothing";
        return num1 + "x " + str;
    }

    //private bool StoreItem(ItemBase lItem)
    //{
    //    this.MarkDirtyDelayed();
    //    this.mnUsedStorage = this.LocalInventory.ItemCount();
    //    ItemBase remainder = this.LocalInventory.AddItem(lItem);
    //    if (remainder == null)
    //        return true;

    //    //for (int index = 0; index < this.mnMaxStorage; ++index)
    //    //{
    //    //    if (this.maStoredItems[index] == null)
    //    //    {
    //    //        this.maStoredItems[index] = lItem;
    //    //        return true;
    //    //    }
    //    //}
    //    return false;
    //}

    private void LeaveStation()
    {
        if (this.FCStation != null)
        {
            this.FCStation.mrCartOnUs = -1f;
            this.FCStation = (FreightCartStation)null;
        }
        if (this.mDotWithPlayerForwards > 0.5 && this.mDistanceToPlayer < 5.0 && (this.meLoadState == FreightCartMob.eLoadState.eLoading && this.mnUsedStorage > 0) && this.mrSpeed < 0.100000001490116)
            FloatingCombatTextManager.instance.QueueText(this.mnX, this.mnY + 1L, this.mnZ, 1f, "Loaded " + this.GetStorageString(), Color.cyan, 1f, 64f);
        this.meLoadState = FreightCartMob.eLoadState.eTravelling;
        this.mrTargetSpeed = (float)this.rand.Next(95, 105) / 100f * this.mrSpeedScalar;
        this.mbNetworkUpdateRequested = true;
        this.LoadCheckIn = false;
        this.LoadCheckOut = false;
        this.StationOfferings = null;
        this.CurrentNetworkStock = null;
        this.NetworkID = null;
    }

    private void StoreNewTrackPiece()
    {
    }

    private void MoveToNewVox(int lnXMove, int lnYMove, int lnZMove)
    {
        this.mrLoadTimer = 0.0f;
        this.mBlockOffset.x -= lnXMove;
        this.mBlockOffset.y -= lnYMove;
        this.mBlockOffset.z -= lnZMove;
        ushort lValue1 = 0;
        byte lFlags1 = 0;
        ushort type = this.GetCube(this.mnX + lnXMove, this.mnY + lnYMove - 1L, this.mnZ + lnZMove, out lValue1, out lFlags1);
        this.mUnderSegment = this.mPrevGetSeg;
        ushort lValue2 = 0;
        byte lFlags2 = 0;
        ushort cube = this.GetCube(this.mnX + (long)lnXMove, this.mnY + lnYMove - 2L, this.mnZ + lnZMove, out lValue2, out lFlags2);
        if (type == 0 || cube == 0)
        {
            this.mBlockOffset.x += lnXMove;
            this.mBlockOffset.y += lnYMove;
            this.mBlockOffset.z += lnZMove;
            FreightCartMob minecartEntity = this;
            Vector3 vector3 = minecartEntity.mBlockOffset - this.mLook * 0.333f * this.mrSpeedPerTick;
            minecartEntity.mBlockOffset = vector3;
            this.mnUpdatesPerTick = 0;
            if (type == 0)
                CentralPowerHub.mnMinecartY = this.mnY + lnYMove - 1L;
            if (cube == 0)
                CentralPowerHub.mnMinecartY = this.mnY + lnYMove - 2L;
            CentralPowerHub.mnMinecartX = this.mnX + lnXMove;
            CentralPowerHub.mnMinecartZ = this.mnZ + lnZMove;
        }
        else if (!MobManager.instance.MoveMob(this, this.mnX + lnXMove, this.mnY + lnYMove, this.mnZ + lnZMove, this.mBlockOffset))
        {
            this.mBlockOffset.x += lnXMove;
            this.mBlockOffset.y += lnYMove;
            this.mBlockOffset.z += lnZMove;
            FreightCartMob minecartEntity = this;
            Vector3 vector3 = minecartEntity.mBlockOffset - this.mLook * 0.2f * this.mrSpeedPerTick;
            minecartEntity.mBlockOffset = vector3;
            CentralPowerHub.mnMinecartX = this.mnX + lnXMove;
            CentralPowerHub.mnMinecartY = this.mnY + lnYMove;
            CentralPowerHub.mnMinecartZ = this.mnZ + lnZMove;
            Debug.LogWarning("Minecart failed to move -  requesting segment page in!");
            this.mnUpdatesPerTick = 0;
        }
        else
        {
            if (this.mbMovedToNewSegment)
            {
                this.mbMovedToNewSegment = false;
                CentralPowerHub.mnMinecartX = this.mnX;
                CentralPowerHub.mnMinecartY = this.mnY;
                CentralPowerHub.mnMinecartZ = this.mnZ;
            }
            this.mbNetworkUpdateRequested = true;
            this.StoreNewTrackPiece();
            if (type == 0)
                Debug.LogError("Error, minecart attempted to continue moving into null segment");
            if (this.mPrevGetSeg == null)
                Debug.LogError(("Error, GetCube returned " + TerrainData.GetNameForValue(type, lValue1) + " but the segment was null!"));
            bool flag = false;
            if (type == 1)
            {
                Segment segment = this.mPrevGetSeg;
                type = cube;
                lFlags1 = lFlags2;
                lValue1 = lValue2;
                if (type == 538 && lValue1 == 2)
                {
                    flag = true;
                }
                else
                {
                    if (type == 0)
                        Debug.LogError("Error, minecart has null under segment!");
                    if (this.mPrevGetSeg == null)
                        Debug.LogError("Error, prevseg was null!");
                    if (segment == null)
                        Debug.LogError("Error, old was null!");
                    if (this.mPrevGetSeg != segment)
                        Debug.LogWarning(("Minecart is looking for a slope, and has had to check across segment boundaries for this![Old/New" + segment.GetName() + " -> " + this.mPrevGetSeg.GetName()));
                    Debug.LogWarning(("Minecart hit air but located no underslope!" + TerrainData.GetNameForValue(type, lValue1)));
                }
            }
            if (type == 538)
            {
                if (this.meLoadState == FreightCartMob.eLoadState.eLoading || this.meLoadState == FreightCartMob.eLoadState.eUnloading || this.meLoadState == FreightCartMob.eLoadState.eCharging)
                    this.LeaveStation();
                this.meLoadState = FreightCartMob.eLoadState.eTravelling;
                Vector3 vector3_1 = SegmentCustomRenderer.GetRotationQuaternion(lFlags1) * Vector3.forward;
                vector3_1.Normalize();
                vector3_1.x = vector3_1.x >= -0.5 ? (vector3_1.x <= 0.5 ? 0.0f : 1f) : -1f;
                vector3_1.y = vector3_1.y >= -0.5 ? (vector3_1.y <= 0.5 ? 0.0f : 1f) : -1f;
                vector3_1.z = vector3_1.z >= -0.5 ? (vector3_1.z <= 0.5 ? 0.0f : 1f) : -1f;
                if (lValue1 == 3)
                {
                    this.mLook = -this.mLook;
                }
                else
                {
                    if (lValue1 == 4)
                    {
                        this.mLook.y = 0.0f;
                        this.mLook.Normalize();
                        if (this.mLook == vector3_1)
                            lValue1 = 0;
                        else if (this.mnUsedStorage == this.mnMaxStorage)
                        {
                            lValue1 = 0;
                        }
                        else
                        {
                            this.mLook = -this.mLook;
                            return;
                        }
                    }
                    if (lValue1 == 5)
                    {
                        this.mLook.y = 0.0f;
                        this.mLook.Normalize();
                        if (this.mLook == vector3_1)
                            lValue1 = 0;
                        else if (this.mnUsedStorage == 0)
                        {
                            lValue1 = 0;
                        }
                        else
                        {
                            this.mLook = -this.mLook;
                            return;
                        }
                    }
                    if (lValue1 == 0)
                    {
                        this.mLook.y = 0.0f;
                        this.mLook.Normalize();
                        if (vector3_1.y > 0.5 || vector3_1.y < -0.5)
                            this.RemoveCart("Track Straight hitting non-straight piece!");
                        else if (!(this.mLook == vector3_1) && !(this.mLook == -vector3_1))
                            this.mLook = vector3_1;
                    }
                    if (lValue1 == 1)
                    {
                        this.mLook.y = 0.0f;
                        this.mLook.Normalize();
                        if (this.mLook == new Vector3(-vector3_1.z, 0.0f, vector3_1.x))
                            this.mLook = new Vector3(this.mLook.z, 0.0f, -this.mLook.x);
                        else if (vector3_1 == -this.mLook)
                            this.mLook = new Vector3(-this.mLook.z, 0.0f, this.mLook.x);
                        else
                            this.RemoveCart("Minecart headed into invalid corner");
                    }
                    if (lValue1 != 2)
                        return;
                    Vector3 vector3_2 = vector3_1;
                    this.mLook.y = 0.0f;
                    this.mLook.Normalize();
                    if (this.mLook == vector3_1)
                    {
                        if (flag)
                        {
                            this.RemoveCart("Minecart needed a downward slope, but found an upwards slope instead!");
                        }
                        else
                        {
                            vector3_2.y = 1f;
                            vector3_2.Normalize();
                            this.mLook = vector3_2;
                            this.mBlockOffset.y = this.mBlockOffset.x + this.mBlockOffset.z;
                        }
                    }
                    else if (this.mLook == -vector3_1)
                    {
                        this.mLook.y = -1f;
                        this.mLook.Normalize();
                    }
                    else
                        Debug.LogWarning(string.Concat(new object[4]
                        {
               "Minecart hit the side of a slope (Look was",
               this.mLook,
               " but track was ",
               vector3_1
                        }));
                }
            }
            else if (type == 539)
            {
                Vector3 vector3 = SegmentCustomRenderer.GetRotationQuaternion(lFlags1) * Vector3.forward;
                if (this.mLook == vector3 || this.mLook == -vector3)
                {
                    if (lValue1 == 2)
                        this.mrSpeedBoost = 2f;
                    if (lValue1 == 3)
                    {
                        //MinecartStation minecartStation = this.mUnderSegment.FetchEntity(eSegmentEntity.MinecartControl, this.mnX, this.mnY - 1L, this.mnZ) as MinecartStation;
                        //if (minecartStation == null)
                            this.LeaveStation();
                        //else if ((double)minecartStation.mrCartOnUs <= 0.0)
                        //{
                        //    this.mStation = minecartStation;
                        //    this.mrTargetSpeed = 0.0f;
                        //    this.meLoadState = FreightCartMob.eLoadState.eUnloading;
                        //    this.mrLoadTimer = 0.0f;
                        //    ++this.mStation.mnCartsSeen;
                        //    if (this.mnUsedStorage > 0 && FloatingCombatTextManager.Initialised)
                        //        FloatingCombatTextManager.instance.QueueText(this.mnX, this.mnY + 1L, this.mnZ, 1f, "Unloading " + this.GetStorageString(), Color.green, 1f, 64f);
                        //}
                    }
                    if (lValue1 != 4 || this.mnUsedStorage == this.mnMaxStorage)
                        return;
                    //MinecartStation minecartStation1 = this.mUnderSegment.FetchEntity(eSegmentEntity.MinecartControl, this.mnX, this.mnY - 1L, this.mnZ) as MinecartStation;
                    //if (minecartStation1 == null)
                    //{
                        this.LeaveStation();
                    //}
                    //else
                    //{
                    //    if ((double)minecartStation1.mrCartOnUs > 0.0)
                    //        return;
                    //    this.mStation = minecartStation1;
                    //    this.mrTargetSpeed = 0.0f;
                    //    this.meLoadState = FreightCartMob.eLoadState.eLoading;
                    //    this.mrLoadTimer = 0.0f;
                    //    ++this.mStation.mnCartsSeen;
                    //}
                }
                else
                    this.RemoveCart("MineCart hit sideways control piece! THIS IS UNFORGIVABLE!");
            }//Inserted my Freight Cart Station type!
            else if (type == FreightStationType)
            {
                Vector3 vector3 = SegmentCustomRenderer.GetRotationQuaternion(lFlags1) * Vector3.forward;
                if (this.mLook == vector3 || this.mLook == -vector3)
                {
                    if (lValue1 == FreightStationValue)
                    {
                        FreightCartStation Station = this.mUnderSegment.FetchEntity(eSegmentEntity.Mod, this.mnX, this.mnY - 1L, this.mnZ) as FreightCartStation;
                        if (Station == null)
                            this.LeaveStation();
                        else if (Station.mrCartOnUs <= 0.0)
                        {
                            this.FCStation = Station;
                            this.NetworkID = Station.NetworkID;
                            this.mrTargetSpeed = 0.0f;
                            this.meLoadState = FreightCartMob.eLoadState.eUnloading;
                            this.mrLoadTimer = 0.0f;
                            ++this.FCStation.mnCartsSeen;
                            //if (this.mnUsedStorage > 0 && FloatingCombatTextManager.Initialised)
                            //    FloatingCombatTextManager.instance.QueueText(this.mnX, this.mnY + 1L, this.mnZ, 1f, "Unloading " + this.GetStorageString(), Color.green, 1f, 64f);
                        }
                    }
                }
                else
                    this.RemoveCart("MineCart hit sideways control piece! THIS IS UNFORGIVABLE!");
            }
            else if (type == 0)
                Debug.LogWarning("Minecart hit edge of loaded area?");
            else
                this.RemoveCart("Minecart hit " + TerrainData.GetNameForValue(type, lValue1) + "\nMinecart was on update " + this.mnCurrentUpdateTick + " of " + this.mnUpdatesPerTick + "\nSpeed was " + this.mrSpeed + " and offset was " + this.mBlockOffset + "\nSpeed per tick was " + this.mrSpeedPerTick + " and current look was " + this.mLook);
        }
    }

    private void UpdateMove(float lrSpeed)
    {
        int lnXMove = 0;
        int lnYMove = 0;
        int lnZMove = 0;
        if (this.mLook.x > 0.5 || this.mLook.x < -0.5)
        {
            this.mLook.z = 0.0f;
            this.mBlockOffset.z = 0.0f;
        }
        if (this.mLook.z > 0.5 || this.mLook.z < -0.5)
        {
            this.mLook.x = 0.0f;
            this.mBlockOffset.x = 0.0f;
        }
        Vector3 vector3_1 = this.mBlockOffset;
        FreightCartMob minecartEntity = this;
        Vector3 vector3_2 = minecartEntity.mBlockOffset + this.mLook * 0.333f * lrSpeed;
        minecartEntity.mBlockOffset = vector3_2;
        if (this.mLook.y < 0.0)
        {
            if (this.mBlockOffset.x > 0.0 && this.mBlockOffset.z == 0.0)
                this.mBlockOffset.y = -this.mBlockOffset.x;
            if (this.mBlockOffset.x < 0.0 && this.mBlockOffset.z == 0.0)
                this.mBlockOffset.y = this.mBlockOffset.x;
            if (this.mBlockOffset.x == 0.0 && this.mBlockOffset.z > 0.0)
                this.mBlockOffset.y = -this.mBlockOffset.z;
            if (this.mBlockOffset.x == 0.0 && this.mBlockOffset.z < 0.0)
                this.mBlockOffset.y = this.mBlockOffset.z;
        }
        if (this.mLook.y > 0.0)
        {
            if (this.mBlockOffset.x > 0.0 && this.mBlockOffset.z == 0.0)
                this.mBlockOffset.y = this.mBlockOffset.x;
            if (this.mBlockOffset.x < 0.0 && this.mBlockOffset.z == 0.0)
                this.mBlockOffset.y = -this.mBlockOffset.x;
            if (this.mBlockOffset.x == 0.0 && this.mBlockOffset.z > 0.0)
                this.mBlockOffset.y = this.mBlockOffset.z;
            if (this.mBlockOffset.x == 0.0 && this.mBlockOffset.z < 0.0)
                this.mBlockOffset.y = -this.mBlockOffset.z;
        }
        if (this.mLook.y == 0.0)
        {
            float num = this.mBlockOffset.y;
            this.mBlockOffset.y *= 0.9f;
        }
        float num1 = 1f;
        if (this.mBlockOffset.x <= -(double)num1)
            lnXMove = -1;
        if (this.mBlockOffset.x >= (double)num1)
            lnXMove = 1;
        if (this.mBlockOffset.y <= -(double)num1)
            lnYMove = -1;
        if (this.mBlockOffset.y >= (double)num1)
            lnYMove = 1;
        if (this.mBlockOffset.z <= -(double)num1)
            lnZMove = -1;
        if (this.mBlockOffset.z >= (double)num1)
            lnZMove = 1;
        if ((double)lnXMove != 0.0)
            this.mBlockOffset.z = 0.0f;
        if ((double)lnZMove != 0.0)
            this.mBlockOffset.x = 0.0f;
        if (lnXMove != 0 || lnYMove != 0 || lnZMove != 0)
            this.MoveToNewVox(lnXMove, lnYMove, lnZMove);
        this.StoreCartPosition(this.mrSpeed);
    }

    private void StoreCartPosition(float lrSpeed)
    {
        if (PersistentSettings.mbHeadlessServer || this.mDistanceToPlayer > 128.0)
            return;
        Vector3 vector3 = WorldScript.instance.mPlayerFrustrum.GetCoordsToUnity(this.mnX, this.mnY, this.mnZ) + this.mBlockOffset;
        vector3.x += 0.5f;
        vector3.y += 0.5f;
        vector3.z += 0.5f;
        this.maCartPositions.Enqueue(new Vector8()
        {
            x = vector3.x,
            y = vector3.y,
            z = vector3.z,
            w = lrSpeed,
            a = this.mLook.x,
            b = this.mLook.y,
            c = this.mLook.z
        });
    }

    private void RemoveCart(string lReason)
    {
        if (!WorldScript.mbIsServer)
            return;
        Debug.Log((object)("****** LITTLE JIMMY DROP CARTS ******[" + lReason + "]"));
        if (this.mnUpdates <= 1)
            Debug.LogWarning((object)"Probable issue - minecart decided to drop on or before first update!");
        System.Random random = new System.Random();
        int itemID = ItemEntries.MineCartT1;
        if (this.meType == FreightCartMob.eMinecartType.FreightCartMK1)
            itemID = ModManager.mModMappings.ItemsByKey["steveman0.FreightCartMK1"].ItemId;

        if (this.meType == FreightCartMob.eMinecartType.FreightCart_T1)
            itemID = ItemEntries.MineCartT1;
        if (this.meType == FreightCartMob.eMinecartType.FreightCart_T2)
            itemID = ItemEntries.MineCartT2;
        if (this.meType == FreightCartMob.eMinecartType.FreightCart_T3)
            itemID = ItemEntries.MineCartT3;
        if (this.meType == FreightCartMob.eMinecartType.FreightCart_T4)
            itemID = ItemEntries.MineCartT4;

        ItemBase itemBase = ItemManager.SpawnItem(itemID);
        DroppedItemData droppedItemData = ItemManager.instance.DropItem(itemBase, this.mnX, this.mnY, this.mnZ, Vector3.up);
        if (droppedItemData != null)
            droppedItemData.mrLifeRemaining = 36000f;

        //Unregister network stock
        List<string> keys = new List<string>();
        if (this.LocalInventory != null)
            keys = this.LocalInventory.Keys.ToList();
        for (int index2 = 0; index2 < keys.Count; index2++)
        {
            for (int index = 0; index < this.LocalInventory[keys[index2]].Inventory.Count; index++)
            {
                ItemBase item = this.LocalInventory[keys[index2]].Inventory[index];
                FreightCartManager.instance.NetworkRemove(keys[index2], item, item.GetAmount());
            }
            this.LocalInventory[keys[index2]].DropOnMobDelete();
        }

        for (int index = 0; index < this.TransferInventory.Inventory.Count; index++)
        {
            ItemBase item = this.TransferInventory.Inventory[index];
            FreightCartManager.instance.NetworkRemove(this.NetworkID, item, item.GetAmount());
        }
        this.TransferInventory.DropOnMobDelete();
        //for (int index = 0; index < this.mnMaxStorage; ++index)
        //{
        //    if (this.maStoredItems[index] != null)
        //    {
        //        Vector3 velocity = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 1.5f, (float)random.NextDouble() - 0.5f);
        //        ItemManager.instance.DropItem(this.maStoredItems[index], this.mnX, this.mnY, this.mnZ, velocity);
        //        this.maStoredItems[index] = (ItemBase)null;
        //    }
        //}
        MobManager.instance.DestroyMob((MobEntity)this);
        this.mnUpdatesPerTick = 0;
    }

    public ushort GetCube(long lTestX, long lTestY, long lTestZ, out ushort lValue, out byte lFlags)
    {
        if (lTestX < 100000L)
            Debug.LogError((object)("Error, either you travelled 500 light years, or the mob is lost! X is " + (object)lTestX));
        if (lTestY < 100000L)
            Debug.LogError((object)("Error, either you travelled 500 light years, or the mob is lost! Y is " + (object)lTestY));
        if (lTestZ < 100000L)
            Debug.LogError((object)("Error, either you travelled 500 light years, or the mob is lost! Z is " + (object)lTestZ));
        Segment segment;
        if (this.mSegment.ContainsCoordinate(lTestX, lTestY, lTestZ))
        {
            segment = this.mSegment;
        }
        else
        {
            long segX;
            long segY;
            long segZ;
            WorldHelper.GetSegmentCoords(lTestX, lTestY, lTestZ, out segX, out segY, out segZ);
            segment = WorldScript.instance.GetSegment(segX, segY, segZ);
        }
        if (segment == null || !segment.mbInitialGenerationComplete || segment.mbDestroyed)
        {
            CentralPowerHub.mnMinecartX = lTestX;
            CentralPowerHub.mnMinecartY = lTestY;
            CentralPowerHub.mnMinecartZ = lTestZ;
            lFlags = (byte)0;
            lValue = (ushort)0;
            this.mPrevGetSeg = (Segment)null;
            return 0;
        }
        lValue = segment.GetCubeData(lTestX, lTestY, lTestZ).mValue;
        lFlags = segment.GetCubeData(lTestX, lTestY, lTestZ).meFlags;
        this.mPrevGetSeg = segment;
        return segment.GetCube(lTestX, lTestY, lTestZ);
    }

    public override int GetVersion()
    {
        return 1;
    }

    public override bool ShouldSave()
    {
        return true;
    }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((int)this.meType);
        writer.Write(this.mrTargetSpeed);
        writer.Write(this.mrSpeed);

        List<string> keys = new List<string>();
        if (this.LocalInventory != null)
            keys = this.LocalInventory.Keys.ToList();
        writer.Write(keys.Count);
        for (int index = 0; index < keys.Count; index++)
        {
            writer.Write(keys[index]);
            this.LocalInventory[keys[index]].WriteInventory(writer);
        }
        this.TransferInventory.WriteInventory(writer);
    }

    public override void Read(BinaryReader reader, int version)
    {
        this.meType = (FreightCartMob.eMinecartType)reader.ReadInt32();
        this.mrTargetSpeed = reader.ReadSingle();
        this.mrSpeed = reader.ReadSingle();
        this.SetStatsFromType();
        this.mrTargetSpeed = (float)this.rand.Next(95, 105) / 100f * this.mrSpeedScalar;

        int count = reader.ReadInt32();
        for (int index = 0; index < count; index++)
        {
            string networkid = reader.ReadString();
            MachineInventory inv = new MachineInventory(this, this.mnMaxStorage);
            inv.ReadInventory(reader);
            this.LocalInventory.Add(networkid, inv);
            this.mnUsedStorage += inv.ItemCount();
        }

        this.TransferInventory.ReadInventory(reader);
    }

    public enum eTrackType
    {
        eStraight,
        eCorner,
        eSlope,
        eBuffer,
        eBufferFull,
        eBufferEmpty,
    }

    public enum eLoadState
    {
        eTravelling,
        eLoading,
        eUnloading,
        eCharging,
    }

    public enum eMinecartType
    {
        FreightCart_T1,
        FreightCart_T2,
        FreightCart_T3,
        FreightCart_T4,
        FreightCartMK1,
        //Basic,
        //Fast,
        //Large,
        //Bulk,
        //Tour,
    }
}


