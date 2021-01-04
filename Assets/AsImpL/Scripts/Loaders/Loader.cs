using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Asynchronous importer and loader
/// </summary>
namespace AsImpL
{

	/// <summary>
	/// Abstract loader to be used as a base class for specific loaders.
	/// </summary>
	public abstract class Loader : MonoBehaviour
	{
		/// <summary>
		/// Total loading progress, for all the models currently loading.
		/// </summary>
		public static LoadingProgress totalProgress = new LoadingProgress();

		/// <summary>
		/// Options to define how the model will be loaded and imported.
		/// </summary>
		public ImportOptions buildOptions;

#if UNITY_EDITOR
		/// <summary>
		/// Alternative texture path: if not null textures will be loaded from here.
		/// </summary>
		public string altTexPath = null;
#endif

		// raw subdivision in percentages of the loading phases (empirically computed loading a large sample OBJ file)
		// TODO: refine or change this method
		protected static float LOAD_PHASE_PERC = 8f;
		protected static float TEXTURE_PHASE_PERC = 1f;
		protected static float MATERIAL_PHASE_PERC = 1f;
		protected static float BUILD_PHASE_PERC = 90f;

		protected static Dictionary<string, GameObject> loadedModels = new Dictionary<string, GameObject>();
		protected static Dictionary<string, int> instanceCount = new Dictionary<string, int>();

		protected DataSet dataSet = new DataSet();
		protected ObjectBuilder objectBuilder = new ObjectBuilder();

		protected List<MaterialData> materialData;

		protected SingleLoadingProgress objLoadingProgress = new SingleLoadingProgress();

		protected Stats loadStats;

		private Texture2D loadedTexture = null;

		/// <summary>
		/// Load the file assuming its vertical axis is Z instead of Y 
		/// </summary>
		public bool ConvertVertAxis
		{
			get
			{
				return buildOptions != null ? buildOptions.zUp : false;
			}
			set
			{
				if (buildOptions == null)
				{
					buildOptions = new ImportOptions();
				}
				buildOptions.zUp = value;
			}
		}


		/// <summary>
		/// Rescaling for the model (1 = no rescaling)
		/// </summary>
		public float Scaling
		{
			get
			{
				return buildOptions != null ? buildOptions.modelScaling : 1f;
			}
			set
			{
				if (buildOptions == null)
				{
					buildOptions = new ImportOptions();
				}
				buildOptions.modelScaling = value;
			}
		}


		/// <summary>
		/// Check if a material library is defined for this model
		/// </summary>
		protected abstract bool HasMaterialLibrary { get; }

#if UNITY_EDITOR
		/// <summary>
		/// Import data as assets in the project (Editor only)
		/// </summary>
		public bool ImportingAssets { get { return !String.IsNullOrEmpty(altTexPath); } }
#endif

		/// <summary>
		/// Event triggered when an object is created.
		/// </summary>
		public event Action<GameObject, string> ModelCreated;

		/// <summary>
		/// Event triggered when an object is successfully loaded.
		/// </summary>
		public event Action<GameObject, string> ModelLoaded;

		/// <summary>
		/// Event triggered if failed to load an object
		/// </summary>
		public event Action<string> ModelError;


		/// <summary>
		/// Get a previusly loaded model by its absolute path
		/// </summary>
		/// <param name="absolutePath">absolute path used to load the model</param>
		/// <returns>The game object previously loaded</returns>
		public static GameObject GetModelByPath(string absolutePath)
		{
			if (loadedModels.ContainsKey(absolutePath))
			{
				return loadedModels[absolutePath];
			}
			return null;
		}


