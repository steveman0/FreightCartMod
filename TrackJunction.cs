using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

public class FreightTrackJunction : MachineEntity
{
    public static int GlobalJunctions;
    private bool mbLinkedToGO = false;
    public bool LinkStatusDirty = false;
    public FreightTrackNetwork TrackNetwork;
    public FreightTrackJunction[] ConnectedJunctions = new FreightTrackJunction[4];
    public FreightTrackSegment[] ConnectedSegments = new FreightTrackSegment[4];
    public int JunctionIndex = 0;
    public int JunctionID;
    public int[] SegmentDistances = new int[4];

    //public List<KeyValuePair<int,FreightTrackJunction>> ConnectedJunctions = new List<KeyValuePair<int, FreightTrackJunction>>(4);
    //public List<KeyValuePair<int, FreightTrackSegment>> ConnectedSegments = new List<KeyValuePair<int, FreightTrackSegment>>(4);

    public static ushort TRACKTYPE = 538;
    public static ushort TRACKSTRAIGHT = 0;
    public static ushort TRACKCORNER = 1;
    public static ushort TRACKSLOPE = 2;
    public static ushort TRACKBUFFER = 3;
    public static ushort TRACKFULL = 4;
    public static ushort TRACKEMPTY = 5;
    public static ushort CONTROLTYPE = 539;
    public static ushort CONTROLTURBO = 2;
    public static ushort CONTROLUNLOAD = 3;
    public static ushort CONTROLLOAD = 4;
    public static ushort FREIGHTSTATIONTYPE = ModManager.mModMappings.CubesByKey["steveman0.FreightCartStation"].CubeType;
    public static ushort JUNCTIONTYPE = ModManager.mModMappings.CubesByKey["steveman0.TrackJunction"].CubeType;
    public static ushort TOURSTATIONTYPE = ModManager.mModMappings.CubesByKey["steveman0.TourCartStation"].CubeType;
    public static ushort ScrapJunctionVal = ModManager.mModMappings.CubesByKey["steveman0.TrackJunction"].ValuesByKey["steveman0.ScrapTrackJunction"].Value;
    public static ushort ScrapTrackType = ModManager.mModMappings.CubesByKey["steveman0.ScrapTrack"].CubeType;
    public static ushort ScrapStraightVal = ModManager.mModMappings.CubesByKey["steveman0.ScrapTrack"].ValuesByKey["steveman0.ScrapTrackStraight"].Value;
    public static ushort ScrapCornerVal = ModManager.mModMappings.CubesByKey["steveman0.ScrapTrack"].ValuesByKey["steveman0.ScrapTrackCorner"].Value;
    public static ushort ScrapSlopeVal = ModManager.mModMappings.CubesByKey["steveman0.ScrapTrack"].ValuesByKey["steveman0.ScrapTrackSlope"].Value;

    private Segment mPrevGetSeg;
    private Segment mUnderSegment;
    private int Updates = 0;
    private int[] DelayCheck = new int[4];

    //Rendering stuff
    public int instanceID = -1;
    public int instanceID2 = -1;
    public static Mesh TrackMesh;
    public static Mesh TrackMesh2;
    public static Material TrackMaterial;
    public static Material ScrapTrackMat; // Move to the scrap track entity...
    private JunctionRenderer TrackRenderer;
    private GameObject CrossTrack;

    public FreightTrackJunction(ModCreateSegmentEntityParameters parameters)
        : base(parameters)
    {
        this.mbNeedsUnityUpdate = true;
        this.mbNeedsLowFrequencyUpdate = true;
        if (WorldScript.mbIsServer)
        {
            this.JunctionID = GlobalJunctions;
            this.RequestImmediateNetworkUpdate(); //Is this necessary?
        }
        else
            this.JunctionID = -1;
        GlobalJunctions++;

        this.TrackNetwork = new FreightTrackNetwork(this);
    }

