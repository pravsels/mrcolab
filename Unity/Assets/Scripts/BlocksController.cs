using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;

public class BlocksController : MonoBehaviour, INetworkComponent, INetworkObject
{           
    NetworkId INetworkObject.Id => new NetworkId("1e1818b3b6cfb401");
    private NetworkContext context;

    public struct Message
    {
        public int layer;

        public Message(int layer)
        {
            this.layer = layer;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this);
    }

    public void setBlocksVisibility(int layer)
    {
        BlocksHider blocksHider = this.gameObject.GetComponent<BlocksHider>();
        if (blocksHider != null)
        {
            blocksHider.SetLayer(layer);
        }
        
        context.SendJson(new Message(layer));
    }

    void INetworkComponent.ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();

        BlocksHider blocksHider = this.gameObject.GetComponent<BlocksHider>();
        if (blocksHider != null)
        {
            blocksHider.SetLayer(msg.layer);
        }
    }
}
