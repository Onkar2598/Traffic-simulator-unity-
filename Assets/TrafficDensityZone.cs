
using UnityEngine;

public class TrafficDensityZone : MonoBehaviour
{
    [Tooltip("Max no. of vehicles in 100 diameter region (Checked before spawning)")]
    [Range(1,40)]
    public int vehicleDensity = 5;

    public enum DensityZoneShape { Sphere,Box};
    public DensityZoneShape densityZoneShape = DensityZoneShape.Sphere; 
    
    public float radius = 50;

    public Bounds bounds { get { return m_Bounds; } set { m_Bounds = value; } }
    [SerializeField]
    private Bounds m_Bounds = new Bounds(Vector3.zero, 20*Vector3.one);

}