    public override string GetPopupText()
    {
        string str1;
        string str2;
        string str3;
        string str4 = "";
        string str5 = "";

        str1 = "Track Junction (ID: " + this.JunctionID + ")\n";
        int count = 0;
        string str = "";
        for (int n = 0; n < 4; n++)
        {
            if (this.ConnectedJunctions[n] != null)
            {
                count++;
                str += SegmentDistances[n].ToString() + ", ";
            }
        }
        if (count > 0)
        {
            str2 = count.ToString() + " valid track connections\n";
            str3 = "Track lengths: " + str.Substring(0, str.Length - 2) + "\nPress Q to reset the junction\n";
        }
        else
        {
            str2 = "No valid track connections detected\n";
            str3 = "";
        }
        if (this.TrackNetwork != null)
        {
            str4 = "Track Network ID: " + this.TrackNetwork.NetworkID.ToString() + "\nJunction Count: " + this.TrackNetwork.TrackJunctions.Count.ToString() + "\n";
            //for (int n = 0; n < this.TrackNetwork.TrackJunctions.Count; n++)
            //    str5 += this.TrackNetwork.TrackJunctions[n].JunctionID.ToString() + ", ";
            //if (str5.Length > 0)
            //    str5 = str5.Substring(0, str5.Length - 2) + "\n";
        }

        if (Input.GetButtonDown("Extract"))
            TrackJunctionWindow.ResetJunction(this);

        return str1 + str2 + str3 + str4 + str5;
    }

    public override void LowFrequencyUpdate()
    {
        Updates++;
        int direction = Updates % 4;
        if (this.DelayCheck[direction] > 0)
        {
            this.DelayCheck[direction]--;
            return;
        }
        if (this.ConnectedJunctions[direction] == null && !this.TrackFollow(direction))
            this.DelayCheck[direction] = 10; //Delay checking this direction again 10 times or about 8 seconds

    }


