using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace raitichan.com.animator_override_controller_editor.SerializableStruct {
	[Serializable]
	public class BlendShapeDataList {
		public SkinnedMeshRenderer target;
		public BlendShapeData[] blendShapeData;

		public BlendShapeDataList(SkinnedMeshRenderer target) {
			this.target = target;
			this.blendShapeData = GetAllBlendShapeData(target).ToArray();
		}

		private BlendShapeDataList() { }

		public BlendShapeDataList Clone() {
			BlendShapeDataList cloned = new BlendShapeDataList {
				target = this.target,
				blendShapeData = new BlendShapeData[this.blendShapeData.Length]
			};
			Array.Copy(this.blendShapeData, cloned.blendShapeData, cloned.blendShapeData.Length);
			return cloned;
		}

		private static IEnumerable<BlendShapeData> GetAllBlendShapeData(SkinnedMeshRenderer skinnedMeshRenderer) {
			Mesh targetMesh = skinnedMeshRenderer.sharedMesh;
			int count = targetMesh.blendShapeCount;
			for (int i = 0; i < count; i++) {
				string name = targetMesh.GetBlendShapeName(i);
				float value = skinnedMeshRenderer.GetBlendShapeWeight(i);
				bool isHidden = BlendShapeFilter.IsFilteredBlendShape(name);
				yield return new BlendShapeData { key = name, index = i, value = value, enable = false, isHidden = isHidden};
			}
		}
	}
}