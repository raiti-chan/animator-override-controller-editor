using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using raitichan.com.animator_override_controller_editor.Editor.UIController.ListViewController;
using raitichan.com.animator_override_controller_editor.Editor.Views;
using raitichan.com.animator_override_controller_editor.SerializableStruct;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace raitichan.com.animator_override_controller_editor.Editor.Windows {
	public class AnimatorOverrideControllerEditor : EditorWindow {
		[MenuItem("Raitichan/AnimatorOverrideControllerEditor")]
		public static void ShowWindow() {
			AnimatorOverrideControllerEditor window = EditorWindow.GetWindow<AnimatorOverrideControllerEditor>();
			window.titleContent = new GUIContent("AnimatorOverrideController Editor");
			window.minSize = new Vector2(600, 300);
			window.Show();
		}

		private SerializedObject _target;
		private AvatarPreviewView _avatarPreviewView;
		private BlendShapeDataList[] _defaultBlendShapeDataLists;

		[SerializeField] private VRCAvatarDescriptor _avatar;
		[SerializeField] private AnimatorOverrideController _overrideController;
		[SerializeField] private PairAnimationClipAndBlendShapeDataLists[] _pairAnimationClipAndBlendShapeDataLists;

		private IMGUIContainer _previewContainer;
		private ObjectField _avatarField;
		private ObjectField _overrideControllerField;
		private AnimationClipListViewController _animationClipListViewController;
		private SkinnedMeshRendererListViewController _skinnedMeshRendererListViewController;
		private BlendShapeListViewController _blendShapeListViewController;
		private Button _saveButton;

		private SkinnedMeshRenderer _editSkinnedMeshTargetInPreview;

		public void OnEnable() {
			this._target = new SerializedObject(this);
			this._avatarPreviewView = null;
		}

		public void OnDisable() {
			this._avatarPreviewView?.Dispose();
			this._avatarPreviewView = null;
			this._target = null;
			this._editSkinnedMeshTargetInPreview = null;
		}

		private void CreateGUI() {
			string uxmlPath = AssetDatabase.GUIDToAssetPath(GuidConstant.ANIMATOR_OVERRIDE_CONTROLLER_EDITOR_UXML);
			VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
			uxml.CloneTree(this.rootVisualElement);

			this._previewContainer = this.rootVisualElement.Q<IMGUIContainer>("Preview");
			this._previewContainer.onGUIHandler = OnPreviewGUI;

			this._avatarField = this.rootVisualElement.Q<ObjectField>("AvatarField");
			this._avatarField.BindProperty(this._target.FindProperty(nameof(this._avatar)));
			this._avatarField.objectType = typeof(VRCAvatarDescriptor);
			this._avatarField.RegisterValueChangedCallback(this.OnAvatarChanged);

			this._overrideControllerField = this.rootVisualElement.Q<ObjectField>("OverrideControllerField");
			this._overrideControllerField.BindProperty(this._target.FindProperty(nameof(this._overrideController)));
			this._overrideControllerField.objectType = typeof(AnimatorOverrideController);
			this._overrideControllerField.RegisterValueChangedCallback(this.OnOverrideControllerChanged);

			this._animationClipListViewController = new AnimationClipListViewController(this.rootVisualElement.Q<ListView>("AnimationList"));
			this._animationClipListViewController.onSelectionChanged += AnimationClipListViewControllerOnSelectionChanged;
			this._animationClipListViewController.SetEnabled(false);

			this._skinnedMeshRendererListViewController = new SkinnedMeshRendererListViewController(this.rootVisualElement.Q<ListView>("SkinnedMeshRendererList"));
			this._skinnedMeshRendererListViewController.onSelectionChanged += SkinnedMeshRendererListViewControllerOnSelectionChanged;

			this._blendShapeListViewController = new BlendShapeListViewController(this.rootVisualElement.Q<ListView>("BlendShapeList"));
			this._blendShapeListViewController.onChangeBlendShape += BlendShapeListViewControllerOnChangeBlendShape;
			this._blendShapeListViewController.SetEnabled(false);

			this._saveButton = this.rootVisualElement.Q<Button>("SaveButton");
			this._saveButton.clicked += Save;
			this._saveButton.SetEnabled(false);
		}

		private void MarkDirty() {
			this._saveButton.SetEnabled(true);
		}

		private void OnPreviewGUI() {
			if (this._avatarPreviewView == null) return;
			if (this._avatarPreviewView.OnGUI(this._previewContainer.contentRect)) {
				this.Repaint();
			}
		}


		private void OnAvatarChanged(ChangeEvent<Object> evt) {
			if (!(evt.newValue is VRCAvatarDescriptor avatarDescriptor)) {
				this._avatarPreviewView?.Dispose();
				this._avatarPreviewView = null;
				this._defaultBlendShapeDataLists = null;
				this._skinnedMeshRendererListViewController.Reset();
				this._blendShapeListViewController.Reset();
				this._animationClipListViewController.SetEnabled(false);
				return;
			}

			this._avatarPreviewView?.Dispose();
			this._avatarPreviewView = new AvatarPreviewView(avatarDescriptor);

			SkinnedMeshRenderer[] targetSkinnedMeshRenderers = this._avatar.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>()
				.Where(renderer => {
					Mesh sharedMesh = renderer.sharedMesh;
					return sharedMesh != null && sharedMesh.blendShapeCount != 0;
				})
				.OrderBy(renderer => renderer.name)
				.ToArray();
			this._skinnedMeshRendererListViewController.SetData(targetSkinnedMeshRenderers);
			this._blendShapeListViewController.Reset();

			this._defaultBlendShapeDataLists = targetSkinnedMeshRenderers.Select(renderer => new BlendShapeDataList(renderer)).ToArray();
			this._animationClipListViewController.SetEnabled(true);
		}

		private void OnOverrideControllerChanged(ChangeEvent<Object> evt) {
			this._target.ApplyModifiedProperties();
			if (!(evt.newValue is AnimatorOverrideController overrideController)) {
				this._pairAnimationClipAndBlendShapeDataLists = null;
				this._animationClipListViewController.Reset();
				this._target.Update();
				return;
			}

			this._pairAnimationClipAndBlendShapeDataLists = overrideController.runtimeAnimatorController.animationClips
				.Distinct()
				.OrderBy(clip => clip.name)
				.Select(clip => new PairAnimationClipAndBlendShapeDataLists { animationClip = clip })
				.ToArray();
			this._animationClipListViewController.SetData(_pairAnimationClipAndBlendShapeDataLists);
			this._target.Update();
		}

		private void AnimationClipListViewControllerOnSelectionChanged(PairAnimationClipAndBlendShapeDataLists pairAnimationClipAndBlendShapeDataLists) {
			if (pairAnimationClipAndBlendShapeDataLists == null || pairAnimationClipAndBlendShapeDataLists.animationClip == null) {
				this._blendShapeListViewController.SetEnabled(false);
				return;
			}

			if (pairAnimationClipAndBlendShapeDataLists.blendShapeDataLists == null || pairAnimationClipAndBlendShapeDataLists.blendShapeDataLists.Length == 0) {
				this._target.ApplyModifiedProperties();
				pairAnimationClipAndBlendShapeDataLists.blendShapeDataLists = this._defaultBlendShapeDataLists.Select(list => list.Clone()).ToArray();
				AnimationClip overrideClip = this._overrideController[pairAnimationClipAndBlendShapeDataLists.animationClip];
				if (pairAnimationClipAndBlendShapeDataLists.animationClip != null) {
					this.AnalyzeAnimationClip(overrideClip, pairAnimationClipAndBlendShapeDataLists.blendShapeDataLists);
				}

				this._target.Update();
			}

			this.UpdateBlendShapeListViewController();
			this.UpdatePreview();
		}

		private void SkinnedMeshRendererListViewControllerOnSelectionChanged(SkinnedMeshRenderer data) {
			if (data == null) {
				this._blendShapeListViewController.Reset();
				this._editSkinnedMeshTargetInPreview = null;
				return;
			}

			this._editSkinnedMeshTargetInPreview = this._avatarPreviewView.FindObjectInPreview(data.transform)?.GetComponent<SkinnedMeshRenderer>();
			this.UpdateBlendShapeListViewController();
			this.UpdatePreview();
		}


		private void BlendShapeListViewControllerOnChangeBlendShape(int shapeIndex, float value, bool enable) {
			if (this._editSkinnedMeshTargetInPreview == null) {
				return;
			}

			float blendShapeWeight = value;
			if (!enable) {
				blendShapeWeight = this._defaultBlendShapeDataLists[this._skinnedMeshRendererListViewController.SelectedIndex].blendShapeData[shapeIndex].value;
			}

			this._editSkinnedMeshTargetInPreview.SetBlendShapeWeight(shapeIndex, blendShapeWeight);
			this.MarkDirty();
		}

		private void UpdateBlendShapeListViewController() {
			int animationClipIndex = this._animationClipListViewController.SelectedIndex;
			int skinnedMashIndex = this._skinnedMeshRendererListViewController.SelectedIndex;

			this._blendShapeListViewController.SetEnabled(animationClipIndex >= 0);
			if (skinnedMashIndex < 0) {
				this._blendShapeListViewController.Reset();
				return;
			}

			if (animationClipIndex < 0) {
				this._blendShapeListViewController.Reset();
				this._blendShapeListViewController.SetData(this._defaultBlendShapeDataLists[skinnedMashIndex].blendShapeData.Where(data => !data.isHidden).ToArray());
			} else {
				SerializedProperty blendShapeDataListProperty = this._target.FindProperty(nameof(this._pairAnimationClipAndBlendShapeDataLists))
					.GetArrayElementAtIndex(animationClipIndex)
					.FindPropertyRelative(nameof(PairAnimationClipAndBlendShapeDataLists.blendShapeDataLists))
					.GetArrayElementAtIndex(skinnedMashIndex)
					.FindPropertyRelative(nameof(BlendShapeDataList.blendShapeData));
				this._blendShapeListViewController.BindProperty(blendShapeDataListProperty);
				this._blendShapeListViewController.SetData(
					this._pairAnimationClipAndBlendShapeDataLists[animationClipIndex].blendShapeDataLists[skinnedMashIndex].blendShapeData.Where(data => !data.isHidden).ToArray());
			}
		}

		private void Save() {
			{
				// エディタ上にロードしていないアニメーションを読み込む
				foreach (PairAnimationClipAndBlendShapeDataLists pairAnimationClipAndBlendShapeDataLists in this._pairAnimationClipAndBlendShapeDataLists) {
					if (pairAnimationClipAndBlendShapeDataLists.blendShapeDataLists != null && pairAnimationClipAndBlendShapeDataLists.blendShapeDataLists.Length != 0) continue;
					this._target.ApplyModifiedProperties();
					pairAnimationClipAndBlendShapeDataLists.blendShapeDataLists = this._defaultBlendShapeDataLists.Select(list => list.Clone()).ToArray();
					AnimationClip overrideClip = this._overrideController[pairAnimationClipAndBlendShapeDataLists.animationClip];
					if (pairAnimationClipAndBlendShapeDataLists.animationClip != null) {
						this.AnalyzeAnimationClip(overrideClip, pairAnimationClipAndBlendShapeDataLists.blendShapeDataLists);
					}

					this._target.Update();
				}
			}

			foreach (PairAnimationClipAndBlendShapeDataLists pairAnimationClipAndBlendShapeDataLists in this._pairAnimationClipAndBlendShapeDataLists) {
				AnimationClip originalClip = pairAnimationClipAndBlendShapeDataLists.animationClip;
				AnimationClip overrideClip = this._overrideController[originalClip];
				if (overrideClip == originalClip) {
					overrideClip = new AnimationClip {
						name = originalClip.name + "_override"
					};
					AssetDatabase.AddObjectToAsset(overrideClip, this._overrideController);
					AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this._overrideController));
					this._overrideController[originalClip] = overrideClip;
				}

				overrideClip.ClearCurves();
				BlendShapeDataList[] blendShapeDataLists = pairAnimationClipAndBlendShapeDataLists.blendShapeDataLists;
				foreach (BlendShapeDataList blendShapeDataList in blendShapeDataLists) {
					foreach (BlendShapeData blendShapeData in blendShapeDataList.blendShapeData) {
						if (!blendShapeData.enable) {
							continue;
						}

						EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
							AnimationUtility.CalculateTransformPath(blendShapeDataList.target.transform, this._avatar.transform),
							typeof(SkinnedMeshRenderer), $"blendShape.{blendShapeData.key}");
						
						AnimationCurve animationCurve = AnimationCurve.Constant(0f, 1f, blendShapeData.value);
						AnimationUtility.SetEditorCurve(overrideClip, binding, animationCurve);
					}
				}
			}

			this._saveButton.SetEnabled(false);
		}

		private void UpdatePreview() {
			int animationClipIndex = this._animationClipListViewController.SelectedIndex;
			BlendShapeDataList[] dataLists = this._defaultBlendShapeDataLists;
			if (animationClipIndex >= 0) {
				dataLists = this._pairAnimationClipAndBlendShapeDataLists[animationClipIndex].blendShapeDataLists;
			}

			for (int skinnedMeshIndex = 0; skinnedMeshIndex < dataLists.Length; skinnedMeshIndex++) {
				BlendShapeDataList blendShapeDataList = dataLists[skinnedMeshIndex];
				SkinnedMeshRenderer updateTarget = this._avatarPreviewView.FindObjectInPreview(blendShapeDataList.target.transform).GetComponent<SkinnedMeshRenderer>();
				for (int blendShapeIndex = 0; blendShapeIndex < blendShapeDataList.blendShapeData.Length; blendShapeIndex++) {
					BlendShapeData blendShapeData = blendShapeDataList.blendShapeData[blendShapeIndex];
					float value = blendShapeData.value;
					if (!blendShapeData.enable) {
						value = this._defaultBlendShapeDataLists[skinnedMeshIndex].blendShapeData[blendShapeIndex].value;
					}

					updateTarget.SetBlendShapeWeight(blendShapeIndex, value);
				}
			}
		}

		private static readonly Regex _BLEND_SHAPE_PROPERTY_PATTERN = new Regex(@"blendShape\.(?<BlendShapeName>.+)", RegexOptions.Compiled);

		private void AnalyzeAnimationClip(AnimationClip animationClip, BlendShapeDataList[] dst) {
			EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(animationClip);
			List<EditorCurveBinding> otherCurveBindings = new List<EditorCurveBinding>();
			Dictionary<SkinnedMeshRenderer, List<(EditorCurveBinding, int)>> blendShapeCurveBindings = new Dictionary<SkinnedMeshRenderer, List<(EditorCurveBinding, int)>>();

			foreach (EditorCurveBinding curveBinding in curveBindings) {
				if (curveBinding.type != typeof(SkinnedMeshRenderer)) {
					otherCurveBindings.Add(curveBinding);
					continue;
				}

				SkinnedMeshRenderer animatedSkinnedMeshRenderer = AnimationUtility.GetAnimatedObject(this._avatar.gameObject, curveBinding) as SkinnedMeshRenderer;
				if (animatedSkinnedMeshRenderer == null) {
					otherCurveBindings.Add(curveBinding);
					continue;
				}

				Match match = _BLEND_SHAPE_PROPERTY_PATTERN.Match(curveBinding.propertyName);
				if (!match.Success) {
					otherCurveBindings.Add(curveBinding);
					continue;
				}

				int blendShapeIndex = animatedSkinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(match.Groups["BlendShapeName"].Value);
				if (blendShapeIndex < 0) {
					otherCurveBindings.Add(curveBinding);
					continue;
				}

				if (!blendShapeCurveBindings.TryGetValue(animatedSkinnedMeshRenderer, out List<(EditorCurveBinding, int)> blendShapeCurveBindingList)) {
					blendShapeCurveBindingList = new List<(EditorCurveBinding, int)>();
					blendShapeCurveBindings[animatedSkinnedMeshRenderer] = blendShapeCurveBindingList;
				}

				blendShapeCurveBindingList.Add((curveBinding, blendShapeIndex));
			}

			foreach (BlendShapeDataList blendShapeDataList in dst) {
				SkinnedMeshRenderer targetSkinnedMeshRenderer = blendShapeDataList.target;

				if (!blendShapeCurveBindings.TryGetValue(targetSkinnedMeshRenderer, out List<(EditorCurveBinding, int)> blendShapeCurveBindingList)) {
					continue;
				}

				foreach ((EditorCurveBinding binding, int blendShapeIndex) blendShapeCurveBinding in blendShapeCurveBindingList) {
					AnimationCurve curve = AnimationUtility.GetEditorCurve(animationClip, blendShapeCurveBinding.binding);
					if (curve.length <= 0) {
						continue;
					}

					float value = curve[0].value;
					blendShapeDataList.blendShapeData[blendShapeCurveBinding.blendShapeIndex].value = value;
					blendShapeDataList.blendShapeData[blendShapeCurveBinding.blendShapeIndex].enable = true;
				}
			}
		}
	}
}