    /// <summary>
    ///     Follows a track segment to find all containing stations until it reaches another junction or determines track is invalid
    /// </summary>
    /// <param name="direction">0 - 3 representing the four directions out of the junction</param>
    /// <returns>True if it found complete segment</returns>
    public bool TrackFollow(int direction)
    {
        //Initialize the check from the junction
        long nextX = this.mnX;
        long nextY = this.mnY;
        long nextZ = this.mnZ;
        Vector3 dirvec = new Vector3();

        //Store the initial junction direction for later recording which direction the connected junction is associated with
        int initialdirection = direction;

        //List of freight cart stations found on this segment -> to be written to the final constructed FreightTrackSegment
        List<FreightCartStation> SegmentStations = new List<FreightCartStation>();

        //Store visited track pieces for catching when the segment enters a closed loop
        List<TrackPiece> VisitedTracks = new List<TrackPiece>();

        //Begin loop here.  Direction can be set and used to check the next location each time through the loop
        //Allow segments only up to 512m long due to cost of loop checking - may revise after testing
        for (int n = 0; n < 512; n++)
        {
            switch (direction)
            {
                case 0:
                    nextX++;
                    dirvec = Vector3.right;
                    break;
                case 1:
                    nextZ++;
                    dirvec = Vector3.forward;
                    break;
                case 2:
                    nextX--;
                    dirvec = Vector3.left;
                    break;
                case 3:
                    nextZ--;
                    dirvec = Vector3.back;
                    break;
                default:
                    nextX++;
                    break;
            }

            ushort lValue1 = 0;
            byte lFlags1 = 0;
            ushort type = this.GetCube(nextX, nextY, nextZ, out lValue1, out lFlags1);
            this.mUnderSegment = this.mPrevGetSeg;
            //Debug.LogWarning("GetCube type: " + type.ToString() + " value: " + lValue1);
            bool foundslope = false;

            //Found air and need to check for a downward slope under it
            if (type == 1)
            {
                ushort lValue2 = 0;
                byte lFlags2 = 0;
                ushort cube = this.GetCube(nextX, nextY - 1L, nextZ, out lValue2, out lFlags2);
                Segment segment = this.mPrevGetSeg;
                type = cube;
                lFlags1 = lFlags2;
                lValue1 = lValue2;
                if ((type == 538 && lValue1 == 2) || (type == ScrapTrackType && lValue1 == ScrapSlopeVal))
                {
                    foundslope = true;
                    nextY--; //decrement Y level for next loop through!
                }
                else
                {
                    if (type == 0)
                        Debug.LogError("Error, track follower has null under segment!");
                    if (this.mPrevGetSeg == null)
                        Debug.LogError("Error, prevseg was null!");
                    if (segment == null)
                        Debug.LogError("Error, old was null!");
                    if (this.mPrevGetSeg != segment)
                        Debug.LogWarning(("Track follower is looking for a slope, and has had to check across segment boundaries for this![Old/New" + segment.GetName() + " -> " + this.mPrevGetSeg.GetName()));
                    return false;
                }
            }

            Vector3 trackvec = SegmentCustomRenderer.GetRotationQuaternion(lFlags1) * Vector3.forward;
            trackvec.Normalize();
            trackvec.x = trackvec.x >= -0.5 ? (trackvec.x <= 0.5 ? 0.0f : 1f) : -1f;
            trackvec.y = trackvec.y >= -0.5 ? (trackvec.y <= 0.5 ? 0.0f : 1f) : -1f;
            trackvec.z = trackvec.z >= -0.5 ? (trackvec.z <= 0.5 ? 0.0f : 1f) : -1f;
            //Begin checking track type
            if (type == TRACKTYPE || type == ScrapTrackType)
            {

                if ((type == TRACKTYPE && (lValue1 == TRACKSTRAIGHT || lValue1 == TRACKEMPTY || lValue1 == TRACKFULL)) || (type == ScrapTrackType && lValue1 == ScrapStraightVal))
                {
                    if (trackvec.y > 0.5 || trackvec.y < -0.5)
                        return false;
                    else if (!(trackvec == dirvec) && !(trackvec == -dirvec))
                    {
                        dirvec = new Vector3(trackvec.x, 0f, trackvec.z);
                    }
                }
                if ((type == TRACKTYPE && lValue1 == TRACKCORNER) || (type == ScrapTrackType && lValue1 == ScrapCornerVal))
                {
                    if (dirvec == new Vector3(-trackvec.z, 0.0f, trackvec.x))
                        dirvec = new Vector3(dirvec.z, 0.0f, -dirvec.x);
                    else if (trackvec == -dirvec)
                        dirvec = new Vector3(-dirvec.z, 0.0f, dirvec.x);
                    else
                        return false;
                }
                if ((type == TRACKTYPE && lValue1 == TRACKSLOPE) || (type == ScrapTrackType && lValue1 == ScrapSlopeVal))
                {
                    Vector3 vector3_2 = trackvec;
                    dirvec.y = 0.0f;
                    dirvec.Normalize();
                    if (dirvec == trackvec)
                    {
                        if (foundslope)
                            return false;
                        else
                            nextY++;
                    }
                    else if (dirvec == -trackvec)
                        ;
                }
                if (type == TRACKTYPE && lValue1 == TRACKBUFFER)
                {
                    dirvec = new Vector3(-dirvec.x, 0f, -dirvec.z);
                }
            }
            //Begin checking special types
            else if (type == CONTROLTYPE)
            {
                if (lValue1 == CONTROLLOAD || lValue1 == CONTROLUNLOAD || lValue1 == CONTROLTURBO)
                {
                    if ((trackvec == dirvec) || (trackvec == -dirvec))
                    {
                        //Do nothing... direction doesn't change
                    }
                    else
                        return false;
                }
            }
            //Check for freight stations
            else if (type == FREIGHTSTATIONTYPE)
            {
                if ((trackvec == dirvec) || (trackvec == -dirvec))
                {
                    Segment segment = this.AttemptGetSegment(nextX, nextY, nextZ);
                    if (segment == null)
                    {
                        segment = WorldScript.instance.GetSegment(nextX, nextY, nextZ);
                        if (segment == null)
                        {
                            Debug.Log((object)"Track junction track follower did not find segment");
                            return false;
                        }
                    }
                    FreightCartStation fcs = segment.FetchEntity(eSegmentEntity.Mod, nextX, nextY, nextZ) as FreightCartStation;
                    if (fcs == null)
                    {
                        Debug.LogWarning("Track Junction Track Follower tried to get a freight cart station but got other mod machine instead?");
                        return false;
                    }
                    if (!SegmentStations.Contains(fcs))
                        SegmentStations.Add(fcs);
                    fcs.ClosestJunction = this;
                    fcs.JunctionDirection = initialdirection;
                }
                else
                    return false;
            }
            //Is it a junction?
            else if (type == JUNCTIONTYPE)
            {
                //Debug.LogWarning("Track follower success!  Found another junction!");
                Segment segment = this.AttemptGetSegment(nextX, nextY, nextZ);
                if (segment == null)
                {
                    segment = WorldScript.instance.GetSegment(nextX, nextY, nextZ);
                    if (segment == null)
                    {
                        Debug.Log((object)"Track junction track follower did not find segment");
                        return false;
                    }
                }
                FreightTrackJunction junction = segment.FetchEntity(eSegmentEntity.Mod, nextX, nextY, nextZ) as FreightTrackJunction;
                if (junction == null)
                {
                    Debug.LogWarning("Track Junction Track Follower tried to get a track junction but got other mod machine instead?");
                    return false;
                }
                this.ConnectedJunctions[initialdirection] = junction;
                FreightTrackSegment tracksegment = new FreightTrackSegment(this, junction, n + 1);
                tracksegment.Stations = SegmentStations;
                //Debug.LogWarning("trackseg station count: " + tracksegment.Stations.Count);
                this.ConnectedSegments[initialdirection] = tracksegment;
                this.SegmentDistances[initialdirection] = n + 1;
                this.LinkStatusDirty = true;

                //handle the connection for the other junction so we don't need to double the work
                //Mirror the direction to reflect the correct side of the connecting junction
                int mirroreddir = direction += 2;
                if (mirroreddir > 3)
                    mirroreddir -= 4;
                junction.ConnectedJunctions[mirroreddir] = this;
                junction.ConnectedSegments[mirroreddir] = tracksegment;
                junction.SegmentDistances[mirroreddir] = n + 1;
                junction.LinkStatusDirty = true;
                return true;
            }
            else if (type == TOURSTATIONTYPE)
            {
                if (trackvec != -dirvec)
                    return false;
                Segment segment = this.AttemptGetSegment(nextX, nextY, nextZ);
                if (segment == null)
                {
                    segment = WorldScript.instance.GetSegment(nextX, nextY, nextZ);
                    if (segment == null)
                    {
                        Debug.Log((object)"Track junction track follower did not find segment");
                        return false;
                    }
                }
                TourCartStation station = segment.FetchEntity(eSegmentEntity.Mod, nextX, nextY, nextZ) as TourCartStation;
                station.TrackNetwork = this.TrackNetwork;
                station.ClosestJunction = this;
                station.JunctionDirection = initialdirection;
                this.ConnectedJunctions[initialdirection] = this;
                FreightTrackSegment tracksegment = new FreightTrackSegment(this, this, 2*n + 1);
                this.SegmentDistances[initialdirection] = 2 * n + 1;
                this.ConnectedSegments[initialdirection] = tracksegment;
                this.LinkStatusDirty = true;
                if (!string.IsNullOrEmpty(station.StationName) && !this.TrackNetwork.TourCartStations.ContainsKey(station.StationName))
                    this.TrackNetwork.TourCartStations.Add(station.StationName, station);
                return true;
            }
            else
                return false;   //Not a track type

            //Update the direction int based on the changed direction vector
            if (dirvec == Vector3.right)
                direction = 0;
            else if (dirvec == Vector3.forward)
                direction = 1;
            else if (dirvec == Vector3.left)
                direction = 2;
            else if (dirvec == Vector3.back)
                direction = 3;

            TrackPiece visitedpiece = new TrackPiece(new Vector3(nextX - this.mnX, nextY - this.mnY, nextZ - this.mnZ), direction);
            //Debug.LogWarning("Visited track piece: " + new Vector4(nextX - this.mnX, nextY - mnY, nextZ - mnZ, direction).ToString());
            //Store every track piece and check every 10th for monitoring for closed, endless loops of track
            if (n % 10 == 0)
            {
                int count = VisitedTracks.Count;
                for (int m = 0; m < count; m++)
                {
                    TrackPiece piece = VisitedTracks[m];
                    if (piece.Position == visitedpiece.Position && piece.Direction == visitedpiece.Direction)
                    {
                        //Debug.LogWarning("piece position: " + piece.Position.ToString() + " visited: " + visitedpiece.Position.ToString());
                        Debug.LogWarning("TrackJunction followed track route and found a closed loop.  Ending search.");
                        return false;
                    }
                }
            }
            VisitedTracks.Add(visitedpiece);
            if (n == 511)
                Debug.LogWarning("Track Junction Found track length > 512m -> ending search.");
        }
        return false;
    }

