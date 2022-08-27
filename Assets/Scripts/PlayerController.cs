
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static bool paused = false;

    public ThirdPersonCamera mainCamera;
    public VehicleController activeVehicle;
    public static Transform mainCam;

    [Header("Radar")]
    public Transform radarCam;
    public MapManager mapManager;
    public PathFinder pathFinder;
    public LayerMask nodeLayer;
    public MeshFilter meshFilter;
    public MeshFilter pathMeshFilter;
    public float gpsPathWidth = 6;
    public Transform playerRadarMarker;

    public enum CameraType { front,close,distant,far}
    public CameraType currentCamType = CameraType.distant;

    int frame = 0;
    List<Node> path = new List<Node>();


    void Start()
    {
        if(mainCamera!=null)
            mainCam = mainCamera.transform;
    }

    public static void ChangePauseStat(bool pause)
    {
        if (pause)
        {
            Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            paused = true;
        }
        else
        {
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = true;
            paused = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (activeVehicle == null) return;
        if (Input.GetKeyDown(KeyCode.Q)) activeVehicle.SwitchHeadlights();
        activeVehicle.Move(Input.GetAxis("Horizontal"), Input.GetKey(KeyCode.W), Input.GetKey(KeyCode.S), Input.GetButton("Jump"));
        if (Input.GetButtonDown("Camera"))
        {
            switch (currentCamType)
            {
                case CameraType.front:
                    currentCamType = CameraType.close;
                    break;
                case CameraType.close:
                    currentCamType = CameraType.distant;
                    break;
                case CameraType.distant:
                    currentCamType = CameraType.far;
                    break;
                case CameraType.far:
                    currentCamType = CameraType.front;
                    break;
                default:
                    currentCamType = CameraType.distant;
                    break;
            }
            ChangeCamType(currentCamType);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            activeVehicle.Honk();
        }

        if (!paused)
        {
            radarCam.position = new Vector3(activeVehicle.transform.position.x, 200, activeVehicle.transform.position.z);
            radarCam.localEulerAngles = new Vector3(90, activeVehicle.transform.eulerAngles.y, 0);
            playerRadarMarker.position = new Vector3(activeVehicle.transform.position.x, 10, activeVehicle.transform.position.z);
            playerRadarMarker.localEulerAngles = new Vector3(0, activeVehicle.transform.eulerAngles.y, 0);
        }
    }

    private void FixedUpdate()
    {
        if (activeVehicle == null) return;
        if (frame == 20)
        {
            // get start node for path finder
            Collider[] cols = Physics.OverlapSphere(activeVehicle.transform.position, 10, nodeLayer);
            Node n = null;
            for (int i = 0;i<cols.Length;i++)
            {
                Collider col = cols[i];
                if (Vector3.Dot(col.transform.forward, activeVehicle.transform.forward) > 0)
                {
                    n = col.GetComponent<Node>();
                    if (path != null)
                    {
                        if (path.Contains(n))
                            break;
                    }
                }
            }
            pathFinder.startNode = n;

            // FIND PATH

            //float startTime = Time.realtimeSinceStartup;
            pathFinder.FindPath();
            path = pathFinder.currentPath;
            //Debug.Log(((Time.realtimeSinceStartup - startTime) * 1000) + "ms");
            if (path != null)
            {
                //float startTime = Time.realtimeSinceStartup;
                pathFinder.DrawPathMesh(path,meshFilter,6);
                if(pathMeshFilter!=null)
                    pathFinder.DrawPathMesh(path, pathMeshFilter, 1);
                //Debug.Log(((Time.realtimeSinceStartup - startTime) * 1000) + "ms");
            }
            else if(meshFilter.mesh.vertices.Length>0 && pathFinder.endNode == null)
            {
                meshFilter.mesh.Clear();
                if(pathMeshFilter!=null)
                    pathMeshFilter.mesh.Clear();
            }
            frame = 0;
        }
        frame += 1;
    }
    
    void ChangeCamType(CameraType type)
    {
        if (mainCam == null) return;
        switch (type)
        {
            case CameraType.front:
                mainCamera.frontCam = true;
                mainCamera.activeVehicle = activeVehicle;
                break;
            case CameraType.close:
                mainCamera.frontCam = false;
                mainCamera.initCamDist = 4;
                break;
            case CameraType.distant:
                mainCamera.frontCam = false;
                mainCamera.initCamDist = 6;
                break;
            case CameraType.far:
                mainCamera.frontCam = false;
                mainCamera.initCamDist = 10;
                break;
            default:
                mainCamera.frontCam = false;
                mainCamera.initCamDist = 6;
                break;
        }
    }
    
}
