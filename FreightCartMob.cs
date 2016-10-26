using System.Linq;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using FortressCraft.Community.Utilities;

public class FreightCartMob : MobEntity
{
    public int mnMaxStorage = 25;
    private float mrSpeedScalar = 1f;
    public float mrSpeed = 1f;
    public List<CubeCoord> mVisitedLocations = new List<CubeCoord>();
    private float mrTargetSpeed;
    public int mnUsedStorage;
    //private ItemBase[] maStoredItems;
    public FreightCartMob.eMinecartType meType;
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
    public static ushort TourStationType = ModManager.mModMappings.CubesByKey["steveman0.TourCartStation"].CubeType;
    public static ushort JunctionType = ModManager.mModMappings.CubesByKey["steveman0.TrackJunction"].CubeType;
    public static ushort FreightStationValue = 0;
    public Dictionary<string, MachineInventory> LocalInventory = new Dictionary<string, MachineInventory>();
    public MachineInventory TransferInventory;
    public MassInventory CurrentNetworkStock;
    List<KeyValuePair<ItemBase, int>> StationOfferings;
    public bool LoadCheckIn = false;
    public bool LoadCheckOut = false;
    public bool FullEscape = false;
    public bool EmptyEscape = false;
    public bool CartCheckin = false;
    private bool CartLoadIn = false;
    private bool TransitCheckIn = true;
    public bool OffloadingExcess = true;
    private List<ItemBase> CheckInList = new List<ItemBase>();

    public FreightTrackJunction LastJunction;
    public FreightTrackJunction NextJunction;
    public FreightTrackJunction DestinationJunction;
    public Stack<FreightTrackJunction> JunctionRoute = new Stack<FreightTrackJunction>();
    public int DestinationDirection = -1;
    public FreightCartStation AssignedStation;