		/// <summary>
		/// Load a model.
		/// </summary>
		/// <param name="objName">name of the GameObject, if empty use file name</param>
		/// <param name="absolutePath">absolute file path</param>
		/// <param name="parentObj">Transform to which attach the loaded object (null=scene)</param>
		/// <returns>You can use StartCoroutine( loader.Load(...) )</returns>
		public IEnumerator Load(string objName, string absolutePath, Transform parentObj)
		{
			string fileName = Path.GetFileName(absolutePath);
			string fileNameNoExt = Path.GetFileNameWithoutExtension(absolutePath);
			string name = objName;
			if (name == null || name == "") objName = fileNameNoExt;

			totalProgress.singleProgress.Add(objLoadingProgress);
			objLoadingProgress.fileName = fileName;
			objLoadingProgress.error = false;
			objLoadingProgress.message = "Loading " + fileName + "...";
			Debug.LogFormat("Loading {0}\n  from: {1}...", objName, absolutePath);

			// if the model was already loaded duplicate the existing object
			if (buildOptions != null && buildOptions.reuseLoaded && loadedModels.ContainsKey(absolutePath) && loadedModels[absolutePath] != null)
			{
				Debug.LogFormat("File {0} already loaded, creating instance.", absolutePath);
				instanceCount[absolutePath]++;
				if (name == null || name == "") objName = objName + "_" + instanceCount[absolutePath];
				objLoadingProgress.message = "Instantiating " + objName + "...";
				while (loadedModels[absolutePath] == null)
				{
					yield return null;
				}

				GameObject newObj = Instantiate(loadedModels[absolutePath]);
				yield return newObj;
				OnCreated(newObj, absolutePath);
				newObj.name = objName;

				if (parentObj != null) newObj.transform.parent = parentObj.transform;
				totalProgress.singleProgress.Remove(objLoadingProgress);
				OnLoaded(newObj, absolutePath);
				yield break;
			}
			loadedModels[absolutePath] = null; // define a key for the dictionary
			instanceCount[absolutePath] = 0; // define a key for the dictionary

			float lastTime = Time.realtimeSinceStartup;
			float startTime = lastTime;
			//TODO(Simon): Multithread
			var modelThread = new Thread(() => LoadModelFile(absolutePath));
			modelThread.Start();

			//LoadModelFile(absolutePath);

			if (objLoadingProgress.error)
			{
				OnLoadFailed(absolutePath);
				yield break;
			}


			while (modelThread.ThreadState == ThreadState.Running)
			{
				yield return null;
			}
			loadStats.modelParseTime = Time.realtimeSinceStartup - lastTime;
			lastTime = Time.realtimeSinceStartup;

			Thread materialThread = null;
			if (HasMaterialLibrary)
			{
				//TODO(Simon): Multithread
				materialThread = new Thread(() => LoadMaterialLibrary(absolutePath));
				materialThread.Start();
				//yield return LoadMaterialLibrary(absolutePath);
			}

			while ((materialThread != null && materialThread.ThreadState == ThreadState.Running) || materialThread == null)
			{
				yield return null;
			}

			loadStats.materialsParseTime = Time.realtimeSinceStartup - lastTime;
			lastTime = Time.realtimeSinceStartup;

			//TODO(Simon): Multithread
			yield return Build(absolutePath, objName, parentObj);
			loadStats.buildTime = Time.realtimeSinceStartup - lastTime;
			loadStats.totalTime = Time.realtimeSinceStartup - startTime;
			Debug.Log("Done: " + objName
				+ "\n  Loaded in " + loadStats.totalTime + " seconds"
				+ "\n  Model data parsed in " + loadStats.modelParseTime + " seconds"
				+ "\n  Material data parsed in " + loadStats.materialsParseTime + " seconds"
				+ "\n  Game objects built in " + loadStats.buildTime + " seconds"
				+ "\n    textures: " + loadStats.buildStats.texturesTime + " seconds"
				+ "\n    materials: " + loadStats.buildStats.materialsTime + " seconds"
				+ "\n    objects: " + loadStats.buildStats.objectsTime + " seconds"
				);
			totalProgress.singleProgress.Remove(objLoadingProgress);

			OnLoaded(loadedModels[absolutePath], absolutePath);
		}


		/// <summary>
		/// Parse the model to get a list of the paths of all used textures
		/// </summary>
		/// <param name="absolutePath">absolute path of the model</param>
		/// <returns>List of paths of the textures referenced by the model</returns>
		public abstract string[] ParseTexturePaths(string absolutePath);

		/// <summary>
		/// Load the main model file
		/// </summary>
		/// <param name="absolutePath">absolute file path</param>
		/// <remarks>This is called by Load() method</remarks>
		protected abstract void LoadModelFile(string absolutePath);

		/// <summary>
		/// Load the material library from the given path.
		/// </summary>
		/// <param name="absolutePath">absolute file path</param>
		/// <remarks>This is called by Load() method</remarks>
		protected abstract void LoadMaterialLibrary(string absolutePath);


