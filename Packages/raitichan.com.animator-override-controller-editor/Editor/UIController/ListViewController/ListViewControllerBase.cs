using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using UnityEngine.UIElements;

namespace raitichan.com.animator_override_controller_editor.Editor.UIController.ListViewController {
	public abstract class ListViewControllerBase<T> {
		private readonly ListView _listView;
		private IList<T> _data;

		public int SelectedIndex => this._listView.selectedIndex;

			public event Action<T> onSelectionChanged;

		protected ListViewControllerBase(ListView listView) {
			this._listView = listView;
			this._listView.makeItem += this.MakeItem;
			this._listView.bindItem += this.BindItem;
			this._listView.onSelectionChanged += OnSelectionChanged;
		}

		public virtual void Reset() {
			this._data = ImmutableArray<T>.Empty;
			this._listView.itemsSource = ImmutableArray<T>.Empty;
			this._listView.selectedIndex = -1;
		}

		public void SetData(IList<T> data) {
			this._data = data;
			this._listView.itemsSource = (IList)data;
			this._listView.selectedIndex = -1;
			this.OnSelectionChanged(null);
		}
		
		public void SetEnabled(bool enable) {
			this._listView.SetEnabled(enable);
		}
		
		protected abstract VisualElement MakeItem();
		
		protected abstract void BindItem(VisualElement element, T data, int index);

		private void BindItem(VisualElement element, int index) {
			this.BindItem(element, this._data[index], index);
		}
		
		private void OnSelectionChanged(List<object> data) {
			this.onSelectionChanged?.Invoke(data == null ? default : data.Count < 1 ? default : (T)data[0]);
		}

	}
}