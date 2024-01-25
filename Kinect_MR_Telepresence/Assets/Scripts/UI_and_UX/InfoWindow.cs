using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InfoWindow : MonoBehaviour
{
    public static InfoWindow Instance { get; private set; }

    private void Awake() {
        Instance = this;
    }

    [SerializeField] private GameObject infoWindow;
    [SerializeField] private float popUpWindowTime;
    private bool alreadySpawned;

    public void SpawnWindow(string textToVisualize, float popUpTime = 0f){
        if(!alreadySpawned){
            GameObject window = Instantiate(infoWindow, Vector3.zero, Quaternion.identity);
            window.transform.Find("Dialog").Find("DescriptionText").GetComponent<TextMeshPro>().text = textToVisualize;
            alreadySpawned = true;

            if(popUpTime == 0) {
                popUpTime = popUpWindowTime;
            }

            StartCoroutine(DestroyWindow(window, popUpTime, ()=>{
                alreadySpawned = false;
            }));
        }
    }

    private IEnumerator DestroyWindow(GameObject window, float time, Action windowDestroyed){
        yield return new WaitForSeconds(time);
        Destroy(window);
        windowDestroyed();
    }
}
