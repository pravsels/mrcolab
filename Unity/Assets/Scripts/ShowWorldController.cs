using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;

public class ShowWorldController : MonoBehaviour, INetworkComponent, INetworkObject
{
    NetworkId INetworkObject.Id => new NetworkId("454ed830c067213b");
    private NetworkContext context;

    public struct Message
    {
        public bool show_world;

        public Message(bool show_world)
        {
            this.show_world = show_world;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this);
    }

    public void setShowWorld(bool showWorld)
    {
        this.gameObject.GetComponent<OVRPassthroughLayer>().enabled = showWorld;
        context.SendJson(new Message(showWorld));
    }

    void INetworkComponent.ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();

        this.gameObject.GetComponent<OVRPassthroughLayer>().enabled = msg.show_world;
    }
}