    public override void SpawnGameObject()
    {
        return;
    }

    public override void UnityUpdate()
    {
        if (!mbLinkedToGO)
        {
            // Update the instanced version
            //this.instanceID = FreightCartMod.TrackInstances.TryAdd();
            //this.instanceID2 = FreightCartMod.TrackInstances.TryAdd();
            //this.UpdateInstancedBase();
            //if (instanceID != -1 && instanceID2 != -1)
            //{
            //    this.mbLinkedToGO = true;
            //    this.LinkStatusDirty = true;
            //}

            if (this.CrossTrack != null || FreightTrackJunction.TrackMesh == null || FreightTrackJunction.TrackMesh2 == null || FreightTrackJunction.TrackMaterial == null)
                return;
            
            Quaternion rot = SegmentCustomRenderer.GetRotationQuaternion(this.mFlags);
            Quaternion rot2 = rot * Quaternion.Euler(Vector3.up * 90);
            MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
            materialPropertyBlock.SetColor("_GlowColor", Color.red);
            materialPropertyBlock.SetFloat("_GlowMult", 5f);

            this.CrossTrack = new GameObject();
            JunctionRenderer ren = this.CrossTrack.AddComponent<JunctionRenderer>();
            Vector3 lUnityPos = WorldScript.instance.mPlayerFrustrum.GetCoordsToUnity(this.mnX, this.mnY, this.mnZ);
            lUnityPos.x += 0.5f;
            lUnityPos.y += 0.5f;
            lUnityPos.z += 0.5f;
            ren.position = lUnityPos;
            ren.rotation = rot;
            ren.rotation2 = rot2;
            ren.mpb = materialPropertyBlock;
            ren.scrap = this.mValue == FreightTrackJunction.ScrapJunctionVal;

            ren.enabled = true;
            ren.gameObject.SetActive(true);
            this.CrossTrack.SetActive(true);
            this.TrackRenderer = ren;
            this.mbLinkedToGO = true;

            //if (mWrapper == null || mWrapper.mGameObjectList == null || SpawnableObjectManagerScript.instance == null || SpawnableObjectManagerScript.instance.maSpawnableObjects == null || SpawnableObjectManagerScript.instance.maSpawnableObjects[(int)SpawnableObjectEnum.Minecart_Track_Straight] == null)
            //{
            //    return;
            //}
            //else
            //{
            //    //this.CrossTrack = (GameObject)GameObject.Instantiate(SpawnableObjectManagerScript.instance.maSpawnableObjects[(int)SpawnableObjectEnum.Minecart_Track_Straight]);
            //    //this.CrossTrack.transform.parent = mWrapper.mGameObjectList[0].gameObject.transform;
            //    //this.CrossTrack.transform.localPosition = new Vector3(0, 0f, 0);//put in the correct relative position
            //    //mWrapper.mGameObjectList[0].gameObject.transform.eulerAngles = new Vector3(0, 0, 0);
            //    //this.CrossTrack.transform.eulerAngles = new Vector3(0, 90f, 0);
            //    //this.CrossTrack.transform.localScale = new Vector3(0.99f, 0.99f, 0.99f);
            //    //this.CrossTrack.SetActive(true);
            //}
        }
        if (this.mbLinkedToGO && this.LinkStatusDirty)
        {
            Color value = Color.red;
            int links = 0;
            for (int n = 0; n < 4; n++)
            {
                if (this.ConnectedJunctions[n] != null)
                    links++;
            }
            switch (links)
            {
                case 1:
                    value = new Color(1f, 0.3f, 0, 1f);
                    break;
                case 2:
                    value = Color.yellow;
                    break;
                case 3:
                    value = Color.green;
                    break;
                case 4:
                    value = Color.blue;
                    break;
            }
            //Renderer[] componentsInChildren = this.CrossTrack.GetComponentsInChildren<Renderer>();
            //Renderer[] comp2 = this.mWrapper.mGameObjectList[0].GetComponentsInChildren<Renderer>();
            //MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
            //materialPropertyBlock.SetColor("_GlowColor", value);
            //materialPropertyBlock.SetFloat("_GlowMult", 5f);
            //for (int i = 0; i < componentsInChildren.Length; i++)
            //    componentsInChildren[i].SetPropertyBlock(materialPropertyBlock);
            //for (int i = 0; i < comp2.Length; i++)
            //    comp2[i].SetPropertyBlock(materialPropertyBlock);

            if (this.TrackRenderer != null)
                this.TrackRenderer.mpb.SetColor("_GlowColor", value);

            if (this.instanceID != -1 && this.instanceID2 != -1)
            {
                //Debug.Log("Setting Track instance color as value: " + value.ToString());
                FreightCartMod.TrackInstances.SetCol(this.instanceID, value);
                FreightCartMod.TrackInstances.SetParamVal(this.instanceID, 1f);
                FreightCartMod.TrackInstances.SetCol(this.instanceID2, value);
                FreightCartMod.TrackInstances.SetParamVal(this.instanceID2, 1f);
            }
            this.LinkStatusDirty = false;
        }
    }

