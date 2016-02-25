using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SebbyLib;
using SharpDX;
using DXColor = SharpDX.Color;
using Color = System.Drawing.Color;
using OKTWPrediction = SebbyLib.Prediction.Prediction;
using FS = System.Drawing.FontStyle;
using SharpDX.Direct3D9;

namespace Jhin_As_The_Virtuoso {

	class Jhin {
		public static Menu Config {get;set;}
		public static Obj_AI_Hero Player => HeroManager.Player;
		public static Orbwalking.Orbwalker Orbwalker { get; private set; }
		public static Spell Q { get; set; }
		public static Spell W { get; set; }
		public static Spell E { get; set; }
		public static int lastwarded { get; set; }
		public static Spell R { get; set; }
		public static bool IsCastingR => R.Instance.Name == "JhinRShot";
		public static Vector3 REndPos { get; private set; }
		public static Dictionary<int,string> KillText { get; set; }
		public static List<Obj_AI_Hero> KillableList { get; set; } = new List<Obj_AI_Hero>();
		public static int[] delay => new[] {
				Config.Item("第一次延迟").GetValue<Slider>().Value,
				Config.Item("第二次延迟").GetValue<Slider>().Value,
				Config.Item("第三次延迟").GetValue<Slider>().Value
		};

		public static Items.Item BlueTrinket = new Items.Item(3342, 3500f);
		public static Items.Item ScryingOrb = new Items.Item(3363, 3500f);

		public static Font KillTextFont = new Font(Drawing.Direct3DDevice,new FontDescription {
			 Height = 28,
			 FaceName = "Microsoft YaHei",
		});
		

		internal static void OnLoad(EventArgs args) {
			if (Player.ChampionName!="Jhin")
			{
				return;
			}

			LoadSpell();
			LoadMenu();
			LoadEvents();

			LastPosition.Load();

			DamageIndicator.DamageToUnit = GetRDmg;
		}

		private static void LoadEvents() {
			Game.OnUpdate += Game_OnUpdate;
			Drawing.OnDraw += Drawing_OnDraw;
			Game.OnWndProc += Game_OnWndProc;
			Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
			Obj_AI_Base.OnDoCast += Obj_AI_Base_OnDoCast;
			AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
			Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
			Obj_AI_Base.OnLevelUp += Obj_AI_Base_OnLevelUp;
			Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
			Orbwalking.AfterAttack += Orbwalking_AfterAttack;
			Orbwalking.OnNonKillableMinion += Orbwalking_OnNonKillableMinion;
			Obj_AI_Base.OnIssueOrder += Obj_AI_Base_OnIssueOrder;
			CustomEvents.Unit.OnDash += Unit_OnDash;
			Game.OnChat += Game_OnChat;
			
		}

		private static void Game_OnChat(GameChatEventArgs args) {

			if (Config.Item("击杀信号提示").GetValue<bool>() && args.Message.Contains(Player.Name) && args.Message.ToGBK().Contains("要求队友"))
			{
				args.Process = false;
			}
		}

		private static void Orbwalking_OnNonKillableMinion(AttackableUnit minion) {
			if (Q.IsReady() && Config.Item("补刀Q").GetValue<bool>())
			{
				Q.Cast(minion as Obj_AI_Base);
			}
		}

		private static void Unit_OnDash(Obj_AI_Base sender, Dash.DashItem args) {
			if (sender.IsEnemy)
			{
				if (Config.Item("位移E").GetValue<bool>() && E.IsReady() && args.EndPos.Distance(Player)<E.Range && NavMesh.IsWallOfGrass(args.EndPos.To3D(), 10))
				{
					E.Cast(args.EndPos);
				}

				if (Config.Item("位移W").GetValue<bool>() && W.IsReady() && (sender as Obj_AI_Hero).HasWBuff() && args.EndPos.Distance(Player) < W.Range && (!E.IsReady() || args.EndPos.Distance(Player) > E.Range ))
				{
					W.Cast(args.EndPos);
				}
			}
		}

