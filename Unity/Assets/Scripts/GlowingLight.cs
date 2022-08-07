using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlowingLight : MonoBehaviour
{
    public Light pointlight;
    public float max_intensity = 2f;
    public float delta_light = 0.01f;
    private bool increase_intensity;
    private bool decrease_intensity; 

    // Start is called before the first frame update
    void Start()
    {
        pointlight.intensity = 0f;
        increase_intensity = true;
        decrease_intensity = false; 
    }

    // Update is called once per frame
    void Update()
    {
        int multiplier = increase_intensity == true ? 1 : -1;

        if (increase_intensity || decrease_intensity)
        {
            pointlight.intensity += multiplier * delta_light;
        }
        
        if (pointlight.intensity > max_intensity)
        {
            increase_intensity = false;
            decrease_intensity = true; 
        }
        if (pointlight.intensity < 0.1f)
        {
            increase_intensity = true;
            decrease_intensity = false; 
        }
    }
}
