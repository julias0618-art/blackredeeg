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
public class IsPinchableTarget : MonoBehaviour
{
    Material materialtarget;
    Material materialtargethit;
    Material materialtargetpinchable;

    public bool graphics = false;
    public Transform pincherObject;
    float d;
    float targetRadius; 
    public bool reached = false;
    bool pinchable = false;
    
    // Reference to PSM controller
    private XRDualPSMDirectController _psmController;
   
    void Start()
    {
        if (pincherObject == null){
            pincherObject = GameObject.Find(Global.tooltip_path)?.transform;
        }
        
        // Find PSM controller
        _psmController = FindObjectOfType<XRDualPSMDirectController>();
        
        materialtarget = Resources.Load<Material>("Materials/Target");
        materialtargethit = Resources.Load<Material>("Materials/TargetReached");
        materialtargetpinchable = Resources.Load<Material>("Materials/TargetPinchable");
        
        // Disable the collider
        gameObject.GetComponent<SphereCollider>().enabled = false;
        gameObject.GetComponent<Rigidbody>().mass = 0;
        gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        gameObject.GetComponent<Rigidbody>().useGravity = false;
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
        Vector3 tool = pincherObject.position;
        Vector3 target = gameObject.transform.position;
        d = Vector3.Distance(target, tool);
        
        if (d < targetRadius) {
            pinchable = true;
        } else {
            pinchable = false;
        }
        
        if (pinchable && pinchingAction) {
            reached = true;
        } 

        if (graphics) {
            Global.Arrow(tool, target, Color.yellow);
        }

        var renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (reached && materialtargethit != null) {
                renderer.material = materialtargethit;
            } else if (pinchable && materialtargetpinchable != null) {
                renderer.material = materialtargetpinchable;
            } else if (!reached && !pinchable && materialtarget != null) {
                renderer.material = materialtarget;
            }
        }
    }
}
