using UnityEngine;
using System.Runtime.InteropServices;


public class MusicController : MonoBehaviour {
	[DllImport("__Internal")]
	private static extern void Initialize_();

	public GameObject manager;
	private AudioSource extraAudioSource;


	// Use this for initialization
	void Start () {
		extraAudioSource = manager.GetComponent<AudioSource>();
	}
	
	// Update is called once per frame
	void Update () {
		if (extraAudioSource.clip != null) {
			if (!extraAudioSource.isPlaying && extraAudioSource.clip.isReadyToPlay)
				extraAudioSource.Play();
		}
	}
	public void MicDown(){
		manager.SetActive(false);
		extraAudioSource.loop = true;
		extraAudioSource.clip = Microphone.Start(null, true,10, 44100);
		while (Microphone.GetPosition(null) <= 0) {}
		extraAudioSource.Play();
		manager.SetActive(true);
	}
	public void SongDown(){
		extraAudioSource.Stop ();
		manager.SetActive(false);
		//ユニティエディター上では実行できないのでプラットフォームをチェックしています
		if (Application.platform != RuntimePlatform.OSXEditor)
		{
			//「MyPlugin.m」で定義した関数です。
			Initialize_ ();    // ネイティブコード上のメソッドを呼び出す
		}
	}
	public void SetAudioSource(){
		WWW wwwFile;
		wwwFile = new WWW("file:///" + Application.persistentDataPath + "/extraAudio.wav");
		Debug.Log ("あああああああああああああああ" + wwwFile);
		extraAudioSource.clip = wwwFile.GetAudioClip(false, false, AudioType.WAV);
		extraAudioSource.loop = false;
		manager.SetActive(true);
	}
}
