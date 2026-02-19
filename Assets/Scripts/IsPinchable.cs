// Copyright (c) 2022 Alberto Rota
// Modified for Quest Standalone - No ROS dependency
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, RequireComponent(typeof(SphereCollider)), RequireComponent(typeof(Rigidbody))]
public class IsPinchable : MonoBehaviour
{
    Material materialpinched;
    Material materialown;
    Material materialpinchable;
    Vector3 tool;
    Vector3 target;
    float d;
    float targetRadius; 
    public bool graphics = false;
    public Transform pincherObject;
    public bool restoreGravity = false;

    public bool pinched = false;
    public bool pinchable = false;
    
    // Reference to PSM controller
    private XRDualPSMDirectController _psmController;

    void Start()
    {
        materialpinched = Resources.Load<Material>("Materials/Pinched");
        materialown = gameObject.GetComponent<Renderer>()?.sharedMaterial;
        materialpinchable = Resources.Load<Material>("Materials/Pinchable");
        
        // Disable the collider
        gameObject.GetComponent<SphereCollider>().enabled = false;
        gameObject.GetComponent<Rigidbody>().mass = 0;

        if (pincherObject == null) {
            pincherObject = GameObject.Find(Global.tooltip_path)?.transform;
        }
        
        // Find PSM controller
        _psmController = FindObjectOfType<XRDualPSMDirectController>();
    }

    void Update()
    {
        if (pincherObject == null) return;
        
        bool pinchingAction = false;
        
        // Get pinch state from PSM controller or keyboard
        if (_psmController != null)
        {
            pinchingAction = _psmController.rightPSM.triggerValue > 0.7f;
        }
        else
        {
            // Keyboard fallback
            pinchingAction = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.RightControl);
        }

        targetRadius = gameObject.GetComponent<SphereCollider>().radius * gameObject.transform.lossyScale.x;
        tool = pincherObject.position;
        target = gameObject.transform.position;
        d = Vector3.Distance(target, tool);
        
        if (d < targetRadius) {
            pinchable = true;
            if (pinchingAction) {
                pinched = true;
            } else {
                pinched = false;
            }
        } else {
            pinchable = false;
        }
       
        if (graphics) {
            Global.Arrow(tool, target, Color.yellow);
        }

        var renderer = gameObject.GetComponent<Renderer>();
        if (pinched) {
            if (renderer != null && materialpinched != null && renderer.sharedMaterial != materialpinched) {
                renderer.sharedMaterial = materialpinched;
            }
            if (gameObject.GetComponent<FixedJoint>() == null && pincherObject.GetComponent<Rigidbody>() != null) {
                gameObject.AddComponent<FixedJoint>();
                gameObject.GetComponent<FixedJoint>().connectedBody = pincherObject.GetComponent<Rigidbody>();
            }
        } else {
            if (gameObject.GetComponent<FixedJoint>() != null) {
                DestroyImmediate(gameObject.GetComponent<FixedJoint>());
            }
            if (restoreGravity) {
                gameObject.GetComponent<Rigidbody>().useGravity = true;
            } else {
                gameObject.GetComponent<Rigidbody>().useGravity = false;
            }
        }
        
        if (renderer != null) {
            if (pinchable && !pinched && materialpinchable != null) {
                renderer.sharedMaterial = materialpinchable;
            }
            if (!pinchable && !pinched && materialown != null) {
                if (renderer.sharedMaterial != materialown) {
                    renderer.sharedMaterial = materialown;
                }
            }
        }
    }
}