		private static void Obj_AI_Base_OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args) {
			if (sender.IsMe 
				&& IsCastingR && Config.Item("禁止移动").GetValue<bool>()
				&& Player.CountEnemiesInRange(Config.Item("禁止距离").GetValue<Slider>().Value) == 0
				&& HeroManager.Enemies.Any(e => e.InRCone() && !e.IsDead && e.IsValid)
			)
			{
				args.Process = false;
			}
		}

		private static void Obj_AI_Base_OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args) {
			if (sender.IsMe)
			{
				if (args.SData.Name == "JhinRShotMis")
				{
					
					RCharge.Index++;
					RCharge.CastT = Game.ClockTime;
				}
				if (args.SData.Name == "JhinRShotMis4")
				{
					
					RCharge.Index = 0;
					RCharge.CastT = Game.ClockTime;
					RCharge.Target = null;
				}
			}
		}

		private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args) {
			#region Q消耗
			if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
			{
				var t = args.Target;
				if (t != null)
				{
					if (t.Type != GameObjectType.obj_AI_Hero)
					{
						var enemy = t as Obj_AI_Base;
						if (enemy.CountEnemiesInRange(200) > 0)
						{
							args.Process = false;
							Q.Cast(enemy);
						}
					}
				}
			}
			#endregion

			//Q清兵
			if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && Config.Item("清兵Q").GetValue<bool>())
			{
				var target = Orbwalker.GetTarget() as Obj_AI_Base;
				if (target !=null && Q.CanCast(target) && Q.GetDmg(target)>target.Health && MinionManager.GetMinions(target.Position,200)?.Count>=2)
				{
					args.Process = false;
					Q.Cast(target);
				}
			}
		}

		private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target) {
			
			if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo
				&& !target.IsDead && target.IsValidTarget(Q.Range) && Q.IsReady())
			{
				Q.Cast(target as Obj_AI_Base);
			}

			if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed 
				&& target.Type == GameObjectType.obj_AI_Hero
				&& !target.IsDead && target.IsValidTarget(Q.Range) && Q.IsReady()
				&& Config.Item("消耗Q").GetValue<bool>())
			{
				Q.Cast(target as Obj_AI_Base);
			}
		}

		private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args) {
			if (Config.Item("打断E").GetValue<bool>() && sender.IsEnemy && E.CanCast(sender))
			{
				E.Cast(sender);
			}
		}

		private static void Obj_AI_Base_OnLevelUp(Obj_AI_Base sender, EventArgs args) {
			if (!sender.IsMe) return;
			
			if (Config.Item("自动加点").GetValue<bool>() && Player.Level >= Config.Item("加点等级").GetValue<Slider>().Value)
			{
				int Delay = Config.Item("加点延迟").GetValue<Slider>().Value;

				if (Player.Level == 6 || Player.Level == 11 || Player.Level == 16)
				{
					Player.Spellbook.LevelSpell(SpellSlot.R);
				}

				if (Q.Level == 0)
				{
					Player.Spellbook.LevelSpell(SpellSlot.Q);
				}
				else if (W.Level == 0)
				{
					Player.Spellbook.LevelSpell(SpellSlot.W);
				}
				else if (E.Level == 0)
				{
					Player.Spellbook.LevelSpell(SpellSlot.E);
				}

				if (Config.Item("加点方案").GetValue<StringList>().SelectedIndex == 0)//主Q副W
				{
					DelayLevels(Delay, SpellSlot.Q);
					DelayLevels(Delay + 50, SpellSlot.W);
					DelayLevels(Delay + 100, SpellSlot.E);
				}
				else if (Config.Item("加点方案").GetValue<StringList>().SelectedIndex == 1)//主Q副E
				{
					DelayLevels(Delay, SpellSlot.Q);
					DelayLevels(Delay + 50, SpellSlot.E);
					DelayLevels(Delay + 100, SpellSlot.W);
				}
				else if (Config.Item("加点方案").GetValue<StringList>().SelectedIndex == 2)//主W副Q
				{
					DelayLevels(Delay, SpellSlot.W);
					DelayLevels(Delay + 50, SpellSlot.Q);
					DelayLevels(Delay + 100, SpellSlot.E);
				}
				else if (Config.Item("加点方案").GetValue<StringList>().SelectedIndex == 3)//主W副E
				{
					DelayLevels(Delay, SpellSlot.W);
					DelayLevels(Delay + 50, SpellSlot.E);
					DelayLevels(Delay + 100, SpellSlot.Q);
				}
				else if (Config.Item("加点方案").GetValue<StringList>().SelectedIndex == 4)//主E副Q
				{
					DelayLevels(Delay, SpellSlot.E);
					DelayLevels(Delay + 50, SpellSlot.Q);
					DelayLevels(Delay + 100, SpellSlot.W);
				}
				else if (Config.Item("加点方案").GetValue<StringList>().SelectedIndex == 5)//主E副W
				{
					DelayLevels(Delay, SpellSlot.E);
					DelayLevels(Delay + 50, SpellSlot.W);
					DelayLevels(Delay + 100, SpellSlot.Q);
				}
			}

			if (!Config.Item("自动加点").GetValue<bool>() && Config.Item("自动点大").GetValue<bool>() 
				&& (Player.Level == 6|| Player.Level == 11|| Player.Level == 16))
			{
				Player.Spellbook.LevelSpell(SpellSlot.R);
			}
		}

		public static void DelayLevels(int time, SpellSlot QWER) {
			Utility.DelayAction.Add(time, () => { Player.Spellbook.LevelSpell(QWER); });
		}

		private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser) {
			if (Config.Item("防突E").GetValue<bool>())
			{
				E.Cast(gapcloser.End);
			}
			if (Config.Item("防突W").GetValue<bool>() && gapcloser.Sender.HasWBuff())
			{
				W.CastSpell(gapcloser.Sender);
			}
		}

		private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args) {
			if (sender.IsMe && args.SData.Name == "JhinR")
			{
				REndPos = args.End;
				if (Config.Item("R放眼").GetValue<bool>()
					&& ( ScryingOrb.IsReady())
					&& HeroManager.Enemies.All(e => !e.InRCone() || !e.IsValid || e.IsDead))
				{
					var bushList = VectorHelper.GetBushInRCone();
					var lpl = VectorHelper.GetLastPositionInRCone();
					if (bushList?.Count > 0)
					{
						if (lpl?.Count > 0)
						{
							var lp = lpl.First(p => Game.Time - p.LastSeen > 2 * 1000);
							if (lp!=null)
							{
								var bush = VectorHelper.GetBushNearPosotion(lp.LastPosition, bushList);
								ScryingOrb.Cast(bush);
							}
							
						}
						else
						{
							var bush = VectorHelper.GetBushNearPosotion(REndPos, bushList);
							ScryingOrb.Cast(bush);
						}
						
					}
					else if (lpl?.Count > 0)
					{
						ScryingOrb.Cast(lpl.First().LastPosition);
					}
				}
			}
			
		}

		private static void Game_OnWndProc(WndEventArgs args) {
			if (args.WParam == Config.Item("半手动R自动").GetValue<KeyBind>().Key && IsCastingR)
			{
				args.Process = false;
			}

			if (!MenuGUI.IsChatOpen && args.WParam == Config.Item("半手动R自动").GetValue<KeyBind>().Key && !IsCastingR && R.IsReady() && RCharge.Target == null)
			{
				var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
				if (t != null && t.IsValid && R.CastSpell(t))
				{
					args.Process = false;
					RCharge.Target = t;
				}
				
			}

		}

		private static Obj_AI_Hero GetTargetInR() {
			var ignoredList = new List<Obj_AI_Hero>();
			foreach (var enemy in HeroManager.Enemies.Where(e => !e.IsValid || e.IsDead || !e.InRCone()))
			{
				ignoredList.Add(enemy);
			}
			var target = TargetSelector.GetTarget(R.Range,TargetSelector.DamageType.Physical,true, ignoredList);
			if (target!=null && target.IsValid && !target.IsDead)
			{
				return target;
			}
			return null;
		}

		private static void AutoR() {
			if (RCharge.Target == null)
			{
				var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
				if (t == null || !t.IsValid)
				{
					return;
				}
				R.Cast(OKTWPrediction.GetPrediction(t, R.Delay).CastPosition);
			}
			if (IsCastingR)
			{
				
				var target = GetTargetInR();
				if (target!=null && R.CastSpell(target))
				{
					
					RCharge.Target = target;
					RCharge.CastT = Game.ClockTime;
					RCharge.Index++;

					Console.WriteLine($"目标{RCharge.Target.Name}\n 上次施放时间{RCharge.CastT}\n 第{RCharge.Index}次R\n是否要放大{RCharge.TapKeyPressed}\n==============");
				}
			}
			else
			{
				RCharge.Target = null;
				RCharge.TapKeyPressed = false;
				RCharge.Index = 0;
				Console.WriteLine($"目标{RCharge.Target.Name}\n 上次施放时间{RCharge.CastT}\n 第{RCharge.Index}次R\n是否要放大{RCharge.TapKeyPressed}\n==============");
			}
			
		}

		private static void Game_OnUpdate(EventArgs args) {

			#region 击杀列表 及 击杀信号提示
			foreach (var enemy in HeroManager.Enemies)
			{
				if (R.CanCast(enemy) && !enemy.IsDead && enemy.IsValid && GetRDmg(enemy) >= enemy.Health)
				{
					if (!KillableList.Contains(enemy))
					{
						KillableList.Add(enemy);
					}

					if (Config.Item("击杀信号提示").GetValue<bool>())
					{
						Game.ShowPing(PingCategory.AssistMe, enemy, true);
					}
				}
				else
				{
					if (KillableList.Contains(enemy))
					{
						KillableList.Remove(enemy);
					}
				}
			}
			#endregion

			#region 其它设置，买蓝眼

			if (Config.Item("买蓝眼").GetValue<bool>() && !ScryingOrb.IsOwned() && (Player.InShop()||Player.InFountain()) && Player.Level >= 9)
			{
				Player.BuyItem(ItemId.Farsight_Orb_Trinket);
			}
			#endregion

			#region 提前结束R时 重置大招次数及目标
			if (!IsCastingR && !R.IsReady())
			{
				RCharge.Index = 0;
				RCharge.Target = null;
			}
			#endregion

			//if (Config.Item("半手动R点射").GetValue<KeyBind>().Active)
			//{
			//	var p = Player.Position;
			//	Console.WriteLine($"位置：new Vector3({p.X},{p.Y},{p.Z})");
   //         }
			
			if (!IsCastingR)
			{
				QLogic();
				WLogic();
				ELogic();
			}
			RLogic();
		}

		private static void ELogic() {
			#region E逻辑
			foreach (var enemy in HeroManager.Enemies)
			{
				#region 硬控E
				if (enemy.IsValidTarget(E.Range + 30) && Config.Item("硬控E").GetValue<bool>() && !OktwCommon.CanMove(enemy))
				{
					E.CastSpell(enemy);
				}
				#endregion

				#region 探草E
				if (enemy.IsDead) continue;
				var path = enemy.GetWaypoints().LastOrDefault().To3D();
				if (!NavMesh.IsWallOfGrass(path, 1)) continue;
				if (enemy.Distance(path) > 200) continue;
				if (NavMesh.IsWallOfGrass(HeroManager.Player.Position, 1) && HeroManager.Player.Distance(path) < 200) continue;

				if (Environment.TickCount - lastwarded > 1000)
				{
					if (E.IsReady() && HeroManager.Player.Distance(path)<E.Range)
					{
						E.Cast(path);
						lastwarded = Environment.TickCount;
					}
				}
				#endregion
			}
			#endregion
			//清兵
			if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
			{
				var minions = MinionManager.GetMinions(E.Range + E.Width,MinionTypes.All,MinionTeam.Enemy,MinionOrderTypes.MaxHealth);
				if (minions?.Count>5)
				{
					var eClear = E.GetCircularFarmLocation(minions, E.Width);
					if (eClear.MinionsHit >= 3)
					{
						E.Cast(eClear.Position);
					}
				}
				
			}
		}

		private static void WLogic() {
			#region W逻辑
			foreach (var enemy in HeroManager.Enemies.Where(e => e.IsValid && !e.IsDead && e.Distance(Player) < W.Range))
			{
				if (Config.Item("标记W").GetValue<bool>()
					&& (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
					&& enemy.CountAlliesInRange(650) > 0
					&& enemy.HasWBuff())
				{
					W.CastSpell(enemy);
				}

				if (Config.Item("硬控W").GetValue<bool>() && !OktwCommon.CanMove(enemy) && enemy.HasWBuff())
				{
					W.CastSpell(enemy);
				}

				if (Config.Item("抢人头W").GetValue<bool>() && enemy.Health < OktwCommon.GetKsDamage(enemy, W)
					&& !Q.CanCast(enemy) && !(Orbwalking.CanAttack() && Orbwalking.InAutoAttackRange(enemy)))
				{
					W.CastSpell(enemy);
				}

			}
			#endregion
		}

		private static void QLogic() {
			#region Q逻辑
			//Q消耗
			//if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
			//{
			//	var t = Orbwalker.GetTarget();
			//	if (t != null)
			//	{
			//		if (t.Type == GameObjectType.obj_AI_Hero)
			//		{
			//			var enemy = t as Obj_AI_Hero;
			//			if (Config.Item("消耗Q").GetValue<bool>() && Q.CanCast(enemy))
			//			{
			//				Q.Cast(enemy);
			//			}
			//		}
			//	}
			//}

			if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
			{
				var target = Orbwalker.GetTarget() as Obj_AI_Hero;
				if (target != null && target.IsValid && !Orbwalking.CanAttack() && !Player.IsWindingUp && target.Health < Q.GetDmg(target) + W.GetDmg(target))
				{
					Q.Cast(target);
				}
			}

			//Q抢人头
			foreach (var enemy in HeroManager.Enemies.Where(e => e.IsValidTarget(Q.Range) && Q.GetDmg(e) > e.Health))
			{
				Q.Cast(enemy);
			}

			#endregion
		}

		private	static void RLogic() {
			#region 自动R逻辑

			/**
			if (Config.Item("半手动R自动").GetValue<KeyBind>().Active && R.IsReady() && RCharge.Target == null)
			{
				var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
				if (t != null && t.IsValid && R.CastSpell(t))
				{
					RCharge.Target = t;
				}
			}
			*/

			if (IsCastingR)
			{
				if (Config.Item("R放眼").GetValue<bool>()
					&& (ScryingOrb.IsReady())
					&& RCharge.Target != null && !RCharge.Target.IsDead && !RCharge.Target.IsVisible)
				{
					var bushList = VectorHelper.GetBushInRCone();
					var lp = VectorHelper.GetLastPositionInRCone().Find(l => l.Hero == RCharge.Target);
					if (bushList?.Count > 0)
					{
						if (lp != null)
						{
							var bush = VectorHelper.GetBushNearPosotion(lp.LastPosition, bushList);
							ScryingOrb.Cast(bush);
						}
						else
						{
							var bush = VectorHelper.GetBushNearPosotion(REndPos, bushList);
							ScryingOrb.Cast(bush);
						}

					}
					else if (lp != null)
					{
						ScryingOrb.Cast(lp.LastPosition);
					}
				}
				/**
				if (Config.Item("R放眼").GetValue<bool>()
					&& (BlueTrinket.IsReady() || ScryingOrb.IsReady())
					&& RCharge.Target != null && !RCharge.Target.IsDead && !RCharge.Target.IsVisible)
				{
					Game.PrintChat("进草眼".ToUTF8());
					Game.PrintChat("目标位置".ToUTF8() + RCharge.Target.Position);
					var bushList = VectorHelper.GetBushInRCone();
					if (bushList?.Count > 0 )
					{
						var bush = VectorHelper.GetBushNearPosotion(RCharge.Target.Position, bushList);
						if (bush == Vector3.Zero)
						{
							BlueTrinket.Cast(RCharge.Target.Position);
							ScryingOrb.Cast(RCharge.Target.Position);
						}
						else
						{
							BlueTrinket.Cast(bush);
							ScryingOrb.Cast(bush);
						}

					}
				}
	*/
				var target = GetTargetInR();
				if (target != null)
				{
					if (RCharge.Index == 0)
					{
						if (R.CastSpell(target))
						{
							RCharge.Target = target;
						}
					}
					else
					{
						Utility.DelayAction.Add(delay[RCharge.Index - 1], () =>
						{
							if (R.CastSpell(target))
							{
								RCharge.Target = target;
							}
						});
					}
				}
				else
				{
					if (Config.Item("R放眼").GetValue<bool>()
						&& (BlueTrinket.IsReady() || ScryingOrb.IsReady())
						&& RCharge.Target != null && !RCharge.Target.IsDead)
					{
						var bushList = VectorHelper.GetBushInRCone();
						var lp = VectorHelper.GetLastPositionInRCone().Find(l => l.Hero == RCharge.Target);
						if (bushList?.Count > 0)
						{
							if (lp != null)
							{
								var bush = VectorHelper.GetBushNearPosotion(lp.LastPosition, bushList);
								ScryingOrb.Cast(bush);
							}
							else
							{
								var bush = VectorHelper.GetBushNearPosotion(REndPos, bushList);
								ScryingOrb.Cast(bush);
							}

						}
						else if (lp != null)
						{
							ScryingOrb.Cast(lp.LastPosition);
						}
					}
				}
			}

			#endregion
		}

		private static float GetRDmg(Obj_AI_Base target) {
			return (IsCastingR || R.IsReady()) ? (5 - RCharge.Index) * (float)R.GetDmg(target) : 0 ;
		}

		private static void Drawing_OnDraw(EventArgs args) {
			#region 范围显示
			var ShowW = Config.Item("W范围").GetValue<Circle>();
			var ShowE = Config.Item("E范围").GetValue<Circle>();
			var ShowR = Config.Item("R范围").GetValue<Circle>();
			var ShowWM = Config.Item("小地图W范围").GetValue<bool>();
			var ShowRM = Config.Item("小地图R范围").GetValue<bool>();

			if (W.IsReady() && ShowW.Active)
			{
				Render.Circle.DrawCircle(Player.ServerPosition, W.Range, ShowW.Color, 2);
			}
			if (W.IsReady() && ShowWM)
			{
				Utility.DrawCircle(Player.ServerPosition, W.Range, ShowW.Color, 2, 30, true);
			}

			if (R.IsReady() && ShowR.Active)
			{
				Render.Circle.DrawCircle(Player.ServerPosition, R.Range, ShowR.Color, 2);
			}
			if (R.IsReady() && ShowRM)
			{
				Utility.DrawCircle(Player.ServerPosition, R.Range, ShowR.Color, 2, 30, true);
			}

			if (E.IsReady() && ShowE.Active)
			{
				Render.Circle.DrawCircle(Player.ServerPosition, E.Range, ShowE.Color, 2);
			}
			#endregion

			var ShowD = Config.Item("大招伤害").GetValue<Circle>();
			DamageIndicator.Enabled = ShowD.Active;
			DamageIndicator.Color = ShowD.Color;

			var ShowT = Config.Item("击杀文本提示").GetValue<Circle>();
			//if (ShowT.Active && KillableList?.Count > 0)
			if (ShowT.Active && KillableList?.Count > 0)
			{
				var killname = "R Kill List\n";
				foreach (var k in KillableList)
				{
					killname += k.Name.ToGBK() + $"({k.ChampionName})\n";
                }

				var KillTextColor = new ColorBGRA
				{
					A = Config.Item("击杀文本提示").GetValue<Circle>().Color.A,
					B = Config.Item("击杀文本提示").GetValue<Circle>().Color.B,
					G = Config.Item("击杀文本提示").GetValue<Circle>().Color.G,
					R = Config.Item("击杀文本提示").GetValue<Circle>().Color.R,
				};

				KillTextFont.DrawText(null,killname,
					(int)(Drawing.Width * ((float)Config.Item("击杀文本X").GetValue<Slider>().Value / 100)),
					(int)(Drawing.Height * ((float)Config.Item("击杀文本Y").GetValue<Slider>().Value / 100)),
					KillTextColor);
			}

			//var ShowK = Config.Item("击杀目标标识").GetValue<Circle>();
			//if (ShowK.Active)
			//{
			//	foreach (var enemy in KillableList)
			//	{
			//		Render.Circle.DrawCircle(enemy.Position, 40, ShowK.Color, 200,true);
			//	}
			//}
		}

		private static void LoadSpell() {
			Q = new Spell(SpellSlot.Q, 600);
			W = new Spell(SpellSlot.W, 2500);
			E = new Spell(SpellSlot.E, 760);
			R = new Spell(SpellSlot.R, 3500);

			W.SetSkillshot(0.75f, 40, float.MaxValue, false, SkillshotType.SkillshotLine);
			E.SetSkillshot(1.3f, 200, 1600, false, SkillshotType.SkillshotCircle);
			R.SetSkillshot(0.2f, 80, 5000, false, SkillshotType.SkillshotLine);
		}

		private static void LoadMenu() {

			Game.PrintChat("Jhin As The Virtuoso".ToHtml(25)+ "Art requires a certain cruelty!".ToHtml(Color.Purple,FontStlye.Cite));

			Config = new Menu("Jhin As The Virtuoso", "JhinAsTheVirtuoso", true);
			Config.AddToMainMenu();

			var OMenu = Config.AddSubMenu(new Menu("Orbwalker", "走砍设置"));
			Orbwalker = new Orbwalking.Orbwalker(OMenu);

			//Q菜单
			var QMenu = Config.AddSubMenu(new Menu("Q Settings","Q设置"));
			QMenu.AddItem(new MenuItem("消耗Q兵", "Q Kill minions to harass enemy").SetValue(true));
			QMenu.AddItem(new MenuItem("消耗Q", "awalys Q harass").SetValue(true));
			QMenu.AddItem(new MenuItem("清兵Q","Q lanclear").SetValue(true));
			QMenu.AddItem(new MenuItem("补刀Q", "Q Farm").SetValue(false));
			QMenu.AddItem(new MenuItem("抢人头Q", "Q KS").SetValue(true));

			//W菜单
			var WMenu = Config.AddSubMenu(new Menu("W Settings", "W设置"));
			WMenu.AddItem(new MenuItem("硬控W", "Auto W CC").SetValue(true));
			WMenu.AddItem(new MenuItem("标记W","W enemy who has W mark").SetValue(true));
			WMenu.AddItem(new MenuItem("抢人头W","W KS").SetValue(true));
			WMenu.AddItem(new MenuItem("防突W", "W anti marked gapcloser ").SetValue(true));
			WMenu.AddItem(new MenuItem("位移W", "W marked dashing enemy").SetValue(true));

			//E菜单
			var EMenu = Config.AddSubMenu(new Menu("E Settings", "E设置"));
			EMenu.AddItem(new MenuItem("硬控E", "Auto E CC").SetValue(true));
			EMenu.AddItem(new MenuItem("防突E", "E Antigap").SetValue(true));
			EMenu.AddItem(new MenuItem("打断E", "E Interrupt").SetValue(true));
			EMenu.AddItem(new MenuItem("探草E", "E Bush Revealer").SetValue(true));
			EMenu.AddItem(new MenuItem("位移E", "E dashing enemy").SetValue(true));

			//R菜单
			var RMenu = Config.AddSubMenu(new Menu("R Settings", "R设置"));
			RMenu.AddItem(new MenuItem("S12", "Move Settings")).SetFontStyle(FS.Bold, DXColor.Orange);
			RMenu.AddItem(new MenuItem("禁止移动", "Disable move/attack when R is casting").SetValue(true));
			RMenu.AddItem(new MenuItem("禁止距离", "Enable move/attack when enemy distence ?").SetValue(new Slider(700,0,2000)));
			RMenu.AddItem(new MenuItem("S13", ""));

			RMenu.AddItem(new MenuItem("S1", "Kill Remind")).SetFontStyle(FS.Bold, DXColor.Orange);
			RMenu.AddItem(new MenuItem("击杀文本提示", "Text Remind").SetValue(new Circle(true, Color.Orange)));
			RMenu.AddItem(new MenuItem("击杀文本X", "Text Position X").SetValue(new Slider(71)));
			RMenu.AddItem(new MenuItem("击杀文本Y", "Text Position Y").SetValue(new Slider(86)));
			RMenu.AddItem(new MenuItem("击杀信号提示", "Local Ping Remind").SetValue(true));
			//RMenu.AddItem(new MenuItem("击杀目标标识", "圆圈标记R可击杀目标").SetValue(new Circle(true, Color.Red)));
			RMenu.AddItem(new MenuItem("S2", ""));

			RMenu.AddItem(new MenuItem("S3", "Semi-manual cast R key")).SetFontStyle(FS.Bold, DXColor.Orange);
			RMenu.AddItem(new MenuItem("半手动R自动", "KeyBind").SetValue(new KeyBind('R',KeyBindType.Press)));
			RMenu.AddItem(new MenuItem("第一次延迟", "Delay Before R2(ms)").SetValue(new Slider(0, 0, 1000)));
			RMenu.AddItem(new MenuItem("第二次延迟", "Delay Before R3(ms)").SetValue(new Slider(0, 0, 1000)));
			RMenu.AddItem(new MenuItem("第三次延迟", "Delay Before R4(ms)").SetValue(new Slider(0, 0, 1000)));
			RMenu.AddItem(new MenuItem("S4", ""));

			//RMenu.AddItem(new MenuItem("S5", "半手动R设置(点射)")).SetFontStyle(FS.Bold, DXColor.Orange);
			//RMenu.AddItem(new MenuItem("半手动R点射", "半手动R(点射)").SetValue(new KeyBind('T', KeyBindType.Press)));
			//RMenu.AddItem(new MenuItem("S6", ""));

			RMenu.AddItem(new MenuItem("R放眼","Auto bule ward when RCasting and target miss").SetValue(true));

			//其它菜单
			var MMenu = Config.AddSubMenu(new Menu("Misc Settings", "其它设置"));
			MMenu.AddItem(new MenuItem("S10", "AutoLevel Settings")).SetFontStyle(FS.Bold, DXColor.Orange);
			MMenu.AddItem(new MenuItem("自动点大", "Auto Level up R").SetValue(true));
			MMenu.AddItem(new MenuItem("自动加点", "Enable Auto Level up").SetValue(false));
			MMenu.AddItem(new MenuItem("加点等级", "Start Level").SetValue(new Slider(3,1,6)));
			MMenu.AddItem(new MenuItem("加点延迟", "Delay before Level up").SetValue(new Slider(700, 0, 2000)));
			MMenu.AddItem(new MenuItem("加点方案", "Level Sequence").SetValue(
				new StringList(new[] {"Q-W-E","Q-E-W","W-Q-E","W-E-Q","E-Q-W","E-W-Q"})));
			
			MMenu.AddItem(new MenuItem("S11", ""));
			MMenu.AddItem(new MenuItem("买蓝眼", "Auto Buy Bule Ward").SetValue(true));

			//显示菜单
			var DMenu = Config.AddSubMenu(new Menu("Draw Settings", "显示设置"));
			DMenu.AddItem(new MenuItem("S7", "Show Range")).SetFontStyle(FS.Bold,DXColor.Orange);
			DMenu.AddItem(new MenuItem("W范围", "W Range").SetValue(new Circle(true, Color.Blue, E.Range)));
			DMenu.AddItem(new MenuItem("小地图W范围", "W Range Minimap").SetValue(false));
			DMenu.AddItem(new MenuItem("E范围", "E Range").SetValue(new Circle(true, Color.Yellow, E.Range)));
			DMenu.AddItem(new MenuItem("R范围", "R Range").SetValue(new Circle(true, Color.YellowGreen, R.Range)));
			DMenu.AddItem(new MenuItem("小地图R范围", "R Range Minimap").SetValue(true));
			DMenu.AddItem(new MenuItem("S8", ""));

			DMenu.AddItem(new MenuItem("S9", "Damage Indicator")).SetFontStyle(FS.Bold, DXColor.Orange);
			DMenu.AddItem(new MenuItem("大招伤害", "Show 4R Damage").SetValue(new Circle(true, Color.Red)));
			//DMenu.AddItem(new MenuItem("连招伤害", "显示连招伤害").SetValue(new Circle(true, Color.Green)));
		}
	}
}
