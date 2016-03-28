using LeagueSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp.Common;

namespace CNLib {
	public static class DeBug {

		public static void Write(string Prefix, string content, DebugLevel level = DebugLevel.Info,
			WriteWay way = WriteWay.Console, bool enable = true) {
			if (!enable) return;

			if (way == WriteWay.Console)
			{
				ConsoleColor color = ConsoleColor.White;
				if (level == DebugLevel.Info)
				{
					color = ConsoleColor.DarkGreen;
				}
				else if (level == DebugLevel.Warning)
				{
					color = ConsoleColor.Yellow;
				}
				else if (level == DebugLevel.Wrang)
				{
					color = ConsoleColor.Red;
				}
				Console.ForegroundColor = color;
				Console.WriteLine(!string.IsNullOrEmpty(Prefix) ? (Prefix + "："+ content) : "" + content);
				Console.ForegroundColor = ConsoleColor.White;
			}
			else
			{
				System.Drawing.Color color = System.Drawing.Color.White;
				if (level == DebugLevel.Info)
				{
					color = System.Drawing.ColorTranslator.FromHtml("#AAAAFF");
				}
				else if (level == DebugLevel.Warning)
				{
					color = System.Drawing.Color.Orange;
				}
				else if (level == DebugLevel.Wrang)
				{
					color = System.Drawing.Color.Red;
				}
				content = (string.IsNullOrEmpty(Prefix) ? (Prefix + "：") : "") + content;
				Game.PrintChat(content.ToHtml(color, FontStlye.Cite));
			}
		}

		public static void Write(string content, DebugLevel level = DebugLevel.Info,
			WriteWay way = WriteWay.Console, bool enable = true) {
			Write(null, content, level, way, enable);
		}

		public static void WriteConsole(string content, DebugLevel level = DebugLevel.Info, bool enable = true) {
			Write(null, content, level, WriteWay.Console, enable);
		}

		public static void WriteChatBox(string content, DebugLevel level = DebugLevel.Info, bool enable = true) {
			Write(null, content, level, WriteWay.ChatBox, enable);
		}

		public static void WriteConsole(string Prefix, string content, DebugLevel level = DebugLevel.Info, bool enable = true) {
			Write(Prefix, content,level,WriteWay.Console,enable);
		}

		public static void WriteChatBox(string Prefix, string content, DebugLevel level = DebugLevel.Info, bool enable = true) {
			Write(Prefix, content, level, WriteWay.ChatBox, enable);
		}



		public static void DebugChat(MenuItem config, string format, params object[] param) {
			if (config.GetValue<bool>())
			{
				string s = string.Format(format, param);
				Game.PrintChat(s.ToHtml("#AAAAFF", FontStlye.Cite));
			}
		}

		public static void DebugChat(string format, params object[] param) {
			string s = string.Format(format, param);
			Game.PrintChat(s.ToHtml("#AAAAFF", FontStlye.Cite));
		}

		public static void DebugConsole(MenuItem config, string Prefix, string format, params object[] param) {
			if (!config.GetValue<bool>()) return;
			if (!string.IsNullOrEmpty(Prefix))
			{
				Prefix += "：";
			}
			Console.ForegroundColor = ConsoleColor.DarkBlue;
			Console.WriteLine(Prefix + format, param);
			Console.ForegroundColor = ConsoleColor.White;
		}

		public static void DebugConsole(string Prefix, string format, params object[] param) {
			if (!string.IsNullOrEmpty(Prefix))
			{
				Prefix += "：";
			}
			Console.ForegroundColor = ConsoleColor.DarkBlue;
			Console.WriteLine(Prefix + format, param);
			Console.ForegroundColor = ConsoleColor.White;
		}

		public static void DebugConsole(string format, params object[] param) {
			Console.ForegroundColor = ConsoleColor.DarkBlue;
			Console.WriteLine(format, param);
			Console.ForegroundColor = ConsoleColor.White;
		}
	}
}
