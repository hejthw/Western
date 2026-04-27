using System;
using FishNet.Object;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class TutorialCanvas : MonoBehaviour
{
    public Button continueButton;
    public GameObject tutorial;
    public PlayerInput playerInput;
    public CinemachineInputAxisController inputAxis;

    [SerializeField] private NetworkObject _networkObject;
    
    public void Awake()
    {
        if (_networkObject != null && !_networkObject.IsOwner)
        {
            tutorial.SetActive(false);
            return;
        }
        
        playerInput.enabled = false;
        foreach (var c in inputAxis.Controllers)
        {
            if (c.Name == "Look X (Pan)")
            {
                c.Enabled = false;

            }
            if (c.Name == "Look Y (Tilt)")
            {
                c.Enabled = false;
            }
        }
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        continueButton.onClick.AddListener(CloseTutorial);
    }

    public void CloseTutorial()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        playerInput.enabled = true;
        foreach (var c in inputAxis.Controllers)
        {
            if (c.Name == "Look X (Pan)")
            {
                c.Enabled = true;

            }
            if (c.Name == "Look Y (Tilt)")
            {
                c.Enabled = true;
            }
        }
        
        tutorial.SetActive(false);
        Debug.Log("Tutorial Closed");
    }
     
}