		/// <summary>
		/// Build the game objects from data set, materials and textures.
		/// </summary>
		/// <param name="absolutePath">absolute file path</param>
		/// <param name="objName">Name of the main game object (model root)</param>
		/// <param name="parentTransform">transform to which the model root will be attached (if null it will be a root aobject)</param>
		/// <remarks>This is called by Load() method</remarks>
		protected IEnumerator Build(string absolutePath, string objName, Transform parentTransform)
		{
			float prevTime = Time.realtimeSinceStartup;
			if (materialData != null)
			{
				string basePath = GetDirName(absolutePath);
				objLoadingProgress.message = "Loading textures...";
				int count = 0;
				foreach (MaterialData mtl in materialData)
				{
					objLoadingProgress.percentage = LOAD_PHASE_PERC + TEXTURE_PHASE_PERC * count / materialData.Count;
					count++;
					if (mtl.diffuseTexPath != null)
					{
#if UNITY_EDITOR
						if (ImportingAssets)
						{
							System.Diagnostics.Stopwatch stopwatch2 = new System.Diagnostics.Stopwatch();
							stopwatch2.Start();

							mtl.diffuseTex = LoadAssetTexture(mtl.diffuseTexPath);

							stopwatch2.Stop();
							Debug.Log($"Code on line 276 took {stopwatch2.ElapsedMilliseconds} millis");
						}
						else
#endif
						{
							System.Diagnostics.Stopwatch stopwatch3 = new System.Diagnostics.Stopwatch();
							stopwatch3.Start();

							yield return LoadMaterialTexture(basePath, mtl.diffuseTexPath);
							mtl.diffuseTex = loadedTexture;

							stopwatch3.Stop();
							Debug.Log($"Code on line 287 took {stopwatch3.ElapsedMilliseconds} millis");
						}
					}

					if (mtl.bumpTexPath != null)
					{
#if UNITY_EDITOR
						if (ImportingAssets)
						{
							System.Diagnostics.Stopwatch stopwatch4 = new System.Diagnostics.Stopwatch();
							stopwatch4.Start();

							mtl.bumpTex = LoadAssetTexture(mtl.bumpTexPath);

							stopwatch4.Stop();
							Debug.Log($"Code on line 303 took {stopwatch4.ElapsedMilliseconds} millis");
						}
						else
#endif
						{
							System.Diagnostics.Stopwatch stopwatch5 = new System.Diagnostics.Stopwatch();
							stopwatch5.Start();

							yield return LoadMaterialTexture(basePath, mtl.bumpTexPath);
							mtl.bumpTex = loadedTexture;

							stopwatch5.Stop();
							Debug.Log($"Code on line 314 took {stopwatch5.ElapsedMilliseconds} millis");
						}
					}

					if (mtl.specularTexPath != null)
					{
#if UNITY_EDITOR
						if (ImportingAssets)
						{
							System.Diagnostics.Stopwatch stopwatch6 = new System.Diagnostics.Stopwatch();
							stopwatch6.Start();

							mtl.specularTex = LoadAssetTexture(mtl.specularTexPath);

							stopwatch6.Stop();
							Debug.Log($"Code on line 330 took {stopwatch6.ElapsedMilliseconds} millis");
						}
						else
#endif
						{
							System.Diagnostics.Stopwatch stopwatch7 = new System.Diagnostics.Stopwatch();
							stopwatch7.Start();

							yield return LoadMaterialTexture(basePath, mtl.specularTexPath);
							mtl.specularTex = loadedTexture;

							stopwatch7.Stop();
							Debug.Log($"Code on line 341 took {stopwatch7.ElapsedMilliseconds} millis");
						}
					}

					if (mtl.opacityTexPath != null)
					{
#if UNITY_EDITOR
						if (ImportingAssets)
						{
							System.Diagnostics.Stopwatch stopwatch8 = new System.Diagnostics.Stopwatch();
							stopwatch8.Start();

							mtl.opacityTex = LoadAssetTexture(mtl.opacityTexPath);

							stopwatch8.Stop();
							Debug.Log($"Code on line 357 took {stopwatch8.ElapsedMilliseconds} millis");
						}
						else
#endif
						{
							System.Diagnostics.Stopwatch stopwatch9 = new System.Diagnostics.Stopwatch();
							stopwatch9.Start();

							yield return LoadMaterialTexture(basePath, mtl.opacityTexPath);
							mtl.opacityTex = loadedTexture;

							stopwatch9.Stop();
							Debug.Log($"Code on line 368 took {stopwatch9.ElapsedMilliseconds} millis");
						}
					}
				}
			}
			loadStats.buildStats.texturesTime = Time.realtimeSinceStartup - prevTime;
			prevTime = Time.realtimeSinceStartup;

			ObjectBuilder.ProgressInfo info = new ObjectBuilder.ProgressInfo();

			objLoadingProgress.message = "Loading materials...";
#if UNITY_EDITOR
			objectBuilder.alternativeTexPath = altTexPath;
#endif
			objectBuilder.buildOptions = buildOptions;
			bool hasColors = dataSet.colorList.Count > 0;
			bool hasMaterials = materialData != null;

			System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
			stopwatch.Start();

			objectBuilder.InitBuildMaterials(materialData, hasColors);

			stopwatch.Stop();
			Debug.Log($"Code on line 394 took {stopwatch.ElapsedMilliseconds} millis");

			float objInitPerc = objLoadingProgress.percentage;
			if (hasMaterials)
			{
				System.Diagnostics.Stopwatch stopwatch2 = new System.Diagnostics.Stopwatch();
				stopwatch2.Start();

				while (objectBuilder.BuildMaterials(info))
				{
					objLoadingProgress.percentage = objInitPerc + MATERIAL_PHASE_PERC * objectBuilder.NumImportedMaterials / materialData.Count;
					yield return null;
				}

				stopwatch2.Stop();
				Debug.Log($"Code on line 405 took {stopwatch2.ElapsedMilliseconds} millis");

				loadStats.buildStats.materialsTime = Time.realtimeSinceStartup - prevTime;
				prevTime = Time.realtimeSinceStartup;
			}

			objLoadingProgress.message = "Building scene objects...";

			GameObject newObj = new GameObject(objName);
			if (buildOptions.hideWhileLoading)
			{
				newObj.SetActive(false);
			}
			if (parentTransform != null) newObj.transform.SetParent(parentTransform.transform, false);
			OnCreated(newObj, absolutePath);
			////newObj.transform.localScale = Vector3.one * Scaling;
			float initProgress = objLoadingProgress.percentage;
			objectBuilder.StartBuildObjectAsync(dataSet, newObj);
			while (objectBuilder.BuildObjectAsync(ref info))
			{
				objLoadingProgress.message = "Building scene objects... " + (info.objectsLoaded + info.groupsLoaded) + "/" + (dataSet.objectList.Count + info.numGroups);
				objLoadingProgress.percentage = initProgress + BUILD_PHASE_PERC * (info.objectsLoaded / dataSet.objectList.Count + (float)info.groupsLoaded / info.numGroups);
				yield return null;
			}
			objLoadingProgress.percentage = 100.0f;
			loadedModels[absolutePath] = newObj;
			loadStats.buildStats.objectsTime = Time.realtimeSinceStartup - prevTime;
		}


