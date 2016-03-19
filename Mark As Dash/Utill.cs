using LeagueSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp.Common;
using CNLib;

namespace Mark_As_Dash {
	public static class Utill {

		public static List<Obj_AI_Base> GetObj() {
			return ObjectManager.Get<Obj_AI_Base>()
				.Where(o => o.Position.Distance(Game.CursorPos) < 100).ToList();
		}

		public static string GetInfo() {
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=========对象信息=============");

			var objs = GetObj();
			foreach (var obj in objs)
			{
				sb.AppendLine("对象名:" + obj.Name.ToGBK() + "\t");
				if (obj != null)
				{
					var hero = obj as Obj_AI_Base;
					sb.AppendLine("名字：" + hero.CharData.BaseSkinName);
					string Buffstr = " ";
					foreach (var buffer in hero.Buffs)
					{
						Buffstr += buffer.Name + "\t";
					}
					sb.AppendLine("Buff:" + Buffstr);
				}
			}
			return sb.ToString();
		}

		
	}
}
