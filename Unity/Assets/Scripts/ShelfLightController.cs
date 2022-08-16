using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;

public class ShelfLightController : MonoBehaviour, INetworkComponent, INetworkObject
{                                                 
    NetworkId INetworkObject.Id => new NetworkId("9e19c092cdf72efd");
    private NetworkContext context; 

    public struct Message
    {
        public bool shelf_light; 

        public Message(bool shelf_light)
        {
            this.shelf_light = shelf_light;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this);
    }

    public void setShelfLight(bool shelfLight)
    {
        this.gameObject.GetComponent<GlowingLight>().enabled = shelfLight;
        context.SendJson(new Message(shelfLight));
    }

    void INetworkComponent.ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();

        this.gameObject.GetComponent<GlowingLight>().enabled = msg.shelf_light;
    }
}
