using UnityEngine;
using System.Collections;


[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(LineRenderer))]

public class Visualizer : MonoBehaviour {
	const int SAMPLE_NUM = 512;
	const int DIFF_NUM = 256;
	const int BUFFER_NUM = 64;

	public bool useMic = false;
	public GameObject cube;
	public GameObject particle;
	public Camera[] MainCamera;
	public RollObject rollObject;
	public float tekitouTest = 60;

	private AudioSource audio_;
	private LineRenderer line_;

	private GameObject[] boxs;


	private float[] vol = new float[2];
	private float[] diff;//音量差の集合.
	private float[] hiraokaBPM;//hiraokaBPMの集合.
	private float[] hiraokaRC;//軽快さの集合.
	private float[] hiraokaMC;//安らぎの集合.
	private float[] volumes;//s音量の集合.


	float cameraColorRate = 0; 
	float boxsSizeRate = 0; 
	float[] boxsColorRate; 


	private bool tekitouflg = true;

	// Use this for initialization
	void Start () {
		//boxの処理.
		boxs = rollObject.allChildObject;
		boxsColorRate = new float[boxs.Length];

		diff = new float[DIFF_NUM];
		hiraokaBPM = new float[BUFFER_NUM];
		hiraokaRC = new float[BUFFER_NUM];
		hiraokaMC = new float[BUFFER_NUM];
		volumes = new float[BUFFER_NUM];

		line_ = GetComponent<LineRenderer>();
		line_.SetWidth(0.1f, 0.1f);
		line_.SetVertexCount(SAMPLE_NUM);
		for (int i = 0; i < SAMPLE_NUM; i++) {
			float theta = 2 * Mathf.PI * i / SAMPLE_NUM;
			line_.SetPosition(i,new Vector3(Mathf.Sin(theta)*4, 0, Mathf.Cos(theta)*4));
		}
		//line_.material = new Material(Shader.Find("Particles/Additive"));
		//line_.SetColors(new Color(1, 0, 0, 0), new Color(0, 1, 0, 0));

		audio_ = GetComponent<AudioSource>();
		if (useMic) {
			// Audio Source の Audio Clip をマイク入力に設定
			// 引数は、デバイス名（null ならデフォルト）、ループ、何秒取るか、サンプリング周波数
			audio_.clip = Microphone.Start(null, true,10, 44100);
			while (Microphone.GetPosition(null) <= 0) {}
			audio_.Play();
		}
	}
	
