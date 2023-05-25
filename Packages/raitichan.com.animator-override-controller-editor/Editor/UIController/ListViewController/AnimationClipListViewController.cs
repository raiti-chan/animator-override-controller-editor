using raitichan.com.animator_override_controller_editor.SerializableStruct;
using UnityEngine.UIElements;

namespace raitichan.com.animator_override_controller_editor.Editor.UIController.ListViewController {
	public class AnimationClipListViewController : ListViewControllerBase<PairAnimationClipAndBlendShapeDataLists> {
		public AnimationClipListViewController(ListView listView) : base(listView) {
			listView.selectionType = SelectionType.Single;
		}

		protected override VisualElement MakeItem() {
			return new Label { style = { paddingLeft = 5 } };
		}

		protected override void BindItem(VisualElement element, PairAnimationClipAndBlendShapeDataLists data, int index) {
			if (!(element is Label label)) return;
			label.text = data.animationClip.name;
		}
		
	}
}