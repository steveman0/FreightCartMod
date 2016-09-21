using System.Collections.Generic;
using UnityEngine;

public class FreightTrackSegment
{
    public List<FreightTrackJunction> ConnectedJunctions = new List<FreightTrackJunction>();
    public int Length;
    public FreightTrackNetwork TrackNetwork;
    public List<FreightCartStation> Stations = new List<FreightCartStation>();

    public FreightTrackSegment(FreightTrackJunction junction1, FreightTrackJunction junction2, int length)
    {
        this.ConnectedJunctions.Add(junction1);
        this.ConnectedJunctions.Add(junction2);
        this.Length = length;
        this.MergeNetworks(junction1, junction2);
    }

    public void MergeNetworks(FreightTrackJunction junction1, FreightTrackJunction junction2)
    {
        //Debug.LogWarning("FTSeg junction1 ID: " + junction1.JunctionID.ToString() + " junction2 ID: " + junction2.JunctionID.ToString());
        if (junction1.TrackNetwork != junction2.TrackNetwork)
        {
            //Merge in smaller network into the larger one for efficiency
            FreightTrackNetwork oldnetwork = junction1.TrackNetwork.TrackJunctions.Count > junction2.TrackNetwork.TrackJunctions.Count ? junction2.TrackNetwork : junction1.TrackNetwork;
            FreightTrackNetwork newnetwork = junction1.TrackNetwork.TrackJunctions.Count > junction2.TrackNetwork.TrackJunctions.Count ? junction1.TrackNetwork : junction2.TrackNetwork;
            this.TransferOwnership(oldnetwork, newnetwork);
        }
        this.TrackNetwork = junction1.TrackNetwork;
        this.TrackNetwork.TrackSegments.Add(this);
    }

    /// <summary>
    ///     For migrating junctions and segments to a new network on merging
    /// </summary>
    /// <param name="oldnetwork"></param>
    /// <param name="newnetwork"></param>
    public void TransferOwnership(FreightTrackNetwork oldnetwork, FreightTrackNetwork newnetwork)
    {
        //Debug.LogWarning("FTSeg oldnetwork: " + oldnetwork.NetworkID.ToString() + " junction count: " + oldnetwork.TrackJunctions.Count.ToString() + " newnetwork ID: " + newnetwork.NetworkID.ToString() + " junction count: " + newnetwork.TrackJunctions.Count.ToString());
        for (int n = 0; n < oldnetwork.TrackJunctions.Count; n++)
        {
            oldnetwork.TrackJunctions[n].TrackNetwork = newnetwork;
            if (!newnetwork.TrackJunctions.Contains(oldnetwork.TrackJunctions[n]))
                newnetwork.TrackJunctions.Add(oldnetwork.TrackJunctions[n]);
        }
        for (int n = 0; n < oldnetwork.TrackSegments.Count; n++)
        {
            oldnetwork.TrackSegments[n].TrackNetwork = newnetwork;
            //if (!newnetwork.TrackSegments.Contains(oldnetwork.TrackSegments[n]))
                newnetwork.TrackSegments.Add(oldnetwork.TrackSegments[n]);
        }
        newnetwork.ResetJunctionIndices();
        newnetwork.ReassignTourCartStations(oldnetwork);
        oldnetwork.TrackJunctions.Clear();
        oldnetwork.TrackSegments.Clear();
        oldnetwork = null;
    }
}

