using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    public static float time_left = 0;
    private TMPro.TextMeshProUGUI clock_text;
    private static TMPro.TextMeshProUGUI clock_text_static;

    // Start is called before the first frame update
    void Start()
    {
        clock_text = GetComponent<TMPro.TextMeshProUGUI>();
        clock_text_static = clock_text;
    }

    // Update is called once per frame
    void Update()
    {
        if (time_left > 0.1)
        {
            time_left -= Time.deltaTime;

            int minutes = (int)Math.Floor(time_left / 60f);
            int seconds = ((int)Math.Floor(time_left)) % 60;

            if (seconds < 10)
            {
                clock_text.text = minutes.ToString() + ":0" + seconds.ToString();
            } else
            {
                clock_text.text = minutes.ToString() + ":" + seconds.ToString();
            }
        } else
        {
            clock_text.text = "0:00";
        }
    }

    public static void ResetTimer()
    {
        time_left = 180;
    }

    public static void EndGame()
    {
        clock_text_static.fontSize = 0.2f;
        clock_text_static.text = "Game Over";
    }
}
