 /*
 * Computer Animation and Gaming Assignment 4
 * Author : Dhananjay Singh
 * Utd Id : 2021250625
 * Net Id : dxs145530
 */ 
using System.IO;
using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
public class FileTokenizer : MonoBehaviour 
{
	Keyframe[] kFrame;
	AnimationCurve anim;
	float xPos,yPos,zPos;

	public float speed=100.0f;
	public float turnSpeed = 30.0f;
	List<string> jointID;
	public int selection;
	List<int> triIndex = new List<int> (); //List of triangles that form the mesh//
	public float scaleFactor;
	Joint j1=new Joint();

	public List<Vector3> positions=new List<Vector3>{}; //Vertices of the Mesh//S
	List<Double[]> skinweights=new List<Double[]>{}; //Binding Weights//
	List<Vector3> normals=new List<Vector3>{}; //Normals for each triangle//
	List<Matrix4x4> bindings=new List<Matrix4x4>{}; //Binding matrices//
	List<Joint> c=new List<Joint>(1){}; //List of Joints to form bones//
	List<Transform> bones=new List<Transform>{};

	//Anim files related data structures//
	List<string> extrapolateMethod=new List<string>{};
	List<int> numkeysPerChannel=new List<int>{};
	List<float[]> keyValues=new List<float[]>{};
	List<string> tangentInOut=new List<string>{};

	int bonecount=0;
	int poscount=0;
	int triangleCount=0;
	Stack ob = new Stack ();
	int endRange=0;

	public Transform[] bone; //In Scene Bones as GameObject Transforms//
	int i=0;
	int n=0; 
	string str=null;
	string[] s2;
	string[] s1;
	StreamReader skelReader=null; //Reader for skel file
	StreamReader skinReader=null; //Reader for skin file
	StreamReader animReader=null; //Reader for anim file

