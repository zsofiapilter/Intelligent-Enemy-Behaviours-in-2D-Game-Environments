using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class userInput : MonoBehaviour
{
    public static userInput instance;
    [HideInInspector] public InputActions control;

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        control = new InputActions();
    }

    private void OnEnable()
    {
        control.Enable();
    }
    private void OnDisable()
    {
        control.Disable();
    }
}

