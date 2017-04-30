using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Priority_Queue;

public class FreightTrackNetwork
{
    public List<FreightTrackSegment> TrackSegments = new List<FreightTrackSegment>();
    public List<FreightTrackJunction> TrackJunctions = new List<FreightTrackJunction>();
    public Dictionary<string, TourCartStation> TourCartStations = new Dictionary<string, TourCartStation>();
    public int NetworkID;
    public static int Networks = 0;
    private object safety = new object();

    public FreightTrackNetwork(FreightTrackJunction junction)
    {
        this.NetworkID = Networks;
        Networks++;
        this.TrackJunctions.Add(junction);
        if (FreightCartManager.instance != null)
            FreightCartManager.instance.GlobalTrackNetworks.Add(this);
        else
            Debug.LogWarning("Attempted to create FreightTrackNetwork before FRM was initialized.  Network won't be registered to the global list!");
    }

    public bool ContainsTrackSegment(FreightTrackSegment track)
    {
        return this.TrackSegments.Contains(track);
    }

    public bool ContainsJunction(FreightTrackJunction junction)
    {
        return this.TrackJunctions.Contains(junction);
    }

    public List<FreightCartStation> GetNetworkStations()
    {
        List<FreightCartStation> stations = new List<FreightCartStation>();
        int count = this.TrackSegments.Count;
        for (int n = 0; n < count; n++)
        {
            int stationcount = this.TrackSegments[n].Stations.Count;
            for (int m = 0; m < stationcount; m++)
            {
                FreightCartStation station = this.TrackSegments[n].Stations[m];
                if (!stations.Contains(station))
                    stations.Add(station);
            }
        }
        return stations;
    }

    public void GetNetworkStats(out int assignedcarts, out int availcarts)
    {
        List<FreightCartStation> stations = this.GetNetworkStations();
        int count = stations.Count;
        assignedcarts = 0;
        availcarts = 0;
        for (int n = 0; n < count; n++)
        {
            FreightCartStation station = stations[n];
            assignedcarts += station.AssignedCarts;
            availcarts += station.AvailableCarts;
        }
    }

    public void NetworkIntegrityCheck(List<FreightTrackJunction> junctions)
    {
        if (junctions == null || junctions[0] == null)
            return;
        FreightTrackNetwork oldnetwork = junctions[0].TrackNetwork;
        int count = junctions.Count;
        if (count < 2)
            return;
        if (count > 4)
        {
            Debug.LogWarning("FreightTrackNetwork NetworkIntegrityCheck tried to check more than 4 junctions at once!?");
            return;
        }
        switch (count)
        {
            case 2:
                this.NetworkIntegrityCheck(junctions[0], junctions[1], null, null);
                break;
            case 3:
                this.NetworkIntegrityCheck(junctions[0], junctions[1], junctions[2], null);
                break;
            case 4:
                this.NetworkIntegrityCheck(junctions[0], junctions[1], junctions[2], junctions[3]);
                break;
        }
        FreightCartManager.instance.GlobalTrackNetworks.Remove(oldnetwork);
        oldnetwork.TrackJunctions.Clear();
        oldnetwork.TrackSegments.Clear();
        oldnetwork = null;
    }

