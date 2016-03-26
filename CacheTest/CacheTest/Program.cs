using LeagueSharp.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacheTest {
	class Program {
		public static Cache cache;
		static void Main(string[] args) {

			CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
		}

		private static void Game_OnGameLoad(EventArgs args) {
			cache = new Cache();
			Console.WriteLine(cache.SavePath);

			Menu menu = new Menu("CacheTest", "CacheTest", true);
			menu.AddToMainMenu();

			var item = new MenuItem("test", "test").SetValue(new KeyBind(32,KeyBindType.Press));
			menu.AddItem(item).ValueChanged += Program_ValueChanged;
		}

		private static void Program_ValueChanged(object sender, OnValueChangeEventArgs e) {

			if (e.GetNewValue<KeyBind>().Active)
			{

				cache.Set("HeroName", HeroManager.Player.ChampionName);
				cache.Set("MoveSpeed", HeroManager.Player.MoveSpeed);
				cache.Set("IsMe", HeroManager.Player.IsMe);
				cache.Save();
				
			}
		}
	}
}
