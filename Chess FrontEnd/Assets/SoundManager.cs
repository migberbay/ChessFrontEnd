using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class SoundManager : MonoBehaviour
{
    public AudioClip[] LedZep;
    public AudioClip[] Muse;
    public AudioClip[] PinkFloyd;
    public AudioClip[] Santana;
    List<AudioClip[]> musicLists;


    public AudioClip[] chessMoves;
    public Transform musicSpot;
    AudioSource audioPlayer;

    GameObject diskPlaying;
    Vector3 originalPositionDisk = new Vector3();
    Coroutine albumPlayingRutine;

    bool disksAreMoving = false;

    Camera active;

    void Start()
    {
        musicLists = new List<AudioClip[]>{LedZep, Muse, PinkFloyd, Santana};
        active = Camera.allCameras[0];
        audioPlayer = gameObject.GetComponent<AudioSource>();
    }

    private void Update() {
        if(Input.GetMouseButtonDown(0)){
            RaycastHit hit;
            Ray ray = active.ScreenPointToRay(Input.mousePosition);
            int layer_mask = LayerMask.GetMask(new string[]{"MusicDisk"});
            Physics.Raycast(ray, out hit, 100f, layer_mask);

            if(hit.point != new Vector3(0,0,0) && !disksAreMoving){
                Debug.Log("No disk should be moving.");
                PlayAlbum(hit.transform.gameObject.GetComponent<MusicRecordScript>());
            }
        }
    }

    public async void PlayAlbum(MusicRecordScript record){
        disksAreMoving = true;
        if(diskPlaying != null){
            if(record.transform.Equals(diskPlaying.transform)){// we click the album that is playing.
                disksAreMoving = false;
                return;
            }
            StartCoroutine(MoveRecordFromTo(diskPlaying.transform, musicSpot.position, originalPositionDisk, 2f));
        }
        originalPositionDisk = record.transform.position;
        diskPlaying = record.transform.gameObject;
        StartCoroutine(MoveRecordFromTo(record.transform, originalPositionDisk, musicSpot.position, 2f));
        audioPlayer.Stop();
        await Task.Delay(2100);
        disksAreMoving = false;

        if(albumPlayingRutine != null){
            StopCoroutine(albumPlayingRutine);
        }
        albumPlayingRutine = StartCoroutine(PlayAlbumLoop(musicLists[record.id]));
    }

    public void PlayChessMoveSound(){
        audioPlayer.PlayOneShot(chessMoves[Random.Range(0,chessMoves.Length)]);
    }

    IEnumerator PlayAlbumLoop(AudioClip[] album){
        for (int i = 0; i < album.Length; i++)
        {
            float songDuration = album[i].length;
            float elapsed = 0;
            audioPlayer.PlayOneShot(album[i]);

            while(elapsed < songDuration){
                if(audioPlayer.isPlaying)
                    elapsed += Time.deltaTime;
                yield return null;
            }
        }
        StartCoroutine(PlayAlbumLoop(album));
    }

    IEnumerator MoveRecordFromTo(Transform record, Vector3 a, Vector3 b, float time){
        float elapsed = 0;
        Debug.Log("Moving record " + record.name + " from: " + a.ToString() + " to: " + b.ToString());
        while(elapsed < time){
            record.position = Vector3.Slerp(a,b, elapsed/time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        disksAreMoving = false;
    }
}
