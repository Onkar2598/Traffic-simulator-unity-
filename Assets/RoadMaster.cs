using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoadMaster : MonoBehaviour
{
    public bool drawRoadGizmo = true;
    public float widthPerLane = 4.5f;
    public float roadThickness = 0.15f;
    public float UVScale = 10;
    public byte materialsCount = 1;

    [HideInInspector]
    public Transform activeRoadway;

    [Header("Components")]
    public Transform nodeContainer;
    public MeshFilter meshFilter;
    public MeshFilter radarMeshFilter;

    public void OnDrawGizmos()
    {
        if (drawRoadGizmo)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform roadway = transform.GetChild(i);
                for (int j = 0; j < roadway.childCount; j++)
                {
                    if (j < roadway.childCount - 1)
                    {
                        Gizmos.DrawLine(roadway.GetChild(j).position+Vector3.up*roadThickness*0.6f, roadway.GetChild(j + 1).position + Vector3.up * roadThickness * 0.6f);
                    }
                }
            }
        }
    }


    public void DrawMesh()
    {
        Debug.Log("Drawing road mesh");
        Mesh mesh;
        if (meshFilter.sharedMesh != null)
        {
            mesh = meshFilter.sharedMesh;
        }
        else
        {
            mesh = new Mesh();
            meshFilter.sharedMesh = mesh;
        }
        List<Vector3> meshVerts = new List<Vector3>();
        List<int>[] meshTriangles = new List<int>[materialsCount];
        for (int i = 0; i < materialsCount; i++)
        {
            meshTriangles[i] = new List<int>();
        }
        List<Vector2> uvList = new List<Vector2>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform roadWay = transform.GetChild(i);
            float blockLength = 0;
            for (int j = 0; j < roadWay.childCount; j++)
            {
                RoadPoint rp = roadWay.GetChild(j).GetComponent<RoadPoint>();
                if (rp != null)
                {
                    for (int k = 0; k <= rp.smoothenAmount; k++)
                    {
                        int ind = meshVerts.Count;// index of first vertex of crossSection
                        meshVerts.AddRange(EvaluateVertexPos(rp,k, j < roadWay.childCount - 1?roadWay.GetChild(j + 1):null));
                        if (j < roadWay.childCount - 1 && !rp.skipMeshDraw)
                        {
                            //meshTriangles[rp.materialIndex].AddRange(QuadTriangles(ind, ind + 6, ind + 9, ind + 3));//RightFace
                            //meshTriangles[rp.materialIndex].AddRange(QuadTriangles(ind, ind + 1, ind + 7, ind + 6));//topRightFace
                            //meshTriangles[rp.materialIndex].AddRange(QuadTriangles(ind + 1, ind + 2, ind + 8, ind + 7));//topLeftFace
                            //meshTriangles[rp.materialIndex].AddRange(QuadTriangles(ind + 2, ind + 5, ind + 11, ind + 8));//leftFace

                            meshTriangles[rp.materialIndex].AddRange(QuadTriangles(ind,ind+1,ind+5,ind+4));
                            meshTriangles[rp.materialIndex].AddRange(QuadTriangles(ind, ind + 4, ind + 6, ind + 2));
                            meshTriangles[rp.materialIndex].AddRange(QuadTriangles(ind+1, ind + 3, ind + 7, ind + 5));
                        }
                        
                        Vector2[] uvs = new Vector2[4];
                        uvs[0] = new Vector2(rp.lanes, (blockLength + rp.UVOffset) / UVScale);
                        //uvs[1] = new Vector2(rp.lanes / 2, (blockLength + rp.UVOffset) / UVScale);
                        uvs[1] = new Vector2(0, (blockLength + rp.UVOffset) / UVScale);
                        uvList.AddRange(uvs);
                        if (j < roadWay.childCount - 1)
                        {
                            float roadLength = (rp.transform.position - roadWay.GetChild(j + 1).position).magnitude;
                            blockLength += roadLength * (1f / (float)(1 + rp.smoothenAmount));
                        }
                        //meshVerts = EvaluateVertexPos(rp);
                        //meshTriangles = new int[] { 0, 5, 2, 0, 3, 5 };
                    }
                }
            }
        }
        meshFilter.sharedMesh.Clear();
        //if(radarMeshFilter!=null)radarMeshFilter.sharedMesh.Clear();
        mesh.subMeshCount = materialsCount;
        mesh.vertices = meshVerts.ToArray();
        //Mesh radarMesh = radarMeshFilter.sharedMesh;List<int> radarTris = new List<int>();
        for (int i = 0; i < materialsCount; i++)
        {
            //radarTris.AddRange(meshTriangles[i]);
            mesh.SetTriangles(meshTriangles[i], i);
        }

        //mesh.colors = SetVertexColors(meshVerts.ToArray());
        mesh.uv = uvList.ToArray();
        mesh.RecalculateNormals();
        //radarMesh.vertices = meshVerts.ToArray();
        //radarMesh.triangles = radarTris.ToArray();
        meshFilter.mesh = mesh;
        //radarMeshFilter.mesh = radarMesh;
    }

    public void AddNodes(Transform roadWay)
    {
        for (int j = 0; j < roadWay.childCount; j++)
        {
            RoadPoint rp = roadWay.GetChild(j).GetComponent<RoadPoint>();
            if (rp != null)
            {
                if (rp.setNodes)
                    rp.ClearNodes();
                else
                {
                    rp.nodes = new Dictionary<int, List<Node>>();
                    for (int i = 0; i < rp.lanes; i++)
                    {
                        rp.nodes.Add(i, new List<Node>());
                    }
                }
                if (rp.setNodes && j < roadWay.childCount - 1 && rp.lanes % 2 == 0)
                {
                    rp.nodes = new Dictionary<int, List<Node>>();
                    RoadPoint rp1 = roadWay.GetChild(j + 1).GetComponent<RoadPoint>();
                    int amount = Mathf.RoundToInt((rp1.transform.position - rp.transform.position).magnitude / rp.EvaluateNodeSpacing());
                    Vector3 r1 = rp.transform.right;
                    Vector3 r2 = rp1.transform.right;
                    for (int l = 0; l < rp.lanes / 2; l++)
                    {
                        // right Lanes
                        Node pNode = null;
                        for (int p = 0; p < amount; p++)
                        {
                            Vector3 pos;
                            if (rp.smoothenAmount > 0)
                            {
                                pos = GetBezierPoint(rp, rp1.transform, (p / (float)amount));
                                r1 = Vector3.Lerp(rp.transform.right, rp1.transform.right, (p / (float)amount)).normalized;
                                pos = pos + r1 * (0.5f * widthPerLane + l * widthPerLane);
                            }
                            else
                                pos = Vector3.Lerp(rp.transform.position + r1 * (0.5f * widthPerLane + l * widthPerLane), rp1.transform.position + r2 * (0.5f * widthPerLane + l * widthPerLane), (p / (float)amount));

                            Node n = AddNodePoint(pNode, nodeContainer, pos,rp.nodeSpeedTier);
                            if (rp.nodes.ContainsKey(l))
                                rp.nodes[l].Add(n);
                            else
                                rp.nodes.Add(l, new List<Node>() { n });
                            pNode = n;
                        }
                    }
                    for (int l = rp.lanes / 2; l < rp.lanes; l++)
                    {
                        // left lanes
                        Node pNode = null;
                        for (int p = 0; p < amount; p++)
                        {
                            Vector3 pos;
                            if (rp.smoothenAmount > 0)
                            {
                                pos = GetBezierPoint(rp, rp1.transform, 1-(p / (float)amount));
                                r1 = Vector3.Lerp(rp.transform.right, rp1.transform.right, 1-(p / (float)amount)).normalized;
                                pos = pos - r1 * (0.5f * widthPerLane + (l - rp.lanes / 2) * widthPerLane);
                            }
                            else
                                pos = Vector3.Lerp(rp1.transform.position - r2 * (0.5f * widthPerLane + (l- rp.lanes / 2) * widthPerLane), rp.transform.position - r1 * (0.5f * widthPerLane + (l - rp.lanes / 2) * widthPerLane), p * (1f / (float)amount));

                            Node n = AddNodePoint(pNode, nodeContainer, pos, rp.nodeSpeedTier);
                            if (rp.nodes.ContainsKey(l))
                                rp.nodes[l].Add(n);
                            else
                                rp.nodes.Add(l, new List<Node>() { n });
                            pNode = n;
                        }
                    }
                    for (int l = 0; l < rp.lanes / 2; l++)
                    {
                        for (int p = 0; p < amount - 1; p++)
                        {
                            if (p % 2 == l % 2)
                            {
                                if (l + 1 < rp.lanes / 2)
                                {
                                    Vector3 pos = Vector3.Lerp(rp.nodes[l][p].transform.position, rp.nodes[l + 1][p + 1].transform.position, 0.5f);
                                    Node divNode = AddNodePoint(rp.nodes[l][p], nodeContainer, pos);
                                    divNode.branches.Add(rp.nodes[l + 1][p + 1]);
                                    divNode.nodeType = Node.NodeType.divert;
                                    rp.nodes[l][p].branches.Add(divNode);
                                    if (rp.nodes.ContainsKey(100))
                                    {
                                        rp.nodes[100].Add(divNode);
                                    }
                                    else
                                    {
                                        rp.nodes.Add(100, new List<Node>() { divNode });
                                    }
                                }
                                if (l> 0)
                                {
                                    Vector3 pos = Vector3.Lerp(rp.nodes[l][p].transform.position, rp.nodes[l - 1][p + 1].transform.position, 0.5f);
                                    Node divNode = AddNodePoint(rp.nodes[l][p], nodeContainer, pos);
                                    divNode.branches.Add(rp.nodes[l - 1][p + 1]);
                                    divNode.nodeType = Node.NodeType.divert;
                                    rp.nodes[l][p].branches.Add(divNode);
                                    if (rp.nodes.ContainsKey(100))
                                    {
                                        rp.nodes[100].Add(divNode);
                                    }
                                    else
                                    {
                                        rp.nodes.Add(100, new List<Node>() { divNode });
                                    }
                                }
                            }
                        }
                    }
                    for (int l = rp.lanes / 2; l < rp.lanes; l++)
                    {
                        for (int p = 0; p < amount - 1; p++)
                        {
                            if (p % 2 == l % 2)
                            {
                                if (l + 1 < rp.lanes)
                                {
                                    Vector3 pos = Vector3.Lerp(rp.nodes[l][p].transform.position, rp.nodes[l + 1][p + 1].transform.position, 0.5f);
                                    Node divNode = AddNodePoint(rp.nodes[l][p], nodeContainer, pos);
                                    divNode.branches.Add(rp.nodes[l + 1][p + 1]);
                                    divNode.nodeType = Node.NodeType.divert;
                                    rp.nodes[l][p].branches.Add(divNode);
                                    if (rp.nodes.ContainsKey(100))
                                    {
                                        rp.nodes[100].Add(divNode);
                                    }
                                    else
                                    {
                                        rp.nodes.Add(100, new List<Node>() { divNode });
                                    }
                                }
                                if (l> rp.lanes/2)
                                {
                                    Vector3 pos = Vector3.Lerp(rp.nodes[l][p].transform.position, rp.nodes[l - 1][p + 1].transform.position, 0.5f);
                                    Node divNode = AddNodePoint(rp.nodes[l][p], nodeContainer, pos);
                                    divNode.branches.Add(rp.nodes[l - 1][p + 1]);
                                    divNode.nodeType = Node.NodeType.divert;
                                    rp.nodes[l][p].branches.Add(divNode);
                                    if (rp.nodes.ContainsKey(100))
                                    {
                                        rp.nodes[100].Add(divNode);
                                    }
                                    else
                                    {
                                        rp.nodes.Add(100, new List<Node>() { divNode });
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        for (int j = 0; j < roadWay.childCount-1; j++)
        {
            RoadPoint rp = roadWay.GetChild(j).GetComponent<RoadPoint>();
            RoadPoint rp1 = roadWay.GetChild(j + 1).GetComponent<RoadPoint>();
            if (!rp.setNodes || !rp1.setNodes) continue;
            if (rp1.setNodes && rp.lanes == rp1.lanes)
            {
                for (int l = 0; l < rp.lanes / 2; l++)
                {
                    if(rp.nodes[l].Count>0 && rp1.nodes[l].Count>0)
                    {
                        if (rp.nodes.ContainsKey(l))
                            rp.nodes[l][rp.nodes[l].Count - 1].branches.Add(rp1.nodes[l][0]);
                    }
                }
                if (j >= roadWay.childCount - 2) break;
                for (int l = rp.lanes / 2; l < rp.lanes; l++)
                {
                    if (rp.nodes.ContainsKey(l))
                    {
                        if (rp1.nodes[l].Count >0 && rp.nodes[l].Count>0)
                        {
                            rp1.nodes[l][rp1.nodes[l].Count - 1].branches.Add(rp.nodes[l][0]);
                        }
                    }
                }
            }
        }
    }

    public void DrawRadarMesh()
    {
        Mesh radarMesh;
        if (radarMeshFilter.sharedMesh != null)
        {
            radarMeshFilter.sharedMesh.Clear();
            radarMesh = radarMeshFilter.sharedMesh;
        }
        else
        {
            radarMesh = new Mesh();
            radarMeshFilter.sharedMesh = radarMesh;
        }
        radarMesh.vertices = meshFilter.sharedMesh.vertices;

        List<int> radarTris = new List<int>();
        for (int i = 0; i < meshFilter.mesh.subMeshCount; i++)
        {
            radarTris.AddRange(meshFilter.mesh.GetTriangles(i));
        }
        radarMesh.triangles = radarTris.ToArray();
        radarMeshFilter.mesh = radarMesh;
    }

    public Vector3[] EvaluateVertexPos(RoadPoint rp,int smoothPointIndex = 0,Transform rp1 = null)
    {
        Vector3[] verts = new Vector3[4];
        // topRight,topMidle,topLeft,bottomRight,bottomMiddle,bottomLeft
        Vector3 pos = rp.transform.position;
        Vector3 rightVec = rp.transform.right;
        if (smoothPointIndex > 0)
        {
            float t = (float)smoothPointIndex / ((float)(1 + rp.smoothenAmount));
            Vector3 a = Vector3.Lerp(rp.transform.position, rp.smoothenHandle, t);
            Vector3 b = Vector3.Lerp(rp.smoothenHandle, rp1.position, t);
            pos = Vector3.Lerp(a, b, t);
            //rightVec = Vector3.Cross((b - a), Vector3.up).normalized;
            rightVec = Vector3.Lerp(rp.transform.right, rp1.right, t).normalized;
        }
        verts[0] = pos + 0.5f*rightVec * widthPerLane * rp.lanes + 0.5f * roadThickness * Vector3.up;
        //verts[1] = pos + 0.5f*roadThickness * Vector3.up;
        verts[1] = pos - 0.5f * rightVec * widthPerLane * rp.lanes + 0.5f * roadThickness * Vector3.up;
        verts[2] = pos + (0.5f * widthPerLane * rp.lanes + 0.1f)*rightVec  - 0.5f * roadThickness * Vector3.up;
        //verts[4] = pos - 0.5f * roadThickness * Vector3.up;
        verts[3] = pos - (0.5f * widthPerLane * rp.lanes + 0.1f)*rightVec - 0.5f * roadThickness * Vector3.up;
        return verts;
    }

    public Vector3 GetBezierPoint(RoadPoint rp,Transform rp1,float t)
    {
        Vector3 a = Vector3.Lerp(rp.transform.position, rp.smoothenHandle, t);
        Vector3 b = Vector3.Lerp(rp.smoothenHandle, rp1.position, t);
        return Vector3.Lerp(a, b, t);
    }

    public Color[] SetVertexColors(Vector3[] meshVerts)
    {
        Color[] colors = new Color[meshVerts.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            if (i % 3 == 0) { colors[i] = Color.green; }
            else if (i % 3 == 1) { colors[i] = new Color(0, 0.5f, 0); }
            else { colors[i] = Color.black; }
        }
        return colors;
    }

    int[] QuadTriangles(int a,int b,int c,int d)
    {
        return new int[] { a, b, c, a, c, d };
    }

    public Node AddNodePoint(Node prevNode, Transform nodeContainer, Vector3 pos,Node.SpeedTier speedTier = Node.SpeedTier.medium)
    {
        if (pos == Vector3.zero) return null;
        GameObject nodeObject = new GameObject("Node " + nodeContainer.childCount, typeof(Node));
        nodeObject.GetComponent<Node>().speedTier = speedTier;
        nodeObject.transform.position = pos + Vector3.up * 0.5f;

        if (prevNode != null)
        {
            //Set Transform
            nodeObject.transform.rotation = Quaternion.LookRotation(nodeObject.transform.position - prevNode.transform.position);
            // Copy previous node properties
            nodeObject.GetComponent<Node>().CopyProperties(prevNode);
            prevNode.branches.Add(nodeObject.GetComponent<Node>());
        }
        nodeObject.transform.SetParent(nodeContainer);

        return nodeObject.GetComponent<Node>();
    }
}
