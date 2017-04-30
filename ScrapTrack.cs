using UnityEngine;
using System.Collections.Generic;

public class ScrapTrack : MachineEntity
{
    public static ushort ScrapTrackType = ModManager.mModMappings.CubesByKey["steveman0.ScrapTrack"].CubeType;
    public static ushort ScrapStraightVal = ModManager.mModMappings.CubesByKey["steveman0.ScrapTrack"].ValuesByKey["steveman0.ScrapTrackStraight"].Value;
    public static ushort ScrapCornerVal = ModManager.mModMappings.CubesByKey["steveman0.ScrapTrack"].ValuesByKey["steveman0.ScrapTrackCorner"].Value;
    public static ushort ScrapSlopeVal = ModManager.mModMappings.CubesByKey["steveman0.ScrapTrack"].ValuesByKey["steveman0.ScrapTrackSlope"].Value;
    public static Dictionary<ushort, Mesh> TrackMeshes = new Dictionary<ushort, Mesh>();
    public static Dictionary<ushort, Material> TrackMaterials = new Dictionary<ushort, Material>();
    public static Mesh StraightTrackMesh;  //Initialized at Mod level
    public static Mesh CornerTrackMesh;
    public static Mesh SlopeTrackMesh;
    public static Material StraightTrackMaterial;
    public static Material CornerTrackMaterial;
    public static Material SlopeTrackMaterial; //Initialized at Mod level


    public bool DoRender;
    public Vector3 position;
    public Quaternion rotation;
    public static bool Initialized;

    public ScrapTrack(ModCreateSegmentEntityParameters parameters)
        : base(parameters)
    {
        mbNeedsUnityUpdate = true;
        Vector3 lUnityPos = WorldScript.instance.mPlayerFrustrum.GetCoordsToUnity(this.mnX, this.mnY, this.mnZ);
        lUnityPos.x += 0.5f;
        lUnityPos.y += 0.5f;
        lUnityPos.z += 0.5f;
        this.position = lUnityPos;
        this.rotation = SegmentCustomRenderer.GetRotationQuaternion(this.mFlags);
        //Stupid corner mirroring...
        if (this.mValue == ScrapCornerVal)
            this.rotation *= Quaternion.Euler(Vector3.up * 180);
        if (this.mValue == ScrapSlopeVal)
            this.position.y += 0.46f;
    }

    public override void SpawnGameObject()
    {
        this.DoRender = true;
    }

    public override void UnitySuspended()
    {
        this.DoRender = false;
    }

    public override void OnUpdateRotation(byte newFlags)
    {
        this.rotation = SegmentCustomRenderer.GetRotationQuaternion(newFlags);
        //Stupid corner mirroring...
        if (this.mValue == ScrapCornerVal)
            this.rotation *= Quaternion.Euler(Vector3.up * 180);
        base.OnUpdateRotation(newFlags);
    }

    public override void UnityUpdate()
    {
        if (!Initialized)
        {
            if (SpawnableObjectManagerScript.instance == null)
                return;
            GameObject obj = SpawnableObjectManagerScript.instance.maSpawnableObjects[(int)SpawnableObjectEnum.Minecart_Track_Corner].transform.gameObject;
            CornerTrackMesh = obj.GetComponentInChildren<MeshFilter>().mesh;
            Material mat = obj.GetComponentInChildren<Renderer>().material;
            CornerTrackMaterial = new Material(mat);
            CornerTrackMaterial.SetColor("_Color", new Color(200/256f, 117/256f, 51/256f));

            obj = SpawnableObjectManagerScript.instance.maSpawnableObjects[(int)SpawnableObjectEnum.Minecart_Track_Slope].transform.gameObject;
            SlopeTrackMesh = obj.GetComponentInChildren<MeshFilter>().mesh;
            Material mat2 = obj.GetComponentInChildren<Renderer>().material;
            SlopeTrackMaterial = new Material(mat);
            SlopeTrackMaterial.SetColor("_Color", new Color(200/256f, 117/256f, 51/256f));

            TrackMeshes.Add(ScrapStraightVal, StraightTrackMesh);
            TrackMeshes.Add(ScrapCornerVal, CornerTrackMesh);
            TrackMeshes.Add(ScrapSlopeVal, SlopeTrackMesh);

            TrackMaterials.Add(ScrapStraightVal, StraightTrackMaterial);
            TrackMaterials.Add(ScrapCornerVal, CornerTrackMaterial);
            TrackMaterials.Add(ScrapSlopeVal, SlopeTrackMaterial);
            Initialized = true;
        }
        else if (DoRender)
            Graphics.DrawMesh(TrackMeshes[this.mValue], position, rotation, TrackMaterials[this.mValue], 0);
    }
}

