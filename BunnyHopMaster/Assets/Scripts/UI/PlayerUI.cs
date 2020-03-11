﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class PlayerUI : MonoBehaviour {
    public bool isPaused;

    [SerializeField]
    GameObject inGameUI;

    [SerializeField]
    PauseMenu pauseMenu;

    [SerializeField]
    WinMenu winMenu;

    public Image crossHair;

    void Start () {
        inGameUI.SetActive(true);
        pauseMenu.gameObject.SetActive(false);
        winMenu.gameObject.SetActive(false);
    }
	
	void Update () {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (isPaused)
            {
                UnPause();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
        isPaused = true;
        Time.timeScale = 0;
        pauseMenu.gameObject.SetActive(true);
        inGameUI.SetActive(false);
        winMenu.gameObject.SetActive(false);
    }

    public void UnPause()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        isPaused = false;
        Time.timeScale = 1;
        pauseMenu.gameObject.SetActive(false);
        inGameUI.SetActive(true);
        winMenu.gameObject.SetActive(false);
    }

    public void ShowWinScreen()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        winMenu.gameObject.SetActive(true);
        winMenu.levelText.text = GameManager.Instance.levelDataContainer.levels[GameManager.Instance.currentLevelBuildIndex - 1].levelName;
        winMenu.completionTimeText.text = "Completion time: " + GetTimeString(GameManager.Instance.currentCompletionTime);

        TimeSpan time = TimeSpan.FromSeconds(GameManager.Instance.levelDataContainer.levels[GameManager.Instance.currentLevelBuildIndex - 1].completionTime);
        winMenu.bestTimeText.text = "Best time: " + time.ToString("hh':'mm':'ss");
    }

    string GetTimeString(float completionTime)
    {
        TimeSpan time = TimeSpan.FromSeconds(completionTime);
        return time.ToString("hh':'mm':'ss");
    }
}
