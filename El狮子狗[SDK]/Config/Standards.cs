// This file is part of LeagueSharp.Common.
// 
// LeagueSharp.Common is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// LeagueSharp.Common is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with LeagueSharp.Common.  If not, see <http://www.gnu.org/licenses/>.
namespace ElRengar.Config
{
    #region

    using System;
    using System.Windows.Forms;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Enumerations;
    using LeagueSharp.SDK.Core.Events;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;
    using LeagueSharp.SDK.Core.UI.INotifications;
    using LeagueSharp.SDK.Core.Wrappers;

    using SharpDX;

    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

    #endregion

    public enum Spells
    {
        Q,

        W,

        E,

        R
    }

    internal class Standards
    {
        #region Static Fields

        public static Menu menu;

        public static OrbwalkerMode ActiveMode { get; set; }

        private static Notification notification;

        public static SpellSlot Ignite;

        public static SpellSlot Smite;

        public static Items.Item Botrk, Cutlass;

        public static String ScriptVersion { get { return typeof(Rengar).Assembly.GetName().Version.ToString(); } }

        #endregion

        #region Public Properties

        public static int Felicity //Felicity Smoak makes me Rengarrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr
        {
            get
            {
                return (int)ObjectManager.Player.Mana;
            }
        }

        public static Obj_AI_Hero Player
        {
            get
            {
                return ObjectManager.Player;
            }
        }
        public static bool HasPassive
        {
            get
            {
                return Player.HasBuff("rengarpassivebuff");
            }
        }

        public static bool RengarQbuffMax //riot is retarded lmao kappahd 
        {
            get
            {
                return Player.HasBuff("RengarQbuffMAX");
            }
        }

        //
        public static bool Rengartrophyicon6 //riot is retarded lmao kappahd 
        {
            get
            {
                return Player.HasBuff("rengarbushspeedbuff");
            }
        }

        public static int LeapRange
        {
            get
            {
                if (HasPassive && Rengartrophyicon6)
                {
                    return 725;
                }

                return 600;
            }
        }

        public static bool RengarR
        {
            get
            {
                return Player.HasBuff("rengarr");
            }
        }

        #endregion

        #region Methods

        protected static void ItemHandler(Obj_AI_Base target)
        {
            if (Player.IsDashing() || !target.IsValidTarget())
            {
                return;
            }

            /*if (Items.CanUseItem(3074)) Items.UseItem(3074); //Hydra
            if (Items.CanUseItem(3077)) Items.UseItem(3077); //Tiamat*/

            //Cutlass = new Items.Item(3144, 450f);
           // Botrk = new Items.Item(3153, 450f);

            if (Items.CanUseItem(3144)) Items.UseItem(3144); //Cutlass
            if (Items.CanUseItem(3153)) Items.UseItem(3153); //Botrk
            if (Items.CanUseItem(3142)) Items.UseItem(3142); //Ghostblade 

            /*new Items.Item(3144, 450f).Cast(target);
            new Items.Item(3153, 450f).Cast(target);*/

            Console.WriteLine("Casted");
        }

        protected static void NotificationHandler()
        {
            //Waiting for old Notifications
        }

        /*
        * PLEASE THIS MENU IS PATENTED BY AUSTRALIAN IP LAWS.
        * KEEP CALM AND (); {} ON
        */

