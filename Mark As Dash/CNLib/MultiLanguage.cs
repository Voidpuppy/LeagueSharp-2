using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp.Common;
using System.Threading;

namespace CNLib {
	public static class MultiLanguage {
		private static Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();
		public static bool IsCN { get; private set; }
		static MultiLanguage() {
			IsCN = IsChinese();
		}

		public static string _(string textToTranslate) {
			var show = string.Empty;
			if (string.IsNullOrEmpty(textToTranslate))
			{
				return "";
			}
			var textToTranslateToLower = textToTranslate.ToLower();
			if (Translations.ContainsKey(textToTranslateToLower))
			{
				show = Translations[textToTranslateToLower];
			}
			else if (Translations.ContainsKey(textToTranslate))
			{
				show = Translations[textToTranslate];
			}
			else
			{
				show = textToTranslate;
			}
			return show;
		}

		public static void AddLanguage(Dictionary<string, Dictionary<string, string>> LanguageDictionary) {
			
			if (!IsChinese() && LanguageDictionary.Keys.Contains("English"))
			{
				Translations = LanguageDictionary["English"];
			}

		}

		private static bool IsChinese() {
			
			if (!string.IsNullOrEmpty(Config.SelectedLanguage))
			{
				
				if (Config.SelectedLanguage != "Chinese")
				{
					return true;
				}
			}
			else
			{
				var CultureName = System.Globalization.CultureInfo.InstalledUICulture.Name;
				var lid = CultureName.Contains("-")
						? CultureName.Split('-')[0].ToUpperInvariant()
						: CultureName.ToUpperInvariant();
				
				if (lid == "ZH")
				{
					return true;
				}
			}
			return false;
		}
	}
}
