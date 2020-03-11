﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerProgress : MonoBehaviour
{
    [SerializeField]
    public Checkpoint currentCheckpoint;
    public PlayerUI playerUI;
    public bool didWin = false;
    [Tooltip("This is the minimum Y value the player can fall to before they are respawned to the last checkpoint")]
    public float playerFallingBoundsReset = 0;

    public PlayerMovement playerMovement;

    private void Start()
    {
        didWin = false;
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (Time.timeScale == 0)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Respawn();
        }

        if (gameObject.transform.position.y < playerFallingBoundsReset)
        {
            Respawn();
        }

        if (currentCheckpoint != null && !didWin)
        {
            if (currentCheckpoint.level == LevelData.LD.numberOfCheckpoints)
            {
                didWin = true;
                UpdateLevelStats();
                playerUI.ShowWinScreen();
                Time.timeScale = 0;
            }
        }
    }

    private void UpdateLevelStats()
    {
        float completionTime = GameManager.Instance.currentCompletionTime;
        Level levelToUpdate = GameManager.Instance.levelDataContainer.levels[GameManager.Instance.currentLevelBuildIndex - 1];

        levelToUpdate.isCompleted = true;

        if (levelToUpdate.completionTime < completionTime)
        {
            levelToUpdate.completionTime = completionTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Checkpoint checkPointHit = other.gameObject.GetComponent<Checkpoint>();
        if (checkPointHit)
        {
            HitNewCheckPoint(checkPointHit);
        }
    }

    public void HitNewCheckPoint(Checkpoint checkpoint)
    {
        if (currentCheckpoint == null)
        {
            checkpoint.SetCompleted();
            currentCheckpoint = checkpoint;
        }
        else
        {
            if (currentCheckpoint.level <= checkpoint.level)
            {
                checkpoint.SetCompleted();
                currentCheckpoint = checkpoint;
            }
        }
    }

    void Respawn()
    {
        Vector3 respawnPosition = currentCheckpoint.gameObject.transform.position + PlayerConstants.PlayerSpawnOffset;
        transform.position = respawnPosition;
        playerMovement.newVelocity = Vector3.zero;
    }
}
