using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlocksHider : MonoBehaviour
{
    private int hideLayer = 8;
    private int defaultLayer = 0; // default
    private int currentLayer = 0;

    private Transform[] childTransforms;
    private Rigidbody[] rigidbodies;
    private Collider[] colliders;

    public int GetCurrentLayer()
    {
        return currentLayer;
    }

    private void Awake()
    {
        childTransforms = GetComponentsInChildren<Transform>();
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
    }

    public void SetLayer(int layer) // not networked
    {
        var previousLayer = currentLayer;
        currentLayer = layer;
        this.gameObject.layer = layer;
        
        foreach (var child in childTransforms)
        {
            child.gameObject.layer = layer;
        }
        // don't set this every frame if nothing changed
        if (previousLayer != layer)
        {
            DisableEnablePhysics();
        }
    }

    public void DisableEnablePhysics()
    {
        if (currentLayer == hideLayer)
        {
            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            foreach (var co in colliders)
            {
                co.enabled = false;
            }
        }
        else if (currentLayer == defaultLayer)
        {
            foreach (var co in colliders)
            {
                co.enabled = true;
            }
            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }
}