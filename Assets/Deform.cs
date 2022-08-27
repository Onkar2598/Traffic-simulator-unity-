
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

public class Deform : MonoBehaviour
{
    public float deformRadius = 0.2f;
    public float stiffness = 0.2f;
    public float damageFallOff = 1;
    public float damageMultiplier = 1;
    public float impulseThreshold = 1;
    public float scaleFactor = 0.01f;
    public float maxDeform = 0.008f;

    public bool useJobs = false;

    MeshFilter meshFilter;
    MeshCollider col;
    Vector3[] verts;
    Vector3[] vertsOriginal;
    NativeArray<float3> _vertsOriginal;


    public void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        col = GetComponent<MeshCollider>();
        vertsOriginal = meshFilter.mesh.vertices;
        verts = meshFilter.mesh.vertices;
    }
    private void OnCollisionEnter(Collision collision)
    {
        float startTime = Time.realtimeSinceStartup;


        float collisionPower = collision.impulse.magnitude;
        if (collisionPower > impulseThreshold)
        {
            Vector3 colPoint = collision.contacts[0].point;
            Vector3 pointPos = transform.InverseTransformPoint(colPoint);

            if (!useJobs)
            {
                for (int i = 0; i < verts.Length; i++)
                {
                    float distFromCol = Vector3.SqrMagnitude(pointPos - verts[i]);
                    float distFromOriginal = Vector3.SqrMagnitude(verts[i] - vertsOriginal[i]);
                    if (distFromCol < deformRadius * deformRadius)
                    {
                        float deformAmount = scaleFactor * 0.00001f * damageMultiplier * collisionPower / (stiffness * distFromCol + 1);
                        Vector3 deformVec = deformAmount * transform.InverseTransformDirection(collision.relativeVelocity.normalized);
                        verts[i].x = Mathf.Clamp(verts[i].x + deformVec.x, vertsOriginal[i].x - maxDeform, vertsOriginal[i].x + maxDeform);
                        verts[i].y = Mathf.Clamp(verts[i].y + deformVec.y + UnityEngine.Random.Range(-deformVec.z * 0.5f, 0.5f * deformVec.z), vertsOriginal[i].y - maxDeform, vertsOriginal[i].y + maxDeform);
                        verts[i].z = Mathf.Clamp(verts[i].z + deformVec.z, vertsOriginal[i].z - maxDeform, vertsOriginal[i].z + maxDeform);
                        //verts[i] += deformAmount * transform.InverseTransformDirection(collision.relativeVelocity.normalized);
                    }
                }
                UpdateMeshVertices();
            }
            else
            {
                float3[] temp2 = new float3[vertsOriginal.Length];
                for (int i = 0; i < vertsOriginal.Length; i++)
                {
                    temp2[i] = vertsOriginal[i];
                }
                _vertsOriginal = new NativeArray<float3>(temp2, Allocator.TempJob);

                float3[] temp1 = new float3[meshFilter.mesh.vertices.Length];
                for (int i = 0; i < temp1.Length; i++)
                {
                    temp1[i] = meshFilter.mesh.vertices[i];
                }
                NativeArray<float3> verts = new NativeArray<float3>(temp1, Allocator.TempJob);
                //NativeArray<Vector3> vertsOriginal = new NativeArray<Vector3>(vertsOriginal,Allocator.TempJob);

                DeformJob deformJob = new DeformJob
                {
                    verts = verts,
                    vertsOriginal = _vertsOriginal,
                    pointPos = pointPos,
                    deformDir = transform.InverseTransformDirection(collision.relativeVelocity.normalized),
                    deformRadius = deformRadius,
                    damageMultiplier = damageMultiplier,
                    scaleFactor = scaleFactor,
                    maxDeform = maxDeform,
                    stiffness = stiffness,
                    collisionPower = collisionPower,
                 };
                JobHandle jobHandle = deformJob.Schedule(meshFilter.mesh.vertices.Length,50);
                jobHandle.Complete();
                Vector3[] temp = new Vector3[deformJob.verts.Length];
                for (int i = 0; i < deformJob.verts.Length; i++)
                {
                    temp[i] = deformJob.verts[i];
                }
                meshFilter.mesh.vertices = temp;
                col.sharedMesh = meshFilter.mesh;

                verts.Dispose();
                _vertsOriginal.Dispose();
            }


            VehicleCharacter veh = GetComponent<VehicleCharacter>();
            if (veh != null) veh.TakeDamage(collisionPower);
        }
        Debug.Log("Total Time: "+((Time.realtimeSinceStartup - startTime) * 1000) + "ms");
    }

    private void UpdateMeshVertices()
    {
        meshFilter.mesh.vertices = verts;
        col.sharedMesh = meshFilter.mesh;
    }

    public void ResetDeformation()
    {
        if (vertsOriginal != null)
        {
            meshFilter.mesh.vertices = vertsOriginal;
            col.sharedMesh = meshFilter.mesh;
            verts = vertsOriginal;
        }
    }
}

[BurstCompile]
public struct DeformJob : IJobParallelFor
{
    public NativeArray<float3> verts;
    public NativeArray<float3> vertsOriginal;
    [ReadOnly] public float3 pointPos;
    [ReadOnly]public float3 deformDir;
    [ReadOnly] public float deformRadius;
    [ReadOnly] public float damageMultiplier;
    [ReadOnly] public float scaleFactor;
    [ReadOnly] public float maxDeform;
    [ReadOnly] public float stiffness;
    [ReadOnly] public float collisionPower;

    public void Execute(int i)
    {
        float distFromCol = SqrMagnitude(pointPos - verts[i]);
        float distFromOriginal = SqrMagnitude(verts[i] - vertsOriginal[i]);
        if (distFromCol < deformRadius * deformRadius)
        {
            float deformAmount = scaleFactor * 0.00001f * damageMultiplier * collisionPower / (stiffness * distFromCol + 1);
            float3 deformVec = deformAmount * deformDir;
            float3 vert1;
            vert1.x = math.clamp(verts[i].x + deformVec.x, vertsOriginal[i].x - maxDeform, vertsOriginal[i].x + maxDeform);
            vert1.y = math.clamp(verts[i].y + deformVec.y, vertsOriginal[i].y - maxDeform, vertsOriginal[i].y + maxDeform);// + Random.Range(-deformVec.z * 0.5f, 0.5f * deformVec.z), vertsOriginal[i].y - maxDeform, vertsOriginal[i].y + maxDeform);
            vert1.z = math.clamp(verts[i].z + deformVec.z, vertsOriginal[i].z - maxDeform, vertsOriginal[i].z + maxDeform);
            verts[i] = vert1;
            //verts[i] += deformAmount * transform.InverseTransformDirection(collision.relativeVelocity.normalized);
        }
    }

    float SqrMagnitude(float3 vec)
    {
        return (vec.x * vec.x + vec.y * vec.y + vec.z * vec.z);
    }
}