    void UpdateInstancedBase()
    {
        if (instanceID == -1 || instanceID2 == -1) return;

        Vector3 lUnityPos = WorldScript.instance.mPlayerFrustrum.GetCoordsToUnity(this.mnX, this.mnY, this.mnZ);
        lUnityPos.x += 0.5f;
        lUnityPos.y += 0.5f;
        lUnityPos.z += 0.5f;

        //lUnityPos += mUp * -0.2174988f;

        //Vector3 rot = SegmentCustomRenderer.GetRotationQuaternion(mFlags).eulerAngles;
        Vector3 rot = Vector3.zero;
        Vector3 rot2 = new Vector3(0, 90f, 0);
        Vector3 sizeadjust = new Vector3(0.99f, 0.99f, 0.99f); // to prevent render flickering

        // Create my own instancer and just call that one instead!
        FreightCartMod.TrackInstances.SetMatrix(instanceID, lUnityPos, rot, Vector3.one);
        FreightCartMod.TrackInstances.SetMatrix(instanceID2, lUnityPos, rot2, sizeadjust);
    }

    public override void UnitySuspended()
    {
        GameObject.Destroy(this.CrossTrack);
        if (this.instanceID != -1)
            FreightCartMod.TrackInstances.Remove(this.instanceID);
        if (this.instanceID2 != -1)
            FreightCartMod.TrackInstances.Remove(this.instanceID2);
        this.CrossTrack = null;
        this.mbLinkedToGO = false;
        base.UnitySuspended();
    }

