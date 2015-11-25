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
			if (Player.ChampionName != "Thresh")
			{
				return;
			}

			LoadSpell();
			LoadMenu();

			Game.OnUpdate += Game_OnUpdate;
			Drawing.OnDraw += Drawing_OnDraw;
			CustomEvents.Unit.OnDash += Unit_OnDash;
			AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
			Spellbook.OnCastSpell += Spellbook_OnCastSpell;
			Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
			Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
			Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
			Game.OnWndProc += Game_OnWndProc;
        }

		private static void Game_OnWndProc(WndEventArgs args) {

			//if (args.WParam == Config.Item("智能Q").GetValue<KeyBind>().Key)
			if (args.WParam == 113 && Config.Item("智能Q").GetValue<bool>())
			{
				if (Player.CountEnemiesInRange(Q.Range)<=0)
				{
					args.Process = false;
				}
				else if (Q.IsReady() && GetQName()== QName.ThreshQ)
				{
					var target = Q.GetTarget();
					if (target != null && target.IsValid && !target.HasSpellShield() && Q.Cast(Q.GetPrediction(target).CastPosition))
					{
						return;
					}
				}

			}
			//if (args.WParam == Config.Item("智能E").GetValue<KeyBind>().Key )
			if (args.WParam == 101 && Config.Item("智能E").GetValue<bool>())
			{
				if (Player.CountEnemiesInRange(E.Range) <= 0)
				{
					args.Process = false;
				}
				if (E.IsReady())
				{
					var target = E.GetTarget();
					if (target != null && target.IsValid && !target.HasSpellShield())
					{
						if (Player.IsFleeing(target))
						{
							E.Cast(target);
						}
						if (Player.IsChaseing(target))
						{
							E.CastToReverse(target);
						}
					}
				}

			}
			//if (args.WParam == Config.Item("智能W").GetValue<KeyBind>().Key && W.IsReady())
			if (args.WParam == 119 && Config.Item("智能E").GetValue<bool>())
			{
				SmartCastW();
            }

			
		}

		private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args) {
			if (Config.Item("辅助模式").GetValue<bool>())
			{
				if (Player.CountAlliesInRange(Config.Item("辅助模式距离").GetValue<Slider>().Value) > 0 && args.Target.Type == GameObjectType.obj_AI_Minion)
				{
					if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
					{
						args.Process = false;
					}
					if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
					{
						if (args.Target.Health <= Player.GetAutoAttackDamage(args.Unit,true))
						{
							args.Process = false;
						}
					}
				}
			}
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

			#region
			if (!sender.IsEnemy || sender.IsMinion || args.SData.IsAutoAttack() || !sender.IsValid<Obj_AI_Hero>() || Player.Distance(sender.ServerPosition) > 2000)
				return;

			if (args.SData.Name == "YasuoWMovingWall")
			{
				OPrediction.yasuoWall.CastTime = Game.Time;
				OPrediction.yasuoWall.CastPosition = sender.Position.Extend(args.End, 400);
				OPrediction.yasuoWall.YasuoPosition = sender.Position;
				OPrediction.yasuoWall.WallLvl = sender.Spellbook.Spells[1].Level;
			}
			#endregion
		}

		private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args) {
			//Q在冲向敌人时不放E
			if (sender.Owner.IsMe && args.Slot == SpellSlot.E && sender.Owner.IsDashing())
			{
				args.Process = false;
			}

			if (Config.Item("Q不进敌塔").GetValue<bool>())
			{
				if (sender.Owner.IsMe && args.Slot == SpellSlot.Q && GetQName()== QName.threshqleap && Qedtarget!=null)
				{
					var tower = Qedtarget.GetMostCloseTower();
					if ((tower != null && Qedtarget.IsInTurret(tower) && tower.IsEnemy)||(Qedtarget.Type == GameObjectType.obj_AI_Hero && ((Obj_AI_Hero)Qedtarget).InFountain()))
					{
						args.Process = false;
					}
                }
            }
			
			//QE塔下敌人
			if (Config.Item("控制塔攻击的敌人").GetValue<bool>() && sender.Owner.IsAlly && sender.Owner is Obj_AI_Turret && args.Target.IsEnemy && args.Target.Type == GameObjectType.obj_AI_Hero)
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

		private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser) {
			if (E.CanCast(gapcloser.Sender) && E.Cast(gapcloser.Sender)== Spell.CastStates.SuccessfullyCasted)
			{
				return;
			}
			else if (Q.CanCast(gapcloser.Sender) && Q.Instance.Name == "ThreshQ")
			{
				CastQ(gapcloser.Sender);
			}
		}

		private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args) {
			if (E.CanCast(sender) && args.DangerLevel == Interrupter2.DangerLevel.High)
			{
				if (Player.CountAlliesInRange(E.Range + 50) < sender.CountAlliesInRange(E.Range + 50))
				{
					E.Cast(sender);
				}
				else
				{
					E.CastToReverse(sender);
				}
			}
			if (Q.CanCast(sender) && args.DangerLevel == Interrupter2.DangerLevel.High && GetQName() == QName.ThreshQ)
			{
				Q.Cast(sender);
			}
		}

		private static void Unit_OnDash(Obj_AI_Base sender, Dash.DashItem args) {
			
			if (sender.IsEnemy && !args.IsBlink && E.IsReady()&& Player.Distance(args.StartPos)<E.Range && E.Cast(args.StartPos))
			{
				return;
			}

			if (sender.IsEnemy && Q.IsReady() && Player.Distance(args.EndPos) < Q.Range && Math.Abs(args.Duration - args.EndPos.Distance(sender) / Q.Speed*1000 )<150)
			{
				List<Vector2> to = new List<Vector2>();
				to.Add(args.EndPos);
				var QCollision = Q.GetCollision(Player.Position.To2D(), to);
				if (QCollision == null || QCollision.Count==0|| QCollision.Any(a => !a.IsMinion))
				{
					if (Q.Cast(args.EndPos))
					{
						return;
					}
				}
			}
			
		}

		private static void Drawing_OnDraw(EventArgs args) {
			if (Config.Item("技能可用才显示").GetValue<bool>() && Config.Item("显示Q").GetValue<Circle>().Active && Q.IsReady())
			{
				Render.Circle.DrawCircle(Player.Position,Q.Range, Config.Item("显示Q").GetValue<Circle>().Color, 1);
			}
			if (Config.Item("技能可用才显示").GetValue<bool>() && Config.Item("显示W").GetValue<Circle>().Active && E.IsReady())
			{
				Render.Circle.DrawCircle(Player.Position, W.Range, Config.Item("显示W").GetValue<Circle>().Color, 1);
			}
			if (Config.Item("技能可用才显示").GetValue<bool>() && Config.Item("显示E").GetValue<Circle>().Active && E.IsReady())
			{
				Render.Circle.DrawCircle(Player.Position, E.Range, Config.Item("显示E").GetValue<Circle>().Color, 1);
			}
			if (Config.Item("技能可用才显示").GetValue<bool>() && Config.Item("显示R").GetValue<Circle>().Active && E.IsReady())
			{
				Render.Circle.DrawCircle(Player.Position, R.Range, Config.Item("显示R").GetValue<Circle>().Color, 1);
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
			TowerCheck();
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
					if (Config.Item("E推人").GetValue<bool>())
					{
						FlayPush();
					}
					if (Config.Item("Q野怪").GetValue<bool>())
					{
						JungleEscape();
					}
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
				CastQ(target);
			}
		}

		private static void LoadSpell() {
			Q = new Spell(SpellSlot.Q, 1075);
			W = new Spell(SpellSlot.W, 950);
			E = new Spell(SpellSlot.E, 450);
			R = new Spell(SpellSlot.R, 430);

			Q.SetSkillshot(0.5f, 80, 1900f, true, SkillshotType.SkillshotLine);
			E.SetSkillshot(0.25f, 100, float.MaxValue, false, SkillshotType.SkillshotLine);
		}

		private static void LoadMenu() {
			Config = new Menu("Thresh As the Chain Warden", "锤石As", true);
			Config.AddToMainMenu();
			var OrbMenu = new Menu("Orbwalker", "走砍设置");
			Orbwalker = new Orbwalking.Orbwalker(OrbMenu);
			Config.AddSubMenu(OrbMenu);

			var SpellConfig = Config.AddSubMenu(new Menu("Spell Settings", "技能设置"));
			SpellConfig.AddItem(new MenuItem("不用Q2", "Don't Auto Q2").SetValue(false));
			SpellConfig.AddItem(new MenuItem("人数比", "Don't Q2 if Enemies > allies").SetValue(new Slider(0,0,5)));

			var FleeConfig = Config.AddSubMenu(new Menu("Flee Settings", "逃跑设置"));
			FleeConfig.AddItem(new MenuItem("逃跑", "Flee").SetValue(new KeyBind('S', KeyBindType.Press)));
			FleeConfig.AddItem(new MenuItem("E推人","Auto E push").SetValue(true));
			FleeConfig.AddItem(new MenuItem("Q野怪", "Auto Q Jungle").SetValue(true));

			var PredictConfig = Config.AddSubMenu(new Menu("Predict Settings", "预判设置"));
			PredictConfig.AddItem(new MenuItem("预判模式", "Prediction Mode").SetValue(new StringList(new[] { "Common", "OKTW" })));
			PredictConfig.AddItem(new MenuItem("命中率", "HitChance").SetValue(new StringList(new[] { "Very High", "High", "Medium" })));

			var BoxConfig = Config.AddSubMenu(new Menu("Box Settings","大招设置"));
			BoxConfig.AddItem(new MenuItem("大招人数","Box Count").SetValue(new Slider(2,1,6)));
			BoxConfig.AddItem(new MenuItem("自动大招模式","Box Mode").SetValue(new StringList(new[] { "Prediction", "Now" })));

			var SupportConfig = Config.AddSubMenu(new Menu("Support Mode", "辅助模式"));
			SupportConfig.AddItem(new MenuItem("辅助模式", "Support Mode").SetValue(true));
			SupportConfig.AddItem(new MenuItem("辅助模式距离", "Support Mode Range").SetValue(new Slider((int)Player.AttackRange, (int)Player.AttackRange, 2000)));

			var DrawConfig = Config.AddSubMenu(new Menu("Drawing Settings","显示设置"));
			DrawConfig.AddItem(new MenuItem("技能可用才显示","Draw when skill is ready").SetValue(true));
			DrawConfig.AddItem(new MenuItem("显示Q", "Draw Q Range").SetValue(new Circle(true,Color.YellowGreen)));
			DrawConfig.AddItem(new MenuItem("显示W", "Draw W Range").SetValue(new Circle(true, Color.Yellow)));
			DrawConfig.AddItem(new MenuItem("显示E", "Draw E Range").SetValue(new Circle(true, Color.GreenYellow)));
			DrawConfig.AddItem(new MenuItem("显示R", "Draw R Range").SetValue(new Circle(true, Color.LightGreen)));

			var SmartKeyConfig = Config.AddSubMenu(new Menu("Smart Cast", "智能施法"));
			SmartKeyConfig.AddItem(new MenuItem("智能Q", "Smart Cast Q").SetValue(true).SetTooltip("开启后按ALT+Q自动Q"));
			SmartKeyConfig.AddItem(new MenuItem("智能W", "Smart Cast W").SetValue(true));
			SmartKeyConfig.AddItem(new MenuItem("智能E", "Smart Cast E").SetValue(true));

			var TowerConfig = Config.AddSubMenu(new Menu("Turret Settings", "防御塔设置"));
			TowerConfig.AddItem(new MenuItem("控制塔攻击的敌人", "Q/E ally Turret’s target").SetValue(true));
			TowerConfig.AddItem(new MenuItem("拉敌人进塔", "Q/E target into ally turret").SetValue(true));
			TowerConfig.AddItem(new MenuItem("Q不进敌塔", "Don't Q2 in enemy turret").SetValue(true));

			//var LevelConfig = Config.AddSubMenu(new Menu("Level Settings", "自动加点"));
			//LevelConfig.AddItem(new MenuItem("启用", "Enable").SetValue(true));
			//LevelConfig.AddItem(new MenuItem("只加大招","Only level R").SetValue(false));
			//LevelConfig.AddItem(new MenuItem("前三级", "2 -  3 Level").SetValue(new StringList(new[] { "Don't Level","Q-W","Q-E","W-Q","W-E","E-Q","E-W" })));
			//LevelConfig.AddItem(new MenuItem("后几级", "4 - 18 Level").SetValue(new StringList(new[] { "Don't Level","Q-W-E","Q-E-W","W-Q-E","W-E-Q","E-Q-W","E-W-Q" })));


			
        }

		private static void Combo() {
			
			var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
			if (target==null || !target.IsValid) return;


			if (E.CanCast(target))
			{
				E.CastToReverse(target);
			}
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
			var hasSpellShield = target.HasSpellShield();
			if (Q.CanCast(target) && !hasSpellShield && !E.IsInRange(target))
			{
				CastQ(target);
			}
			LanternCheck();

		}

		private static void LaneClear() {
			if (E.IsReady() && Player.Mana > Q.ManaCost+W.ManaCost+E.ManaCost+R.ManaCost)
			{
				var minions = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All,MinionTeam.NotAlly);
				var Efarm = Q.GetLineFarmLocation(minions, E.Width);
				if (Efarm.MinionsHit >= 3)
				{
					E.Cast(Efarm.Position);
				}
			}
		}

		private static void SmartCastW() {
			try
			{
				var FurthestAlly = GrabFurthestAlly();
				if (FurthestAlly!=null && W.Cast(FurthestAlly.Position))
				{
					return;
                }
			}
			catch
			{
				Console.WriteLine("智能W:取最远队友异常");
			}

			try
			{
				var InTowerAlly = GetAllyInTower();
				if (InTowerAlly!=null && W.Cast(InTowerAlly.Position))
				{
					return;
				}
			}
			catch (Exception)
			{
				Console.WriteLine("智能W:取塔下队友异常");
			}

			try
			{
				var FocusAlly = GetFocusAlly();
				if (FocusAlly!=null && W.Cast(FocusAlly.Position))
				{
					return;
				}
            }
			catch (Exception)
			{

				throw;
			}
        }

		private static Obj_AI_Hero GetFocusAlly() {
			foreach (var ally in HeroManager.Allies.Where(a => a.IsValid && a.Distance(Player)<W.Range+100))
			{
				if (ally.Distance(Player)>400 || ally.HasWall(Player) && ally.CountEnemiesInRange(600)> Player.CountEnemiesInRange(600))
				{
					return ally;
				}
			}
			return null;
		}

		private static Obj_AI_Hero GetAllyInTower() {
			foreach (var ally in HeroManager.Allies.Where(a => a.IsValid))
			{
				try
				{
					var tower = ally.GetMostCloseTower();
					if (tower!=null && tower.IsEnemy && tower.IsValid && (Obj_AI_Base)tower.Target == ally)
					{
						return ally;
					}
				}
				catch (Exception)
				{

					Console.WriteLine("取塔下队友for异常");
					return null;
				}
			}
			return null;
		}

		private static void LanternCheck() {
			if (!W.IsReady()) return;

			Obj_AI_Hero FurthestAlly = null, AoeAlly = null, PriorityAlly = null;
			try
			{
				var target = Qedtarget != null ? Qedtarget : TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
				FurthestAlly = GrabFurthestAlly(target);
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
				if (Qedtarget!=null && !Qedtarget.IsMinion && W.Cast(Prediction.GetPrediction(FurthestAlly, 1f).CastPosition))
				{
					Console.WriteLine("给最远队友灯笼");
					return;
				}
			}
			else if (AoeAlly != null && W.Cast(AoeAlly.Position))
			{
				Console.WriteLine("给AOE队友灯笼");
				return;
			}
		}

		private static Obj_AI_Hero PriorityLantern() {
			Obj_AI_Hero PriorityAlly = null;
			float AllyPriority = 0;
            foreach (var ally in HeroManager.Allies.Where(a => !a.IsDead && Player.Distance(a)<= W.Range && a.CountEnemiesInRange(1000)>0))
			{
				float Priority = TargetSelector.GetPriority(ally);
				
				var AllyPriorityTemp = ally.Armor * ally.MagicalShield + ally.Health * Priority;
				if (ally.HealthPercent<=60 && Priority!=0 && (PriorityAlly == null || (PriorityAlly != null && AllyPriorityTemp < AllyPriority)))
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
				var allies = ally.ListAlliesInRange(400);
				if (allies.All(a =>!a.IsDead && a.HealthPercent<0.5))
				{
					if ((aoeAlly == null && allies.Count > 0) || (aoeAlly != null && allies.Count > aoeCount))
					{
						aoeAlly = ally;
						aoeCount = allies.Count;
					}
				}
			}
			return aoeAlly;
        }

		private static bool WFurthestAlly() {
			var FurthestAlly = GrabFurthestAlly();
			if (FurthestAlly!=null)
			{
				return  W.Cast(FurthestAlly.Position); 
            }
			return false;
        }

		private static Obj_AI_Hero GrabFurthestAlly(Obj_AI_Base target = null) {
			Obj_AI_Hero FurthestAlly = null;

			

			if (target != null)
			{
				foreach (var ally in HeroManager.Allies.Where(a => a.Distance(target) > 1000 && a.Distance(Player) < W.Range + 100 && !a.IsDead && !a.IsMe))
				{
					if (FurthestAlly == null)
					{
						FurthestAlly = ally;
					}
					else if (FurthestAlly != null && Player.Distance(ally) > Player.Distance(FurthestAlly))
					{
						FurthestAlly = ally;
					}
				}
			}
			else
			{
				foreach (var ally in HeroManager.Allies.Where(a => a.Distance(Player) < W.Range + 100 && !a.IsDead && !a.IsMe))
				{
					if (FurthestAlly == null)
					{
						FurthestAlly = ally;
					}
					else if (FurthestAlly != null && Player.Distance(ally) > Player.Distance(FurthestAlly))
					{
						FurthestAlly = ally;
					}
				}
			}

			
			return FurthestAlly;
        }

		private static void FlayPush() {
			foreach (var enemy in HeroManager.Enemies.Where(e => e.IsValid && !e.IsDead && E.IsInRange(e)))
			{
				E.Cast(enemy);
			}
		}

		private static void AutoBox() {

			var AutoBoxCount = Config.Item("大招人数").GetValue<Slider>().Value;
			var EnemiesCount = Config.Item("自动大招模式").GetValue<StringList>().SelectedIndex == 0
				? Player.CountEnemiesInRangeDeley(R.Range, 0.75f)
				: Player.CountEnemiesInRange(R.Range);

			if (R.IsReady()&& EnemiesCount >= AutoBoxCount)
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

			if (target == null)
			{
				return;
			}

			var tower = target.GetMostCloseTower();
			if (tower!=null && tower.IsAlly)
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

		private static bool CastQ(Obj_AI_Hero target = null) {
			if (GetQName()== QName.ThreshQ && target!=null && !Orbwalking.InAutoAttackRange(target))
			{
				return CastThreshQ1(target);
            }
			else if(GetQName()== QName.threshqleap && !Config.Item("不用Q2").GetValue<bool>())
			{
				var EnemiesCount = 0;
				if (Qedtarget.IsMinion)
				{
					EnemiesCount = Qedtarget.CountEnemiesInRange(700);
				}
				else
				{
					EnemiesCount = Qedtarget.CountEnemiesInRange(700)+1;
				}
				if (EnemiesCount - Player.CountAlliesInRange(700)>= Config.Item("人数比").GetValue<Slider>().Value)
				{
					return false;
				}

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

		public static bool CastThreshQ1(Obj_AI_Base target) {
			if (Config.Item("预判模式").GetValue<StringList>().SelectedIndex == 0)
			{
				var hitChangceList = new[] { HitChance.VeryHigh, HitChance.High, HitChance.Medium };
				return Q.CastIfHitchanceEquals(target, hitChangceList[Config.Item("命中率").GetValue<StringList>().SelectedIndex]);
			}
			if (Config.Item("预判模式").GetValue<StringList>().SelectedIndex == 1)
			{
				var hitChangceList = new[] { OPrediction.HitChance.VeryHigh, OPrediction.HitChance.High, OPrediction.HitChance.Medium };
				return Q.CastOKTW(target, hitChangceList[Config.Item("命中率").GetValue<StringList>().SelectedIndex]);
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
