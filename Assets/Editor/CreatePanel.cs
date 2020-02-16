using UnityEditor;
using UnityEngine;
using System.Collections;

public class CreatePanel : MonoBehaviour
{

	[MenuItem ("Create Object/Panel")]
	static void Create ()
	{
		GameObject newGameobject = new GameObject ("CustomPanel");

		MeshRenderer meshRenderer = newGameobject.AddComponent<MeshRenderer> ();
		meshRenderer.material = new Material (Shader.Find ("Diffuse"));
		//meshRenderer.sharedMaterial.mainTexture = 
		//	(Texture)AssetDatabase.LoadAssetAtPath("Assets/test.png", typeof(Texture2D));

		MeshFilter meshFilter = newGameobject.AddComponent<MeshFilter> ();

		meshFilter.mesh = new Mesh ();
		Mesh mesh = meshFilter.sharedMesh;
		mesh.name = "CustomPanel";

		mesh.vertices = new Vector3[]{
			new Vector3 (0f, 1f, 0f),
			new Vector3 (1f, 0f, 0.0f),
			new Vector3 (-1f, 0f, 0.0f),
			//new Vector3 (-0.5f, -0.5f, 0.0f)
		};
		mesh.triangles = new int[]{
			0, 1, 2
		};
		mesh.uv = new Vector2[] {
			new Vector2 (0.5f, 1f),
			new Vector2 (1f, 0),
			new Vector2 (0, 0),
		};
		/*mesh.uv = new Vector2[]{
			new Vector2 (0.0f, 1.0f),
			new Vector2 (1.0f, 1.0f),
			new Vector2 (1.0f, 0.0f),
			new Vector2 (0.0f, 0.0f)
		};*/

		mesh.RecalculateNormals ();	// 法線の再計算
		mesh.RecalculateBounds ();	// バウンディングボリュームの再計算
		mesh.Optimize ();

		AssetDatabase.CreateAsset (mesh, "Assets/" + mesh.name + ".asset");
		AssetDatabase.SaveAssets ();
	}
}