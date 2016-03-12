using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp.Common;

namespace CNLib {
	public static class MultiLanguage {
		private static Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();
		private static string _language = "English";
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

		public static void SetLanguage(string l) {
			_language = l;
		}

		public static void Load(Dictionary<string, string> LanguageDictionary) {
			//DeBug.Debug("[MultiLanguage]", $"加载字典中，语言：{Config.SelectedLanguage}");
			if (!string.IsNullOrEmpty(Config.SelectedLanguage))
			{
				if (Config.SelectedLanguage != "Chinese")
				{
					Translations = LanguageDictionary;
				}
			}
			else
			{
				if (_language != "Chinese")
				{
					Translations = LanguageDictionary;
				}
			}
		}

		
	}
}