		/// <summary>
		/// Get the directory name of the given path, appending the final slash if eeded.
		/// </summary>
		/// <param name="absolutePath">the absolute path</param>
		/// <returns>the directory name ending with `/`</returns>
		protected string GetDirName(string absolutePath)
		{
			string basePath;
			if (absolutePath.Contains("//"))
			{
				basePath = absolutePath.Remove(absolutePath.LastIndexOf('/') + 1);
			}
			else
			{
				string dirName = Path.GetDirectoryName(absolutePath);
				basePath = String.IsNullOrEmpty(dirName) ? "" : dirName;
				if (!basePath.EndsWith("/"))
				{
					basePath += "/";
				}
			}
			return basePath;
		}


		protected virtual void OnLoaded(GameObject obj, string absolutePath)
		{
			if (obj == null)
			{
				if (ModelError != null)
				{
					ModelError(absolutePath);
				}
			}
			else
			{
				if (buildOptions != null)
				{
					obj.transform.localPosition = buildOptions.localPosition;
					obj.transform.localRotation = Quaternion.Euler(buildOptions.localEulerAngles); ;
					obj.transform.localScale = buildOptions.localScale;
					if (buildOptions.inheritLayer)
					{
						obj.layer = obj.transform.parent.gameObject.layer;
						MeshRenderer[] mrs = obj.transform.GetComponentsInChildren<MeshRenderer>(true);
						for (int i = 0; i < mrs.Length; i++)
						{
							mrs[i].gameObject.layer = obj.transform.parent.gameObject.layer;
						}
					}
				}
				if (buildOptions.hideWhileLoading)
				{
					obj.SetActive(true);
				}

				if (ModelLoaded != null)
				{
					ModelLoaded(obj, absolutePath);
				}
			}
		}


