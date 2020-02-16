using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;  ////ここを追加////

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(LineRenderer))]

public class SpectrumAnalyzer : MonoBehaviour 
{
	public GameObject cube;
	public GameObject enn;

	private Camera MainCamera;

	private AudioSource audio_;
	private LineRenderer line;
	private double[] vol = new double[2];
	//private Queue<double> diff = new Queue<double>(){};
	//private List<float> diffTime = new List<float>();  //float型のリスト
	//private List<double> diff = new List<double>();  //float型のリスト
	private double[] diff;
	private float[] diffTime;
	private float[] hiraokaBPM;


	private float totleTime = 0;

	private int mainBPMs = 0;

	const int SAMPLE_NUM = 512;
	const int DIFF_NUM = 256;


	void Start(){
		MainCamera = GameObject.Find( "Main Camera" ).GetComponent<Camera>();

		diff = new double[DIFF_NUM];
		diffTime = new float[DIFF_NUM];
		hiraokaBPM = new float[32];
		for (int i = 0; i < hiraokaBPM.Length; i++) {
			hiraokaBPM [i] = 0;
		}
		audio_ = GetComponent<AudioSource>();
		// Audio Source の Audio Clip をマイク入力に設定
		// 引数は、デバイス名（null ならデフォルト）、ループ、何秒取るか、サンプリング周波数
		audio_.clip = Microphone.Start(null, true,10, 44100);
		while (Microphone.GetPosition(null) <= 0) {}
		audio_.Play();

		line = GetComponent<LineRenderer>();
		line.SetWidth(0.065f, 0.065f);
		line.SetVertexCount(SAMPLE_NUM);
		for (int i = 0; i < DIFF_NUM; i++) {
			diff[i] = 0;
			diffTime[i] = 0;
		}
		vol [0] = vol [1] = 0;
	}

	void Update() {
		totleTime += Time.deltaTime;
		double volSum = 0;
		float[] audioWaveFloat = new float[SAMPLE_NUM];
		audio_.GetSpectrumData(audioWaveFloat, 0, FFTWindow.Rectangular);//スペクトル解析
		float[] waveData_ = new float[SAMPLE_NUM];
		audio_.GetOutputData(waveData_, 0);//波形の実データ

		var deltaFreq = AudioSettings.outputSampleRate / SAMPLE_NUM;
		float lowFreqThreshold = 5000;

		List<double> diffPeak = new List<double>();


		for (int i = 0; i<SAMPLE_NUM; i++)
		{
			line.SetPosition(i, new Vector3(Mathf.Log(i + 1)*2 -7, audioWaveFloat[i]*5, 0));
			//line.SetPosition(i, new Vector3(Mathf.Log(i + 1) -7, waveData_[i]*5, 0));
			//max = System.Math.Max(max, audioWaveFloat[i]);
			//max += waveData_[i];
			var freq = deltaFreq * i;
			if(freq <= lowFreqThreshold)  volSum += audioWaveFloat[i];
			//if(120<=freq&&freq <= 600)  volSum += audioWaveFloat[i];
			//volSum += audioWaveFloat[i];
		}

		//テンポの解析
		vol [0] = vol [1];
		vol [1] = volSum;
		//vol[1] = waveData_.Select(x => x*x).Sum() / waveData_.Length;//音量の取得
		var diffVolume = (vol [1] - vol [0] >= 0) ? vol [1] - vol [0] : -1;//前フレームとの音量の差を取得する。
		for(int i = 0;i<DIFF_NUM - 1;i++){
			diff [i] = diff [i + 1];
			diffTime [i] = diffTime [i + 1];
		}
		diff [DIFF_NUM - 1] = diffVolume;
		diffTime [DIFF_NUM - 1] = totleTime;
		double[] findPeak = new double[DIFF_NUM];
		int k = 0;
		var sum = 0.0;
		double max = -1;
		int maxCun = 0;
		foreach (double num in diff) {
			var aaa = (num == -1) ? 0 : 1;
			sum += num * aaa;
			//line.SetPosition(k, new Vector3(k*(float)0.025-7, (float)num*aaa, 0));
			k++;
		}
		sum = (sum / k);
		/*if (diffVolume > sum + 0.3) {
			float t = Mathf.PingPong( Time.time, 0.05f ) / 0.05f; 
			MainCamera.backgroundColor = Color.Lerp( new Color(0.3f, 0.3f, 0.3f), new Color(0f, 0f, 0f), t ); 
		}*/
		for (int i = 0; i < DIFF_NUM-1; i++) {
			if (diff [i] >= 0.1 && diff [i] >= diff [i+1]) {
				findPeak [i] = diff [i];
				maxCun++;
				max = System.Math.Max(max, findPeak [i]);
			} else {
				findPeak [i] = 0;
			}
		}

		float finalBPM = 0;
		for (int i = 0; i < hiraokaBPM.Length-1; i++) {
			hiraokaBPM [i] = hiraokaBPM [i + 1];
			finalBPM += hiraokaBPM[i];
		}
		hiraokaBPM [hiraokaBPM.Length - 1] = maxCun;
		finalBPM += hiraokaBPM[hiraokaBPM.Length - 1];
		finalBPM = finalBPM / hiraokaBPM.Length;

		cube.transform.Rotate(new Vector3(0f,0.005f*finalBPM*finalBPM,0f));
		if (diffVolume > sum) {
			float t = Mathf.PingPong( Time.time, 0.05f ) / 0.05f; 
			//enn.GetComponent<Renderer>().material.color = Color.Lerp( new Color(1.0f, 0.3f, 0.3f), new Color(1f, 1f, 1f), t );
			//enn.transform.localScale = new Vector3 (Mathf.Sqrt((float)diffVolume)*0.5f + 1f, Mathf.Sqrt((float)diffVolume)*0.5f + 1f, 1);
			enn.transform.localScale = new Vector3 (Mathf.Sqrt((float)diffVolume)+ 0.5f, Mathf.Sqrt((float)diffVolume)+ 0.5f, 1);
			//cube.transform.localScale = new Vector3 ((float)diffVolume + 1, (float)diffVolume + 1, (float)diffVolume + 1);
		} else {
			//cube.transform.localScale = new Vector3 (1, 1, 1);
		}


		Debug.Log ("平岡BPM : " + finalBPM);
	}

	public void ClickButton(){
		foreach (double num in diff) {
			Debug.Log(num);
		}
	}
}
