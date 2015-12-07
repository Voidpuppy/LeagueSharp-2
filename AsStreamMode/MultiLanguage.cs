using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsStreamMode {
	public class MultiLanguage {
		public static Dictionary<string, string> Chinese { get; private set; } = new Dictionary<string, string> {
			{ "AsStreamMode","屏蔽显示" },
			{ "屏蔽显示", "屏蔽显示开关"},
			{ "超神屏蔽", "超神屏蔽显示"},
			{ "连杀人数", "已连杀人数"},
			{ "多杀屏蔽", "多杀屏蔽显示" },
			{ "屏蔽时长","屏蔽时长" },
			{ "商店屏蔽", "购买东西时屏蔽显示"},
            { "语言选择", "MultiLanguage Settings"},
			{ "选择语言", "Selecte Language"},
		};

		public static Dictionary<string, string> English { get; private set; } = new Dictionary<string, string> {
			{ "AsStreamMode","As Stream Mode" },
			{ "屏蔽显示", "Disable Drawings"},
			{ "商店屏蔽", "Disable Drawings when shopping"},
			{ "超神屏蔽", "Disable Drawings when legendary"},
			{ "连杀人数", "Kills"},
			{ "多杀屏蔽", "Disable Drawings when DoubleKill or more"},
			{ "屏蔽时长", "Disable Duration" },
			{ "语言选择", "多语言设置"},
			{ "选择语言", "选择语言"},
		};

	}
}