        protected static void CreateMenu()
        {
            menu = new Menu("ElRengar", "EL狮子狗", true);
            //This will be a loader functionality later on
            Bootstrap.Init(null); 

            var comboMenu = new Menu("combo.settings", "连招设置");
            {
                comboMenu.Add(new MenuSeparator("General", "通常"));
                comboMenu.Add(new MenuBool("combo.spell.q", "使用 Q", true));
                comboMenu.Add(new MenuBool("combo.spell.w", "使用 W", true));
                comboMenu.Add(new MenuBool("combo.spell.e", "使用 E", true));
                comboMenu.Add(new MenuBool("combo.spell.e.outofrange", "A不到用E"));
                comboMenu.Add(new MenuSeparator("Miscellaneous", "其它"));
                comboMenu.Add(new MenuBool("combo.spell.ignite", "使用点燃", true));
                comboMenu.Add(new MenuBool("combo.spell.smite", "使用惩戒", true));
                comboMenu.Add(new MenuSeparator("Prioritized", "优先"));
                //comboMenu.Add(new MenuList<string>("combo.prioritize", "优先使用技能", new[] { "Q", "E" }));

                comboMenu.Add(new MenuList<string>("combo.prioritize", "优先使用技能", new[] { "Q", "E" })
                {
                    SelectedValue = "Q"
                });

                menu.Add(comboMenu);
            }

            var harassMenu = new Menu("harass.settings", "消耗设置");
            {
                harassMenu.Add(new MenuSeparator("General", "通常"));
                harassMenu.Add(new MenuBool("harass.spell.q", "使用 Q", true));
                harassMenu.Add(new MenuBool("harass.spell.w", "使用 W", true));
                harassMenu.Add(new MenuBool("harass.spell.e", "使用 E", true));
                harassMenu.Add(new MenuSeparator("Prioritized", "优先"));
                harassMenu.Add(new MenuList<string>("harass.prioritize", "优先使用技能", new[] { "Q", "W", "E" }));

                menu.Add(harassMenu);
            }

            var laneclearMenu = new Menu("laneclear.settings", "清线设置");
            {
                laneclearMenu.Add(new MenuSeparator("General", "通常"));
                laneclearMenu.Add(new MenuBool("laneclear.spell.q", "使用 Q", true));
                laneclearMenu.Add(new MenuBool("laneclear.spell.w", "使用 W", true));
                laneclearMenu.Add(new MenuBool("laneclear.spell.e", "使用 E", false));
                laneclearMenu.Add(new MenuSeparator("Items", "道具"));
                laneclearMenu.Add(new MenuBool("laneclear.items.hydra", "贪欲九头蛇", true));
                laneclearMenu.Add(new MenuSeparator("Ferocity", "怒气"));
                laneclearMenu.Add(new MenuBool("laneclear.save.ferocity", "少用怒气", true));
                laneclearMenu.Add(new MenuList<string>("laneclear.prioritize", "优先使用技能", objects: new[] { "Q", "W", "E" }));

                menu.Add(laneclearMenu);
            }

            var jungleClearMenu = new Menu("jungleclear.settings", "清野设置");
            {
                jungleClearMenu.Add(new MenuSeparator("General", "通常"));
                jungleClearMenu.Add(new MenuBool("jungleclear.spell.q", "使用 Q", true));
                jungleClearMenu.Add(new MenuBool("jungleclear.spell.w", "使用 W", true));
                jungleClearMenu.Add(new MenuBool("jungleclear.spell.e", "使用 E", false));
                jungleClearMenu.Add(new MenuSeparator("Items", "道具"));
                jungleClearMenu.Add(new MenuBool("jungleclear.items.hydra", "贪欲九头蛇", true));
                jungleClearMenu.Add(new MenuSeparator("Ferocity", "怒气"));
                jungleClearMenu.Add(new MenuBool("jungleclear.save.ferocity", "少用怒气", true));
                jungleClearMenu.Add(new MenuList<string>("jungleclear.prioritize", "优先使用技能", objects: new[] { "Q", "W", "E" }));

                menu.Add(jungleClearMenu);
            }

            var healMenu = new Menu("heal.settings", "治疗设置");
            {
                healMenu.Add(new MenuSeparator("General", "通常"));
                healMenu.Add(new MenuBool("heal.activated", "使用 W", true));
                healMenu.Add(new MenuSlider("heal.player.hp", "生命值%", value: 25, minValue: 1, maxValue: 100));

                menu.Add(healMenu);
            }

            var miscMenu = new Menu("misc.settings", "其它设置");
            {
                miscMenu.Add(new MenuSeparator("Notifications", "通知"));
                miscMenu.Add(new MenuBool("misc.notifications", "小菜单显示优先技能", true));

                miscMenu.Add(new MenuSeparator("Items", "道具"));
                miscMenu.Add(new MenuBool("misc.items.tiamat", "提亚马特", true));
                miscMenu.Add(new MenuBool("misc.items.hydra", "贪欲九头蛇", true));
                miscMenu.Add(new MenuBool("misc.items.ghostblade", "幽梦", true));
                miscMenu.Add(new MenuBool("misc.items.cutlass", "锈水弯刀", true));
                miscMenu.Add(new MenuBool("misc.items.botrk", "破败", true));

                miscMenu.Add(new MenuSeparator("Drawings", "显示"));
                miscMenu.Add(new MenuBool("misc.drawing.deactivate", "禁用所有显示"));
                miscMenu.Add(new MenuBool("misc.drawing.draw.spell.q", "Q 范围", false));
                miscMenu.Add(new MenuBool("misc.drawing.draw.spell.w", "W 范围", false));
                miscMenu.Add(new MenuBool("misc.drawing.draw.spell.e", "E 范围", false));
                miscMenu.Add(new MenuBool("misc.drawing.draw.spell.r", "R 范围", false));
                miscMenu.Add(new MenuBool("misc.drawing.draw.helper.canjump", "跳跃提示", true));


                miscMenu.Add(new MenuSeparator("Debug", "调试"));
                miscMenu.Add(new MenuBool("misc.debug.active", "调试", false));

                menu.Add(miscMenu);
            }

            var creditsMenu = new Menu("credits.settings", "捐助方式");
            {
                creditsMenu.Add(new MenuSeparator("credits.title.1", "如果你喜欢这个脚本可以捐助这个via PayPal账号:"));
                creditsMenu.Add(new MenuSeparator("credits.title.2", "Info@zavox.nl"));
                creditsMenu.Add(new MenuSeparator("credits.title.3", "  "));
                creditsMenu.Add(new MenuSeparator("credits.title.4", "本脚本作者:"));
                creditsMenu.Add(new MenuSeparator("credits.title.5", "jQuery"));
                creditsMenu.Add(new MenuSeparator("credits.title.6", " "));
                creditsMenu.Add(new MenuSeparator("credits.title.7",  String.Format("版本: {0}", Rengar.ScriptVersion)));
				creditsMenu.Add(new MenuSeparator("credits.title.8", "汉化：晴依 "));
				creditsMenu.Add(new MenuSeparator("credits.title.9", "http://lsharp.xyz L#相关资源"));

                menu.Add(creditsMenu);
            }

            menu.Attach();
        }

        #endregion
    }
}
