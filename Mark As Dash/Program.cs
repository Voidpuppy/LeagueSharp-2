using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using CNLib;
using SebbyLib;
using SebbyPredict = SebbyLib.Prediction;
using Orbwalking = LeagueSharp.Common.Orbwalking;

namespace Mark_As_Dash {
	class Program {

		public static Obj_AI_Hero Player => ObjectManager.Player;
		public static Obj_AI_Base MarkTarget { get; set; }
		public static Menu Config { get; set; }
		public static Spell SnowBall { get; set; } = null;
		public static Spell Q, W, E, R;
		public static Font font;
		public static List<Obj_AI_Hero> ignoreList = new List<Obj_AI_Hero>();
		public static TargetSelector.DamageType DamageType { get; set; }

		public static bool IsCN => CNLib.MultiLanguage.IsCN;

		static void Main(string[] args) {
			CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
			TargetSelector.GetSelectedTarget();
		}

		private static void Game_OnGameLoad(EventArgs args) {
			if (!LoadSpell())
			{
				return;
			}
			font = new Font(Drawing.Direct3DDevice, new FontDescription { FaceName = "微软雅黑", Height = 30 });

			LoadMenu();

			Game.OnUpdate += Game_OnUpdate;
			Drawing.OnDraw += Drawing_OnDraw;
		}

		private static void Drawing_OnDraw(EventArgs args) {
			var DrawMarkShow = Config.GetCircle("标记提醒");
			if (Config.GetStringIndex("伤害类型" + Player.ChampionName) == 0)
			{
				font.DrawScreenPercent("[雪球] 必须先设置伤害类型，才能正确获取目标", DrawMarkShow.Color, 50,50);
				return;
			}

			
			if (MarkTarget!=null && DrawMarkShow.Active)
			{
				var MarkTargetName = IsCN ? "标记中目标" : "Marked Enemy";
				var MarkTargetHealth = IsCN ? "目标血量":"Target Health";

				if (MarkTarget.Type == GameObjectType.obj_AI_Hero)
				{
					var target = MarkTarget as Obj_AI_Hero;



					var text = $"{MarkTargetName}：{target.ChampionName.ToCN(IsCN)}({target.Name.ToGBK()})\n"
						+ $"{MarkTargetHealth}：{(int)target.Health}/{(int)target.MaxHealth}     ({(int)target.HealthPercent}%)";

					font.DrawScreenPercent(text,
                        DrawMarkShow.Color,
						50, 60);
				}
				else if(MarkTarget.Type == GameObjectType.obj_AI_Minion)
				{
					var enemies = MarkTarget.GetEnemiesInRange(500);
                    if (enemies.Count<=0)
					{
						font.DrawScreenPercent("标记中小兵附近没有敌人",
							DrawMarkShow.Color,
							50, 60);
					}
					else
					{
						var text = IsCN
							? $"标记中小兵500码内有{enemies.Count}个敌人\n"
							: $"{enemies.Count}enemies nearby marked minion\n";
						if (enemies.Any(e => e.HealthPercent<20))
						{
							text += IsCN? "标记中小兵500码内有残血敌人": "Find low health enemy";
							
							foreach (var item in enemies.Where(e => e.HealthPercent < 20))
							{
								text += $"\n{item.ChampionName.ToCN(IsCN)}({item.Name.ToGBK()})   {(int)item.HealthPercent}%";
							}

							font.DrawScreenPercent(text,
									DrawMarkShow.Color,
									50, 60);
						}
						
					}
					
				}

			}
			var DrawRangeShow = Config.Item("显示范围").GetValue<Circle>();
            if (DrawRangeShow.Active && GetSnowBallState() == SnowBallState.Mark)
			{
				Render.Circle.DrawCircle(Player.Position,SnowBall.Range,DrawRangeShow.Color,2);
			}
			if (Config.GetBool("小地图显示") && GetSnowBallState() == SnowBallState.Mark)
			{
				Utility.DrawCircle(Player.Position, SnowBall.Range, DrawRangeShow.Color, 2, 23, true);
			}
		}

