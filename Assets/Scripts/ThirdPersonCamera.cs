using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour {

    public GameObject player;
    public Vector3 camOffset;
    public float initCamDist = 3f;
    public bool smooth = true;
    public float smoothTime = 5f;
    [Range(1,200)]
    public float xSens = 100f;
    [Range(1, 200)]
    public float ySens = 100f;
    float rotX = 0f;
    float rotY = 0f;
    public Transform camReference;
    public LayerMask layerMask;
    public float reallignTime = 5;
    float lastMoved = 0;
    [HideInInspector]
    public bool frontCam = false;
    [HideInInspector]
    public VehicleController activeVehicle;
	// Use this for initialization
	void Start () {
        this.transform.position = camReference.position + camOffset - camReference.forward * initCamDist;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
	
	// Update is called once per frame
	void Update () {
        if (frontCam)
        {
            transform.position = activeVehicle.frontCamPos.position;
            transform.rotation = Quaternion.LookRotation(camReference.parent.forward);
        }
        else
        {
            if (Input.GetAxis("Mouse X") != 0f || Input.GetAxis("Mouse Y") != 0f)
            {
                rotX = Input.GetAxis("Mouse X") * xSens * Time.deltaTime;
                rotY = Input.GetAxis("Mouse Y") * ySens * Time.deltaTime;
                Vector3 target = Quaternion.AngleAxis(-rotY, this.transform.right) * camReference.forward;
                camReference.rotation = Quaternion.LookRotation(Quaternion.AngleAxis(rotX, Vector3.up) * target);
                Vector3 camRot = camReference.eulerAngles;
                if (camRot.x > 60f && camRot.x < 90f) camRot.x = 60f;
                if (camRot.x < 320f && camRot.x > 270f) camRot.x = 320f;
                camReference.eulerAngles = new Vector3(camRot.x, camRot.y, 0);
                lastMoved = Time.time;
            }

            //this.transform.rotation = Quaternion.Slerp(this.transform.rotation,Quaternion.LookRotation(camReference.position-this.transform.position),smoothTime*Time.deltaTime);
            this.transform.rotation = (smooth ? Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(camReference.position - this.transform.position), smoothTime * Time.deltaTime) : Quaternion.LookRotation(camReference.position - this.transform.position));
            RaycastHit hit;
            if (Physics.Raycast(camReference.position, -camReference.forward, out hit, initCamDist, layerMask))
            {
                this.transform.position = (smooth ? hit.point + hit.normal * 0.2f : Vector3.Lerp(this.transform.position, hit.point, smoothTime * Time.deltaTime));
                //this.transform.position = Vector3.Lerp(this.transform.position,hit.point,smoothTime*Time.deltaTime);
                //this.transform.position = hit.point+hit.normal*0.2f;
            }
            else
            {
                //this.transform.position = Vector3.Lerp(this.transform.position, camReference.position - camReference.forward * initCamDist,smoothTime*Time.deltaTime);
                this.transform.position = camReference.position - camReference.forward * initCamDist;
            }
            //this.transform.position = Vector3.Lerp(this.transform.position, camReference.position - camReference.forward * initCamDist, smoothTime * Time.deltaTime);

        }
        if (Time.time > lastMoved + reallignTime && Time.time < lastMoved + reallignTime + 4)
        {
            camReference.rotation = Quaternion.Slerp(camReference.rotation, Quaternion.LookRotation(camReference.parent.forward - 0.25f * Vector3.up), 1.5f * Time.deltaTime);
        }
    }
    
}
