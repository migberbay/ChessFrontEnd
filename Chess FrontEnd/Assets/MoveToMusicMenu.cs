using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveToMusicMenu : MonoBehaviour
{
    Camera active;
    public Transform musicTransform;
    Vector3 oldPosition, oldRotation;
    public float transitionTime = 2f;
    bool cameraMoving = false;

    int black_y = 270, white_y = -90, x_rot=35;

    private void Start() {
        active = Camera.allCameras[0];
        oldPosition = active.transform.position;
        oldRotation = active.transform.rotation.eulerAngles;
    }

    public void MoveActiveCameraToMusicMenu(){ 
        StartCoroutine(MoveCam(true));
    }

    public void MoveActiveCameraToGame(){
        StartCoroutine(MoveCam(false));
    }

    IEnumerator MoveCam(bool toMenu){
        Vector3 destination;
        Vector3 destinationRotation;

        if(toMenu){
            destination = musicTransform.position;

            if(GameObject.FindObjectOfType<ChessManager>().playerColor() == "w"){
                destinationRotation = new Vector3(35, -90, 0);
            }else{
                destinationRotation = new Vector3(35, 270, 0);
            }
        }else{
            destination = oldPosition;

            if(GameObject.FindObjectOfType<ChessManager>().playerColor() == "w"){
                destinationRotation = new Vector3(65, 0, 0);
            }else{
                destinationRotation = new Vector3(65, 180, 0);
            }
        }

        Vector3 originPos = active.transform.position;
        Vector3 originRot = active.transform.rotation.eulerAngles; 

        float elapsed = 0;
        cameraMoving = true;

        while(elapsed < transitionTime){
            active.transform.position = Vector3.Lerp(originPos, destination, elapsed/transitionTime);
            active.transform.rotation = Quaternion.Euler(Vector3.Lerp(originRot, destinationRotation, elapsed/transitionTime));
            elapsed += Time.deltaTime;
            yield return null;
        }
        cameraMoving = false;
    }
}