	// Update is called once per frame
	void Update () {
		float volSum = 0;
		float bpmVolSum = 0;
		float lowSum = 0.0001f;//ゼロになるのを防ぐ
		float highSum = 0.0001f;
		float[] audioWaveFloat = new float[SAMPLE_NUM];
		audio_.GetSpectrumData(audioWaveFloat, 0, FFTWindow.Rectangular);//スペクトル解析.
		float[] waveData_ = new float[SAMPLE_NUM];
		AudioListener.GetOutputData(waveData_, 1);//波形の実データ.

		var deltaFreq = AudioSettings.outputSampleRate / SAMPLE_NUM;
		float lowFreqThreshold = 3500;//ローパスフィルターかな?(よくわからない).

		float[] freqLevels = new float[boxs.Length];
		int[] calcfreqLevels = new int[boxs.Length];
		for (int i = 0; i<SAMPLE_NUM; i++){
			var freq = deltaFreq * i;
			int floor = ConvertHertzTo16(freq);
			//if (tekitouflg) {
				//Debug.Log (floor);
			//}
			freqLevels [floor] += audioWaveFloat [i];
			calcfreqLevels [floor] += 1;
			if (200 <= freq && freq <= 500)	lowSum += audioWaveFloat [i];
			else if(3000 <= freq && freq <= 5000) highSum += audioWaveFloat [i];
			if(freq <= lowFreqThreshold)  bpmVolSum += audioWaveFloat[i];
			//volSum += audioWaveFloat[i];
		}
		tekitouflg = false;
		//古いデータを押し出す.
		foreach(float num in waveData_){
			volSum += num * num;
		}
		volSum = Mathf.Sqrt (volSum / SAMPLE_NUM)*255;
		if (volSum < 1.0) volSum = 0.0000001f;
		float volAve = Average(volumes);
		FirstOut(volumes,volSum);

		//テンポの解析
		vol [0] = vol [1];
		vol [1] = bpmVolSum;
		var diffVolume = (vol [1] - vol [0] >= 0) ? vol [1] - vol [0] : -1;//前フレームとの音量の差を取得する。
		FirstOut(diff,diffVolume);

		float[] findPeak = new float[DIFF_NUM];
		float peakMax = -1;
		int peakCun = 0;

		for (int i = 0; i < DIFF_NUM-1; i++) {
			if (diff [i] >= 0.15 && diff [i] >= diff [i+1]) {
				findPeak [i] = diff [i];
				peakCun++;
				peakMax = System.Math.Max(peakMax, findPeak [i]);
			} else {
				findPeak [i] = 0;
			}
		}
		float finalBPM = 0;
		FirstOut (hiraokaBPM,peakCun);
		finalBPM = Average (hiraokaBPM);

		float finalRC = 0;//軽快因子.
		float R = lowSum/highSum;//高音域と低音域の音量比
		if(R == 1)R = 0;
		float RC = 0.02f * finalBPM + 5.0f*R + 0.1f;
		FirstOut(hiraokaRC,RC);
		finalRC = Average (hiraokaRC);


		//安らぎ因子は相関が薄い.
		float myu = Average(volumes);//音量の平均値.
		float sd = StandardDeviation(volumes,myu);//音量の標準偏差.
		float MC = -0.05f * myu - 0.1f * sd + 15f;//安らぎ因子. max:15
		FirstOut(hiraokaMC,MC);
		float finalMC = Average (hiraokaMC);

		//ドーンとなるタイミング.
		if (volSum > volAve * 3 && volSum>5) {
			cameraColorRate = 1;
		} else {
			cameraColorRate = Mathf.Clamp01(cameraColorRate - 0.01f);
		}
		foreach (Camera cam in MainCamera) {
			cam.backgroundColor = Color.Lerp( new Color(0f, 0f, 0f), new Color(0.6f, 0.6f, 0.6f), cameraColorRate ); 
		}

		//boxたちの操作
		if (findPeak [DIFF_NUM - 2] > 0.1 && volSum > 0.1) {
			boxsSizeRate = Mathf.Clamp01(boxsSizeRate - 0.006f*finalBPM - peakMax * 0.001f);
		} else {
			boxsSizeRate = Mathf.Clamp01(boxsSizeRate + 0.0003f*finalBPM + peakMax * 0.001f);
		}
		var t = boxsSizeRate*0.3f;
		rollObject.transform.localScale = new Vector3 (1-t, 1-t, 1-t);
		rollObject.speed = (finalRC * 0.002f + 0.2f)* -1;
		for(int i = 0;i<boxs.Length;i++){
			float tmpVolSum = (volSum > 1) ? 120/volSum : 0;
			//if (freqLevels [i]> 0.002f) {
			//	boxsColorRate[i] = Mathf.Clamp01(boxsColorRate[i] + boxsColorRate[i]);
			//} else {
			//	boxsColorRate[i] = Mathf.Clamp01(boxsColorRate[i] - 0.002f*boxsColorRate[i]);
			//}
			//boxsColorRate [i] = freqLevels [i]*60/volSum;
			float rate = i / (float)boxs.Length;
			float luminous = Mathf.Clamp(freqLevels[i]*tmpVolSum,0.35f,1.0f);
			Color hsvColor = Color.HSVToRGB (rate, 0.3f, 0.3f);
			Color hsvColor2 = Color.HSVToRGB (rate, 0.3f, 1);
			Color boxColor = Color.Lerp( hsvColor, hsvColor2, luminous);
			boxs[i].GetComponent<Renderer> ().material.SetColor("_EmissionColor",boxColor);
		}


		//キューブの動作.

		cube.transform.Rotate(new Vector3(0f,0.005f*finalBPM*finalBPM,0));
		particle.transform.RotateAround(Vector3.zero, Vector3.up,-0.05f*finalBPM+0.1f);
		var hue = finalBPM / 60;
		if (hue > 1) {
			hue = 1;
		}
		hue = Mathf.Abs(hue);
		Color lightColor = Color.HSVToRGB (hue, 0.4f, Mathf.Clamp01(finalMC/15 + 0.2f));//キューブの色
		cube.GetComponent<Renderer> ().material.SetColor("_EmissionColor", lightColor);

		//Debug.Log (myu);
		//for (int i = 0; i < 16; i++) {
		//	Debug.Log (i + " : "+ freqLevels[i]);
		//}
	}


	//配列の最初を押し出す.
	void FirstOut(float[] arry,float last){
		for (int i = 0; i < arry.Length-1; i++) {
			arry [i] = arry [i + 1];
		}
		arry [arry.Length - 1] = last;
	}
	//平均値を求める.アルゴリズムの効率化は後で.
	float Average(float[] arry){
		float ave = 0;
		foreach(float num in arry){
			ave += num;
		}
		ave = ave / arry.Length;
		return ave;
	}
	//標準偏差を求める.
	float StandardDeviation(float[] arry,float ave){
		float sd = 0;
		foreach (float num in arry) {
			sd = Mathf.Pow(num - ave, 2);
		}
		sd = sd / arry.Length;
		sd = Mathf.Sqrt (sd);
		return sd;
	}
	//周波数から音を16段階に分解.
	int ConvertHertzTo16(float hertz){
		hertz += 2750;
		if (hertz <= 2750.0f) return 0;
		int rev = Mathf.FloorToInt(4.0f * Mathf.Log(hertz / 2750.0f) / Mathf.Log(2.0f));
		if (rev >= 16) rev = 15;
		return rev;
	}

}
