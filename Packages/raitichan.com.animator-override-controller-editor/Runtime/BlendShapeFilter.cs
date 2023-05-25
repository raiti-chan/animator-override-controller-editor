using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace raitichan.com.animator_override_controller_editor {
	public static class BlendShapeFilter {
		private static readonly Regex _VRC_PATTERN = new Regex(@"^[Vv][Rr][Cc][._ -].+", RegexOptions.Compiled);
		private static readonly string[] _FILTER_PATTERN = {
			@".+sil$",
			@".+PP$",
			@".+FF$",
			@".+TH$",
			@".+DD$",
			@".+kk$",
			@".+CH$",
			@".+SS$",
			@".+nn$",
			@".+RR$",
			@".+aa$",
			@".+E$",
			@".+ih$",
			@".+oh$",
			@".+ou$"
		};

		public static bool IsFilteredBlendShape(string blendShapeName) {
			return _VRC_PATTERN.IsMatch(blendShapeName) || _FILTER_PATTERN.Any(pattern => Regex.IsMatch(blendShapeName, pattern));
		}
	}
}