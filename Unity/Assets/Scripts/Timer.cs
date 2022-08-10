using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    public static float time_elapsed = 0;
    public static bool paused = false;
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
        if (paused)
            return;

        if (time_elapsed >= 0.001)
        {
            time_elapsed += Time.deltaTime;

            int minutes = (int)Math.Floor(time_elapsed / 60f);
            int seconds = ((int)Math.Floor(time_elapsed)) % 60;

            if (seconds < 10)
            {
                clock_text.text = minutes.ToString() + ":0" + seconds.ToString();
            }
            else
            {
                clock_text.text = minutes.ToString() + ":" + seconds.ToString();
            }
        }
        else
        {
            clock_text.text = "0:00";
        }
    }

    public static void ResetTimer()
    {
        time_elapsed = 0.001f;
    }

    public static void EndGame()
    {
        clock_text_static.fontSize = 0.2f;
        clock_text_static.text = "Game Over";
    }
}