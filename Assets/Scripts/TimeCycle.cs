using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeCycle : MonoBehaviour
{
    public bool pauseTime = false;
    public Transform sun;
    [Range(0, 5)]
    public float timeScale = 1f;
    [Range(0, 24)]
    public float currentTime = 10f;
    [Range(1, 60)]
    public float dayInMinutes = 24;
    public static bool isDay = true;

    public static Action DayNightTransition;

    // Start is called before the first frame update
    void Start()
    {
        if (Vector3.Dot(sun.forward, Vector3.up) > 0)
        {
            isDay = false;
        }
        else { isDay = true;  }
    }

    // Update is called once per frame
    void Update()
    {
        if (!pauseTime)
        {
            sun.Rotate(Vector3.forward, (6 / dayInMinutes) * Time.deltaTime * 30 * Mathf.Deg2Rad, Space.World);
            sun.GetComponent<Light>().intensity = Mathf.Clamp(Vector3.Dot(-Vector3.up, sun.forward), 0f, 1f);
            currentTime = (sun.localEulerAngles.z % 360) / 15;
            if (isDay && Vector3.Dot(sun.forward, Vector3.up) > 0.1f)
            {
                // Day ----> Night
                isDay = false;
                DayNightTransition?.Invoke();
            }
            if (!isDay && Vector3.Dot(sun.forward, Vector3.up) < 0.1f)
            {
                //Night ----> Day
                isDay = true;
                DayNightTransition?.Invoke();
            }
        }
    }
}
