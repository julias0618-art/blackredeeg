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

public class CheckTaskCompletion : MonoBehaviour
{
    public List<Transform> targets;
    public int targetReached = 0;
    public int totalTargets = 0;
    
    [Header("UI (Optional)")]
    public GameObject completionCanvasRight;
    public GameObject completionCanvasLeft;

    void Start()
    {
        targets = new List<Transform>();
        foreach (Transform child in gameObject.transform)
        {
            targets.Add(child);
        }
        totalTargets = targets.Count;
        
        // Try to find completion canvases if not assigned
        if (completionCanvasRight == null)
            completionCanvasRight = GameObject.Find("Text/Canvas");
        if (completionCanvasLeft == null)
            completionCanvasLeft = GameObject.Find("Text/CanvasL");
            
        // Hide completion UI at start
        if (completionCanvasRight != null) completionCanvasRight.SetActive(false);
        if (completionCanvasLeft != null) completionCanvasLeft.SetActive(false);
    }

    void Update()
    {
        targetReached = 0;
        
        foreach (Transform child in gameObject.transform) {
            // Check IsTarget component
            var isTarget = child.GetComponent<IsTarget>();
            if (isTarget != null && isTarget.reached) {
                targetReached++;
                continue;
            }
            
            // Check IsPinchableTarget component
            var isPinchableTarget = child.GetComponent<IsPinchableTarget>();
            if (isPinchableTarget != null && isPinchableTarget.reached) {
                targetReached++;
            }
        }
        
        // Check if all targets reached
        if (targetReached >= totalTargets && totalTargets > 0) {
            OnTaskCompleted();
        } else {
            OnTaskInProgress();
        }
    }
    
    void OnTaskCompleted()
    {
        // Show completion UI
        if (completionCanvasRight != null) completionCanvasRight.SetActive(true);
        if (completionCanvasLeft != null) completionCanvasLeft.SetActive(true);
    }
    
    void OnTaskInProgress()
    {
        // Hide completion UI
        if (completionCanvasRight != null) completionCanvasRight.SetActive(false);
        if (completionCanvasLeft != null) completionCanvasLeft.SetActive(false);
    }
    
    /// <summary>
    /// Reset all targets to not reached state
    /// </summary>
    public void ResetAllTargets()
    {
        foreach (Transform child in gameObject.transform) {
            var isTarget = child.GetComponent<IsTarget>();
            if (isTarget != null) {
                isTarget.reached = false;
            }
            
            var isPinchableTarget = child.GetComponent<IsPinchableTarget>();
            if (isPinchableTarget != null) {
                isPinchableTarget.reached = false;
            }
        }
        targetReached = 0;
    }
}
