using System;
using raitichan.com.animator_override_controller_editor.SerializableStruct;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace raitichan.com.animator_override_controller_editor.Editor.UIElement {
	public class BlendShapeElement : VisualElement {
		public int Index { get; set; }
		public string Text { get => this._slider.label; set => this._slider.label = value; }
		public float Value => this._slider.value;
		public bool Enable => this._toggle.value;

		public event Action<BlendShapeElement> onChangeValue;

		public event Action<BlendShapeElement> onChangeEnable;

		private SerializedProperty _bindProperty;
		
		private readonly Toggle _toggle;
		private readonly Slider _slider;
		private readonly FloatField _float;

		public BlendShapeElement() {
			string uxmlPath = AssetDatabase.GUIDToAssetPath(GuidConstant.BLEND_SHAPE_ELEMENT_UXML);
			VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
			uxml.CloneTree(this);

			this._toggle = this.Q<Toggle>("Toggle");
			this._toggle.RegisterValueChangedCallback(this.OnToggleChanged);
			
			this._slider = this.Q<Slider>("Slider");
			this._slider.RegisterValueChangedCallback(this.OnSliderChanged);
			
			this._float = this.Q<FloatField>("Float");
			this._float.RegisterValueChangedCallback(this.OnFloatChanged);
		}

		public void BindProperty(SerializedProperty property) {
			if (property == null) {
				this._slider.Unbind();
				this._float.Unbind();
				this._toggle.Unbind();
			} else {
				SerializedProperty valueProperty = property.FindPropertyRelative(nameof(BlendShapeData.value));
				SerializedProperty enableProperty = property.FindPropertyRelative(nameof(BlendShapeData.enable));
				
				this._slider.SetValueWithoutNotify(valueProperty.floatValue);
				this._float.SetValueWithoutNotify(valueProperty.floatValue);
				this._toggle.SetValueWithoutNotify(enableProperty.boolValue);
				
				this._slider.BindProperty(valueProperty);
				this._float.BindProperty(valueProperty);
				this._toggle.BindProperty(enableProperty);
			}
		}

		private void OnToggleChanged(ChangeEvent<bool> evt) {
			this.onChangeEnable?.Invoke(this);
		}

		private void OnSliderChanged(ChangeEvent<float> evt) {
			this._float.SetValueWithoutNotify(evt.newValue);
			this.onChangeValue?.Invoke(this);
			this._toggle.value = true;
		}
		
		
		private void OnFloatChanged(ChangeEvent<float> evt) {
			this._slider.SetValueWithoutNotify(evt.newValue);
			this.onChangeValue?.Invoke(this);
			this._toggle.value = true;
		}
	}
}