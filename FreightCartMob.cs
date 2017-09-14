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
    //public List<CubeCoord> mVisitedLocations = new List<CubeCoord>();
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
    private int SlopeCatch = -1;   //Adding this to try to catch odd derailing carts

    private float lrTimer;
    private int mnUpdatesPerTick;
    private float mrSpeedPerTick;
    private int mnUpdates;
    private int mnCurrentUpdateTick;
    private float mrSpeedBoost;
    private Segment mUnderSegment;
    private Segment mPrevGetSeg;
    private bool SlowTrack;

    public static ushort FreightStationType = ModManager.mModMappings.CubesByKey["steveman0.FreightCartStation"].CubeType;
    public static ushort TourStationType = ModManager.mModMappings.CubesByKey["steveman0.TourCartStation"].CubeType;
    public static ushort JunctionType = ModManager.mModMappings.CubesByKey["steveman0.TrackJunction"].CubeType;
    public static ushort JunctionVal = ModManager.mModMappings.CubesByKey["steveman0.TrackJunction"].ValuesByKey["steveman0.TrackJunctionStandard"].Value;
    public static ushort ScrapJunctionVal = ModManager.mModMappings.CubesByKey["steveman0.TrackJunction"].ValuesByKey["steveman0.ScrapTrackJunction"].Value;
    public static ushort ScrapTrackType = ModManager.mModMappings.CubesByKey["steveman0.ScrapTrack"].CubeType;
    public static ushort ScrapStraightVal = ModManager.mModMappings.CubesByKey["steveman0.ScrapTrack"].ValuesByKey["steveman0.ScrapTrackStraight"].Value;
    public static ushort ScrapCornerVal = ModManager.mModMappings.CubesByKey["steveman0.ScrapTrack"].ValuesByKey["steveman0.ScrapTrackCorner"].Value;
    public static ushort ScrapSlopeVal = ModManager.mModMappings.CubesByKey["steveman0.ScrapTrack"].ValuesByKey["steveman0.ScrapTrackSlope"].Value;
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
    public bool CartQueuelock = false;
    private bool TransitCheckIn = true;
    public bool OffloadingExcess = true;
    private List<ItemBase> CheckInList = new List<ItemBase>();
    public ItemBase OreFreighterItem;

    public FreightTrackJunction LastJunction;
    public FreightTrackJunction NextJunction;
    public FreightTrackJunction DestinationJunction;
    public Stack<FreightTrackJunction> JunctionRoute = new Stack<FreightTrackJunction>();
    public int DestinationDirection = -1;
    public FreightCartStation AssignedStation;

    public static bool ReadNullDebug = false;

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
        if (this.meType == FreightCartMob.eMinecartType.FreightCart_T1 || this.meType == eMinecartType.OreFreighter_T1)
            this.mObjectType = SpawnableObjectEnum.Minecart_T1;
        if (this.meType == FreightCartMob.eMinecartType.FreightCart_T2 || this.meType == eMinecartType.OreFreighter_T2)
            this.mObjectType = SpawnableObjectEnum.Minecart_T2;
        if (this.meType == FreightCartMob.eMinecartType.FreightCart_T3 || this.meType == eMinecartType.OreFreighter_T3)
            this.mObjectType = SpawnableObjectEnum.Minecart_T3;
        if (this.meType == FreightCartMob.eMinecartType.FreightCart_T4 || this.meType == eMinecartType.OreFreighter_T4)
            this.mObjectType = SpawnableObjectEnum.Minecart_T4;
        if (this.meType == FreightCartMob.eMinecartType.FreightCartTour || this.meType == eMinecartType.FreightCartTourBasic)
            this.mObjectType = SpawnableObjectEnum.Minecart_T10;

        if (this.meType == FreightCartMob.eMinecartType.FreightCartMK1)
            this.mObjectType = SpawnableObjectEnum.Minecart_T4;

        base.SpawnGameObject();
        if (this.mWrapper.mGameObjectList == null && !CartQueuelock)
        {
            //this.mWrapper.mGameObjectList = new List<GameObject>();
            ManagerSync.CartLoader.Enqueue(this);
            this.CartQueuelock = true; // Prevent requeueing for another script if the manager gets bogged down by many cart requests
            if (!this.CartLoadIn)
                this.CartLoadIn = true;
            else
                ManagerSync.instance.CartCounter--;
        }
    }

    private void SetStatsFromType()
    {
        if (this.meType == FreightCartMob.eMinecartType.ScrapCartMK1)
        {
            this.mrSpeedScalar = 0.5f;
            this.mnMaxStorage = 5;
            this.IsScrapCart = true;
            this.TransferInventory = new MachineInventory(this, this.mnMaxStorage);
        }
        if (this.meType == eMinecartType.ScrapOreFreighterMK1)
        {
            this.mrSpeedScalar = 0.5f;
            this.mnMaxStorage = 50;
            this.IsScrapCart = true;
            this.IsOreFreighter = true;
            this.TransferInventory = new MachineInventory(this, this.mnMaxStorage);
        }
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
        if (this.meType == FreightCartMob.eMinecartType.OreFreighter_T1)
        {
            this.mrSpeedScalar = 1f;
            this.mnMaxStorage = 250;
            this.IsOreFreighter = true;
            this.TransferInventory = new MachineInventory(this, this.mnMaxStorage);
        }
        if (this.meType == FreightCartMob.eMinecartType.OreFreighter_T2)
        {
            this.mrSpeedScalar = 2f;
            this.mnMaxStorage = 250;
            this.IsOreFreighter = true;
            this.TransferInventory = new MachineInventory(this, this.mnMaxStorage);
        }
        if (this.meType == FreightCartMob.eMinecartType.OreFreighter_T3)
        {
            this.mrSpeedScalar = 1f;
            this.mnMaxStorage = 500;
            this.IsOreFreighter = true;
            this.TransferInventory = new MachineInventory(this, this.mnMaxStorage);
        }
        if (this.meType == FreightCartMob.eMinecartType.OreFreighter_T4)
        {
            this.mrSpeedScalar = 2f;
            this.mnMaxStorage = 500;
            this.IsOreFreighter = true;
            this.TransferInventory = new MachineInventory(this, this.mnMaxStorage);
        }
    }

    public bool IsOreFreighter
    {
        get; private set;
    }

    public bool IsScrapCart
    {
        get; private set;
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

        //if (this.AssignedStation == null)
        //{

        //}

        if (this.mnUsedStorage < 0)
            this.RecountInventory();

        ++this.mnUpdates;
        if (this.SlopeCatch - this.mnUpdates > 5)
            this.SlopeCatch = -1;
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
        if (this.SlowTrack && !this.IsScrapCart) // Slow down regular carts on scrap track
        {
            float speedfactor = 0.4f * this.mrSpeedScalar;
            if (this.mrSpeed > (double)speedfactor)
            {
                this.mrSpeed *= 0.4f;
                if (this.mrSpeed < (double)speedfactor)
                    this.mrSpeed = speedfactor;
                this.ConfigSpeedTicks();
            }
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
            if (this.LocalInventory == null)
            {
                Debug.LogWarning("FreightCartMob is trying to do transit check prior to having an inventory!");
                return;
            }
            List<string> keys = this.LocalInventory.Keys.ToList();
            int count = keys.Count;
            for (int m = 0; m < count; m++)
            {
                string networkid = keys[m];
                if (string.IsNullOrEmpty(networkid) || this.LocalInventory[networkid] == null || this.LocalInventory[networkid].Inventory == null)
                {
                    Debug.LogWarning("FreightCartMob TransitCheck found null network or inventory!  Networking failure?");
                    return;
                }
                MachineInventory checkinv;
                if (this.LocalInventory.TryGetValue(networkid, out checkinv))
                {
                    int count2 = checkinv.Inventory.Count;
                    for (int n = 0; n < count2; n++)
                    {
                        ItemBase item = checkinv.Inventory[n];
                        if (item == null)
                        {
                            Debug.LogWarning("FreightCartMob Transit Check got a null item.  Suspect data corruption!");
                            continue;
                        }
                        if (FreightCartManager.instance == null)
                            return;
                        checkreg = FreightCartManager.instance.NetworkAdd(networkid, item, item.GetAmount());
                        if (!checkreg)
                        {
                            if (n == 0)
                                return;
                            continue;
                        }
                    }
                }
            }
            this.TransitCheckIn = true;
        }
    }

    private void UpdateLoadStatus()
    {
        this.mrLoadTimer += MobUpdateThread.mrPreviousUpdateTimeStep;
        if (this.FCStation == null && (this.meLoadState == eLoadState.eLoading || this.meLoadState == eLoadState.eUnloading) || (this.FCStation != null && this.FCStation.mbDelete))
        {
            Debug.LogError((object)"Error, our station went null or player destroyed it while it had a cart!");
            this.LeaveStation();
        }
        // Scrap carts only check load/unload once a second-ish effectively slowing their throughput at a station to 60 items/min.
        if (this.IsScrapCart && this.mnUpdates % 5 > 0)
            return;
        if (this.meLoadState == FreightCartMob.eLoadState.eUnloading)
            this.UpdateCartUnload();
        if (this.meLoadState == FreightCartMob.eLoadState.eLoading)
            this.UpdateCartLoad();
    }

    private bool HasAttachedConsumer()
    {
        return (this.FCStation.ConnectedInventory != null || this.FCStation.AttachedInterface != null || this.FCStation.HopperInterface != null);
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
            if (this.FCStation.mValue != FreightStationValue)
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
                if (HasAttachedConsumer() && this.TransferInventory.ItemCount() == 0 && this.LocalInventory.ContainsKey(this.NetworkID) && this.LocalInventory[this.NetworkID].ItemCount() != 0 && !string.IsNullOrEmpty(this.FCStation.NetworkID))
                {
                    List<KeyValuePair<ItemBase, int>> needs = FreightCartManager.instance.GetStationNeeds(this.FCStation);
                    for (int index = 0; index < needs.Count; index++)
                    {
                        this.RetrieveItem(needs[index]);
                        this.mrVisualLoadTimer = 1f;
                    }

                    //Check for all items that aren't needed by the network and transfer them if applicable
                    if (this.OffloadingExcess && this.FCStation.massStorageCrate != null)
                        this.RetrieveExcess();
                }
                if (this.TransferInventory.ItemCount() > 0 && HasAttachedConsumer() && !(this.FCStation.HopperInterface != null && this.FCStation.HopperInterface.Machine.IsFull()))
                {
                    ItemBase deposited = this.TransferInventory.RemoveAnySingle(1);
                    int remainder = this.FCStation.DepositItem(deposited);
                    if (remainder == 0)
                    {
                        FreightCartManager.instance.NetworkRemove(this.NetworkID, deposited, 1);
                        this.mnUsedStorage--;
                        this.mrVisualLoadTimer = 1f;
                        if (this.mnUsedStorage == 0 && this.OreFreighterItem != null)
                            this.OreFreighterItem = null;
                    }
                    else
                    {
                        if (deposited != null && this.FCStation.ConnectedInventory != null)
                        {
                            Debug.LogWarning("Station named " + (!String.IsNullOrEmpty(this.FCStation.StationName) ? this.FCStation.StationName : "UNNAMED") + " failed to deposit item " + deposited.ToString() + " into inventory " + (!String.IsNullOrEmpty(this.FCStation.ConnectedInventory.Name) ? this.FCStation.ConnectedInventory.Name : "UNNAMED") + " at FreightCart unload step. Returning to local inventory.");
                        }
                        else if (this.FCStation.HopperInterface != null)
                            Debug.Log("FreightCart attempted to deposit item at FreightInterface but interface was full?  We already check for this!");
                        else
                            Debug.LogWarning("FreightCartMob failed to deposit item to freight interface most likely!");
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
                }
                else if (this.TransferInventory.ItemCount() > 0 && !(this.FCStation.HopperInterface != null && this.FCStation.HopperInterface.Machine.IsFull()))
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
                // Refresh the offerings for the wait for full case in case items change while we're waiting
                if (HasAttachedConsumer() && this.StationOfferings == null || this.mrLoadTimer > 35)
                {
                    this.StationOfferings = FreightCartManager.instance.GetStationOfferings(this.FCStation);
                    this.mrLoadTimer = 0;
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
                                if (this.IsOreFreighter)
                                    this.OreFreighterItem = item;
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

        if (FreightCartManager.instance.NetworkNeeds(this.NetworkID, item) > 0 && this.FreighterAccepted(item))
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
                if (!this.FreighterAccepted(item))
                    continue;
                if (FreightCartManager.instance.NetworkNeeds(this.NetworkID, item) > 0)
                    return item.NewInstance().SetAmount(1);
            }
        }
        this.FullEscape = true;
        return null;
    }

    private bool FreighterAccepted(ItemBase item)
    {
        if (!this.IsOreFreighter)
            return true;
        else if (this.OreFreighterItem != null && item.Compare(this.OreFreighterItem))
            return true;
        else if (this.OreFreighterItem == null && item.mType == ItemType.ItemCubeStack && CubeHelper.IsOre((item as ItemCubeStack).mCubeType))
            return true;
        return false;
    }

    private void RetrieveItem(KeyValuePair<ItemBase, int> keypair)
    {
        ItemBase item = keypair.Key;
        int amount = keypair.Value;
        this.MarkDirtyDelayed();
        if (this.LocalInventory.ContainsKey(this.NetworkID))
        {
            if (this.LocalInventory[this.NetworkID].IsEmpty())
            {
                Debug.LogError("Error, can't retrieve item from Minecart as it's empty.");
                this.LocalInventory.Remove(this.NetworkID);
            }
            this.LocalInventory[this.NetworkID].RemoveWhiteList(ref this.TransferInventory.Inventory, item, this.TransferInventory.StorageCapacity, amount);
            if (this.LocalInventory[this.NetworkID].IsEmpty())
            {
                this.LocalInventory.Remove(this.NetworkID);
                if (this.IsOreFreighter && this.mnUsedStorage <= 0)
                    this.OreFreighterItem = null;
            }
        }
    }

    private void RetrieveExcess()
    {
        int count = 0;
        ItemBase item = null;
        if (this.LocalInventory.ContainsKey(this.NetworkID))
        {
            count = this.LocalInventory[this.NetworkID].ItemCount();
            for (int n = 0; n < this.LocalInventory[this.NetworkID].Inventory.Count; n++)
            {
                ItemBase item2 = this.LocalInventory[this.NetworkID].Inventory[n];
                if (FreightCartManager.instance.RegistryDeficit(this.NetworkID, item) == 0)
                {
                    List<FreightRegistry> reg = FreightCartManager.instance.GetFreightEntries(this.NetworkID, this.FCStation.massStorageCrate);
                    if (reg.Exists(x => x.FreightItem.Compare(item)))
                    {
                        item = item2;
                        this.LocalInventory[this.NetworkID].RemoveWhiteList(ref this.TransferInventory.Inventory, item, this.TransferInventory.StorageCapacity, item.GetAmount());
                        if (this.LocalInventory[this.NetworkID].IsEmpty())
                        {
                            this.LocalInventory.Remove(this.NetworkID);
                            break;
                        }
                    }
                }
            }
            //if (this.LocalInventory.ContainsKey(this.NetworkID))
            //    Debug.LogWarning("FreightCartMob offloaded " + (count - this.LocalInventory[this.NetworkID].ItemCount()).ToString() + " items as excess to " + (string.IsNullOrEmpty(this.FCStation.StationName) ? "UNAMED" : this.FCStation.StationName) + " freight cart station from network: " + this.NetworkID + ".  Last item was: " + (item != null ? (item.ToString() + " and deficit: " + FreightCartManager.instance.NetworkNeeds(this.NetworkID, item)) : ""));
            //else
            //    Debug.LogWarning("FreightCartMob offloaded " + count + " items as excess to " + (string.IsNullOrEmpty(this.FCStation.StationName) ? "UNAMED" : this.FCStation.StationName) + " freight cart station" + " freight cart station from network: " + this.NetworkID + ".  Last item was: " + (item != null ? (item.ToString() + " and deficit: " + FreightCartManager.instance.NetworkNeeds(this.NetworkID, item)) : ""));
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
                if ((type == 538 && lValue1 == 2) || (type == ScrapTrackType && lValue1 == ScrapSlopeVal))
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
            if (type == 538 || type == ScrapTrackType)
            {
                this.SlowTrack = type == 538 ? false : true;
                if (this.meLoadState == FreightCartMob.eLoadState.eLoading || this.meLoadState == FreightCartMob.eLoadState.eUnloading || this.meLoadState == FreightCartMob.eLoadState.eCharging)
                    this.LeaveStation();
                this.meLoadState = FreightCartMob.eLoadState.eTravelling;
                Vector3 vector3_1 = SegmentCustomRenderer.GetRotationQuaternion(lFlags1) * Vector3.forward;
                vector3_1.Normalize();
                vector3_1.x = vector3_1.x >= -0.5 ? (vector3_1.x <= 0.5 ? 0.0f : 1f) : -1f;
                vector3_1.y = vector3_1.y >= -0.5 ? (vector3_1.y <= 0.5 ? 0.0f : 1f) : -1f;
                vector3_1.z = vector3_1.z >= -0.5 ? (vector3_1.z <= 0.5 ? 0.0f : 1f) : -1f;
                if (type == 538 && lValue1 == 3)
                {
                    this.mLook = -this.mLook;
                }
                else
                {
                    if (type == 538 && lValue1 == 4)
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
                    if (type == 538 && lValue1 == 5)
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
                    if ((type == 538 && lValue1 == 0) || (type == ScrapTrackType && lValue1 == ScrapStraightVal))
                    {
                        this.mLook.y = 0.0f;
                        this.mLook.Normalize();
                        if (vector3_1.y > 0.5 || vector3_1.y < -0.5)
                            this.RemoveCart("Track Straight hitting non-straight piece!");
                        else if (!(this.mLook == vector3_1) && !(this.mLook == -vector3_1))
                            this.mLook = vector3_1;
                    }
                    if ((type == 538 && lValue1 == 1) || (type == ScrapTrackType && lValue1 == ScrapCornerVal))
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
                    if ((type == 538 && lValue1 != 2) || (type == ScrapTrackType && lValue1 != ScrapSlopeVal))
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
               "Freightcart hit the side of a slope (Look was",
               this.mLook,
               " but track was ",
               vector3_1
                        }));
                }
            }
            else if (type == 539)
            {
                this.SlowTrack = false;
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
                this.SlowTrack = false;
                this.FullEscape = false;
                Vector3 vector3 = SegmentCustomRenderer.GetRotationQuaternion(lFlags1) * Vector3.forward;
                if (this.mLook == vector3 || this.mLook == -vector3)
                {
                    if (lValue1 == FreightStationValue)
                    {
                        this.EmptyEscape = false;
                        FreightCartStation Station = this.mUnderSegment.FetchEntity(eSegmentEntity.Mod, this.mnX, this.mnY - 1L, this.mnZ) as FreightCartStation;
                        if (Station == null || string.IsNullOrEmpty(Station.NetworkID))
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
                this.SlowTrack = lValue1 == ScrapJunctionVal ? true : false;
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
                {
                    this.RemoveCart("Arrived at null track junction?");
                    return;
                }

                // Check if we're at the same junction... maybe it's checking itself twice?
                if (this.NextJunction != null && this.NextJunction != junction && junction != this.LastJunction)
                {
                    //TODO: Add a check to try to navigate to the destination from here... DONE!
                    // If we can find it then maybe the network was already rebuilt?  Or at least safe...
                    // This might cut down significantly on the false rebuilds.
                    Stack<FreightTrackJunction> updatedroute = junction.TrackNetwork.RouteFind(junction, this.DestinationJunction);

                    if (updatedroute.Count > 0)
                    {
                        this.JunctionRoute = updatedroute;
                    }
                    else
                    {
                        if (junction != null && this.NextJunction != null)
                            Debug.LogWarning("FreightCartMob arrived at junction with ID " + junction.JunctionID.ToString() + " expecting junction " + this.NextJunction.JunctionID.ToString() + ".  Suspect track network outdated... rebuilding...");
                        if (this.LastJunction == null)
                            Debug.LogWarning("FreightCartMob NextJunction wasn't null when navigating to wrong junction but last is null?");
                        else if (ManagerSync.RebuildTimer <= 0 && WorldScript.mbIsServer) //Don't rebuild on clients... seems to break things since data is missing....
                        {
                            this.LastJunction.InvalidConnection(this.NextJunction);
                            List<FreightTrackJunction> junctions = new List<FreightTrackJunction>(2);
                            junctions.Add(this.LastJunction);
                            junctions.Add(this.NextJunction);
                            this.NextJunction.TrackNetwork.NetworkIntegrityCheck(junctions);
                            ManagerSync.RebuildTimer = 60;
                        }
                        //Cart is lost so let's clear our destination and recalculate
                        this.NextJunction = null;
                        this.DestinationJunction = null;
                    }
                }

                if (this.DestinationJunction == null)
                    this.ChooseDestination(junction);
                this.LastJunction = junction;
                if (this.DestinationJunction != null)
                {
                    //Debug.LogWarning("We have a destination junction with ID: " + this.DestinationJunction.JunctionID.ToString());
                    if (this.DestinationJunction != junction)
                    {
                        //Debug.LogWarning("Freight Cart Destination junction != current junction");
                        if (this.JunctionRoute.Count != 0)
                        {
                            this.NextJunction = this.JunctionRoute.Pop();
                            dest = this.GetLookFromDirection(junction.GetConnectedDirection(this.NextJunction));
                            if (!dest)
                                Debug.LogWarning("FreightCartMob Failed to get look from direction with next junction");
                        }
                        else
                        {
                            //if (WorldScript.mbIsServer)
                                //Debug.LogWarning("FreightCartMob has a destination junction but no junctionroute to navigate!");
                            dest = this.GetLookFromDirection(-1);
                            if (!dest)
                                Debug.LogWarning("FreightCartMob failed to get look from direction with no route case!");
                        }
                    }
                    else
                    {
                        //Debug.LogWarning("The current junction is the destination");
                        dest = this.GetLookFromDirection(this.DestinationDirection);
                        if (!dest)
                            Debug.LogWarning("FreightCartMob failed to get look from direction with final junction");

                        // Don't set the next junction to avoid the risk of rebuilding the network unnecessarily because of a long hold at a station
                        //if (this.DestinationDirection >= 0 && this.DestinationDirection <= 3)
                        //    this.NextJunction = junction.ConnectedJunctions[this.DestinationDirection];
                        this.NextJunction = null;
                        this.DestinationDirection = -1;
                        this.DestinationJunction = null;
                        //Need to set expected junction for track network verification purposes on RemoveCart
                    }
                }
                else
                {
                    dest = this.GetLookFromDirection(-1);
                    if (!dest)
                        Debug.LogWarning("FreightCartMob failed to get look from direction with no destination junction");
                }

                //Debug.LogWarning("Freight Cart after pathfinding dest bool is: " + dest.ToString());

                //New Default!! If we haven't decided... turn around.  No more derailing!
                //this.mLook = -this.mLook;

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
        if (FreightCartManager.instance == null)
            return;
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
                this.LocalInventory.Remove(networkid);
                return;
            }
            FreightCartStation station = null;

            // First look for stations that will handle goods we have in inventory
            for (int n = 0; n < this.LocalInventory[networkid].Inventory.Count; n++)
            {
                ItemBase item = this.LocalInventory[networkid].Inventory[n];
                // Check if the item is needed, if it isn't needed we'll offload at a compatible station
                if (FreightCartManager.instance.RegistryDeficit(networkid, item) == 0)
                {
                    this.OffloadingExcess = true;
                    //Debug.LogWarning("FreightCartMob needs to offload excess of " + item.ToString() + " from network: " + networkid);
                }
                // This will find a station to offload or deliver a serviceable good to
                station = FreightCartManager.instance.GetStation(networkid, item, this.OffloadingExcess);
                if (station != null)
                    break;
                else  //Make sure this is not still true if we didn't find a place to offload the item
                    this.OffloadingExcess = false;
            }
            // If we still don't have a station we'll look for a fitting match
            if (station == null)
            {
                if (this.mnUsedStorage != this.mnMaxStorage)
                {
                    if (!this.IsOreFreighter)
                        station = FreightCartManager.instance.GetStation(networkid, null, false);
                    else if (this.OreFreighterItem != null)
                        station = FreightCartManager.instance.GetStation(networkid, this.OreFreighterItem, false, true);
                    else
                        Debug.LogWarning("FreightCartMob: Ore freighter looking for station with filled inventory but null freighter item");
                }
                // If we still don't find one we'll take up an assignment with a station so we can at least idle there
                if (station == null)
                {
                    if (this.AssignedStation == null)
                        this.TryAssignCart(currentloc.TrackNetwork);
                    if (this.AssignedStation != null)
                        station = this.AssignedStation;
                    if (station == null)
                    {
                        Debug.LogWarning("FreightCartManager returned null station for ChooseDestination of FreightCart");
                        return;
                    }
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
        else // Cart is empty
        {
            // Take up an assignment if we don't have one or check for a new assignment if ours is over serviced
            if (this.AssignedStation == null || this.AssignedStation.AvailableCarts > this.AssignedStation.AssignedCarts || !this.CheckCartTier(this.AssignedStation.CartTier))
                this.TryAssignCart(currentloc.TrackNetwork);
            if (this.AssignedStation != null)
            {
                //Debug.LogWarning("Freight Cart assigned station final selection, direction: " + this.AssignedStation.JunctionDirection.ToString());
                //Debug.LogWarning("Available/Assigned station available carts: " + this.AssignedStation.AvailableCarts.ToString() + "/" + this.AssignedStation.AssignedCarts.ToString());

                this.DestinationJunction = null;
                //If our assigned station has nothing to offer look for requests to satisfy them by seeking out offering stations
                if (FreightCartManager.instance.GetStationOfferings(this.AssignedStation).Count == 0)
                {
                    List<KeyValuePair<ItemBase, int>> needs = FreightCartManager.instance.GetStationNeeds(this.AssignedStation);
                    if (needs.Count != 0)
                    {
                        FreightCartStation station;
                        for (int n = 0; n < needs.Count; n++)
                        {
                            ItemBase item = needs[n].Key;
                            if (needs[n].Value == 0)
                                continue;
                            //Debug.Log("FreightCartMob looking for station with needs item: " + item.ToString());
                            // Skip station checks if we're an ore freighter and the item isn't a valid type
                            if (this.IsOreFreighter && (item.mType != ItemType.ItemCubeStack || (!CubeHelper.IsOre((item as ItemCubeStack).mCubeType))))
                                continue;
                            station = FreightCartManager.instance.GetStation(this.AssignedStation.NetworkID, item, false, true);
                            if (station != null)
                            {
                                //Debug.Log("FreightCartMob chose station with ID: " + station.StationID.ToString());
                                this.DestinationJunction = station.ClosestJunction;
                                this.DestinationDirection = station.JunctionDirection;
                                this.JunctionRoute = null;
                                this.JunctionRoute = currentloc.TrackNetwork.RouteFind(currentloc, this.DestinationJunction);
                                break;
                            }
                        }
                    }
                }
                //Wait at our assigned station for either offers or for needs
                if (this.DestinationJunction == null) 
                {
                    this.DestinationJunction = this.AssignedStation.ClosestJunction;
                    this.DestinationDirection = this.AssignedStation.JunctionDirection;
                    this.JunctionRoute = null;
                    this.JunctionRoute = currentloc.TrackNetwork.RouteFind(currentloc, this.DestinationJunction);
                }
            }
        }
    }

    private void TryAssignCart(FreightTrackNetwork network)
    {
        int lowestoverage = int.MaxValue;
        FreightCartStation Candidate = null;
        if (network == null || network.TrackSegments == null)
            return;
        //Debug.LogWarning("Freight Cart Trying to assign cart");
        for (int n = 0; n < network.TrackSegments.Count; n++)
        {
            FreightTrackSegment seg = network.TrackSegments[n];
            if (seg == null)
                continue;
            //Debug.LogWarning("seg station count: " + seg.Stations.Count);
            for (int m = 0; m < seg.Stations.Count; m++)
            {
                FreightCartStation station = seg.Stations[m];
                if (station == null || string.IsNullOrEmpty(station.NetworkID) || !this.CheckCartTier(station.CartTier))
                    continue;
                // Assign the cart to the station if it has explicit free slots, mark it for smart assignment if it has the least excess carts
                int overage = station.AvailableCarts - station.AssignedCarts;
                if (overage < 0)
                {
                    this.ExchangeStation(this.AssignedStation, station);
                    return;
                }
                else if (overage < lowestoverage)
                {
                    lowestoverage = overage;
                    Candidate = station;
                }
            }
        }
        //No stations still requiring assigned carts... assign 'intelligently' -> new code with evenly distribute the excess carts
        FreightCartStation oldstation = this.AssignedStation;
        this.ExchangeStation(oldstation, Candidate);
        // This station needs to be on the same track network.  Currently it doesn't guarantee that so we need to get rid of others if they come up. (For now)
        //FreightCartStation newstation = FreightCartManager.instance.GetNeedyStation();
        //if (newstation.ClosestJunction != null && newstation.ClosestJunction.TrackNetwork != null && newstation.ClosestJunction.TrackNetwork.NetworkID == network.NetworkID)
        //    this.ExchangeStation(oldstation, newstation);
        //Debug.LogWarning("Freight Cart had to revert to getting needy station");
    }

    private void ExchangeStation(FreightCartStation oldstation, FreightCartStation newstation)
    {
        if (newstation == null)
            return;
        this.AssignedStation = newstation;
        this.AssignedStation.AvailableCarts++;
        this.AssignedStation.CartList.Add(this);
        if (oldstation != null)
        {
            oldstation.AvailableCarts--;
            oldstation.CartList.Remove(this);
            oldstation.RequestImmediateNetworkUpdate();
        }
        this.AssignedStation.RequestImmediateNetworkUpdate();
    }

    private bool CheckCartTier(int carttier)
    {
        switch (carttier)
        {
            case 0:  // Any
                return true;
            case 1:  // T2/3+ 
                return !(this.meType == eMinecartType.FreightCart_T1 || this.meType == eMinecartType.OreFreighter_T1 || this.meType == eMinecartType.ScrapCartMK1);
            case 2:  // T4
                return this.meType == eMinecartType.FreightCart_T4 || this.meType == eMinecartType.OreFreighter_T4;
            case 3:  // Ore Freighters Only
                return this.IsOreFreighter;
            case 4:  // T2/3+ Ore Freighters
                return this.IsOreFreighter && this.meType != eMinecartType.OreFreighter_T1;
            case 5: // T4 Ore Freighters
                return this.meType == eMinecartType.OreFreighter_T4;
            default: // Unknown
                return false;
        }
    }

    /// <summary>
    ///     Set the Freight Cart look for leaving a junction in the given direction
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    private bool GetLookFromDirection(int direction)
    {
        //Try returning on a safe route out of the junction rather than passing through to a possible demise if no path
        if (direction == -1)
        {
            for (int n = 0; n < 4; n++)
            {
                if (this.LastJunction.ConnectedJunctions[n] != null)
                {
                    direction = n;
                    this.DestinationJunction = null;
                    this.NextJunction = null;
                    this.DestinationDirection = -1;
                }
            }
        }
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
            Debug.LogWarning("FreightCartMob attempted to set look by direction but failed with direction: " + direction.ToString() + " Dropping destination to pick a new one");
            this.DestinationJunction = null;
            this.NextJunction = null;
            this.DestinationDirection = -1;
            this.mLook = -this.mLook; //Stay at the junction because there's literally no safe path out!
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
        Vector3 vector3 = WorldScript.instance.mPlayerFrustrum.GetCoordsToUnity(this.mnX, this.mnY, this.mnZ);

        //Hack to prevent a one off derailing of the cart
        if (this.SlopeCatch == -1)
        {
            Debug.Log("FreightCartMob tried to remove itself but skipped the first check for unnecessary derailment protections");
            this.SlopeCatch = this.mnUpdates;
            return;
        }

        Debug.Log("****** LITTLE JIMMY DROP CARTS ******[" + lReason + "],  Cart was at position: " + vector3.ToString() + " with look: " + this.mLook.ToString() + " and speed: " + this.mrSpeed.ToString());
        if (this.mnUpdates <= 1)
            Debug.LogWarning((object)"Probable issue - minecart decided to drop on or before first update!");

        if (MissionManager.instance != null)
            MissionManager.instance.AddMission("Freightcart derailed!", 60.0f, Mission.ePriority.eCritical, false, false);

        System.Random random = new System.Random();
        int itemID = ItemEntries.MineCartT1;

        switch (this.meType)
        {
            case eMinecartType.FreightCart_T1:
                itemID = ItemEntries.MineCartT1;
                break;
            case eMinecartType.FreightCart_T2:
                itemID = ItemEntries.MineCartT2;
                break;
            case eMinecartType.FreightCart_T3:
                itemID = ItemEntries.MineCartT3;
                break;
            case eMinecartType.FreightCart_T4:
                itemID = ItemEntries.MineCartT4;
                break;
            case eMinecartType.FreightCartTour:
                itemID = ItemEntries.TourCart;
                break;
            case eMinecartType.FreightCartTourBasic:
                itemID = ModManager.mModMappings.ItemsByKey["steveman0.FreightTourBasic"].ItemId;
                break;
            case eMinecartType.FreightCartMK1:
                itemID = ModManager.mModMappings.ItemsByKey["steveman0.FreightCartMK1"].ItemId;
                break;
            case eMinecartType.OreFreighter_T1:
                itemID = ModManager.mModMappings.ItemsByKey["steveman0.OreFreighterT1"].ItemId;
                break;
            case eMinecartType.OreFreighter_T2:
                itemID = ModManager.mModMappings.ItemsByKey["steveman0.OreFreighterT2"].ItemId;
                break;
            case eMinecartType.OreFreighter_T3:
                itemID = ModManager.mModMappings.ItemsByKey["steveman0.OreFreighterT3"].ItemId;
                break;
            case eMinecartType.OreFreighter_T4:
                itemID = ModManager.mModMappings.ItemsByKey["steveman0.OreFreighterT4"].ItemId;
                break;
            case eMinecartType.ScrapCartMK1:
                itemID = ModManager.mModMappings.ItemsByKey["steveman0.ScrapCartMK1"].ItemId;
                break;
            case eMinecartType.ScrapOreFreighterMK1:
                itemID = ModManager.mModMappings.ItemsByKey["steveman0.ScrapOreFreighterMK1"].ItemId;
                break;
        }
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

        //Remove cart assignment!
        if (this.AssignedStation != null)
        {
            this.AssignedStation.AvailableCarts--;
            this.AssignedStation.CartList.Remove(this);
        }

        MobManager.instance.DestroyMob(this);
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

    public override void ReadNetworkUpdate(BinaryReader reader, int version)
    {
        try
        {
            if (ReadNullDebug)
                Debug.LogWarning("FreightCartMob ReadNullDebug at start of reading");
            this.meType = (FreightCartMob.eMinecartType)reader.ReadInt32();
            this.mrTargetSpeed = reader.ReadSingle();
            this.mrSpeed = reader.ReadSingle();
            this.SetStatsFromType();
            this.mrTargetSpeed = (float)this.rand.Next(95, 105) / 100f * this.mrSpeedScalar;

            FreightTrackNetwork tracknetwork = null;
            if (this.LastJunction != null && this.LastJunction.TrackNetwork != null)
                tracknetwork = this.LastJunction.TrackNetwork;

            int juncid = reader.ReadInt32();
            if (juncid != -1 && tracknetwork != null)
                this.DestinationJunction = tracknetwork.GetJunctionFromID(juncid);
            else
                this.DestinationJunction = null;
            this.DestinationDirection = reader.ReadInt32();

            juncid = reader.ReadInt32();
            if (juncid != -1 && tracknetwork != null)
                this.NextJunction = tracknetwork.GetJunctionFromID(juncid);
            else
                this.NextJunction = null;                

            int count = reader.ReadInt32();
            this.mnUsedStorage = 0;
            if (ReadNullDebug)
                Debug.LogWarning("FreightCartMob ReadNullDebug prior to inventory loop");
            for (int index = 0; index < count; index++)
            {
                string networkid = reader.ReadString();
                if (string.IsNullOrEmpty(networkid))
                {
                    Debug.LogError("FreightCartMob read in inventory with null/empty network ID.  CORRUPTION SUSPECTED!");
                    continue;
                }
                MachineInventory inv = new MachineInventory(this, this.mnMaxStorage);
                inv.ReadInventory(reader);
                if (!this.LocalInventory.ContainsKey(networkid))
                    this.LocalInventory.Add(networkid, inv);
                else
                    this.LocalInventory[networkid] = inv;
                this.mnUsedStorage += inv.ItemCount();
                int count2 = this.LocalInventory[networkid].Inventory.Count;
                if (ReadNullDebug)
                    Debug.LogWarning("FreightCartMob ReadNullDebug in inventory loop before items at loop iteration: " + index.ToString());
                for (int n = 0; n < count2; n++)
                {
                    ItemBase item = this.LocalInventory[networkid].Inventory[n];
                    if (item == null)
                    {
                        Debug.LogError("FreightCartMob loaded in with a null item in its inventory.  CORRUPTION SUSPECTED!");
                        this.LocalInventory[networkid].Inventory.RemoveAt(n);
                        n--;
                        continue;
                    }
                    //bool checkreg;
                    //if (FreightCartManager.instance == null)
                    //    checkreg = false;
                    //else
                    //    checkreg = FreightCartManager.instance.NetworkAdd(networkid, item, item.GetAmount());
                    //if (!checkreg)
                    //{
                    //    this.TransitCheckIn = false;
                    //    break;
                    //}
                }
            }
            if (ReadNullDebug)
                Debug.LogWarning("FreightCartMob ReadNullDebug after inventory reading");
            if (this.TransferInventory == null)
            {
                Debug.LogWarning("FreightCartMob was reading from the network but failed to get a cart type, Transfer inventory defined!");
                this.TransferInventory = new MachineInventory(this, 500);
            }
            this.TransferInventory.ReadInventory(reader);
        }
        catch (NullReferenceException e)
        {
            Debug.LogError("FreightCartMob read had an NRE!  Activating additional debugging.  Please provide steveman0 the log file!");
            ReadNullDebug = true;
        }
    }

    public override void WriteNetworkUpdate(BinaryWriter writer)
    {
        writer.Write((int)this.meType);
        writer.Write(this.mrTargetSpeed);
        writer.Write(this.mrSpeed);

        if (this.DestinationJunction != null)
            writer.Write(this.DestinationJunction.JunctionID);
        else
            writer.Write(-1);
        writer.Write(this.DestinationDirection);
        if (this.NextJunction != null)
            writer.Write(this.NextJunction.JunctionID);
        else
            writer.Write(-1);

        //writer.Write((byte)meLoadState);

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
        try
        {
            if (ReadNullDebug)
                Debug.LogWarning("FreightCartMob ReadNullDebug at start of reading");
            this.meType = (FreightCartMob.eMinecartType)reader.ReadInt32();
            this.mrTargetSpeed = reader.ReadSingle();
            this.mrSpeed = reader.ReadSingle();
            this.SetStatsFromType();
            this.mrTargetSpeed = (float)this.rand.Next(95, 105) / 100f * this.mrSpeedScalar;

            int count = reader.ReadInt32();
            this.mnUsedStorage = 0;
            if (ReadNullDebug)
                Debug.LogWarning("FreightCartMob ReadNullDebug prior to inventory loop");
            for (int index = 0; index < count; index++)
            {
                string networkid = reader.ReadString();
                if (string.IsNullOrEmpty(networkid))
                {
                    Debug.LogError("FreightCartMob read in inventory with null/empty network ID.  CORRUPTION SUSPECTED!");
                    continue;
                }
                MachineInventory inv = new MachineInventory(this, this.mnMaxStorage);
                inv.ReadInventory(reader);
                if (this.LocalInventory == null)
                {
                    Debug.LogWarning("FreightCartMob read found a null local inventory.  Did initialization fail?");
                    continue;
                }
                if (!this.LocalInventory.ContainsKey(networkid))
                    this.LocalInventory.Add(networkid, inv);
                else
                    this.LocalInventory[networkid] = inv;
                this.mnUsedStorage += inv.ItemCount();
                if (this.LocalInventory[networkid] == null || this.LocalInventory[networkid].Inventory == null)
                {
                    Debug.LogWarning("FreightCartMob read had a null inventory when it was just set.  How could this happen?");
                    continue;
                }
                int count2 = this.LocalInventory[networkid].Inventory.Count;
                if (ReadNullDebug)
                    Debug.LogWarning("FreightCartMob ReadNullDebug in inventory loop before items at loop iteration: " + index.ToString());
                for (int n = 0; n < count2; n++)
                {
                    ItemBase item = this.LocalInventory[networkid].Inventory[n];
                    if (item == null)
                    {
                        Debug.LogError("FreightCartMob loaded in with a null item in its inventory.  CORRUPTION SUSPECTED!");
                        this.LocalInventory[networkid].Inventory.RemoveAt(n);
                        n--;
                        continue;
                    }
                    bool checkreg;
                    if (FreightCartManager.instance == null)
                        checkreg = false;
                    else
                        checkreg = FreightCartManager.instance.NetworkAdd(networkid, item, item.GetAmount());
                    if (!checkreg)
                    {
                        this.TransitCheckIn = false;
                        break;
                    }
                }
            }
            if (ReadNullDebug)
                Debug.LogWarning("FreightCartMob ReadNullDebug after inventory reading");
            if (this.TransferInventory == null)
            {
                Debug.LogWarning("FreightCartMob was reading from the network but failed to get a cart type, Transfer inventory defined!");
                this.TransferInventory = new MachineInventory(this, 500);
            }
            this.TransferInventory.ReadInventory(reader);
        }
        catch (Exception e)
        {
            Debug.LogError("FreightCartMob read had an Exception!  Activating additional debugging.  Please provide steveman0 the log file! Excpetion: " + e.Message);
            ReadNullDebug = true;
        }
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
        FreightCartMK1, //Not currently used - just a test cart
        FreightCartTour, 
        FreightCartTourBasic,
        OreFreighter_T1,
        OreFreighter_T2,
        OreFreighter_T3,
        OreFreighter_T4,
        ScrapCartMK1,
        ScrapOreFreighterMK1,
        //Basic,
        //Fast,
        //Large,
        //Bulk,
        //Tour,
    }
}