		protected virtual void OnCreated(GameObject obj, string absolutePath)
		{
			if (obj == null)
			{
				if (ModelError != null)
				{
					ModelError(absolutePath);
				}
			}
			else
			{
				if (ModelCreated != null)
				{
					ModelCreated(obj, absolutePath);
				}
			}
		}


		protected virtual void OnLoadFailed(string absolutePath)
		{
			if (ModelError != null)
			{
				ModelError(absolutePath);
			}
		}


#if UNITY_EDITOR
		/// <summary>
		/// Load a texture from the asset database
		/// </summary>
		/// <param name="texturePath">texture path inside the asset database</param>
		/// <returns>the loaded texture or null on error</returns>
		private Texture2D LoadAssetTexture(string texturePath)
		{
			FileInfo textFileInfo = new FileInfo(texturePath);
			string texpath = altTexPath + textFileInfo.Name;
			texpath = texpath.Replace("//", "/");
			Debug.LogFormat("Loading texture asset '{0}'", texpath);
			return AssetDatabase.LoadAssetAtPath<Texture2D>(texpath);
		}
#endif


		/// <summary>
		/// Convert a texture path to a texture URL and update the progress message
		/// </summary>
		/// <param name="basePath">base texture path</param>
		/// <param name="texturePath">relative texture path</param>
		/// <returns>URL of the texture</returns>
		private string GetTextureUrl(string basePath, string texturePath)
		{
			string texPath = texturePath.Replace("\\", "/").Replace("//", "/");
			if (!Path.IsPathRooted(texPath))
			{
				texPath = basePath + texturePath;
			}
			if (!texPath.Contains("//"))
			{
				texPath = "file:///" + texPath;
			}
			objLoadingProgress.message = "Loading textures...\n" + texPath;
			return texPath;
		}


		private IEnumerator LoadMaterialTexture(string basePath, string path)
		{
			loadedTexture = null;
			string texPath = GetTextureUrl(basePath, path);
#if UNITY_2018_3_OR_NEWER
			using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(texPath))
			{
				yield return uwr.SendWebRequest();

				if (uwr.isNetworkError || uwr.isHttpError)
				{
					Debug.LogError(uwr.error);
				}
				else
				{
					// Get downloaded asset bundle
					loadedTexture = DownloadHandlerTexture.GetContent(uwr);
				}
			}
#else
            WWW loader = new WWW(texPath);
            yield return loader;

            if (loader.error != null)
            {
                Debug.LogError(loader.error);
            }
            else
            {
                loadedTexture = LoadTexture(loader);
            }
#endif
		}


		/// <summary>
		/// Load a texture from the URL got from the parameter.
		/// </summary>
#if UNITY_2018_3_OR_NEWER
		private Texture2D LoadTexture(UnityWebRequest loader)
#else
        private Texture2D LoadTexture(WWW loader)
#endif
		{
			string ext = Path.GetExtension(loader.url).ToLower();
			Texture2D tex = null;

			// TODO: add support for more formats (bmp, gif, dds, ...)
			if (ext == ".tga")
			{
				tex = TextureLoader.LoadTextureFromUrl(loader.url);
				//tex = TgaLoader.LoadTGA(new MemoryStream(loader.bytes));
			}
			else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
			{
#if UNITY_2018_3_OR_NEWER
				tex = DownloadHandlerTexture.GetContent(loader);
#else
                tex = loader.texture;
#endif
			}
			else
			{
				Debug.LogWarning("Unsupported texture format: " + ext);
			}

			if (tex == null)
			{
				Debug.LogErrorFormat("Failed to load texture {0}", loader.url);
			}
			else
			{
				//tex.alphaIsTransparency = true;
				//tex.filterMode = FilterMode.Trilinear;
			}

			return tex;
		}


		protected struct BuildStats
		{
			public float texturesTime;
			public float materialsTime;
			public float objectsTime;
		}


		protected struct Stats
		{
			public float modelParseTime;
			public float materialsParseTime;
			public float buildTime;
			public BuildStats buildStats;
			public float totalTime;
		}
	}
}
