using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlowingLight : MonoBehaviour
{
    public Light pointlight;
    public float max_intensity = 3f;
    public float delta_light = 0.2f;
    private bool increase_intensity = true;
    private bool decrease_intensity = false; 

    // Start is called before the first frame update
    void Awake()
    {
        pointlight.intensity = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        int multiplier = increase_intensity == true ? 1 : decrease_intensity == true ? -1 : 0;

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
