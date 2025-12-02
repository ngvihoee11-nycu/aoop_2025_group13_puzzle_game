using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUIManager : Singleton<PlayerUIManager>
{
    public GameObject crosshair;

    public void SetCrosshair(int state)
    {
        if (crosshair != null)
        {
            crosshair.SetActive(state != 0);
        }
    }
}
