using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;

public class TestAudio : MonoBehaviour
{
    /*
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource audioSourceReconstructed;
    private AudioClip micClip;
    private string micName;
    private float[] audioClipData;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log($"There are {Microphone.devices.Length} mics connected");
        micName = Microphone.devices[0];
    }

    // Update is called once per frame
    void Update()
    {
        if(micName != null){
            if(Input.GetKeyDown(KeyCode.V)){
                if(!Microphone.IsRecording(micName)){
                    micClip = Microphone.Start(micName, true, 5, 1600);
                }else{
                    Microphone.End(micName);

                    //---GENERAZIONE ARRAY DI FLOAT DALLA CLIP AUDIO

                    audioClipData = new float[micClip.samples * micClip.channels];
                    micClip.GetData(audioClipData, 0);

                    Debug.Log("Raw audio data : " + audioClipData.Length + " first value : " + audioClipData[0]);

                    for(int i = 0; i < audioClipData.Length; i++){
                        audioClipData[i] = audioClipData[i] * 0.5f;
                    }

                    //---CREAZIONE DI NUOVA CLIP E RIEMPIEMENTO DI QUEST'ULTIMA TRAMITE ARRAY DI FLOAT
                    audioSourceReconstructed.clip = AudioClip.Create("test", micClip.samples, micClip.channels, micClip.frequency, false);
                    audioSourceReconstructed.clip.SetData(audioClipData, 0);
                }
            }

            if(Input.GetKeyDown(KeyCode.P)){
                if(!Microphone.IsRecording(micName)){
                    audioSource.Play();
                }
            }

            if(Input.GetKeyDown(KeyCode.L)){
                if(audioSourceReconstructed.clip != null){
                    audioSourceReconstructed.Play();
                }
            }

            audioSource.clip = micClip;
        }
    }
    */
    [SerializeField] private AudioSource audioSource;
    [SerializeField] public AudioSource audioSourceReconstructed;
    [SerializeField] private ClientTest clientTest;
    private AudioClip micClip;
    private string micName;
    private float[] audioClipData;

    private int lastSample = 0;
    private bool notRecording = false;
    private bool sending = false;
    public static int FREQUENCY = 22050; // or 44100 . Frequenza di campionamento della clip audio in hz
    [SerializeField] private float sendRate;
    private float currSend;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log($"There are {Microphone.devices.Length} mics connected");
        micName = Microphone.devices[0];
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (micName != null && clientTest.IsClientConnected())
        {
            if (notRecording)
            {
                notRecording = false;
                micClip = Microphone.Start(micName, true, 100, FREQUENCY); // Inzio registrazione microfono
                sending = true;
            }
            else if (sending)
            {
                int pos = Microphone.GetPosition(micName);
                int diff = pos - lastSample;

                if(currSend <= 0){
                    if (diff > 0)
                    {
                        float[] samples = new float[diff * micClip.channels]; //si genera l'array relativo all'audioclip (della grandezza pari a quanto deve essere trasmesso)
                        micClip.GetData(samples, lastSample);
                        
                        audioSource.clip = micClip;

                        clientTest.SendAudioRaw(samples, micClip.channels); //si trasmette l'audio in rete tramite il client locale
                        /*
                        //RECEIVER SIDE
                        audioSourceReconstructed.clip = AudioClip.Create("test", samples.Length, micClip.channels, FREQUENCY, false);
                        audioSourceReconstructed.clip.SetData(samples, 0);
                        if(!audioSourceReconstructed.isPlaying) audioSourceReconstructed.Play();

                        */
                    }
                    lastSample = pos;
                    currSend = sendRate;
                }else{
                    currSend -= Time.deltaTime;
                }

            }
        }
    }
    /*
    private float[] DownSample(float[] rawData, AudioClip clip, int diff){
        float modifier = (float)44100 / (float)FREQUENCY;

        float lenght = clip.length;
        int newSamplesNumber = Mathf.FloorToInt(lenght * FREQUENCY);
        int newBuffLenght = diff * clip.channels;

        float[] newBuffer = new float[newBuffLenght];

        for (int i = 0; i < newBuffLenght; i++){
            newBuffer[i] = rawData[Mathf.FloorToInt((float)i * modifier)];
        }

        return newBuffer;
    }
    */

    private void Update(){
        if(Input.GetKeyDown(KeyCode.M)){
            notRecording = !notRecording;
            sending = false;
        }
    }
}