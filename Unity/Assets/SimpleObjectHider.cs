using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;

public class SimpleObjectHider : MonoBehaviour, INetworkObject, INetworkComponent
{
    public NetworkId Id { get; } = new NetworkId();
    private NetworkContext context;
    private Transform[] childTransforms;
    private Renderer[] renderers;

    public struct Message
    {
        public bool show;

        public Message(bool show)
        {
            this.show = show;
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();
        foreach (Renderer child in renderers)
        {
            child.enabled = msg.show;
        }
    }

    public void SetVisibility(bool show)
    {
        foreach (Renderer child in renderers)
        {
            child.enabled = show;
        }
        context.SendJson(new Message(show));
    }

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this);
        renderers = GetComponentsInChildren<Renderer>();
        Debug.Log(renderers.Length);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
