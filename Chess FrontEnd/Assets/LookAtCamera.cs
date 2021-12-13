using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    
    public GameObject cameraToLook;

    public void InitPanel(string color)
    {
        var cameraForward = cameraToLook.gameObject.transform.rotation.eulerAngles.x;
        gameObject.GetComponent<Canvas>().worldCamera = cameraToLook.GetComponent<Camera>();

        if(color == "w"){
            this.gameObject.transform.Rotate(new Vector3(cameraForward,0,0), Space.Self);
        }else{
            this.gameObject.transform.Rotate(new Vector3(cameraForward,180,0), Space.Self);
        }
    }
}