    /// <summary>
    ///     Checks for all combinations of connections between up to four junctions and, if necessary, constructs new networks to contain isolated junctions
    /// </summary>
    /// <param name="junction1"></param>
    /// <param name="junction2"></param>
    /// <param name="junction3"></param>
    /// <param name="junction4"></param>
    public void NetworkIntegrityCheck(FreightTrackJunction junction1, FreightTrackJunction junction2, FreightTrackJunction junction3, FreightTrackJunction junction4)
    {
        //Flag when this network is compromised by threading
        lock (safety)
        {
            //Map entire network of junctions from one and check if any other junctions exist in it
            //Repeat with other networks that aren't found in the first
            List<FreightTrackJunction> junctions1 = new List<FreightTrackJunction>();
            List<FreightTrackJunction> junctions2 = new List<FreightTrackJunction>();
            List<FreightTrackJunction> junctions3 = new List<FreightTrackJunction>();
            List<FreightTrackJunction> junctions4 = new List<FreightTrackJunction>();

            //If we have junction1 build it's network of junctions - we need at least one to compare to
            if (junction1 != null)
            {
                junctions1 = this.JunctionNetMap(junction1);
                this.ReconstructNetwork(junctions1);
            }

            //Build net of junctions only if we're not already in the first junctions network
            if (junction2 != null && !junctions1.Contains(junction2))
            {
                junctions2 = this.JunctionNetMap(junction2);
                this.ReconstructNetwork(junctions2);
            }

            //Check if junction 3 isn't in either 1 or 2
            if (junction3 != null && (!junctions1.Contains(junction3) && !junctions2.Contains(junction3)))
            {
                junctions3 = this.JunctionNetMap(junction3);
                this.ReconstructNetwork(junctions3);
            }

            //Check if 4 isn't a part of any of the previously constructed nets and build it if needed
            if (junction4 != null && (!junctions1.Contains(junction4) && !junctions2.Contains(junction4) && !junctions3.Contains(junction4)))
            {
                junctions4 = this.JunctionNetMap(junction4);
                this.ReconstructNetwork(junctions4);
            }
        }
    }

    /// <summary>
    ///     Finds all junctions connected to the provided junction
    /// </summary>
    /// <param name="junction">Junction to find connections for</param>
    /// <returns>The list of all connecting junctions</returns>
    public List<FreightTrackJunction> JunctionNetMap(FreightTrackJunction junction)
    {
        if (junction == null)
            return null;
        List<FreightTrackJunction> connectedjunctions = new List<FreightTrackJunction>();
        Queue<FreightTrackJunction> junctionstocheck = new Queue<FreightTrackJunction>();
        connectedjunctions.Add(junction);
        junctionstocheck.Enqueue(junction);
        
        while (junctionstocheck.Count > 0)
        {
            FreightTrackJunction testjunction = junctionstocheck.Dequeue();
            for (int n = 0; n < 4; n++)
            {
                FreightTrackJunction jun = testjunction.ConnectedJunctions[n];
                if (jun == null)
                    continue;
                if (!connectedjunctions.Contains(jun))
                {
                    connectedjunctions.Add(jun);
                    junctionstocheck.Enqueue(jun);
                }
            }
        }
        return connectedjunctions;
    }

    /// <summary>
    ///     Construct a network from a list of junctions (for when a network is found to have been broken)
    ///     Includes only the segments valid for the list of junctions
    /// </summary>
    /// <param name="junctions">List of juctions to build the new network</param>
    public void ReconstructNetwork(List<FreightTrackJunction> junctions)
    {
        if (junctions == null || junctions.Count == 0)
            return;
        FreightTrackNetwork network = new FreightTrackNetwork(junctions[0]);

        for (int n = 0; n < junctions.Count; n++)
        {
            FreightTrackJunction junction = junctions[n];
            for (int m = 0; m < 4; m++)
            {
                FreightTrackSegment trackseg = junction.ConnectedSegments[m];
                if (trackseg == null)
                    continue;
                if (!network.ContainsTrackSegment(trackseg))
                {
                    network.TrackSegments.Add(trackseg);
                    trackseg.TrackNetwork = network;
                }
            }
            junction.TrackNetwork.TrackJunctions.Remove(junction);
            if (junction.TrackNetwork.TrackJunctions.Count == 0)
                FreightCartManager.instance.GlobalTrackNetworks.Remove(junction.TrackNetwork);
            junction.TrackNetwork = network;
            if (!network.TrackJunctions.Contains(junction))
                network.TrackJunctions.Add(junction);
        }
        network.ResetJunctionIndices();
        network.ReassignTourCartStations(this);
        if (!FreightCartManager.instance.GlobalTrackNetworks.Contains(network))
            FreightCartManager.instance.GlobalTrackNetworks.Add(network);
    }