		private static bool MarkByMe(Obj_AI_Base enemy) {
			var mark = enemy.GetBuff("snowballfollowupself");
			if (mark!=null && mark.Caster.IsMe)
			{
				return true;
			}
			mark = enemy.GetBuff("snowballfollowup");
			if (mark != null && mark.Caster.IsMe)
			{
				return true;
			}
			mark = enemy.GetBuff("porothrowfollowup");
			if (mark != null && mark.Caster.IsMe)
			{
				return true;
			}
			return false;
		}

		private static bool InAttackRange(Obj_AI_Hero target) {
			if ((Config.GetStringIndex("伤害类型" + Player.ChampionName) == 1 || Config.GetStringIndex("伤害类型" + Player.ChampionName) == 3) && Orbwalking.InAutoAttackRange(target))
			{
				return true;
			}

			if (Config.GetStringIndex("伤害类型" + Player.ChampionName) == 2 && (Q.CanCast(target) || W.CanCast(target) || E.CanCast(target) || R.CanCast(target) || Orbwalking.InAutoAttackRange(target)))
			{
				return true;
			}
			return false;
		}

		private static void Game_OnUpdate(EventArgs args) {
			if (Config.GetStringIndex("伤害类型" + Player.ChampionName)==0)
			{
				return;
			}


			#region 取雪球标记目标
			foreach (var enemy in ObjectManager.Get<Obj_AI_Base>().Where(o => o.IsEnemy && o.IsValid && !o.IsDead  && (o.Type == GameObjectType.obj_AI_Minion || o.Type == GameObjectType.obj_AI_Hero)))
			{
				if (MarkByMe(enemy))
				{
					MarkTarget = enemy;
					
					break;
				}
				else
				{
					MarkTarget = null;
				}
			}
			
			#endregion

			#region 连招 消耗时 标记目标
			var IsComb = Orbwalking.Orbwalker.Instances.Any(o => o.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
				|| SebbyLib.Orbwalking.Orbwalker.Instances.Any(o => o.ActiveMode == SebbyLib.Orbwalking.OrbwalkingMode.Combo);
			var IsHars = Orbwalking.Orbwalker.Instances.Any(o => o.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
				|| SebbyLib.Orbwalking.Orbwalker.Instances.Any(o => o.ActiveMode == SebbyLib.Orbwalking.OrbwalkingMode.Mixed);
			

			if ((Config.GetBool("连招") && IsComb)
				|| (Config.GetBool("消耗") && IsHars))
			{
				if (GetSnowBallState() == SnowBallState.Mark)
				{
					var Target = TargetSelector.GetTarget(SnowBall.Range, DamageType, false, ignoreList);
					if (Target!=null && !InAttackRange(Target))
					{
						CastMark(Target);
					}
					
                }
			}
			#endregion

			#region 抢人头
			if (Config.GetBool("抢人头"))
			{
				if (GetSnowBallState() == SnowBallState.Mark)
				{
					foreach (var enemy in HeroManager.Enemies.Where(o => o.IsValid && !o.IsDead && o.Distance(Player) <= SnowBall.Range - 10))
					{
						if (enemy.Health < GetSnowBallDmg())
						{
							CastMark(enemy);
						}
					}
				}
				
			}
			

			#endregion

			#region 半手动
			if ((Config.Item("半手动").GetValue<KeyBind>().Active 
				|| Config.Item("一直用").GetValue<KeyBind>().Active) 
				&& GetSnowBallState() == SnowBallState.Mark)
			{
                var Target = TargetSelector.GetTarget(SnowBall.Range, DamageType, false, ignoreList);
				if (Target != null)
				{
					CastMark(Target);
				}
			}

			#endregion
		}

		private static double GetSnowBallDmg() {
			if (GetSnowBallState() == SnowBallState.Mark)
			{
				if ((int)Game.Type == 6)
				{
					return 20 + (10 * Player.Level);
				}
				else if (Game.Type == GameType.ARAM)
				{
					return 10 + (5 * Player.Level);
				}
			}
			return 0;
		}

		private enum SnowBallState {
			Cooldown,
			Mark,
			Dash
		}

		private static SnowBallState GetSnowBallState() {
			if (!SnowBall.IsReady())
			{
				return SnowBallState.Cooldown;
			}
			else
			{
				if ("summonersnowball" == SnowBall.Instance.Name.ToLower() 
					|| "summonerporothrow" == SnowBall.Instance.Name.ToLower())
				{
					return SnowBallState.Mark;
				}
				else
				{
					return SnowBallState.Dash;
				}
				
			}
		}

		private static TargetSelector.DamageType GetDamageType() {
			var DamageTypeList = new List<TargetSelector.DamageType>
			{
				TargetSelector.DamageType.Magical,
				TargetSelector.DamageType.Physical,
				TargetSelector.DamageType.True
			};
			var TypeIndex = Config.GetStringIndex("伤害类型" + Player.ChampionName);
			return DamageTypeList[TypeIndex - 1];

		}

		private static bool LoadSpell() {
			
			var slotARAM = Player.GetSpellSlot("SummonerSnowball");
			var slotPORO = Player.GetSpellSlot("SummonerPorothrow");
			if (slotARAM != SpellSlot.Unknown)
			{
				SnowBall = new Spell(slotARAM, 1450);
			}
			else if (slotPORO != SpellSlot.Unknown)
			{
				SnowBall = new Spell(slotPORO, 2450);
			}
			else
			{
				return false;
			}
			SnowBall.SetSkillshot(0.33f, 50f, 1600, true, SkillshotType.SkillshotLine);

			Q = new Spell(SpellSlot.Q);
			W = new Spell(SpellSlot.W);
			E = new Spell(SpellSlot.E);
			R = new Spell(SpellSlot.R);
			return true;

		}

		private static void LoadMenu() {

			Config = MenuExtensions.CreatMainMenu("AsMarkDash", "晴依扔雪球");

			var ListMenu = Config.AddMenu("砸雪球名单", "砸雪球名单");
			foreach (var enemy in HeroManager.Enemies)
			{
				ListMenu.AddBool("名单" + enemy.NetworkId, enemy.ChampionName.ToCN(IsCN) + "(" + enemy.Name.ToGBK() + ")",true).ValueChanged += Program_ValueChanged; 
			}

			var PredictConfig = Config.AddMenu("预判设置", "预判设置");
			PredictConfig.AddStringList("预判模式", "预判模式", new[] { "基本库", "OKTW" }, 1);
			PredictConfig.AddStringList("命中率", "命中率", new[] { "非常高", "高", "一般" });

			Config.AddStringList("伤害类型" + Player.ChampionName, "选择自己的主要伤害", new[] { "未设置", "物理伤害", "魔法伤害", "真实伤害" }).ValueChanged += DamageType_ValueChanged;

			Config.AddBool("抢人头", "抢人头",true);

			Config.AddBool("连招","连招时使用",true);
			Config.AddBool("消耗", "消耗时使用", false);
			Config.AddKeyBind("半手动", "半手动施放", 'G', KeyBindType.Press);
			Config.AddKeyBind("一直用", "一直使用", 'O', KeyBindType.Toggle);

			Config.AddCircle("显示范围", "显示范围",true,Color.GreenYellow);
			Config.AddBool("小地图显示", "小地图显示范围");
			Config.AddCircle("标记提醒", "标记提醒", true, Color.OrangeRed);

			Config.AddBool("调试", "调试");


			if (Config.GetStringIndex("伤害类型" + Player.ChampionName) == 0)
			{
				Game.PrintChat("[雪球] 必须先设置伤害类型，才能正确获取目标".ToHtml(Color.Orange));
			}
		}

		private static void DamageType_ValueChanged(object sender, OnValueChangeEventArgs e) {
			switch (e.GetNewValue<StringList>().SelectedIndex)
			{
				case 1:
					DamageType = TargetSelector.DamageType.Physical;
					break;
				case 2:
					DamageType = TargetSelector.DamageType.Magical;
					break;
				case 3:
					DamageType = TargetSelector.DamageType.True;
					break;
				default:
					DamageType = TargetSelector.DamageType.Physical;
					break;
			}
		}

		private static void Program_ValueChanged1(object sender, OnValueChangeEventArgs e) {
			throw new NotImplementedException();
		}

		public static bool CastMark(Obj_AI_Hero target) {
			if (Config.GetBool("调试"))
			{
				DeBug.Debug("[释放技能]", $"目标 {target.Name.ToUTF8()}", DebugLevel.Warning, Output.ChatBox);
				DeBug.Debug("[释放技能]", $"模式 {Config.GetStringList("预判模式").SelectedValue}", DebugLevel.Warning, Output.ChatBox);
				DeBug.Debug("[释放技能]", $"命中率 {Config.GetStringList("命中率").SelectedValue}", DebugLevel.Warning, Output.ChatBox);
			}



			var hitChangceIndex = Config.GetStringIndex("命中率");
			var PredictMode = Config.GetStringIndex("预判模式");


			if (PredictMode == 0)
			{
				var hitChangceList = new[] { HitChance.VeryHigh, HitChance.High, HitChance.Medium };
				return SnowBall.CastIfHitchanceEquals(target, hitChangceList[hitChangceIndex]);
			}
			else if (PredictMode == 1)
			{
				var hitChangceList = new[] { SebbyPredict.HitChance.VeryHigh, SebbyPredict.HitChance.High, SebbyPredict.HitChance.Medium };
				return CastSpell(SnowBall,target, hitChangceList[hitChangceIndex]);
			}
			return false;
		}

		public static bool CastSpell(Spell spell, Obj_AI_Base target, SebbyLib.Prediction.HitChance hitChance) {
			SebbyLib.Prediction.SkillshotType CoreType2 = SebbyLib.Prediction.SkillshotType.SkillshotLine;
			bool aoe2 = false;

			if (spell.Type == SkillshotType.SkillshotCircle)
			{
				CoreType2 = SebbyLib.Prediction.SkillshotType.SkillshotCircle;
				aoe2 = true;
			}

			if (spell.Width > 80 && !spell.Collision)
				aoe2 = true;

			var predInput2 = new SebbyLib.Prediction.PredictionInput
			{
				Aoe = aoe2,
				Collision = spell.Collision,
				Speed = spell.Speed,
				Delay = spell.Delay,
				Range = spell.Range,
				From = HeroManager.Player.ServerPosition,
				Radius = spell.Width,
				Unit = target,
				Type = CoreType2
			};
			var poutput2 = SebbyLib.Prediction.Prediction.GetPrediction(predInput2);

			if (spell.Speed != float.MaxValue && OktwCommon.CollisionYasuo(HeroManager.Player.ServerPosition, poutput2.CastPosition))
				return false;

			if (poutput2.Hitchance >= hitChance)
			{
				return spell.Cast(poutput2.CastPosition);
			}
			else if (predInput2.Aoe && poutput2.AoeTargetsHitCount > 1 && poutput2.Hitchance >= SebbyLib.Prediction.HitChance.High)
			{
				return spell.Cast(poutput2.CastPosition);
			}

			return false;
		}

		private static void Program_ValueChanged(object sender, OnValueChangeEventArgs e) {
		
			var menuItem = sender as MenuItem;
			foreach (var enemy in HeroManager.Enemies)
			{
				if (menuItem.Name == "名单" + enemy.NetworkId)
				{
					if (e.GetNewValue<bool>())
					{
						ignoreList.Add(enemy);
					}
					else
					{
						ignoreList.Remove(enemy);
					}
					
					break;
				}
			}
			Console.WriteLine("ignoreList.Count:"+ ignoreList.Count);
		}
	}
}