    /// <summary>
    ///     Returns the direction corresponding with the route out of this junction to get to the destination junction
    /// </summary>
    /// <param name="junction">Destination junction</param>
    /// <returns>Direction integer</returns>
    public int GetConnectedDirection(FreightTrackJunction junction)
    {
        for (int n = 0; n < 4; n++)
        {
            if (this.ConnectedJunctions[n] == junction)
                return n;
        }
        return -1;
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
            if (segment == null)
                this.AttemptGetSegment(segX, segY, segZ);
        }
        if (segment == null || !segment.mbInitialGenerationComplete || segment.mbDestroyed)
        {
            //CentralPowerHub.mnMinecartX = lTestX;
            //CentralPowerHub.mnMinecartY = lTestY;
            //CentralPowerHub.mnMinecartZ = lTestZ;
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

    public override void OnUpdateRotation(byte newFlags)
    {
        int x = (int)(this.mnX - mSegment.baseX);
        int y = (int)(this.mnY - mSegment.baseY);
        int z = (int)(this.mnZ - mSegment.baseZ);

        // nodeworker automatically sets the new flags in the cubedata that is actually used by the diskserializer, so we have to restore them!
        mSegment.maCubeData[(y << 8) + (z << 4) + x].meFlags = this.mFlags;
    }

    public void ResetJunction()
    {
        this.ClearConnections();
        Array.Clear(this.ConnectedJunctions, 0, 4);
        Array.Clear(this.ConnectedSegments, 0, 4);
        Array.Clear(this.SegmentDistances, 0, 4);
        this.TrackNetwork = new FreightTrackNetwork(this);
    }

    private void ClearConnections()
    {
        for (int n = 0; n < 4; n++)
        {
            //Remove this from connected junctions/segments from all connected neighbors
            FreightTrackJunction junc = this.ConnectedJunctions[n];
            if (junc == null)
                continue;
            for (int m = 0; m < 4; m++)
            {
                FreightTrackJunction junc2 = junc.ConnectedJunctions[m];
                if (junc2 == null)
                    continue;
                if (junc2 == this)
                {
                    junc.ConnectedJunctions[m] = null;
                    junc.LinkStatusDirty = true;
                }
            }
            for (int m = 0; m < 4; m++)
            {
                FreightTrackSegment seg = junc.ConnectedSegments[m];
                if (seg == null)
                    continue;
                if (seg.ConnectedJunctions.Contains(this))
                    junc.ConnectedSegments[m] = null;
            }
        }
        this.TrackNetwork.TrackJunctions.Remove(this);
        this.TrackNetwork.NetworkIntegrityCheck(this.ConnectedJunctions.ToList());
    }

    public override void OnDelete()
    {
        if (this.instanceID != -1)
            FreightCartMod.TrackInstances.Remove(this.instanceID);
        if (this.instanceID2 != -1)
            FreightCartMod.TrackInstances.Remove(this.instanceID2);
        GameObject.Destroy(this.CrossTrack);
        this.ClearConnections();
    }

    public void InvalidConnection(FreightTrackJunction missingtarget)
    {
        for (int n = 0; n < 4; n++)
        {
            //Remove this from connected junctions/segments from all connected neighbors
            FreightTrackJunction junc = this.ConnectedJunctions[n];
            if (junc == null || junc != missingtarget)
                continue;
            for (int m = 0; m < 4; m++)
            {
                FreightTrackJunction junc2 = missingtarget.ConnectedJunctions[m];
                if (junc2 == null)
                    continue;
                if (junc2 == this)
                {
                    junc.ConnectedJunctions[m] = null;
                    junc.LinkStatusDirty = true;
                }
            }
            for (int m = 0; m < 4; m++)
            {
                FreightTrackSegment seg = missingtarget.ConnectedSegments[m];
                if (seg == null)
                    continue;
                if (seg.ConnectedJunctions.Contains(this))
                    junc.ConnectedSegments[m] = null;
            }
            this.ConnectedJunctions[n] = null;
            this.ConnectedSegments[n] = null;
        }
    }

    public override bool ShouldSave()
    {
        return true;
    }

    public override int GetVersion()
    {
        return 2;
    }

    public override bool ShouldNetworkUpdate()
    {
        return true;
    }

    public override void WriteNetworkUpdate(BinaryWriter writer)
    {
        writer.Write(this.JunctionID);
    }

    public override void ReadNetworkUpdate(BinaryReader reader)
    {
        this.JunctionID = reader.ReadInt32();
    }

    public override void Write(BinaryWriter writer)
    {
        //Maybe don't do this to start?  See if the cost is noticeable
        //Write coordinates for connected junctions so that every load it doesn't need to follow every track segment again
        //Will need to write segment length as well for segment recreation on load
        //... nah :D

        writer.Write(this.JunctionID);

        base.Write(writer);
    }

    public override void Read(BinaryReader reader, int entityVersion)
    {
        if (entityVersion > 1)
        {
            this.JunctionID = reader.ReadInt32();
            if (this.JunctionID >= GlobalJunctions)
                GlobalJunctions = this.JunctionID + 1;
        }
        base.Read(reader, entityVersion);
    }
}

public class TrackPiece
{
    public Vector3 Position;
    public int Direction;

    public TrackPiece(Vector3 position, int direction)
    {
        this.Position = position;
        this.Direction = direction;
    }
}

