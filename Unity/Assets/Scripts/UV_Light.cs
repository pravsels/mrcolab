using System; 
using System.Collections;
using System.Collections.Generic;
using Ubiq.XR;
using UnityEngine;
using UnityEngine.Networking;

public class UV_Light : MonoBehaviour, IGraspable
{
    public Light pointlight;
    public bool is_switched = false;
    public float update_frequency = .1f;
    public string switch_url = "http://127.0.0.1:5000/";

    struct StatusReponse
    {
        public string status;
    };

    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating("StatusUpdate", 0f, update_frequency);
    }

    void IGraspable.Grasp(Hand controller)
    {
        Debug.Log("GRASPED!");
        StartCoroutine(Toggle(switch_url + "toggle"));
    }

    void switch_update()
    {
        pointlight.intensity = Convert.ToInt32(is_switched) * 2f;
    }

    IEnumerator Toggle(string uri)
    {
        using (UnityWebRequest getRequest = UnityWebRequest.Get(uri))
        {
            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(getRequest.error);
            }
        }
    }

    IEnumerator GetStatus(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(webRequest.error);
            } else if (webRequest.result == UnityWebRequest.Result.Success)
            {
                StatusReponse response = JsonUtility.FromJson<StatusReponse>(webRequest.downloadHandler.text);

                if (response.status == "ON")
                {
                    is_switched = true;
                    this.gameObject.GetComponentInChildren<GlowingLight>().pointlight.intensity = 0f;
                    this.gameObject.GetComponentInChildren<GlowingLight>().enabled = false;
                }
                else
                {
                    is_switched = false;
                    this.gameObject.GetComponentInChildren<GlowingLight>().enabled = true;
                }
            }
        }
    }

    // Update is called once per frame
    void StatusUpdate()
    {
        StartCoroutine(GetStatus(switch_url + "status"));
        switch_update();
    }

    void IGraspable.Release(Hand controller)
    {
        Debug.Log("UNGRASPED!");
        StartCoroutine(Toggle(switch_url + "toggle"));
    }
}