	SkinnedMeshRenderer rend;
	String file1="/Resources/Wasp/wasp_walk.skel";
	String file2="/Resources/Wasp/wasp_walk.skin";
	String file3="/Resources/Wasp/wasp_walk.anim";
	public int count=0;
	void Start () 
	{
		FileStream f1 = new FileStream (Application.dataPath + file1, FileMode.Open); 
		skelReader = new StreamReader (f1);
		scaleFactor = 1f;
		f1 = new FileStream (Application.dataPath + file2, FileMode.Open);
		skinReader = new StreamReader (f1);
		f1 = new FileStream (Application.dataPath + file3, FileMode.Open);
		animReader = new StreamReader (f1);
		jointID = new List<string> ();
		//Read the skeleton File//
		readSkel ();
		//Read the anim file//
		readAnim();
		//Read the Skin File//
		readSkin ();
		//Render the skinned Mesh//
		renderSkin ();
	}
	/*
	 * Reading the Anim File.
	 */
	public void readAnim()
	{
		while ((str = animReader.ReadLine ()) != null)
		{
			if (str.Contains ("animation")) 
			{
			}
			else if (str.Contains ("range")) 
			{
				str = Regex.Replace (str, @"\s+", " ");
				s1 = str.Split (' ');
				endRange = (int)double.Parse (s1 [3]);
			}
			else if (str.Contains ("numchannels")) 
			{
				str = Regex.Replace (str, @"\s+", " ");
				s1 = str.Split (' ');
				int numchannels = int.Parse (s1 [2]);
				Debug.Log (numchannels);
			} 
			else if (str.Contains ("channel")) 
			{
				str = Regex.Replace (str, @"\s+", " ");
				s1 = str.Split (' ');
				str = animReader.ReadLine ();
				str = Regex.Replace (str, @"\s+", " ");
				if (str.Contains ("extrapolate")) 
				{
					s1 = str.Split (' ');
					extrapolateMethod.Add (s1 [2]);
					extrapolateMethod.Add (s1 [3]);
				}
			}
			else if (str.Contains ("keys")) 
			{
				str = Regex.Replace(str, @"\s+", " ");
				s1 = str.Split (' ');
				int numkeys = int.Parse(s1 [2]);
				numkeysPerChannel.Add (numkeys);
				for (i = 0; i < numkeys; i++) 
				{
					str = animReader.ReadLine ();
					str = Regex.Replace(str, @"\s+", " ");
					s1 = str.Split (' ');
					float[] a = new float[2];
					a [0] = float.Parse (s1 [1]);
					a [1] = float.Parse (s1 [2]);
					keyValues.Add (a);
					tangentInOut.Add (s1 [3]);  
					tangentInOut.Add (s1 [4]);
				}
			}
			else 
			{
			}
		}
	}
	//Now we try to animate the joints in their local space//
	void FixedUpdate () 
	{
		reCal ();
		//X position for root//
		kFrame = new Keyframe[1];
		kFrame [0] = new Keyframe ((keyValues [0]) [0], (keyValues [0]) [1]);
		anim = new AnimationCurve (kFrame);
		xPos = anim.Evaluate (Time.time);
		anim.postWrapMode = WrapMode.Loop;

		//Y position for root//
		kFrame=new Keyframe[1];
		kFrame [0] = new Keyframe ((keyValues [1]) [0], (keyValues [1]) [1]);
		kFrame [0].tangentMode = 1;
		anim = new AnimationCurve (kFrame);
		yPos = anim.Evaluate (Time.time);
		anim.postWrapMode = WrapMode.Loop;

		//Z position for root//
		kFrame=new Keyframe[2];
		kFrame[0]=new Keyframe((keyValues[2])[0],(keyValues[2])[1]);
		kFrame[1]=new Keyframe((keyValues[3])[0],(keyValues[3])[1]);
		kFrame[0].tangentMode=1;
		kFrame[1].tangentMode=1;
		anim=new AnimationCurve(kFrame);
		zPos=anim.Evaluate(Time.time);
		anim.postWrapMode=WrapMode.Loop;

		//We divide by constant 35 to make the translation look smoother and more relative to the animation//
		if(zPos>0)
		transform.Translate (0, 0, zPos / 35);
	}
	/*
	 * Reading the Skeleton files.
	 * Similar to Assignment 2.
	 * Regular Expressions used to handle multiple white spaces as in the given files.
    */
	public void readSkel()
	{
		while((str=skelReader.ReadLine())!=null)
		{
			str = Regex.Replace(str, @"\s+", " ");
			if (str.Contains ("{"))
			{
				if (count == 0) 
				{
					i++;
					j1 = new Joint ();
					j1.child = new List<Joint>{ };
					count++;
					s1 = str.Split (' ');
					j1.name = s1 [1];
					jointID.Add (j1.name);
				} 
				else 
				{
					if (i == 1) 
					{
						c.Add (j1);
						i++;
					}
					ob.Push (j1);                        
					j1 = new Joint ();
					j1.child = new List<Joint>{ };
					count++;
					s1 = str.Split (' ');
					j1.name = s1 [2];
					jointID.Add (j1.name);
				}
			}
			else if (str.Contains ("offset")) 
			{
				s1 = str.Split (' ');
				j1.offset = new float[3];
				j1.offset [0] = float.Parse (s1 [2]);
				j1.offset [1] = float.Parse (s1 [3]);
				j1.offset [2] = float.Parse (s1 [4]);
			} 
			else if (str.Contains ("Jointmin")) 
			{
				s1 = str.Split (' ');
				j1.Jointmin = new float[3];
				j1.Jointmin [0] = float.Parse (s1 [2]);
				j1.Jointmin [1] = float.Parse (s1 [3]);
				j1.Jointmin [2] = float.Parse (s1 [4]);
			} 
			else if (str.Contains ("Jointmax")) 
			{
				s1 = str.Split (' ');
				j1.Jointmax = new float[3];
				j1.Jointmax [0] = float.Parse (s1 [2]);
				j1.Jointmax [1] = float.Parse (s1 [3]);
				j1.Jointmax [2] = float.Parse (s1 [4]);
			} 
			else if (str.Contains ("pose")) 
			{
				s1 = str.Split (' ');
				j1.pose = new float[3];
				j1.pose [0] = float.Parse (s1 [2]);
				j1.pose [1] = float.Parse (s1 [3]);
				j1.pose[2]=float.Parse(s1[4]);
			} 
			else if (str.Contains ("}")) 
			{
				count--;
				Joint j2=new Joint();
				if(count!=0)
					j2 = (Joint)ob.Pop();
				if (count != 0)
					j2.child.Add (j1);
				j1 = j2;
			}                
			else 
			{
			}
		}
		rendering ();
	}

	/*
	 * Rendering the joints.
	 * Similar to Assignment 2.
	*/
	void rendering()
	{
		foreach (Joint k in c) 
		{    
			GameObject go2 = new GameObject ();
			go2.name = k.name;
			go2.transform.parent = gameObject.transform;
			go2.transform.localPosition= new Vector3(k.offset [0], k.offset [1], k.offset [2]);
			float x = (k.pose [0] * 180f) / 3.14f;
			float y = (k.pose [1] * 180f) / 3.14f;
			float z = (k.pose [2] * 180f) / 3.14f;
			go2.transform.localRotation = Quaternion.AngleAxis (z, Vector3.forward) * Quaternion.AngleAxis (y, Vector3.up) * Quaternion.AngleAxis (x, Vector3.right);
			bonecount++;
			bones.Add (go2.transform);
			childrender (k); 
		}
	}
	public void childrender(Joint m)
	{
		foreach (Joint d in m.child) 
		{
			GameObject go2 = new GameObject();
			go2.name = d.name;
			GameObject  parent=(GameObject.Find(m.name));
			go2.transform.parent=parent.transform;    
			go2.transform.localPosition= new Vector3(d.offset [0], d.offset [1], d.offset [2]);
			float x = (d.pose [0] * 180f) / 3.14f;
			float y = (d.pose [1] * 180f) / 3.14f;
			float z = (d.pose [2] * 180f) / 3.14f;
			go2.transform.localRotation = Quaternion.AngleAxis (z, Vector3.forward) * Quaternion.AngleAxis (y, Vector3.up) * Quaternion.AngleAxis (x, Vector3.right);
			bonecount++;
			bones.Add (go2.transform);
			childrender (d);
		}
	}

