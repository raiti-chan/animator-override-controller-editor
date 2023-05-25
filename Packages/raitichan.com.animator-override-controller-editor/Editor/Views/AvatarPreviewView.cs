using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Core;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace raitichan.com.animator_override_controller_editor.Editor.Views {
	public class AvatarPreviewView : IDisposable {
		private readonly Scene _scene;
		private RenderTexture _renderTexture;
		private Material _material;

		private readonly GameObject _cameraObj;
		private readonly GameObject _lightObj;
		private readonly GameObject _avatarObj;

		private Camera _camera;
		private Light _light;
		private VRCAvatarDescriptor _original;
		private VRCAvatarDescriptor _descriptor;

		private int _height;
		private int _width;


		public AvatarPreviewView(VRCAvatarDescriptor descriptor) {
			this._original = descriptor;
			this._scene = EditorSceneManager.NewPreviewScene();
			this._material = CreateMaterial();

			this._cameraObj = this.CreateCameraObject();
			this.AddGameObject(this._cameraObj);

			this._lightObj = this.CreateLightObj();
			this.AddGameObject(this._lightObj);

			this._avatarObj = this.CreateAvatarObj(descriptor);
			this.AddGameObject(this._avatarObj);

			this.ResetCameraTransform();
		}

		public bool OnGUILayout(int size) {
			Rect rect = GUILayoutUtility.GetRect(size, size);
			return this.OnGUI(rect);
		}

		public bool OnGUILayout(int width, int height) {
			Rect rect = GUILayoutUtility.GetRect(width, height);
			rect.width = width;
			rect.height = height;
			return this.OnGUI(rect);
		}

		public bool OnGUI(Rect rect) {
			int intWidth = Mathf.CeilToInt(rect.width);
			int intHeight = Mathf.CeilToInt(rect.height);
			if (this._width != intWidth || this._height != intHeight) {
				this.ResizeMonitor(intWidth, intHeight);
			}

			Event e = Event.current;

			if (rect.Contains(e.mousePosition)) {
				// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
				switch (e.type) {
					case EventType.MouseDrag:
						switch (e.button) {
							case 0:
							case 1:
								this.RotationCamera(e.delta);
								break;
							case 2:
								this.MoveCamera(e.delta);
								break;
						}

						return true;
					case EventType.ScrollWheel:
						this.ZoomCamera(e.delta);
						return true;
				}
			}

			if (e.type == EventType.Layout) return false;
			// Render
			bool oldAllowPipes = Unsupported.useScriptableRenderPipeline;
			Unsupported.useScriptableRenderPipeline = false;
			this._camera.Render();
			Unsupported.useScriptableRenderPipeline = oldAllowPipes;

			if (this._material == null) {
				this._material = CreateMaterial();
			}

			Graphics.DrawTexture(rect, this._renderTexture, this._material);
			return false;
		}


		public void Dispose() {
			this._camera.targetTexture = null;
			if (this._renderTexture != null) Object.DestroyImmediate(this._renderTexture);
			if (this._avatarObj != null) Object.DestroyImmediate(this._cameraObj);
			if (this._lightObj != null) Object.DestroyImmediate(this._lightObj);
			if (this._avatarObj != null) Object.DestroyImmediate(this._avatarObj);
			EditorSceneManager.ClosePreviewScene(this._scene);
		}

		private void ResetCameraTransform() {
			this._cameraObj.transform.position = new Vector3(0, this._descriptor.ViewPosition.y, 1);
			this._cameraObj.transform.rotation = Quaternion.Euler(0, 180, 0);
		}

		private void ResizeMonitor(int width, int height) {
			this._renderTexture = new RenderTexture(width, height, 32);
			this._camera.targetTexture = this._renderTexture;
			this._width = width;
			this._height = height;
		}

		private void ZoomCamera(Vector2 delta) {
			if (delta == Vector2.zero) return;
			float newOrthographicSize = this._camera.orthographicSize + delta.y / Mathf.Abs(delta.y) * 0.01f;
			if (newOrthographicSize > 0.01f) {
				this._camera.orthographicSize = newOrthographicSize;
			}
		}

		private void MoveCamera(Vector2 delta) {
			Transform transform = this._camera.transform;
			Vector3 cameraPos = transform.position;
			float dragScale = 0.02f * this._camera.orthographicSize;
			if (delta.x != 0) cameraPos.x += delta.x * dragScale;
			if (delta.y != 0) cameraPos.y += delta.y * dragScale;
			transform.position = cameraPos;
		}

		private void RotationCamera(Vector2 delta) {
			if (delta.x == 0.0f) return;
			Transform transform = this._avatarObj.transform;
			Vector3 rotation = transform.rotation.eulerAngles;
			rotation.y -= delta.x;
			transform.rotation = Quaternion.Euler(rotation);
		}

		private static Material CreateMaterial() {
			string shaderPath = AssetDatabase.GUIDToAssetPath(GuidConstant.PREVIEW_SHADER);
			Shader previewShader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
			return new Material(previewShader);
		}

		private GameObject CreateCameraObject() {
			GameObject cameraObj = new GameObject("Camera", typeof(Camera));
			this._camera = cameraObj.GetComponent<Camera>();
			this._camera.cameraType = CameraType.Preview;
			this._camera.orthographic = true;
			this._camera.orthographicSize = 0.1f;
			this._camera.forceIntoRenderTexture = true;
			this._camera.scene = this._scene;
			this._camera.enabled = false;
			this._camera.nearClipPlane = 0.01f;
			this._camera.clearFlags = CameraClearFlags.SolidColor;
			this._camera.backgroundColor = Color.gray;
			return cameraObj;
		}

		private GameObject CreateLightObj() {
			GameObject lightObj = new GameObject("Directional Light", typeof(Light)) {
				transform = {
					rotation = Quaternion.Euler(50, -30, 0)
				}
			};
			this._light = lightObj.GetComponent<Light>();
			this._light.type = LightType.Directional;
			return lightObj;
		}

		private GameObject CreateAvatarObj(VRCAvatarDescriptor descriptor) {
			GameObject avatarObj = Object.Instantiate(descriptor.gameObject);
			avatarObj.name = descriptor.gameObject.name;
			avatarObj.transform.position = Vector3.zero;
			avatarObj.transform.rotation = Quaternion.identity;
			this._descriptor = avatarObj.GetComponent<VRCAvatarDescriptor>();
			
			return avatarObj;
		}

		public Transform FindObjectInPreview(Transform target) {
			string path = VRC.Core.ExtensionMethods.GetHierarchyPath(target, this._original.transform);
			Transform transform = this._avatarObj.transform.Find(path);
			return transform;
		}


		private void AddGameObject(GameObject obj) {
			SceneManager.MoveGameObjectToScene(obj, this._scene);
		}
	}
}