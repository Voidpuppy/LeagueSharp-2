using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace 锤石 {
	class Program {

		public static Obj_AI_Hero Player = ObjectManager.Player;
		public static Spell Q, W, E, R;
		public static List<Vector3> MobList = new List<Vector3>();
		public static Obj_AI_Base Qedtarget = null;
		public static Menu Config;
		public static Orbwalking.Orbwalker Orbwalker;

		static void Main(string[] args) {
			CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
		}

		private static void Game_OnGameLoad(EventArgs args) {
			if (Player.ChampionName!= "Thresh")
			{
				return;
			}

			LoadSpell();
			LoadMenu();
			InitMobList();

			Game.OnUpdate += Game_OnUpdate;
			Drawing.OnDraw += Drawing_OnDraw;
			CustomEvents.Unit.OnDash += Unit_OnDash;
			AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
			Spellbook.OnCastSpell += Spellbook_OnCastSpell;
			Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
			
			//Obj_AI_Base.OnBuffAdd += Obj_AI_Base_OnBuffAdd;
			//Obj_AI_Base.OnBuffRemove += Obj_AI_Base_OnBuffRemove;
        }

		private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args) {
			#region
			if (!W.IsReady() || !sender.IsEnemy || !sender.IsValidTarget(1500))
				return;
			double value = 20 + (Player.Level * 20) + (0.4 * Player.FlatMagicDamageMod);

			foreach (var ally in HeroManager.Allies.Where(ally => ally.IsValid && !ally.IsDead && Player.Distance(ally.ServerPosition) < W.Range + 200))
			{
				double dmg = 0;
				if (args.Target != null && args.Target.NetworkId == ally.NetworkId)
				{
					dmg = dmg + sender.GetSpellDamage(ally, args.SData.Name);
				}
				else
				{
					var castArea = ally.Distance(args.End) * (args.End - ally.ServerPosition).Normalized() + ally.ServerPosition;
					if (castArea.Distance(ally.ServerPosition) < ally.BoundingRadius / 2)
						dmg = dmg + sender.GetSpellDamage(ally, args.SData.Name);
					else
						continue;
				}

				if (dmg > 0)
				{
					if (dmg > value)
						W.Cast(ally.Position);
					else if (Player.Health - dmg < Player.CountEnemiesInRange(700) * Player.Level * 20)
						W.Cast(ally.Position);
					else if (ally.Health - dmg < ally.Level * 10)
						W.Cast(ally.Position);
				}
			}
			#endregion

		}

		private static void LoadSpell() {
			Q = new Spell(SpellSlot.Q, 1075);
			W = new Spell(SpellSlot.W, 950);
			E = new Spell(SpellSlot.E, 450);
			R = new Spell(SpellSlot.R, 430);

			Q.SetSkillshot(0.5f, 80, 1900f, true, SkillshotType.SkillshotLine);
			E.SetSkillshot(0.25f, 100, float.MaxValue, false, SkillshotType.SkillshotLine);
		}

		private static void Obj_AI_Base_OnBuffRemove(Obj_AI_Base sender, Obj_AI_BaseBuffRemoveEventArgs args) {
			if (sender.IsEnemy && args.Buff.Name == "threshqfakeknockup" && args.Buff.Caster.IsMe)
			{
				Qedtarget = null;
			}
		}

		private static void Obj_AI_Base_OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args) {
			if (sender.IsEnemy && args.Buff.Name == "threshqfakeknockup" && args.Buff.Caster.IsMe)
			{
				Qedtarget = sender;
            }
		}

		private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args) {
			//Q在冲向敌人时不放E
			if (sender.Owner.IsMe && args.Slot == SpellSlot.E && sender.Owner.IsDashing())
			{
				args.Process = false;
			}
			
			
			if (sender.Owner.IsAlly && sender.Owner is Obj_AI_Turret && args.Target.IsEnemy && args.Target.Type == GameObjectType.obj_AI_Hero)
			{
				var target = args.Target as Obj_AI_Hero;
				var turret = sender.Owner as Obj_AI_Turret;

				if (E.IsInRange(target))
				{
					if (target.Distance(turret) < Player.Distance(turret))
					{
						if (E.Cast(target) == Spell.CastStates.SuccessfullyCasted)
						{
							return;
						}
					}
					else
					{
						if (E.CastToReverse(target))
						{
							return;
						}
					}
				}
				if (Player.Distance(turret) < turret.AttackRange)
				{
					CastQ(target);
				}

                
			}
		
		}

		private static void InitMobList() {
			MobList.Add(new Vector3 { X = 1684, Y = 55, Z = 8207 });
			MobList.Add(new Vector3 { X = 8217, Y = 54, Z = 2534 });
			MobList.Add(new Vector3 { X = 7917, Y = 54, Z = 2534 });
			MobList.Add(new Vector3 { X = 3324, Y = 56, Z = 6373 });
			MobList.Add(new Vector3 { X = 3524, Y = 56, Z = 6223 });
			MobList.Add(new Vector3 { X = 3374, Y = 56, Z = 6223 });
			MobList.Add(new Vector3 { X = 6583, Y = 53, Z = 5108 });
			MobList.Add(new Vector3 { X = 6654, Y = 59, Z = 5278 });
			MobList.Add(new Vector3 { X = 6496, Y = 61, Z = 5365 });
			MobList.Add(new Vector3 { X = 6446, Y = 56, Z = 5215 });
			MobList.Add(new Vector3 { X = 12337, Y = 55, Z = 6263 });
			MobList.Add(new Vector3 { X = 6140, Y = 40, Z = 11935 });
			MobList.Add(new Vector3 { X = 5846, Y = 40, Z = 11915 });
			MobList.Add(new Vector3 { X = 10452, Y = 66, Z = 8116 });
			MobList.Add(new Vector3 { X = 10696, Y = 65, Z = 7965 });
			MobList.Add(new Vector3 { X = 10652, Y = 64, Z = 8116 });
			MobList.Add(new Vector3 { X = 7450, Y = 55, Z = 9350 });
			MobList.Add(new Vector3 { X = 7350, Y = 56, Z = 9230 });
			MobList.Add(new Vector3 { X = 7480, Y = 56, Z = 9091 });
			MobList.Add(new Vector3 { X = 7580, Y = 55, Z = 9250 });
			MobList.Add(new Vector3 { X = 9460, Y = -61, Z = 4193 });
			MobList.Add(new Vector3 { X = 4600, Y = -63, Z = 10250 });
			//MobList.Add(new Vector3 );
		}

		private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser) {
			if (E.CanCast(gapcloser.Sender))
			{
				E.Cast(gapcloser.Sender);
			}
			if (Q.CanCast(gapcloser.Sender) && Q.Instance.Name == "ThreshQ")
			{
				CastQ(gapcloser.Sender);
			}
		}

		private static void Unit_OnDash(Obj_AI_Base sender, Dash.DashItem args) {
			if (sender.IsEnemy && args.IsBlink && E.IsReady()&& Player.Distance(args.StartPos)<E.Range)
			{
				E.Cast(args.StartPos);
			}
		}

		private static void Drawing_OnDraw(EventArgs args) {
			if (Q.IsReady())
			{
				Render.Circle.DrawCircle(Player.Position,Q.Range, Color.Cyan, 1);
			}
			if (E.IsReady())
			{
				Render.Circle.DrawCircle(Player.Position, E.Range, Color.Cyan, 1);
			}
		}

		private static void Game_OnUpdate(EventArgs args) {
			if (Player.IsDead) return;

			foreach (var unit in ObjectManager.Get<Obj_AI_Base>().Where(o => o.Distance(Player)<Q.Range+20 && !o.IsMe))
			{
				if (unit.HasBuff("ThreshQ"))
				{
					Qedtarget = unit;
					break;
				}
				else
				{
					Qedtarget = null;
                }
			}

			AutoBox();
			//AutoGab();

			switch (Orbwalker.ActiveMode)
			{
				case Orbwalking.OrbwalkingMode.LaneClear:
					LaneClear();
                    break;
				case Orbwalking.OrbwalkingMode.Combo:
					Combo();
					break;
			}

			if (Config.Item("逃跑").GetValue<KeyBind>().Active)
			{
				try
				{
					FlayPush();
					JungleEscape();
					Orbwalking.MoveTo(Game.CursorPos);
				}
				catch (Exception)
				{
					Console.WriteLine("逃跑异常");
				}
				
			}
		}

		private static void AutoGab() {
			var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
			if (Q.Instance.Name == "ThreshQ" && Q.CanCast(target)&& !E.IsInRange(target) && !Player.IsRecalling())
			{
				CastQ(target, HitChance.Collision);
				CastQ(target, HitChance.Immobile);
			}
		}

		private static void LoadMenu() {
			Config = new Menu("Thresh", "锤石As", true);
			var OrbMenu = new Menu("Orbwalker", "走砍设置");
			Orbwalker = new Orbwalking.Orbwalker(OrbMenu);
			Config.AddSubMenu(OrbMenu);
			Config.AddItem(new MenuItem("逃跑", "Flee").SetValue(new KeyBind('S', KeyBindType.Press)));
			Config.AddItem(new MenuItem("大招人数","Box Count").SetValue(new Slider(2,1,5)));

			Config.AddToMainMenu();
        }

		private static void Combo() {
			
			var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
			if (target==null || !target.IsValid) return;
			try
			{
				if (E.CanCast(target))
				{
					E.CastToReverse(target);
				}
			}
			catch (Exception)
			{
				Console.WriteLine("连招E异常");
			}

			try
			{
				if (!E.IsInRange(target) && Qedtarget != null && Q.Instance.Name == "threshqleap")
				{
					if (Qedtarget.IsMinion)
					{
						if (E.IsReady())
						{
							CastQ();
						}
					}
					else
					{
						CastQ();
					}
				}
			}
			catch (Exception)
			{

				Console.WriteLine("连招二段Q异常");
			}
			try
			{
				var hasSpellShield = false;
                try
				{
					hasSpellShield = target.HasSpellShield();
				}
				catch (Exception)
				{
					Console.WriteLine("获取技能盾异常");
				}
                if (Q.CanCast(target) && !hasSpellShield && !E.IsInRange(target))
				{
					CastQ(target);
				}
			}
			catch (Exception)
			{
				Console.WriteLine("连招Q异常");
			}
			try
			{
				LanternCheck();
			}
			catch (Exception)
			{

				Console.WriteLine("自动灯笼出错");
			}
			
        }

		private static void LaneClear() {
			if (E.IsReady() && Player.Mana > Q.ManaCost+W.ManaCost+E.ManaCost+R.ManaCost)
			{
				var minions = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All);
				var Efarm = Q.GetLineFarmLocation(minions, E.Width);
				if (Efarm.MinionsHit >= 3)
				{
					E.Cast(Efarm.Position);
				}
			}
		}

		private static void LanternCheck() {
			if (!W.IsReady()) return;

			Obj_AI_Hero FurthestAlly = null, AoeAlly = null, PriorityAlly = null;
			try
			{
				FurthestAlly = GrabFurthestAlly();
			}
			catch (Exception)
			{
				Console.WriteLine("取最远队友异常");
			}
			try
			{
				AoeAlly = AOELantern();
			}
			catch (Exception)
			{
				Console.WriteLine("取AOE队友异常");
			}
			try
			{
				PriorityAlly = PriorityLantern();
			}
			catch (Exception)
			{
				Console.WriteLine("取优先队友");
			}
			
			if (FurthestAlly != null)
			{
				if (Qedtarget!=null && !Qedtarget.IsMinion)
				{
					var enemyCount = Player.CountEnemiesInRange(1500);
					var allyCount = Player.CountAlliesInRange(1500);
					if (enemyCount >= allyCount + 1 && W.Cast(Prediction.GetPrediction(FurthestAlly, 1f).CastPosition))
					{
						Console.WriteLine("给最远队友灯笼");
						return;
					}
	
				}
			}
			else if (AoeAlly != null && W.Cast(AoeAlly.Position))
			{
				Console.WriteLine("给AOE队友灯笼");
				return;
			}
			//else if(PriorityAlly != null && W.Cast(Prediction.GetPrediction(PriorityAlly, 1f).CastPosition))
			//{
			//	Console.WriteLine("给优先队友灯笼");
			//	return ;
			//}


		}

		private static Obj_AI_Hero PriorityLantern() {
			Obj_AI_Hero PriorityAlly = null;
			float AllyPriority = 0;
            foreach (var ally in HeroManager.Allies.Where(a => !a.IsDead && Player.Distance(a)<= W.Range && a.CountEnemiesInRange(1000)>0))
			{
				float Priority = TargetSelector.GetPriority(ally);
				
				var AllyPriorityTemp = ally.Armor * ally.MagicalShield + ally.Health * Priority;
				if (ally.HealthPercent<=80 && Priority!=0 && (PriorityAlly == null || (PriorityAlly != null && AllyPriorityTemp < AllyPriority)))
				{
					PriorityAlly = ally;
					AllyPriority = AllyPriorityTemp;
				}
				
            }
			return PriorityAlly;
		}

		private static Obj_AI_Hero AOELantern() {
			Obj_AI_Hero aoeAlly = null;
			int aoeCount = 0;
			foreach (var ally in HeroManager.Allies.Where(a => !a.IsDead && Player.Distance(a)<W.Range && a.CountEnemiesInRange(500)>0))
			{
				int allyCount = ally.CountAlliesInRange(400);
                if ((aoeAlly == null && allyCount > 0)||(aoeAlly != null && allyCount > aoeCount))
				{
					aoeAlly = ally;
					aoeCount = allyCount;
                }
			}
			return aoeAlly;
        }

		private static Obj_AI_Hero GrabFurthestAlly() {
			Obj_AI_Hero FurthestAlly = null;

			var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
			if (Player.Distance(target) < 500)
			{
				foreach (var ally in HeroManager.Allies.Where(a => a.Distance(Player)< W.Range+50 && !a.IsDead && !a.IsMe))
				{
					if (FurthestAlly==null)
					{
						FurthestAlly = ally;
					}
					else if(Player.Distance(ally) < Player.Distance(FurthestAlly))
					{
						FurthestAlly = ally;
					}
                }
			}
			return FurthestAlly;
        }

		private static void FlayPush() {
			
			foreach (var enemy in HeroManager.Enemies)
			{
				if (E.CanCast(enemy) && Player.IsFleeing(enemy) && enemy.IsChaseing(Player))
				{
					E.Cast(enemy);
				}
			}
		}

		private static void AutoBox() {
			//设置R个数
			var AutoBoxCount = Config.Item("大招人数").GetValue<Slider>().Value;
            if (R.IsReady()&& Player.CountEnemiesInRangeDeley(R.Range,0.75f)>= AutoBoxCount)
			{
				R.Cast();
			}
		}

		private static void JungleEscape() {

			foreach (var Camp in Jungle.Camps)
			{
				Obj_AI_Base JungleTarget = null;
                foreach (var mob in MinionManager.GetMinions(Camp.Position, 200, MinionTypes.All,MinionTeam.Neutral).Where(m => !m.IsDead && m.IsVisible))
				{
					JungleTarget = mob;
                }
				if (JungleTarget!=null)
				{
					if (Q.CanCast(JungleTarget) && Player.Distance(JungleTarget) < Q.Range && Q.Instance.Name == "ThreshQ" && (Player.Distance(JungleTarget) > Q.Range * 2 / 3|| Player.HasWall(JungleTarget)))
					{
						Q.Cast(Q.GetPrediction(JungleTarget).CastPosition);
					}
					if (Qedtarget== JungleTarget && Q.Instance.Name == "threshqleap")
					{
						Q.Cast();
					}
				}
				else
				{
					if (Q.IsReady() && Player.Distance(Camp.Position) < Q.Range && Q.Instance.Name == "ThreshQ" && (Player.Distance(Camp.Position) > Q.Range * 2 / 3 || Player.HasWall(Camp.Position)))
					{
						Q.Cast(Camp.Position);
					}
					if (Qedtarget == JungleTarget && Q.Instance.Name == "threshqleap")
					{
						Q.Cast();
					}
				}

			}
		}

		private static void TowerCheck() {
			var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
			var tower = target.GetMostCloseTower();
			if (tower.IsAlly)
			{
				if (Player.IsInTurret(tower) && tower.Target == null && Q.IsReady() && target.Distance(tower)<Q.Range/2 && GetQName()== QName.ThreshQ)
				{
					CastQ(target);
                }

				if (!Player.IsInTurret(tower) && tower.Target == null && target.Distance(tower)<E.Range/2 && E.CanCast(target))
				{
					if (target.Distance(tower)<Player.Distance(tower))
					{
						E.Cast(target);
					}
					else
					{
						E.CastToReverse(target);
					}
				}
			}


		}

		private static bool CastQ(Obj_AI_Hero target = null, HitChance hitChance = HitChance.VeryHigh) {
			if (GetQName()== QName.ThreshQ && target!=null)
			{
				var Qpre = Q.GetPrediction(target);
				if (Qpre.Hitchance >= hitChance)
				{
					return Q.Cast(Qpre.CastPosition);
				}
			}
			else if(GetQName()== QName.threshqleap )
			{
				if (Qedtarget is Obj_AI_Hero && Qedtarget.GetPassiveTime("ThreshQ") < 0.3)
				{
					return Q.Cast();
				}
				if (Qedtarget.IsMinion)
				{
					return Q.Cast();
				}
				
			}
			return false;
		}
		enum QName {
			ThreshQ,
			threshqleap
		}
		private static QName GetQName() {
			if (Q.Instance.Name == "ThreshQ")
			{
				return QName.ThreshQ;
			}
			else
			{
				return QName.threshqleap;
			}
		}
    }
}