    /// <summary>
    ///     Redefines the JunctionIndex for each junction in the network
    /// </summary>
    public void ResetJunctionIndices()
    {
        for (int n = 0; n < this.TrackJunctions.Count; n++)
            this.TrackJunctions[n].JunctionIndex = n;
    }

    public FreightTrackJunction GetJunctionFromID(int junctionID)
    {
        return this.TrackJunctions.FirstOrDefault(x => x.JunctionID == junctionID);
    }

    /// <summary>
    ///     Reassigns tour carts to new track network based on connected junctions
    /// </summary>
    /// <param name="original">Track network to transfer the stations from</param>
    public void ReassignTourCartStations(FreightTrackNetwork original)
    {
        if (original == null || original.TourCartStations.Count == 0)
            return;
        List<string> keys = original.TourCartStations.Keys.ToList();
        int count = keys.Count;
        for (int n = 0; n < count; n++)
        {
            if (string.IsNullOrEmpty(keys[n]) || !original.TourCartStations.ContainsKey(keys[n]))
                continue;
            TourCartStation station = original.TourCartStations[keys[n]];
            if (station.ClosestJunction == null || station.TrackNetwork == null)
                continue;
            if (station.ClosestJunction.TrackNetwork == this && station.TrackNetwork.TourCartStations.ContainsKey(keys[n]) && !this.TourCartStations.ContainsKey(keys[n]))
            {
                station.TrackNetwork.TourCartStations.Remove(keys[n]);
                this.TourCartStations.Add(keys[n], station);
            }
        }
    }