	//Rendering the skinned mesh .//
	//Adopted from https://docs.unity3d.com/ScriptReference/Mesh-bindposes.html .//
	public void renderSkin()
	{
		gameObject.AddComponent<SkinnedMeshRenderer>();
		rend = GetComponent<SkinnedMeshRenderer>();
		Mesh mesh = new Mesh();
		rend.material =new Material (Shader.Find("Diffuse"));
		mesh.vertices = positions.ToArray();
		mesh.normals = normals.ToArray ();
		mesh.triangles = triIndex.ToArray ();
		mesh.RecalculateNormals ();
		rend.material.color=Color.white;
		BoneWeight[] weights = new BoneWeight[poscount];
		int j;
		for (i = 0; i < poscount; i++) 
		{
			j = (int)(skinweights [i]) [0];
			if (j == 1) 
			{
				weights [i].boneIndex0 = (int)((skinweights [i]) [1]);
				weights [i].weight0 = (float)(skinweights [i]) [2];
			}
			else if (j == 2) 
			{
				weights [i].boneIndex0 = (int)((skinweights [i]) [1]);
				weights [i].weight0 = (float)(skinweights [i]) [2];
				weights [i].boneIndex1 = (int)((skinweights [i]) [3]);
				weights [i].weight1 = (float)(skinweights [i]) [4];
			}
			else if (j == 3) 
			{
				weights [i].boneIndex0 = (int)((skinweights [i]) [1]);
				weights [i].weight0 = (float)(skinweights [i]) [2];
				weights [i].boneIndex1 = (int)((skinweights [i]) [3]);
				weights [i].weight1 = (float)(skinweights [i]) [4];
				weights [i].boneIndex2 = (int)((skinweights [i]) [5]);
				weights [i].weight2 = (float)(skinweights [i]) [6];
			}
			else 
			{
				weights [i].boneIndex0 = (int)((skinweights [i]) [1]);
				weights [i].weight0 = (float)(skinweights [i]) [2];
				weights [i].boneIndex1 = (int)((skinweights [i]) [3]);
				weights [i].weight1 = (float)(skinweights [i]) [4];
				weights [i].boneIndex2 = (int)((skinweights [i]) [5]);
				weights [i].weight2 = (float)(skinweights [i]) [6];
				weights [i].boneIndex3 = (int)((skinweights [i]) [7]);
				weights [i].weight3 = (float)(skinweights [i]) [8];
			}
		}
		mesh.boneWeights = weights;
		bone = new Transform[bonecount];
		Matrix4x4[] bindpose=new Matrix4x4[bonecount];
		foreach (Transform tr in bones) 
		{
		}
		for (i = 0; i < bonecount; i++) 
		{
			bone [i] = bones [i];
		}
		for(i=0;i<bonecount;i++)
		{
			bindpose [i] = bindings [i];
			bindpose [i] = bindpose [i].inverse;
		}
		mesh.RecalculateNormals ();
		mesh.RecalculateBounds ();
		mesh.Optimize ();
		rend.bones = bone;
		rend.sharedMesh = mesh;
		rend.updateWhenOffscreen = true;
		mesh.bindposes = bindpose;
	}

