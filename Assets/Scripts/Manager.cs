// Copyright (c) 2022 Alberto Rota
// Modified for Quest Standalone - No ROS/VF dependency
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Manager : MonoBehaviour
{
    List<string> scenes = new List<string>();
    private LogData _logData;

    void Start() {
        _logData = gameObject.GetComponent<LogData>();
        if (_logData != null) {
            _logData.enabled = false;
        }
        
        // List of scene names for random selection
        scenes.Add("Training1");
        scenes.Add("Training2");
        scenes.Add("Training3");
        scenes.Add("Training4");
        scenes.Add("Thymectomy");
        scenes.Add("Nephrectomy");
        scenes.Add("LiverResection");
        scenes.Add("Suturing");
    }

    void Update()
    {
        // Quits when the ESC key is pressed
        if (Input.GetKey(KeyCode.Escape)) {
            Application.Quit();
        }

        // Toggle logging when L key is pressed
        if (Input.GetKeyDown(KeyCode.L)) {
            if (_logData != null) {
                _logData.enabled = !_logData.enabled;
                Debug.Log($"[Manager] Logging: {(_logData.enabled ? "ON" : "OFF")}");
            }
        }

        // Loads a scene of choice when the user presses the corresponding key (with Shift held)
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
            if (Input.GetKeyDown(KeyCode.Alpha0)) {
                SceneManager.LoadScene("Playground");
            } else if (Input.GetKeyDown(KeyCode.Alpha1)) {
                SceneManager.LoadScene("Training1");
            } else if (Input.GetKeyDown(KeyCode.Alpha2)) {
                SceneManager.LoadScene("Training2"); 
            } else if (Input.GetKeyDown(KeyCode.Alpha3)) {
                SceneManager.LoadScene("Training3");
            } else if (Input.GetKeyDown(KeyCode.Alpha4)) {
                SceneManager.LoadScene("Training4");
            } else if (Input.GetKeyDown(KeyCode.Alpha5)) {
                SceneManager.LoadScene("Thymectomy");
            } else if (Input.GetKeyDown(KeyCode.Alpha6)) {
                SceneManager.LoadScene("Nephrectomy");
            } else if (Input.GetKeyDown(KeyCode.Alpha7)) {
                SceneManager.LoadScene("LiverResection");
            } else if (Input.GetKeyDown(KeyCode.Alpha8)) {
                SceneManager.LoadScene("Suturing");
            }
        }

        // N key loads a random scene
        if (Input.GetKeyDown(KeyCode.N)) {
            string randomScene = scenes[Random.Range(0, scenes.Count)];
            Debug.Log($"[Manager] Loading random scene: {randomScene}");
            SceneManager.LoadScene(randomScene);
        }

        // M key reloads the current scene
        if (Input.GetKeyDown(KeyCode.M)) {
            Debug.Log($"[Manager] Reloading scene: {SceneManager.GetActiveScene().name}");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // D key toggles debug mode
        if (Input.GetKeyDown(KeyCode.D)) {
            Global.debugmode = !Global.debugmode;
            Debug.Log($"[Manager] Debug mode: {(Global.debugmode ? "ON" : "OFF")}");
        }
    }
}
