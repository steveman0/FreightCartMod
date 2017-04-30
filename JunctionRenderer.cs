using UnityEngine;

public class JunctionRenderer : MonoBehaviour
{
    public Vector3 position;
    public Quaternion rotation;
    public Quaternion rotation2;
    public MaterialPropertyBlock mpb;
    public bool scrap;

    public void Update()
    {
        if (scrap)
        {
            Graphics.DrawMesh(FreightTrackJunction.TrackMesh, position, rotation, FreightTrackJunction.ScrapTrackMat, 0, null, 0, mpb);
            Graphics.DrawMesh(FreightTrackJunction.TrackMesh2, position, rotation2, FreightTrackJunction.ScrapTrackMat, 0, null, 0, mpb);
        }
        else
        {
            Graphics.DrawMesh(FreightTrackJunction.TrackMesh, position, rotation, FreightTrackJunction.TrackMaterial, 0, null, 0, mpb);
            Graphics.DrawMesh(FreightTrackJunction.TrackMesh2, position, rotation2, FreightTrackJunction.TrackMaterial, 0, null, 0, mpb);
        }
    }

}