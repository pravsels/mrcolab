using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Spawning;

public class PosterController : MonoBehaviour, INetworkObject, INetworkComponent, ISpawnable
{
    public NetworkId Id { get; set; } = new NetworkId("a234-5996-4920-a196");
    private NetworkContext context;

    public struct Message
    {
        public string posterSetName;

        public Message(string posterSetName)
        {
            this.posterSetName = posterSetName;
        }
    }


    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(child.name == msg.posterSetName);
        }
    }

    public void setPosterVisibility(string setName)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(child.name == setName);
        }
        context.SendJson(new Message(setName));
    }

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnSpawned(bool local)
    {
    }

    public bool IsLocal()
    {
        return true;
    }
}