	/*
	 *Reading the skin file entries.
	 *Regular Expressions used to replace multiple white spaces with a single white space.
	*/
	public void readSkin()
	{
		while((str=skinReader.ReadLine())!=null)
		{
			str = Regex.Replace(str, @"\s+", " ");
			if (str.Contains ("positions")) 
			{
				int i;
				s1 = str.Split (' ');
				n = int.Parse (s1 [1]);
				poscount = n;
				float[] a = new float[3];
				for (i = 0; i <n; i++) 
				{
					str = skinReader.ReadLine ();
					str = Regex.Replace(str, @"\s+", " ");
					s1 = str.Split (' ');
					a [0] = float.Parse (s1 [1]);
					a [1] = float.Parse (s1 [2]);
					a [2] = float.Parse (s1 [3]);
					positions.Add (new Vector3(a[0],a[1],a[2]));
				}
			}
			else if (str.Contains ("normals")) 
			{
				s1 = str.Split (' ');
				float[] a = new float[3];
				n = int.Parse (s1 [1]);
				for (i = 0; i < n; i++) 
				{
					str = skinReader.ReadLine ();
					str = Regex.Replace(str, @"\s+", " ");
					s1 = str.Split (' ');
					a [0] = float.Parse (s1 [1]);
					a [1] = float.Parse (s1 [2]);
					a [2] = float.Parse (s1 [3]);
					normals.Add (new Vector3(a[0],a[1],a[2]));
				}
			} 
			else if (str.Contains ("skinweights"))
			{
				s1 = str.Split (' ');
				int j;
				int n1;
				n = int.Parse (s1 [1]);
				for (i = 0; i < n; i++) 
				{
					str = skinReader.ReadLine ();
					str = Regex.Replace(str, @"\s+", " ");
					s1 = str.Split (' ');
					Double[] a = new Double[(int.Parse(s1 [1]) * 2)+1];
					n1 = int.Parse (s1 [1]) * 2;
					a [0] = double.Parse(s1 [1]);
					for (j = 1; j <= n1; j++) 
					{
						a [j] = Double.Parse (s1 [j + 1]);
					}
					skinweights.Add (a);
				}
			} 
			else if (str.Contains ("triangles")) 
			{
				s1 = str.Split (' ');
				n = int.Parse (s1 [1]);
				triangleCount = n;
				for (i = 0; i <n; i++) 
				{
					str = skinReader.ReadLine ();
					str = Regex.Replace(str, @"\s+", " ");
					s1 = str.Split (' ');
					int x = int.Parse (s1 [1]);
					int y= int.Parse (s1 [2]);
					int z = int.Parse (s1 [3]);
					triIndex.Add(x);
					triIndex.Add(y);
					triIndex.Add(z);
				}
			}
			else if (str.Contains ("bindings")) 
			{
				s1 = str.Split (' ');
				float[] a = new float[3];
				n = int.Parse (s1 [1]);
				int j;
				str=skinReader.ReadLine ();
				for (j = 0; j < n; j++) 
				{
					Matrix4x4 b=new Matrix4x4();
					str = skinReader.ReadLine ();
					str = Regex.Replace (str, @"\s+", " ");
					s1 = str.Split (' ');
					for (i = 0; i < 4; i++) 
					{
						if (i != 0) 
						{
							str = skinReader.ReadLine ();
							str = Regex.Replace (str, @"\s+", " ");
							s1 = str.Split (' ');
						}
						a [0] = (float)Double.Parse (s1 [1]);
						a [1] = (float)Double.Parse (s1 [2]);
						a [2] = (float)Double.Parse (s1 [3]);
						if (i == 3) 
						{
							str = skinReader.ReadLine ();
							str = skinReader.ReadLine ();
						}    
						b.SetColumn (i, new Vector3 (a [0], a [1], a [2]));
					}
					b.SetRow (3, new Vector4 (0, 0, 0, 1));
					bindings.Add (b);
				}
			}                
			else 
			{
			}
		}
	}
	void reCal()
	{
		//NOTE : We have already used the first three channels for the root translation. We need the fourth one onwards for the rest of the joints//
		int tI = 4;

		int tJ = 3;
		for (i = 0; i < 23; i++)
		{
			float[] rot = new float[3];
			for (int k = 0; k < 3; k++) 
			{
				int temp = numkeysPerChannel [tJ];
				tJ++;
				kFrame=new Keyframe[temp];
				for (int j = 0; j < temp; j++) 
				{
					kFrame [j] = new Keyframe ((keyValues [tI]) [0], (keyValues [tI]) [1]);
					if (tangentInOut [tI] == "smooth") 
					{
						kFrame [j].tangentMode = 1;
					}
					else 
					{
						//For constannt : tangent mode =2//
						kFrame [j].tangentMode = 2;
					}
					tI++;
				}
				anim = new AnimationCurve (kFrame);
				anim.postWrapMode = WrapMode.Loop;
				rot [k] = anim.Evaluate (Time.time);
			}
			//Convert from Radian to degrees//
			float x = (rot [0] * 180f) / 3.14f;
			float y = (rot [1] * 180f) / 3.14f;
			float z = (rot [2] * 180f) / 3.14f;
			//Apply rotations as per keyframe values in depth first order of joints//
			GameObject go2 = GameObject.Find (bones [i].name);
			go2.transform.localRotation = Quaternion.AngleAxis (z, Vector3.forward) * Quaternion.AngleAxis (y, Vector3.up) * Quaternion.AngleAxis (x, Vector3.right);
		}
	}
}
