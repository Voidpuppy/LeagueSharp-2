using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 锤石 {
	public static class Extensions {
		public static bool InBase(this Obj_AI_Hero hero) {
			foreach (var item in ObjectManager.Get<Obj_Shop>())
			{
				if (hero.Distance(item) < 5000)
				{
					return true;
				}
			}
			return false;
		}

		public static float GetPassiveTime(this Obj_AI_Base target, String buffName) {
			return
				target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
					.Where(buff => buff.Name == buffName)
					.Select(buff => buff.EndTime)
					.FirstOrDefault() - Game.Time;
		}

		public static int CountEnemiesInRangeDeley(this Obj_AI_Hero hero, float range, float delay) {
			int count = 0;
			foreach (var t in HeroManager.Enemies.Where(t => t.IsValidTarget()))
			{
				Vector3 prepos = Prediction.GetPrediction(t, delay).CastPosition;
				if (hero.Distance(prepos) < range)
					count++;
			}
			return count;
		}

		/// <summary>
		/// 是否正在远离某个目标
		/// </summary>
		/// <param name="hero">自己</param>
		/// <param name="target">远离目标</param>
		/// <returns></returns>
		public static bool IsFleeing(this Obj_AI_Hero hero,Obj_AI_Base target) {
			if (target.Distance(hero.Position)<target.Distance(hero.Path.Last()))
			{
				return true;
			}
			return false;
		}
		/// <summary>
		/// 是否某个目标正在追击
		/// </summary>
		/// <param name="hero">自己</param>
		/// <param name="target">追击目标</param>
		/// <returns></returns>
		public static bool IsChaseing(this Obj_AI_Hero hero, Obj_AI_Base target) {
			if (hero.Distance(target.Position) > hero.Distance(target.Path.Last()))
			{
				return true;
			}
			return false;
		}

		public static bool CastToReverse(this Spell spell,Obj_AI_Base target) {
			var eCastPosition = spell.GetPrediction(target).CastPosition;
			var position =Program.Player.ServerPosition + Program.Player.ServerPosition  - eCastPosition;
			return spell.Cast(position);
		}

		public static bool HasSpellShield(this Obj_AI_Hero target) {
            return target.HasBuffOfType(BuffType.SpellShield) || target.HasBuffOfType(BuffType.SpellImmunity);
		}

		public static Obj_AI_Turret GetMostCloseTower(this Obj_AI_Hero target) {
			Obj_AI_Turret tur = null;
			foreach (var turret in ObjectManager.Get<Obj_AI_Turret>().Where(t =>
				t.IsValid && !t.IsDead && t.Health > 1f && t.IsVisible && t.Distance(target)< 1000))
			{
				if (tur == null)
				{
					tur = turret;
				}
				else if(tur.Distance(target)>turret.Distance(target))
				{
					
					tur = turret;
				}
            }
			return tur;
		}

		public static bool IsInTurret(this Obj_AI_Hero targetHero, Obj_AI_Turret targetTurret = null) {
			if (targetTurret == null)
			{
				targetTurret = targetHero.GetMostCloseTower();
            }
			if (targetHero.Distance(targetTurret)<850)
			{
				return true;
			}
			return false;
		}

		public static bool HasWall(this Obj_AI_Base from, Obj_AI_Base target) {
			if (GetFirstWallPoint(from.Position, target.Position) != null)
			{
				return true;
			}
			return false;
		}

		public static bool HasWall(this Obj_AI_Base from, Vector3 to) {
			if (GetFirstWallPoint(from.Position, to) != null)
			{
				return true;
			}
			return false;
		}

		public static Vector2? GetFirstWallPoint(Vector3 from, Vector3 to, float step = 25) {
			return GetFirstWallPoint(from.To2D(), to.To2D(), step);
		}

		public static Vector2? GetFirstWallPoint(Vector2 from, Vector2 to, float step = 25) {
			var direction = (to - from).Normalized();

			for (float d = 0; d < from.Distance(to); d = d + step)
			{
				var testPoint = from + d * direction;
				var flags = NavMesh.GetCollisionFlags(testPoint.X, testPoint.Y);
				if (flags.HasFlag(CollisionFlags.Wall) || flags.HasFlag(CollisionFlags.Building))
				{
					return from + (d - step) * direction;
				}
			}
			return null;
		}
	}
}
