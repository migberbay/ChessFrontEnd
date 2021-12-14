using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class RegulateMasterVolume : MonoBehaviour
{
    public AudioMixer mainMixer;
    int maxDecibels = 0, minDecibels = -80;
    Camera active;
    Canvas volumeScrollerCanvas;

    private void Start() {
        active = Camera.allCameras[0];
        volumeScrollerCanvas = GetComponentInParent<Canvas>();
        volumeScrollerCanvas.worldCamera = active;
    }

    public void UpdateMasterDecibels(float value){
        mainMixer.SetFloat("MasterVol", Mathf.Log10(value) * 20);
    }
}
