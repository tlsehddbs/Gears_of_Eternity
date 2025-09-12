using System;
using NUnit.Framework.Constraints;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool isDrawingCards = false;
    public bool isDraggingCard = false;
    public bool isHoveringCard = false;
    
    public bool isInteractable = true;
    

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        isInteractable = true;
    }
}
