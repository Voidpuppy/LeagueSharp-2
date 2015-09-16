namespace ElRengar
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ElRengar.Config;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Enumerations;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;
    using LeagueSharp.SDK.Core.Wrappers;
    using LeagueSharp.SDK.Core.Events;
    using LeagueSharp.SDK.Core.Utils;

    using SharpDX;

    using Color = System.Drawing.Color;

    internal class Rengar : Standards
    {
        #region Static Fields

        protected static int LastSwitch;
        public static Dictionary<Spells, Spell> spells = new Dictionary<Spells, Spell>()
        {
            { Spells.Q, new Spell(SpellSlot.Q, 250) },
            { Spells.W, new Spell(SpellSlot.W, 500) },
            { Spells.E, new Spell(SpellSlot.E, 1000) },
            { Spells.R, new Spell(SpellSlot.R, 2000) }
        };

        #endregion

        #region Public Methods and Operators

        public static void OnLoad(object sender, EventArgs e)
        {
            if (Player.CharData.BaseSkinName != "Rengar")
            {
                Console.WriteLine("Champion is not supported.");
                return;
            }
            try
            {

                spells[Spells.E].SetSkillshot(0.25f, 70f, 1500f, true, SkillshotType.SkillshotLine);

                CreateMenu();

                Game.OnUpdate += OnUpdate;
                Orbwalker.OnAction += OnAction;
                Dash.OnDash += OnDash;
                Drawing.OnDraw += OnDraw;
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        #endregion


        private static void OnUpdate(EventArgs args)
        {
            spells[Spells.R].Range = 1000 + spells[Spells.R].Level * 1000;

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Orbwalk:
                    DoCombo();
                    break;

                case OrbwalkerMode.LastHit:
                    DoLastHit();
                    break;

                case OrbwalkerMode.LaneClear:
                    DoLaneClear();
                    DoJungleClear();
                    break;

                case OrbwalkerMode.Hybrid:
                    DoHybrid();
                    break;
            }

            //Console.WriteLine("Buffs: {0}", string.Join(" | ", Player.Buffs.Select(b => b.DisplayName)));
            NotificationHandler();
            HealHandler();

        }

        #region Methods

        private static void DoCombo()
        {
            var target = TargetSelector.GetTarget(spells[Spells.W].Range);
            if (target == null || !target.IsValidTarget())
            {
                return;
            }

            var useQ = menu["combo.settings"]["combo.spell.q"].GetValue<MenuBool>().Value;
            var useW = menu["combo.settings"]["combo.spell.w"].GetValue<MenuBool>().Value;
            var useE = menu["combo.settings"]["combo.spell.e"].GetValue<MenuBool>().Value;
            var useEoutOfRange = menu["combo.settings"]["combo.spell.e.outofrange"].GetValue<MenuBool>().Value;
            var prioritized = menu["combo.settings"]["combo.prioritize"].GetValue<MenuList>();

            if (Felicity <= 4)
            {
                if (useQ && spells[Spells.Q].IsReady() && Player.Distance(target) < spells[Spells.Q].Range)
                {
                    spells[Spells.Q].Cast();
                }

                if (RengarR) return;

                if (useW && spells[Spells.W].IsReady() && Vector3.Distance(Player.ServerPosition, target.ServerPosition) <= spells[Spells.W].Range * 0x1 / 0x3)
                {
                    spells[Spells.W].Cast();
                }

                if (useE && spells[Spells.E].IsReady() && !Player.IsDashing() && Player.Distance(target.ServerPosition) < spells[Spells.E].Range)
                {
                    spells[Spells.E].Cast(target);
                }

                if (Felicity == 4)
                {
                    if (!spells[Spells.Q].IsReady() && !spells[Spells.W].IsReady() && spells[Spells.E].IsReady())
                    {
                        spells[Spells.E].Cast(target);
                    }
                }
            }

            if (Felicity == 5)
            {
                switch (prioritized.Index)
                {
                    case 0:
                        if (useQ && spells[Spells.Q].IsReady() && Player.Distance(target) < spells[Spells.Q].Range + 0x32)
                        {
                            spells[Spells.Q].Cast();
                        }
                        break;

   
                    case 2:
                        if (useE && spells[Spells.E].IsReady() && Player.Distance(target.ServerPosition) < spells[Spells.E].Range && !RengarR)
                        {
                            //waiting for prediction
                            spells[Spells.E].Cast(target);
                        }
                        break;
                }

                if (useEoutOfRange && Player.Distance(target.ServerPosition) > spells[Spells.Q].Range + 0x64)
                {
                    spells[Spells.E].Cast(target);
                }
            }

            if (!Player.IsDashing() && Vector3.Distance(Player.ServerPosition, target.ServerPosition) <= 380)
            {
                if (Items.CanUseItem(3074)) Items.UseItem(3074); //Hydra
                if (Items.CanUseItem(3077)) Items.UseItem(3077); //Tiamat
            }
 
            if (Player.Distance(target.ServerPosition) <= 400f)
            {
                ItemHandler(target);
            }
        }
        #region DoHybrid
        private static void DoHybrid()
        {
            var target = TargetSelector.GetTarget(spells[Spells.E].Range);
            if (target == null || !target.IsValidTarget())
            {
                return;
            }

            var useQ = menu["harass.settings"]["harass.spell.q"].GetValue<MenuBool>().Value;
            var useW = menu["harass.settings"]["harass.spell.w"].GetValue<MenuBool>().Value;
            var useE = menu["harass.settings"]["harass.spell.e"].GetValue<MenuBool>().Value;
            var prioritized = menu["harass.settings"]["harass.prioritize"].GetValue<MenuList>();

            if (Felicity <= 4)
            {
                if (useQ && spells[Spells.Q].IsReady() && Player.Distance(target) < spells[Spells.Q].Range + 0x1e)
                {
                    spells[Spells.Q].Cast();
                }

                if (useE && spells[Spells.E].IsReady() && Player.Distance(target.ServerPosition) < spells[Spells.E].Range)
                {
                    //waiting for prediction
                    spells[Spells.E].Cast(target);
                }

                if (useW && spells[Spells.W].IsReady() && Vector3.Distance(Player.ServerPosition, target.ServerPosition) < spells[Spells.W].Range * 0x1 / 0x3)
                {
                    spells[Spells.W].Cast();
                }
            }

            if (Felicity == 5)
            {
                switch (prioritized.Index)
                {
                    case 0:
                        if (useQ && spells[Spells.Q].IsReady()
                            && Player.Distance(target) < spells[Spells.Q].Range + 0x32)
                        {
                            spells[Spells.Q].Cast();
                        }
                        break;

                    case 1:
                        if (useW && spells[Spells.W].IsReady() &&  Vector3.Distance(Player.ServerPosition, target.ServerPosition) < spells[Spells.W].Range * 0x1 / 0x3)
                        {
                            spells[Spells.W].Cast();
                        }
                        break;

                    case 2:
                        if (useE && spells[Spells.E].IsReady() && Vector3.Distance(Player.ServerPosition, target.ServerPosition) <= LeapRange)
                        {
                            //waiting for prediction
                            spells[Spells.E].Cast(target);
                        }
                        break;
                }
            }
        }
        #endregion

        #region DoJungleClear
        private static void DoJungleClear()
        {
            var useQ = menu["jungleclear.settings"]["jungleclear.spell.q"].GetValue<MenuBool>().Value;
            var useW = menu["jungleclear.settings"]["jungleclear.spell.w"].GetValue<MenuBool>().Value;
            var useE = menu["jungleclear.settings"]["jungleclear.spell.e"].GetValue<MenuBool>().Value;
            var useHydra = menu["jungleclear.settings"]["jungleclear.items.hydra"].GetValue<MenuBool>().Value;
            var saveFerocity = menu["jungleclear.settings"]["jungleclear.save.ferocity"].GetValue<MenuBool>().Value;
            var prioritized = menu["jungleclear.settings"]["jungleclear.prioritize"].GetValue<MenuList>();

            var minions =
                GameObjects.Jungle.Where(m => m.IsValid && m.Distance(Player) < spells[Spells.W].Range).ToList();

            if (!minions.Any())
                return;

            if (Felicity <= 4)
            {
                if (useQ && spells[Spells.Q].IsReady()
                    && Player.Distance(minions[0]) < spells[Spells.Q].Range + 0x32)
                {
                    spells[Spells.Q].Cast();
                }

                if (useW && spells[Spells.W].IsReady() && minions.Count() > 1 && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < spells[Spells.W].Range * 0x1 / 0x3)
                {
                    spells[Spells.W].Cast(minions[0], false, true);
                }

                if (useE && spells[Spells.E].IsReady())
                {
                    spells[Spells.E].Cast(minions[0]);
                }
            }

            if (Felicity == 5)
            {
                if (saveFerocity) return;

                switch (prioritized.Index)
                {
                    case 0:
                        if (useQ && spells[Spells.Q].IsReady()
                            && Player.Distance(minions[0]) < spells[Spells.Q].Range + 0x32)
                        {
                            spells[Spells.Q].Cast();
                        }
                        break;

                    case 1:
                        if (useW && spells[Spells.W].IsReady() && minions.Count() > 1 && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < spells[Spells.W].Range * 0x1 / 0x3)
                        {
                            spells[Spells.W].Cast(minions[0], false, true);
                        }
                        break;

                    case 2:
                        if (useE && spells[Spells.E].IsReady())
                        {
                            spells[Spells.E].Cast(minions[0]);
                        }
                        break;
                }
            }

            if (useHydra && Items.CanUseItem(3074) || Items.CanUseItem(3077) && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < 400f && minions.Count() > 3)
            {
                Items.UseItem(3074); Items.UseItem(3077);
            }
        }
        #endregion

        #region DoLaneClear
        private static void DoLaneClear()
        {
            var useQ = menu["laneclear.settings"]["laneclear.spell.q"].GetValue<MenuBool>().Value;
            var useW = menu["laneclear.settings"]["laneclear.spell.w"].GetValue<MenuBool>().Value;
            var useE = menu["laneclear.settings"]["laneclear.spell.e"].GetValue<MenuBool>().Value;
            var useHydra = menu["laneclear.settings"]["laneclear.items.hydra"].GetValue<MenuBool>().Value;
            var saveFerocity = menu["laneclear.settings"]["laneclear.save.ferocity"].GetValue<MenuBool>().Value;
            var prioritized = menu["laneclear.settings"]["laneclear.prioritize"].GetValue<MenuList>();

            var minions =
                GameObjects.EnemyMinions.Where(m => m.IsValid && m.Distance(Player) < spells[Spells.W].Range).ToList();

            if (!minions.Any())
                return;

            if (Felicity <= 4)
            {
                if (useQ && spells[Spells.Q].IsReady()
                    && Player.Distance(minions[0]) < spells[Spells.Q].Range + 0x32)
                {
                    spells[Spells.Q].Cast();
                }

                if (useW && spells[Spells.W].IsReady() && minions.Count() > 2 && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < spells[Spells.W].Range * 0x1 / 0x3)
                {
                    spells[Spells.W].Cast(minions[0], false, true);
                }

                if (useE && spells[Spells.E].IsReady())
                {
                    spells[Spells.E].Cast(minions[0]);
                }
            }

            if (Felicity == 5)
            {
                if (saveFerocity) return;

                switch (prioritized.Index)
                {
                    case 0:
                        if (useQ && spells[Spells.Q].IsReady()
                            && Player.Distance(minions[0]) < spells[Spells.Q].Range + 0x32)
                        {
                            spells[Spells.Q].Cast();
                        }
                        break;

                    case 1:
                        if (useW && spells[Spells.W].IsReady() && minions.Count() > 2 && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < spells[Spells.W].Range * 0x1 / 0x3)
                        {
                            spells[Spells.W].Cast(minions[0], false, true);
                        }
                        break;

                    case 2:
                        if (useE && spells[Spells.E].IsReady())
                        {
                            spells[Spells.E].Cast(minions[0]);
                        }
                        break;
                }
            }

            if (useHydra && Items.CanUseItem(3074) || Items.CanUseItem(3077) && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < 400f && minions.Count() > 3)
            {
                Items.UseItem(3074); Items.UseItem(3077);
            }

            if (menu["misc.settings"]["misc.debug.active"].GetValue<MenuBool>().Value)
            {
                Drawing.DrawText(
                    Drawing.Width * 0.44f, Drawing.Height * 0.80f, System.Drawing.Color.Red, "Minions in range: " + minions.Count().ToString());

                Console.WriteLine(minions.Count());
            }
        }
        #endregion

        private static void DoLastHit()
        {
          
        }

        private static void HealHandler()
        {
            if (Player.IsRecalling() || Player.InFountain() || Felicity <= 4)
                return;

            var useW = menu["heal.settings"]["heal.activated"].GetValue<MenuBool>().Value;
            var playerHealth = menu["heal.settings"]["heal.player.hp"].GetValue<MenuSlider>().Value;

            if (useW && (Player.Health / Player.MaxHealth) * 100 <= playerHealth && spells[Spells.W].IsReady())
            {
                spells[Spells.W].Cast();
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name == "RengarR" && Items.CanUseItem(3142))
                {
                    DelayAction.Add(0x5dc, () => Items.UseItem(3142));
                }
            }
        }

        #region OnAction
        private static void OnAction(object sender, Orbwalker.OrbwalkerActionArgs orbwalk)
        {
            var target = orbwalk.Target;
            if (!target.IsValidTarget())
                return;

            if (orbwalk.Type == OrbwalkerType.AfterAttack && spells[Spells.Q].IsReady() && target.IsValidTarget() && Felicity == 0x5 && ActiveMode == OrbwalkerMode.Hybrid || ActiveMode == OrbwalkerMode.Orbwalk)
            {
                spells[Spells.Q].Cast();
            }
        }
        #endregion

        private static void OnDraw(EventArgs args)
        {
            var drawNone = menu["misc.settings"]["misc.drawing.deactivate"].GetValue<MenuBool>().Value;
            var drawQ = menu["misc.settings"]["misc.drawing.draw.spell.q"].GetValue<MenuBool>().Value;
            var drawW = menu["misc.settings"]["misc.drawing.draw.spell.w"].GetValue<MenuBool>().Value;
            var drawE = menu["misc.settings"]["misc.drawing.draw.spell.e"].GetValue<MenuBool>().Value;
            var drawR = menu["misc.settings"]["misc.drawing.draw.spell.r"].GetValue<MenuBool>().Value;
            var canJump = menu["misc.settings"]["misc.drawing.draw.helper.canjump"].GetValue<MenuBool>().Value;

            var target = TargetSelector.GetTarget(spells[Spells.E].Range);
            if (target == null || !target.IsValidTarget())
            {
                return;
            }

            if (HasPassive && canJump)
            {
                Drawing.DrawText(target.HPBarPosition.X + 40, target.HPBarPosition.Y - 5, Color.Green, "璺宠繃鍘绘墦浠栵紒");
            }

            if (drawNone) return;

            if (drawQ && Rengar.spells[Spells.Q].Level > 0)
                Drawing.DrawCircle(GameObjects.Player.Position, Rengar.spells[Spells.Q].Range, Color.Blue);

            if (drawW && Rengar.spells[Spells.W].Level > 0)
                Drawing.DrawCircle(GameObjects.Player.Position, Rengar.spells[Spells.W].Range, Color.Blue);

            if (drawE && Rengar.spells[Spells.E].Level > 0)
                Drawing.DrawCircle(GameObjects.Player.Position, Rengar.spells[Spells.E].Range, Color.Blue);

            if (drawR && Rengar.spells[Spells.R].Level > 0)
                Drawing.DrawCircle(GameObjects.Player.Position, Rengar.spells[Spells.R].Range, Color.Blue);
        }

        private static void OnDash(object sender, Dash.DashArgs e)
        {
            if (e.Unit.IsMe)
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.Orbwalk || Orbwalker.ActiveMode == OrbwalkerMode.Hybrid)
                {
                    var prioritized = menu["combo.settings"]["combo.prioritize"].GetValue<MenuList>();
                    var target = TargetSelector.GetTarget(spells[Spells.E].Range);
                    if (target == null || !target.IsValidTarget())
                    {
                        return;
                    }

                    switch (prioritized.Index)
                    {
                        case 0:
                            if (spells[Spells.Q].IsReady() && Player.Distance(target) < spells[Spells.Q].Range + 0x32)
                            {
                                spells[Spells.Q].Cast();
                            }
                            break;

                        case 1:
                            if (spells[Spells.W].IsReady() && Vector3.Distance(Player.ServerPosition, target.ServerPosition) < spells[Spells.W].Range * 0x1 / 0x3)
                            {
                                spells[Spells.W].Cast();
                            }
                            break;

                        case 2:
                            if (spells[Spells.E].IsReady() && Vector3.Distance(Player.ServerPosition, target.ServerPosition) <= LeapRange)
                            {
                                spells[Spells.E].Cast(target);
                            }
                            break;
                    }

                    ItemHandler(target); 
                }
            }
            else
            {
                return;
            }
        }

        #endregion
    }
}
