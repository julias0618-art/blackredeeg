// Copyright (c) 2022 Alberto Rota
// Modified for Quest Standalone - No VF dependency
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks this object as an obstacle.
/// In Quest standalone mode, this provides visual feedback only (no force feedback).
/// </summary>
public class IsObstacle : MonoBehaviour
{
    [Header("Subject to track proximity")]
    public Transform subject;
    
    [Header("Visual Feedback")]
    public bool showProximityWarning = true;
    public float warningDistance = 0.05f;
    public Color warningColor = Color.red;
    
    private Renderer _renderer;
    private Color _originalColor;
    private bool _isNearby = false;

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _originalColor = _renderer.material.color;
        }
        
        // Try to find subject if not assigned
        if (subject == null)
        {
            var psmController = FindObjectOfType<XRDualPSMDirectController>();
            if (psmController != null && psmController.rightPSM.rollLink != null)
            {
                subject = psmController.rightPSM.rollLink;
            }
            else
            {
                var obj = GameObject.Find(Global.tooltip_path);
                if (obj != null) subject = obj.transform;
            }
        }
    }

    void Update()
    {
        if (subject == null || !showProximityWarning) return;
        
        float distance = Vector3.Distance(transform.position, subject.position);
        
        if (distance < warningDistance)
        {
            if (!_isNearby)
            {
                _isNearby = true;
                if (_renderer != null)
                {
                    _renderer.material.color = warningColor;
                }
            }
        }
        else
        {
            if (_isNearby)
            {
                _isNearby = false;
                if (_renderer != null)
                {
                    _renderer.material.color = _originalColor;
                }
            }
        }
    }
}