    public FreightCartMob(FreightCartMob.eMinecartType leType, ModCreateMobParameters modmobtype)
    : base(modmobtype, SpawnableObjectEnum.Minecart_T3)
  {
        ++FreightCartMob.MineCartID;
        this.rand = new System.Random();
        this.mrSpeed = 0.17345f;
        this.mrSpeedScalar = 1f;
        this.meType = leType;
        this.mType = MobType.Mod;
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
        if (this.meType == FreightCartMob.eMinecartType.FreightCartTour || this.meType == eMinecartType.FreightCartTourBasic)
            this.mObjectType = SpawnableObjectEnum.Minecart_T10;

        if (this.meType == FreightCartMob.eMinecartType.FreightCartMK1)
            this.mObjectType = SpawnableObjectEnum.Minecart_T4;

        base.SpawnGameObject();
        if (this.mWrapper.mGameObjectList == null)
        {
            this.mWrapper.mGameObjectList = new List<GameObject>();
            ManagerSync.CartLoader.Enqueue(this);
            if (!this.CartLoadIn)
                this.CartLoadIn = true;
            else
                ManagerSync.instance.CartCounter--;
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
        if (this.meType == FreightCartMob.eMinecartType.FreightCartTour)
        {
            this.mrSpeedScalar = 4f;
            this.mnMaxStorage = 0;
            this.TransferInventory = new MachineInventory(this, this.mnMaxStorage);
        }
        if (this.meType == FreightCartMob.eMinecartType.FreightCartTourBasic)
        {
            this.mrSpeedScalar = 2f;
            this.mnMaxStorage = 0;
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
        if (FreightCartMod.CartCheckin && !this.CartCheckin)
        {
            FreightCartMod.LiveCarts++;
            this.CartCheckin = true;
        }
        else if (!FreightCartMod.CartCheckin && this.CartCheckin)
            this.CartCheckin = false;

        this.TransitCheck();

        if (this.mnUsedStorage < 0)
            this.RecountInventory();

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

    /// <summary>
    ///     Registers the current inventory as 'in transit' on the network if the network registry wasn't loaded in the Master when the cart was loaded.
    ///     Not currently ideal as if the registry is missing for one item it will be skipped and we don't return as the previous ones have already been handled
    ///     would need to revert previous changes, check that all have valid registries first, or remember which ones still need to be registered as in transit...
    /// </summary>
    private void TransitCheck()
    {
        if (!this.TransitCheckIn)
        {
            bool checkreg = true;
            List<string> keys = this.LocalInventory.Keys.ToList();
            int count = keys.Count;
            for (int m = 0; m < count; m++)
            {
                string networkid = keys[m];
                int count2 = this.LocalInventory[networkid].Inventory.Count;
                for (int n = 0; n < count2; n++)
                {
                    ItemBase item = this.LocalInventory[networkid].Inventory[n];
                    checkreg = FreightCartManager.instance.NetworkAdd(networkid, item, item.GetAmount());
                    if (!checkreg)
                    {
                        if (n == 0)
                            return;
                        continue;
                    }
                }
            }
            this.TransitCheckIn = true;
        }
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
                this.LoadCheckOut = false;
                this.EmptyEscape = true;
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

                    //Check for all items that aren't needed by the network and transfer them if applicable
                    if (this.OffloadingExcess)
                        this.RetrieveExcess();
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
                        if (!string.IsNullOrEmpty(this.NetworkID) && this.LocalInventory.ContainsKey(this.NetworkID))
                            this.LocalInventory[this.NetworkID].AddItem(deposited.SetAmount(remainder));
                        else
                        {
                            if (!string.IsNullOrEmpty(this.NetworkID))
                            {
                                this.LocalInventory.Add(this.NetworkID, new MachineInventory(this, this.mnMaxStorage));
                                this.LocalInventory[this.NetworkID].AddItem(deposited.SetAmount(remainder));
                            }
                            else
                                Debug.LogWarning("NetworkID is null so we can't restore the item! Initialization error?");
                        }
                    }
                    //this.CurrentNetworkStock.Inventory[deposited]--;
                    this.mrVisualLoadTimer = 1f;
                }
                else if (this.TransferInventory.ItemCount() > 0)
                {
                    Debug.LogWarning("Freight cart still had items to transfer but lost mass storage! Remaining items: " + this.TransferInventory.ItemCount());
                }
                this.FCStation.mrCartOnUs = 1f;
                //this.FCStation.mrLastCartLeavTimer = this.mrLoadTimer;
                this.LoadCheckIn = true;
            }
        }
    }

    private void UpdateCartLoad()
    {
        if (string.IsNullOrEmpty(this.FCStation.NetworkID))
        {
            this.FullEscape = false;
            this.LeaveStation();
        }
        else
        {
            this.mBlockOffset = Vector3.zero;
            if ((int)this.FCStation.mValue != FreightStationValue)
                Debug.LogError(("Attempted to unload into the wrong type of station! (Expected FreightStationValue, got " + this.FCStation.mValue));
            if ((this.mnUsedStorage == this.mnMaxStorage || this.LoadCheckOut || this.mrLoadTimer > 30.0f) && !(FCStation.mbWaitForFullLoad && this.mnUsedStorage < this.mnMaxStorage))
            {
                //if (this.LoadCheckOut)
                //    FloatingCombatTextManager.instance.QueueText(this.mnX, this.mnY + 2L, this.mnZ, 1f, "LoadCheckOut", Color.red, 1f, 64f);
                //if (this.mrLoadTimer > 30.0)
                //    FloatingCombatTextManager.instance.QueueText(this.mnX, this.mnY + 2L, this.mnZ, 1f, "LoadTimer", Color.red, 1f, 64f);
                //if (this.mnUsedStorage == this.mnMaxStorage)
                //    FloatingCombatTextManager.instance.QueueText(this.mnX, this.mnY + 2L, this.mnZ, 1f, "MaxStorage", Color.red, 1f, 64f);
                this.LeaveStation();
            }
            else
            {
                //if (this.CurrentNetworkStock == null)
                //    this.CurrentNetworkStock = FreightCartManager.instance.GetNetworkStock(this.FCStation);
                this.FCStation.mrCartOnUs = 1f;
                //this.FCStation.mrLastCartLeavTimer = this.mrLoadTimer;
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
                                Debug.LogWarning("Freight cart withdrawal failed? Trying to collect item: " + remainder.ToString() + " with cart inventory of: " + this.mnUsedStorage.ToString() + " and inventory count: " + this.LocalInventory[FCStation.NetworkID].ItemCount());
                                if (this.mnUsedStorage < this.LocalInventory[FCStation.NetworkID].ItemCount())
                                    this.RecountInventory();
                                this.FCStation.DepositItem(remainder);
                            }
                        }
                        else
                        {
                            this.LoadCheckOut = true;
                            //FloatingCombatTextManager.instance.QueueText(this.mnX, this.mnY + 3L, this.mnZ, 1f, "null itemout", Color.red, 1f, 64f);
                        }
                    }
                    else
                    {
                        this.LoadCheckOut = true;
                        //FloatingCombatTextManager.instance.QueueText(this.mnX, this.mnY + 3L, this.mnZ, 1f, "null next offer", Color.red, 1f, 64f);
                    }
                }
                else
                {
                    this.LoadCheckOut = true;
                    //FloatingCombatTextManager.instance.QueueText(this.mnX, this.mnY + 3L, this.mnZ, 1f, "station has no offers list", Color.red, 1f, 64f);
                }

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

    private void RecountInventory()
    {
        if (this.LocalInventory == null || this.LocalInventory.Count == 0)
            return;
        var keys = this.LocalInventory.Keys.ToList();
        int itemcount = 0;
        for (int n = 0; n < keys.Count; n++)
            itemcount += this.LocalInventory[keys[n]].ItemCount();
        this.mnUsedStorage = itemcount;
        Debug.LogWarning("FreightCartMob Used Storage had to be updated for missing item count!");
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
            return item.NewInstance().SetAmount(1);
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
                    return item.NewInstance().SetAmount(1);
            }
        }
        this.FullEscape = true;
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
    }

    private void RetrieveExcess()
    {
        if (this.LocalInventory.ContainsKey(this.NetworkID))
        {
            for (int n = 0; n < this.LocalInventory[this.NetworkID].Inventory.Count; n++)
            {
                ItemBase item = this.LocalInventory[this.NetworkID].Inventory[n];
                if (FreightCartManager.instance.NetworkNeeds(this.NetworkID, item) <= 0)
                {
                    List<FreightRegistry> reg = FreightCartManager.instance.GetFreightEntries(this.NetworkID, this.FCStation.massStorageCrate);
                    if (reg.Exists(x => x.FreightItem.Compare(item)))
                        this.LocalInventory[this.NetworkID].RemoveWhiteList(ref this.TransferInventory.Inventory, item, this.TransferInventory.StorageCapacity, item.GetAmount());
                }
            }
        }
        this.OffloadingExcess = false;
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
            FloatingCombatTextManager.instance.QueueText(this.mnX, this.mnY + 1L, this.mnZ, 1f, "Loaded " + this.mnUsedStorage.ToString() + "x Freight", Color.cyan, 1f, 64f);
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
                        Debug.LogError("Error, freightCart has null under segment!");
                    if (this.mPrevGetSeg == null)
                        Debug.LogError("Error, prevseg was null!");
                    if (segment == null)
                        Debug.LogError("Error, old was null!");
                    if (this.mPrevGetSeg != segment)
                        Debug.LogWarning(("FreightCart is looking for a slope, and has had to check across segment boundaries for this![Old/New" + segment.GetName() + " -> " + this.mPrevGetSeg.GetName()));
                    Debug.LogWarning(("FreightCart hit air but located no underslope!" + TerrainData.GetNameForValue(type, lValue1)));
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
                        else if (this.mnUsedStorage == this.mnMaxStorage || this.FullEscape)
                        {
                            lValue1 = 0;
                            this.FullEscape = false;
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
                        else if (this.mnUsedStorage == 0 || this.EmptyEscape)
                        {
                            lValue1 = 0;
                            this.EmptyEscape = false;
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
                this.FullEscape = false;
                Vector3 vector3 = SegmentCustomRenderer.GetRotationQuaternion(lFlags1) * Vector3.forward;
                if (this.mLook == vector3 || this.mLook == -vector3)
                {
                    if (lValue1 == FreightStationValue)
                    {
                        this.EmptyEscape = false;
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
            else if (type == JunctionType)
            {
                ///////////////////////////////////////////////////////////////////////////////
                //Add handling for when a cart arrives at a junction other than the anticipated one!

                //Temp force to the old cross-over junction
                bool dest = false;

                if (this.meLoadState == FreightCartMob.eLoadState.eLoading || this.meLoadState == FreightCartMob.eLoadState.eUnloading || this.meLoadState == FreightCartMob.eLoadState.eCharging)
                    this.LeaveStation();
                this.meLoadState = FreightCartMob.eLoadState.eTravelling;
                Vector3 vector3_1 = SegmentCustomRenderer.GetRotationQuaternion(lFlags1) * Vector3.forward;
                vector3_1.Normalize();
                vector3_1.x = vector3_1.x >= -0.5 ? (vector3_1.x <= 0.5 ? 0.0f : 1f) : -1f;
                vector3_1.y = vector3_1.y >= -0.5 ? (vector3_1.y <= 0.5 ? 0.0f : 1f) : -1f;
                vector3_1.z = vector3_1.z >= -0.5 ? (vector3_1.z <= 0.5 ? 0.0f : 1f) : -1f;
                this.mLook.y = 0.0f;
                this.mLook.Normalize();
                
                FreightTrackJunction junction = this.mUnderSegment.FetchEntity(eSegmentEntity.Mod, this.mnX, this.mnY - 1L, this.mnZ) as FreightTrackJunction;
                if (junction == null)
                    this.RemoveCart("Arrived at null track junction?");

                if (this.NextJunction != null && this.NextJunction != junction)
                {
                    Debug.LogWarning("FreightCartMob arrived at junction other than intended.  Suspect track network outdated... rebuilding...");
                    if (this.LastJunction == null)
                        Debug.LogWarning("FreightCartMob NextJunction wasn't null when navigating to wrong junction but last is null?");
                    else
                    {
                        this.LastJunction.InvalidConnection(this.NextJunction);
                        List<FreightTrackJunction> junctions = new List<FreightTrackJunction>(2);
                        junctions.Add(this.LastJunction);
                        junctions.Add(this.NextJunction);
                        this.NextJunction.TrackNetwork.NetworkIntegrityCheck(junctions);
                    }
                }

                if (this.DestinationJunction == null)
                    this.ChooseDestination(junction);

                if (this.DestinationJunction != null)
                {
                    //Debug.LogWarning("We have a destination junction with ID: " + this.DestinationJunction.JunctionID.ToString());
                    if (this.DestinationJunction != junction)
                    {
                        //Debug.LogWarning("Freight Cart Destination junction != current junction");
                        this.LastJunction = junction;
                        if (this.JunctionRoute.Count != 0)
                        {
                            this.NextJunction = this.JunctionRoute.Pop();
                            dest = this.GetLookFromDirection(junction.GetConnectedDirection(this.NextJunction));
                        }
                    }
                    else if (junction == this.DestinationJunction)
                    {
                        //Debug.LogWarning("The current junction is the destination");
                        dest = this.GetLookFromDirection(this.DestinationDirection);
                        if (this.DestinationDirection >= 0 && this.DestinationDirection <= 3)
                            this.NextJunction = junction.ConnectedJunctions[this.DestinationDirection];
                        this.DestinationDirection = -1;
                        this.DestinationJunction = null;
                        this.LastJunction = junction;
                        //Need to set expected junction for track network verification purposes on RemoveCart
                    }
                }
                //Debug.LogWarning("Freight Cart after pathfinding dest bool is: " + dest.ToString());
                //Vanilla code for a straight track -> filler for now (default if destination setting fails!)
                if (vector3_1.y > 0.5 || vector3_1.y < -0.5)
                    this.RemoveCart("Track Straight hitting non-straight piece!");
                else if (!(this.mLook == vector3_1) && !(this.mLook == -vector3_1) && (Vector3.Dot(this.mLook, vector3_1) > 0.05f) && !dest)
                    this.mLook = vector3_1;
                else if (!(this.mLook == vector3_1) && !(this.mLook == -vector3_1) && (Vector3.Dot(this.mLook, vector3_1) < -0.05f) && !dest)
                    this.mLook = -vector3_1;
                else if (Vector3.Dot(this.mLook, vector3_1) < 0.05f && !dest)
                {
                    this.mLook.x = this.mLook.x >= -0.5 ? (this.mLook.x <= 0.5 ? 0f : 1f) : -1f;
                    this.mLook.z = this.mLook.z >= -0.5 ? (this.mLook.z <= 0.5 ? 0f : 1f) : -1f;
                }
                //else
                //{
                //    Debug.LogWarning("Freight Cart skipped other processes to use decided look vector");
                //}
            }
            else if (type == TourStationType)
            {
                //Facing opposite of station, cart has arrived otherwise cart continues with original look
                if (Vector3.Dot(this.mLook, SegmentCustomRenderer.GetRotationQuaternion(lFlags1) * Vector3.forward) < -0.5 && (this.meType == eMinecartType.FreightCartTour || this.meType == eMinecartType.FreightCartTourBasic))
                {
                    TourCartStation Station = this.mUnderSegment.FetchEntity(eSegmentEntity.Mod, this.mnX, this.mnY - 1L, this.mnZ) as TourCartStation;
                    bool insert = false;
                    ItemBase itemBase = null;
                    if (this.meType == eMinecartType.FreightCartTour)
                        itemBase = ItemManager.SpawnItem(111);
                    else if (this.meType == eMinecartType.FreightCartTourBasic)
                        itemBase = ItemManager.SpawnItem(ModManager.mModMappings.ItemsByKey["steveman0.FreightTourBasic"].ItemId);
                    if (Station != null && Station.hopper != null && Station.hopper.IsNotFull() && itemBase != null)
                        insert = Station.hopper.TryInsert(Station, itemBase);
                    else if (WorldScript.mLocalPlayer.mInventory.CanFit(itemBase))
                        insert = WorldScript.mLocalPlayer.mInventory.AddItem(itemBase);
                    if (WorldScript.mbIsServer && !insert)
                    {
                        DroppedItemData droppedItemData = ItemManager.instance.DropItem(itemBase, this.mnX, this.mnY, this.mnZ, Vector3.up);
                        if (droppedItemData != null)
                            droppedItemData.mrLifeRemaining = 36000f;
                    }
                    MobManager.instance.DestroyMob(this);
                    WorldScript.instance.localPlayerInstance.mbRidingCart = false;
                    WorldScript.instance.localPlayerInstance.mRideable = null;
                    WorldScript.instance.localPlayerInstance.mbGravity = true;
                    this.mnUpdatesPerTick = 0;
                }
            }
            else if (type == 0)
                Debug.LogWarning("Minecart hit edge of loaded area?");
            else
                this.RemoveCart("Minecart hit " + TerrainData.GetNameForValue(type, lValue1) + "\nMinecart was on update " + this.mnCurrentUpdateTick + " of " + this.mnUpdatesPerTick + "\nSpeed was " + this.mrSpeed + " and offset was " + this.mBlockOffset + "\nSpeed per tick was " + this.mrSpeedPerTick + " and current look was " + this.mLook);
        }
    }

    private void ChooseDestination(FreightTrackJunction currentloc)
    {
        if (currentloc == null || currentloc.TrackNetwork == null)
        {
            Debug.LogWarning("FreightCartMob Trying to ChooseDestination for null junction or junction with null network!");
            return;
        }
        //Debug.LogWarning("Freight Cart ChoosingDestination");
        if (this.mnUsedStorage > 0)
        {
            //Debug.LogWarning("Freight Cart based on filled inventory");
            //Go to requesting station
            if (this.LocalInventory == null || this.LocalInventory.Keys.Count == 0)
            {
                Debug.LogWarning("ChooseDestination trying to access null inventory?");
                return;
            }
            List<string> keys = this.LocalInventory.Keys.ToList();
            if (keys.Count == 0)
            {
                Debug.LogWarning("FreightCart tried to choose destination for filled cart but no inventory? Used storage: " + this.mnUsedStorage.ToString());
                return;
            }
            string networkid = keys[0];
            if (this.LocalInventory[networkid].Inventory.Count == 0)
            {
                Debug.LogWarning("FreightCart tried to choose destination while having empty inventory with used storage?");
                return;
            }
            FreightCartStation station = null;
            for (int n = 0; n < this.LocalInventory[networkid].Inventory.Count; n++)
            {
                ItemBase item = this.LocalInventory[networkid].Inventory[n];
                if (FreightCartManager.instance.NetworkNeeds(networkid, item) <= 0)
                    this.OffloadingExcess = true;
                station = FreightCartManager.instance.GetStation(networkid, item, this.OffloadingExcess);
                if (station != null)
                {

                    break;
                }
            }
            if (station == null)
            {
                if (this.mnUsedStorage != this.mnMaxStorage)
                    station = FreightCartManager.instance.GetStation(networkid, null, false);
                if (station == null)
                {
                    Debug.LogWarning("FreightCartManager returned null station for ChooseDestination of FreightCart");
                    return;
                }
            }
            if (station.ClosestJunction == null || station.JunctionDirection == -1)
            {
                Debug.LogWarning("Trying to navigate to a station with null ClosestJunction?");
                return;
            }
            this.DestinationJunction = station.ClosestJunction;
            this.DestinationDirection = station.JunctionDirection;
            this.JunctionRoute = currentloc.TrackNetwork.RouteFind(currentloc, this.DestinationJunction);
        }
        else
        {
            //Debug.LogWarning("Freight Cart based on empty cart");
            if (this.AssignedStation == null || this.AssignedStation.AvailableCarts > this.AssignedStation.AssignedCarts)
                this.TryAssignCart(currentloc.TrackNetwork);
            if (this.AssignedStation != null)
            {
                //Debug.LogWarning("Freight Cart assigned station final selection, direction: " + this.AssignedStation.JunctionDirection.ToString());
                //Debug.LogWarning("Available/Assigned station available carts: " + this.AssignedStation.AvailableCarts.ToString() + "/" + this.AssignedStation.AssignedCarts.ToString());
                this.DestinationJunction = this.AssignedStation.ClosestJunction;
                this.DestinationDirection = this.AssignedStation.JunctionDirection;
                this.JunctionRoute = null;
                this.JunctionRoute = currentloc.TrackNetwork.RouteFind(currentloc, this.DestinationJunction);
            }
        }
    }

    private void TryAssignCart(FreightTrackNetwork network)
    {
        //Debug.LogWarning("Freight Cart Trying to assign cart");
        for (int n = 0; n < network.TrackSegments.Count; n++)
        {
            FreightTrackSegment seg = network.TrackSegments[n];
            //Debug.LogWarning("seg station count: " + seg.Stations.Count);
            for (int m = 0; m < seg.Stations.Count; m++)
            {
                FreightCartStation station = seg.Stations[m];
                if (!this.CheckCartTier(station.CartTier))
                    continue;
                if (station.AvailableCarts < station.AssignedCarts)
                {
                    //Debug.LogWarning("Freight Cart station for assignment found on direction " + station.JunctionDirection.ToString());
                    if (this.AssignedStation != null)
                    {
                        this.AssignedStation.AvailableCarts--;
                        this.AssignedStation.CartList.Remove(this);
                    }
                    this.AssignedStation = station;
                    this.AssignedStation.AvailableCarts++;
                    this.AssignedStation.CartList.Add(this);
                    return;
                }
            }
        }
        //No stations still requiring assigned carts... assign 'intelligently'
        this.AssignedStation = FreightCartManager.instance.GetNeedyStation();
        //Debug.LogWarning("Freight Cart had to revert to getting needy station");
    }

    private bool CheckCartTier(int carttier)
    {
        if (carttier == 0)
            return true;
        else if (carttier == 1 && this.meType > 0)
            return true;
        else if (carttier == 2 && this.meType == eMinecartType.FreightCart_T4)
            return true;
        else
            return false;
    }

    /// <summary>
    ///     Set the Freight Cart look for leaving a junction in the given direction
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    private bool GetLookFromDirection(int direction)
    {
        if (direction == 0)
            this.mLook = Vector3.right;
        else if (direction == 1)
            this.mLook = Vector3.forward;
        else if (direction == 2)
            this.mLook = Vector3.left;
        else if (direction == 3)
            this.mLook = Vector3.back;
        else
        {
            Debug.LogWarning("FreightCartMob attempted to set look by direction but failed with direction: " + direction.ToString());
            return false;
        }
        //Debug.LogWarning("Look selected from direction with look: " + this.mLook.ToString() + " from direction: " + direction.ToString());
        return true;
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
            //float num = this.mBlockOffset.y;
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
        if (this.meType == FreightCartMob.eMinecartType.FreightCartTour)
            itemID = ItemEntries.TourCart;
        if (this.meType == FreightCartMob.eMinecartType.FreightCartTourBasic)
            itemID = ModManager.mModMappings.ItemsByKey["steveman0.FreightTourBasic"].ItemId;

        if (WorldScript.mbIsServer)
        {
            ItemBase itemBase = ItemManager.SpawnItem(itemID);
            DroppedItemData droppedItemData = ItemManager.instance.DropItem(itemBase, this.mnX, this.mnY, this.mnZ, Vector3.up);
            if (droppedItemData != null)
                droppedItemData.mrLifeRemaining = 36000f;
        }
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

        if (this.NextJunction != null)
        {
            Debug.LogWarning("FreightCartMob fell off track attempting to get to junction (ID: " + this.NextJunction.JunctionID.ToString() +").  Suspect track network outdated... rebuilding...");
            if (this.LastJunction == null)
                Debug.LogWarning("FreightCartMob NextJunction wasn't null when navigating to wrong junction but last is null?");
            else
            {
                this.LastJunction.InvalidConnection(this.NextJunction);
                this.NextJunction.TrackNetwork.NetworkIntegrityCheck(this.LastJunction, this.NextJunction, null, null);
            }
        }

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
        this.mnUsedStorage = 0;
        for (int index = 0; index < count; index++)
        {
            string networkid = reader.ReadString();
            MachineInventory inv = new MachineInventory(this, this.mnMaxStorage);
            inv.ReadInventory(reader);
            if (!this.LocalInventory.ContainsKey(networkid))
                this.LocalInventory.Add(networkid, inv);
            else
                this.LocalInventory[networkid] = inv;
            this.mnUsedStorage += inv.ItemCount();
            int count2 = this.LocalInventory[networkid].Inventory.Count;
            for (int n = 0; n < count2; n++)
            {
                ItemBase item = this.LocalInventory[networkid].Inventory[n];
                bool checkreg = FreightCartManager.instance.NetworkAdd(networkid, item, item.GetAmount());
                if (!checkreg)
                {
                    this.TransitCheckIn = false;
                    break;
                }
            }
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
        FreightCartTour,
        FreightCartTourBasic,
        //Basic,
        //Fast,
        //Large,
        //Bulk,
        //Tour,
    }
}


