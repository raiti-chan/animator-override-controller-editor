using System;
using raitichan.com.animator_override_controller_editor.Editor.UIElement;
using raitichan.com.animator_override_controller_editor.SerializableStruct;
using UnityEditor;
using UnityEngine.UIElements;

namespace raitichan.com.animator_override_controller_editor.Editor.UIController.ListViewController {
	public class BlendShapeListViewController : ListViewControllerBase<BlendShapeData> {
		public event Action<int, float, bool> onChangeBlendShape;

		private SerializedProperty _bindProperty;

		public BlendShapeListViewController(ListView listView) : base(listView) {
			listView.selectionType = SelectionType.None;
		}

		public override void Reset() {
			base.Reset();
			this._bindProperty = null;
		}

		public void BindProperty(SerializedProperty property) {
			this._bindProperty = property;
		}

		protected override VisualElement MakeItem() {
			BlendShapeElement blendShapeElement = new BlendShapeElement();
			blendShapeElement.onChangeEnable += BlendShapeElementOnChangeEnable;
			blendShapeElement.onChangeValue += BlendShapeElementOnChangeValue;
			return blendShapeElement;
		}

		protected override void BindItem(VisualElement element, BlendShapeData data, int index) {
			if (!(element is BlendShapeElement blendShapeElement)) return;
			blendShapeElement.Index = data.index;
			blendShapeElement.Text = data.key;
			blendShapeElement.BindProperty(this._bindProperty?.GetArrayElementAtIndex(data.index));
		}


		private void BlendShapeElementOnChangeEnable(BlendShapeElement obj) {
			this.onChangeBlendShape?.Invoke(obj.Index, obj.Value, obj.Enable);
		}

		private void BlendShapeElementOnChangeValue(BlendShapeElement obj) {
			this.onChangeBlendShape?.Invoke(obj.Index, obj.Value, obj.Enable);
		}
	}
}