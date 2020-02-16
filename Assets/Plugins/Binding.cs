using UnityEngine;
using System.Runtime.InteropServices;

public class Binding : MonoBehaviour
{
	[DllImport("__Internal")]
	private static extern void Initialize_();

	public static void Initialize()
	{
		//ユニティエディター上では実行できないのでプラットフォームをチェックしています
		if (Application.platform != RuntimePlatform.OSXEditor)
		{
			//「MyPlugin.m」で定義した関数です。
			Initialize_();
		}
	}

}