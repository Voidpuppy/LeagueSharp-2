using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace AsStreamMode {
	class Program {

		public static Menu Config;
		public static float EndTime = 0;
		public static float OnChampionDieTime = 0;
		public static float OnMoreKillTime = 0;

		static void Main(string[] args) {
			LoadMenu();
			Game.OnUpdate += Game_OnUpdate;
			Game.OnNotify += Game_OnNotify;
			Game.OnStart += Game_OnStart;
		}

		private static void Game_OnStart(EventArgs args) {
			Config.Item("连杀人数").SetValue(new Slider(0, 0, 8));
		}

		private static void Game_OnNotify(GameNotifyEventArgs args) {

			if (args.NetworkId == ObjectManager.Player.NetworkId 
				&& (args.EventId == GameEventId.OnChampionKill || args.EventId == GameEventId.OnChampionKillPre))
            {
				Config.Item("连杀人数").SetValue(new Slider(0,0,8));
			}
			if (args.EventId== GameEventId.OnChampionDie && args.NetworkId == ObjectManager.Player.NetworkId)
			{
				OnChampionDieTime = Game.Time;
                int kills = Config.Item("连杀人数").GetValue<Slider>().Value + 1;
                if (kills >= 8)
				{
					Config.Item("连杀人数").SetValue(new Slider(8, 0, 8));
					var tempTime = Game.Time + Config.Item("屏蔽时长").GetValue<Slider>().Value;
					if (tempTime> EndTime)
					{
						EndTime = tempTime;
                    }
				}
				else
				{
					Config.Item("连杀人数").SetValue(new Slider(kills, 0, 8));
				}
			}
			
            if (args.EventId == GameEventId.OnChampionDoubleKill 
				|| args.EventId == GameEventId.OnChampionTripleKill
				|| args.EventId == GameEventId.OnChampionQuadraKill
				|| args.EventId == GameEventId.OnChampionPentaKill
				|| args.EventId == GameEventId.OnChampionUnrealKill
				)
			{
                if (Game.Time == OnChampionDieTime)
				{
					var tempTime = Game.Time + Config.Item("屏蔽时长").GetValue<Slider>().Value;
					if (tempTime > EndTime)
					{
						EndTime = tempTime;
					}
				}
				
			}
			
		}

		private static void Game_OnUpdate(EventArgs args) {
			
			if (Config.Item("屏蔽显示").GetValue<KeyBind>().Active)
			{
				Hacks.DisableDrawings = true;
			}
			else
			{
				if (Game.Time < EndTime
					|| Config.Item("商店屏蔽").GetValue<bool>() && MenuGUI.IsShopOpen
					)
				{
					Hacks.DisableDrawings = true;
				}
				else
				{
					Hacks.DisableDrawings = false;
				}
			}


		}

		private static void LoadMenu() {
			Config = new Menu("屏蔽显示", "AsStreamMode", true);
			Config.AddToMainMenu();

			Config.AddItem(new MenuItem("屏蔽显示", "屏蔽显示").SetValue(new KeyBind('I',KeyBindType.Toggle,Hacks.DisableDrawings)));
			Config.AddItem(new MenuItem("商店屏蔽", "购买东西时屏蔽显示").SetValue(true));
			//Config.AddItem(new MenuItem("比分屏蔽", "查看比分时屏蔽显示").SetValue(true));
			Config.AddItem(new MenuItem("超神屏蔽", "超神屏蔽显示").SetValue(true));
			Config.AddItem(new MenuItem("连杀人数", "已连杀人数").SetValue(new Slider(0,0,8)).SetTooltip("不要轻易改动这个，除非这个已经不准了"));
			Config.AddItem(new MenuItem("多杀屏蔽", "多杀屏蔽显示").SetValue(true));
			Config.AddItem(new MenuItem("屏蔽时长","屏蔽时长").SetValue(new Slider(4,0,9)));

			var MultiLanguageConfig = Config.AddSubMenu(new Menu("MultiLanguage Settings", "语言选择"));
			MultiLanguageConfig.AddItem(new MenuItem("选择语言", "Selecte Language").SetValue(new StringList(new[] { "English", "中文" }))).ValueChanged += MultiLanguage_ValueChanged;

			ChangeLanguage(MultiLanguageConfig.Item("选择语言").GetValue<StringList>().SelectedIndex);
		}

		private static void ChangeLanguage(int SelectedIndex) {
			List<Dictionary<string, string>> Languages = new List<Dictionary<string, string>> {
				MultiLanguage.English,
				MultiLanguage.Chinese
			};
			var Language = Languages[SelectedIndex];

			List<object> menus = GetSubMenus(Config);

			foreach (var item in menus)
			{
				if (item is Menu)
				{
					var m = item as Menu;
					var DisplayName = Language.Find(l => l.Key == m.Name).Value;
					if (!string.IsNullOrEmpty(DisplayName))
					{
						m.DisplayName = DisplayName;
					}
				}
				else
				{
					var m = item as MenuItem;
					var DisplayName = Language.Find(l => l.Key == m.Name).Value;
					if (!string.IsNullOrEmpty(DisplayName))
					{
						m.DisplayName = DisplayName;
					}
				}
			}
		}

		private static List<object> GetSubMenus(Menu menu) {
			List<object> AllMenus = new List<object>();
			AllMenus.Add(menu);
			foreach (var item in menu.Items)
			{
				AllMenus.Add(item);
			}
			foreach (var item in menu.Children)
			{
				AllMenus.AddRange(GetSubMenus(item));
			}
			return AllMenus;
		}

		private static void MultiLanguage_ValueChanged(object sender, OnValueChangeEventArgs e) {
			ChangeLanguage(e.GetNewValue<StringList>().SelectedIndex);
		}
	}
}
