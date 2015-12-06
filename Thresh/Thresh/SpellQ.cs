using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace Thresh {

	public enum QState {
		ThreshQ,
		threshqleap,
		Cooldown
	}

	public class SpellQ {

		public static bool CastQ1(Obj_AI_Hero target) {
			var Config = Thresh.Config;
			var Q = Thresh.Q;
			var hitChangceIndex = Config.Item("命中率").GetValue<StringList>().SelectedIndex;

			if (Config.Item("预判模式").GetValue<StringList>().SelectedIndex == 0)
			{
				var hitChangceList = new[] { HitChance.VeryHigh, HitChance.High, HitChance.Medium };
				return Q.CastIfHitchanceEquals(target, hitChangceList[hitChangceIndex]);
			}
			else if (Config.Item("预判模式").GetValue<StringList>().SelectedIndex == 1)
			{
				var hitChangceList = new[] { OKTWPrediction.HitChance.VeryHigh, OKTWPrediction.HitChance.High, OKTWPrediction.HitChance.Medium };
				return CastOKTW(target, hitChangceList[hitChangceIndex]);
			}
			return false;
		}

		public static bool CastOKTW(Obj_AI_Hero target,OKTWPrediction.HitChance hitChance) {
			var spell = Thresh.Q;
			var Player = Thresh.Player;

            OKTWPrediction.SkillshotType CoreType2 = OKTWPrediction.SkillshotType.SkillshotLine;
			bool aoe2 = false;

			var predInput2 = new OKTWPrediction.PredictionInput
			{
				Aoe = aoe2,
				Collision = spell.Collision,
				Speed = spell.Speed,
				Delay = spell.Delay,
				Range = spell.Range,
				From = Player.ServerPosition,
				Radius = spell.Width,
				Unit = target,
				Type = CoreType2
			};
			var poutput2 = OKTWPrediction.Prediction.GetPrediction(predInput2);

			if (spell.Speed != float.MaxValue && OKTWPrediction.CollisionYasuo(Player.ServerPosition, poutput2.CastPosition))
				return false;

			if (poutput2.Hitchance >= hitChance)
			{
				return spell.Cast(poutput2.CastPosition);
			}
			return false;
		}

		public static bool CastQ2() {
			if (Thresh.QTarget is Obj_AI_Hero && Thresh.QTarget.GetPassiveTime("ThreshQ") < 0.3)
			{
				return Thresh.Q.Cast();
			}
			else if (Thresh.QTarget.IsMinion)
			{
				return Thresh.Q.Cast();
			}
			return false;
		}

		public static QState GetState() {
			if (!Thresh.Q.IsReady())
			{
				return QState.Cooldown;
			}
			else
			{
				if (Thresh.Q.Instance.Name == "ThreshQ")
				{
					return QState.ThreshQ;
				}
				else
				{
					return QState.threshqleap;
				}
			}
		}
	}
}