    /// <summary>
    ///     Dijkstra's Algorithm pathfinding along track junctions
    /// </summary>
    /// <param name="start">Starting track junction</param>
    /// <param name="destination">Destination track junction</param>
    /// <returns>A stack containing the junctions the cart must visit to reach the destination</returns>
    public Stack<FreightTrackJunction> RouteFind(FreightTrackJunction start, FreightTrackJunction destination)
    {
        //Address the trivial case
        if (start == null || destination == null || start == destination)
            return new Stack<FreightTrackJunction>();

        //Use a local copy of the junction list to avoid potential nasty consequences should the network change
        Stack<FreightTrackJunction> CartRoute = new Stack<FreightTrackJunction>();
        lock (safety)
        {
            List<FreightTrackJunction> localjunctions = this.TrackJunctions;

            if (!localjunctions.Contains(start) || !localjunctions.Contains(destination))
            {
                Debug.LogWarning("FreightTrackNetwork RouteFind attempted to rount to/from inaccessible junction\n start id " + start.JunctionID + " destination id " + destination.JunctionID + " trackjunctions count: " + localjunctions.Count);
                return CartRoute;
            }
            int junctioncount = localjunctions.Count;

            //Initialize priority queue with only 0 distance for the start
            SimplePriorityQueue<FreightTrackJunction> tentativedistances = new SimplePriorityQueue<FreightTrackJunction>();
            for (int n = 0; n < junctioncount; n++)
                tentativedistances.Enqueue(localjunctions[n], float.MaxValue);
            tentativedistances.UpdatePriority(start, 0);

            //Initialize a separate array of distances for pulling the tentative distances, initialize the start to 0
            int[] distances = Enumerable.Repeat(int.MaxValue, junctioncount).ToArray();
            int currentnode = start.JunctionIndex;
            if (currentnode >= distances.Length)
            {
                Debug.LogWarning("FreightTrackNetwork RouteFind trying to navigate from junction outside of range");
                this.ResetJunctionIndices();
                return new Stack<FreightTrackJunction>();
            }
            distances[currentnode] = 0;

            //Initialize an array for storing the parent junction for the path finding to later trace back to the beginning, indexed to JunctionIndex
            //Start node previous is marked -1 to flag completion of the route trace
            int[] previousnode = new int[junctioncount];
            previousnode[currentnode] = -1;

            //Initialize variables used in the core algorithm
            int neighborindex;          //Index of neighbor junction as found in JunctionIndex
            int initialdis;             //Distance to neighbor from start (tentative)
            int currentdis;             //Distance to active junction
            int tryroute;               //Distance to route to neighbor through active junction
            FreightTrackJunction freightnode = tentativedistances.Dequeue();
            FreightTrackJunction neighbor;
            //Debug.LogWarning("FreightTrackNetwork RouteFind DEBUG - prior to loop");
            for (int m = 0; m < junctioncount; m++)
            {
                for (int n = 0; n < 4; n++)
                {
                    neighbor = freightnode.ConnectedJunctions[n];

                    //Skip the neighbor if it is null or previously visited
                    if (neighbor == null || !tentativedistances.Contains(neighbor))
                        continue;
                    neighborindex = neighbor.JunctionIndex;
                    if (neighborindex >= distances.Length || currentnode >= distances.Length)
                    {
                        Debug.LogWarning("FreightTrackNetwork RouteFind checking neighbor junction outside of range");
                        this.ResetJunctionIndices();
                        return new Stack<FreightTrackJunction>();
                    }
                    initialdis = distances[neighborindex];
                    currentdis = distances[currentnode];
                    tryroute = currentdis + neighbor.SegmentDistances[n];
                    //If the newly calculated distance to the neighbor is shorter than the previous replace it and update pathing accordingly
                    if (tryroute < initialdis)
                    {
                        distances[neighborindex] = tryroute;
                        tentativedistances.UpdatePriority(neighbor, tryroute);
                        previousnode[neighborindex] = currentnode;
                    }

                }
                //I feel like this check should come after dequeue... if the dequeue is the destination is there really any value in calculating for all its neighbors?
                //GASP!  Wikipedia might be wrong!!?
                if (!tentativedistances.Contains(destination))
                    break;
                freightnode = tentativedistances.Dequeue();
                currentnode = freightnode.JunctionIndex;
            }
            //Debug.LogWarning("FreightTrackNetwork RouteFind DEBUG - after loop");
            if (freightnode != destination)
            {
                Debug.LogWarning("FreightTrackNetwork RouteFind completed path finding with junction other than the destination?");
                return new Stack<FreightTrackJunction>();
            }
            if (currentnode >= previousnode.Length)
            {
                Debug.LogWarning("FreightTrackNetwork RouteFind trying to trace back previousnode out of range");
                this.ResetJunctionIndices();
                return new Stack<FreightTrackJunction>();
            }
            //Push the destination onto the route stack and get the first previous junction
            CartRoute.Push(freightnode);
            int prevnode = previousnode[currentnode];
            int safetycount = 0;
            //Trace back to the start by each of the previous junctions anding with the starting junction
            while (prevnode != -1)
            {
                freightnode = localjunctions[prevnode];
                CartRoute.Push(freightnode);
                prevnode = previousnode[prevnode];
                if (prevnode >= previousnode.Length)
                {
                    Debug.LogWarning("FreightTrackNetwork RouteFind trying to trace back previousnode out of range on path");
                    this.ResetJunctionIndices();
                    return new Stack<FreightTrackJunction>();
                }
                safetycount++;
                if (safetycount > 255)
                {
                    Debug.LogWarning("FreightTrackNetwork RouteFind failed construction or path has over 255 visited junctions!");
                    CartRoute = null;
                    return new Stack<FreightTrackJunction>();
                }
            }
            //The above loop automatically pushes the start junction and since the cart is assumed to be there we'll just toss it out as part of a sanity check
            if (CartRoute.Pop() != start)
            {
                Debug.LogWarning("FreightTrackNetwork RouteFind returned a path that didn't start with original start junction!");
                return new Stack<FreightTrackJunction>();
            }
        }
        return CartRoute;
    }
}