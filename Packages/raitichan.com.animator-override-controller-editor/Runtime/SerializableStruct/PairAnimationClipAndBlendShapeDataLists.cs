using System;
using UnityEngine;

namespace raitichan.com.animator_override_controller_editor.SerializableStruct {
	[Serializable]
	public class PairAnimationClipAndBlendShapeDataLists {
		public AnimationClip animationClip;
		public BlendShapeDataList[] blendShapeDataLists;
	}
}