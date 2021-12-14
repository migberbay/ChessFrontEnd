using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MusicRecordScript : MonoBehaviour
{
    public int id;
    public GameObject informationPanelPrefab;
    [TextArea(15,20)]
    public string musicDiskInfo;
    GameObject infoPanelInstance;
    
    private void OnMouseOver() {
        infoPanelInstance.SetActive(true);
    }

    private void OnMouseExit() {
        infoPanelInstance.SetActive(false);
    }

    private void Start() {
        infoPanelInstance = Instantiate(informationPanelPrefab);
        infoPanelInstance.transform.position = this.gameObject.transform.position + new Vector3(-0.5f,1f,0);
        infoPanelInstance.GetComponentInChildren<TMP_Text>().text = musicDiskInfo;
        infoPanelInstance.SetActive(false);
    }
    
}
