// Copyright (c) 2022 Alberto Rota
// Modified for Quest Standalone Mode - VF/ROS removed
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

public class LogData : MonoBehaviour
{
    [Header("Settings")]
    public string TASKNAME;
    public string saveTo;
    public Transform subject;
    
    [Header("Optional References")]
    public Transform cameraTransform;
    public XRDualPSMDirectController psmController;
    
    string foldername;
    string folderpath;
    string path;
    bool isLogging = false;

    string GetUniqueName(string name, string folderPath) {
        string validatedName = name + "_0";
        int tries = 0;
        while (Directory.Exists(folderPath + "\\" + validatedName)) {
            tries++;
            validatedName = name + "_" + tries.ToString();
        }
        return validatedName;
    }

    public void OnEnable() {
        TASKNAME = SceneManager.GetActiveScene().name;
        
        // Find PSM controller if not assigned
        if (psmController == null)
        {
            psmController = FindObjectOfType<XRDualPSMDirectController>();
        }
        
        // Find camera if not assigned
        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }
        
        // Find subject (tool tip) if not assigned
        if (subject == null)
        {
            // Try to get from PSM controller
            if (psmController != null && psmController.rightPSM != null)
            {
                if (psmController.rightPSM.rollLink != null)
                    subject = psmController.rightPSM.rollLink;
                else if (psmController.rightPSM.gripperLeft != null)
                    subject = psmController.rightPSM.gripperLeft;
            }
            
            // Fallback to Global path
            if (subject == null)
            {
                var obj = GameObject.Find(Global.GetTooltipPath());
                if (obj != null) subject = obj.transform;
            }
        }

        // Setup save path
        if (string.IsNullOrEmpty(saveTo))
        {
            saveTo = Application.persistentDataPath + "\\Logs";
        }
        
        // Ensure directory exists
        if (!Directory.Exists(saveTo))
        {
            Directory.CreateDirectory(saveTo);
        }

        // Creates the UNIQUE folder to save the logs
        foldername = GetUniqueName(TASKNAME, saveTo);
        folderpath = saveTo + "\\" + foldername;

        // Creates the folder
        Directory.CreateDirectory(folderpath);
        Debug.Log("[LogData] Saving to: " + folderpath);

        // Create log file with headers
        path = folderpath + "\\" + foldername + "_data.csv";
        WriteHeaders();
        
        isLogging = true;
    }

    void WriteHeaders()
    {
        using (StreamWriter writer = new StreamWriter(path, false))
        {
            writer.Write("Time,");
            writer.Write("SubjectPosX,SubjectPosY,SubjectPosZ,");
            writer.Write("SubjectFwdX,SubjectFwdY,SubjectFwdZ,");
            writer.Write("RightTrigger,LeftTrigger,");
            writer.Write("RightYaw,RightPitch,RightInsertion,");
            writer.Write("LeftYaw,LeftPitch,LeftInsertion,");
            writer.Write("CameraDistance");
            writer.WriteLine();
        }
    }

    void Update()
    {
        if (!isLogging) return;
        if (subject == null) return;

        using (StreamWriter writer = new StreamWriter(path, true))
        {
            // Time
            writer.Write(Time.realtimeSinceStartup.ToString("F4"));
            writer.Write(",");

            // Subject position
            writer.Write(subject.position.x.ToString("F4")); writer.Write(",");
            writer.Write(subject.position.y.ToString("F4")); writer.Write(",");
            writer.Write(subject.position.z.ToString("F4")); writer.Write(",");

            // Subject forward
            writer.Write(subject.forward.x.ToString("F4")); writer.Write(",");
            writer.Write(subject.forward.y.ToString("F4")); writer.Write(",");
            writer.Write(subject.forward.z.ToString("F4")); writer.Write(",");

            // Trigger values
            if (psmController != null)
            {
                writer.Write(psmController.rightPSM.triggerValue.ToString("F2")); writer.Write(",");
                writer.Write(psmController.leftPSM.triggerValue.ToString("F2")); writer.Write(",");
                
                // Right arm values
                writer.Write(psmController.rightPSM.yawValue.ToString("F2")); writer.Write(",");
                writer.Write(psmController.rightPSM.pitchValue.ToString("F2")); writer.Write(",");
                writer.Write(psmController.rightPSM.insertionValue.ToString("F4")); writer.Write(",");
                
                // Left arm values
                writer.Write(psmController.leftPSM.yawValue.ToString("F2")); writer.Write(",");
                writer.Write(psmController.leftPSM.pitchValue.ToString("F2")); writer.Write(",");
                writer.Write(psmController.leftPSM.insertionValue.ToString("F4")); writer.Write(",");
            }
            else
            {
                writer.Write("0,0,0,0,0,0,0,0,");
            }

            // Distance from camera
            if (cameraTransform != null)
            {
                float dist = Vector3.Distance(subject.position, cameraTransform.position);
                writer.Write(dist.ToString("F4"));
            }
            else
            {
                writer.Write("0");
            }

            writer.WriteLine();
        }
    }

    void OnDisable()
    {
        if (isLogging)
        {
            Debug.Log("[LogData] Logging stopped. File saved to: " + path);
            isLogging = false;
        }
    }
    
    /// <summary>
    /// Stop logging and close file
    /// </summary>
    public void StopLogging()
    {
        isLogging = false;
        Debug.Log("[LogData] Logging stopped manually.");
    }
    
    /// <summary>
    /// Resume logging
    /// </summary>
    public void ResumeLogging()
    {
        isLogging = true;
        Debug.Log("[LogData] Logging resumed.");
    }
}
