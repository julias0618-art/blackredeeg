// Copyright (c) 2022 Alberto Rota
// Modified for Quest Standalone
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple target that is reached by contact (no pinching required).
/// </summary>
[ExecuteInEditMode, RequireComponent(typeof(SphereCollider))]
public class IsTarget : MonoBehaviour
{
    [Header("References")]
    public Transform toolTip;
    
    [Header("Visual")]
    public bool graphics = false;
    
    [Header("State")]
    public bool reached = false;
    public bool isNearby = false;
    
    private Material _materialTarget;
    private Material _materialReached;
    private Material _materialNearby;
    private float _targetRadius;
    private Renderer _renderer;

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        
        // Try to find tooltip if not assigned
        if (toolTip == null)
        {
            var psmController = FindObjectOfType<XRDualPSMDirectController>();
            if (psmController != null && psmController.rightPSM.rollLink != null)
            {
                toolTip = psmController.rightPSM.rollLink;
            }
            else
            {
                var obj = GameObject.Find(Global.GetTooltipPath());
                if (obj != null) toolTip = obj.transform;
            }
        }
        
        // Load materials
        _materialTarget = Resources.Load<Material>("Materials/Target");
        _materialReached = Resources.Load<Material>("Materials/TargetReached");
        _materialNearby = Resources.Load<Material>("Materials/TargetPinchable");
        
        // Setup collider
        var collider = GetComponent<SphereCollider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    void Update()
    {
        if (toolTip == null) return;
        
        var collider = GetComponent<SphereCollider>();
        if (collider == null) return;
        
        _targetRadius = collider.radius * transform.lossyScale.x;
        
        float distance = Vector3.Distance(transform.position, toolTip.position);
        
        // Check if reached
        if (distance < _targetRadius)
        {
            reached = true;
            isNearby = true;
        }
        else if (distance < _targetRadius * 2f)
        {
            isNearby = true;
        }
        else
        {
            isNearby = false;
        }
        
        // Update visuals
        if (_renderer != null)
        {
            if (reached && _materialReached != null)
            {
                _renderer.material = _materialReached;
            }
            else if (isNearby && _materialNearby != null)
            {
                _renderer.material = _materialNearby;
            }
            else if (_materialTarget != null)
            {
                _renderer.material = _materialTarget;
            }
        }
        
        // Debug graphics
        if (graphics)
        {
            Global.Arrow(toolTip.position, transform.position, reached ? Color.green : Color.yellow);
        }
    }
    
    /// <summary>
    /// Reset the target state
    /// </summary>
    public void ResetTarget()
    {
        reached = false;
        isNearby = false;
    }
}
