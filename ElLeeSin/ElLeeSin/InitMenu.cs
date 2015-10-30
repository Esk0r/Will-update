namespace ElLeeSin
{
    using System.Drawing;

    using LeagueSharp.Common;

    using Color = SharpDX.Color;

    public class InitMenu
    {
        #region Static Fields

        public static Menu Menu;

        #endregion

        #region Public Methods and Operators

        public static void Initialize()
        {
            Menu = new Menu("ElLeeSin", "LeeSin", true);
            Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Program.Orbwalker = new Orbwalking.Orbwalker(Menu.SubMenu("Orbwalker"));

            var ts = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(ts);
            Menu.AddSubMenu(ts);

            Menu.AddSubMenu(new Menu("Combo", "Combo"));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.Q", "Use Q").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.Q2", "Use Q2").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.W2", "Use W").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.E", "Use E").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.R", "Use R").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.PassiveStacks", "Min Stacks").SetValue(new Slider(1, 1, 2)));


            Menu.SubMenu("Combo")
                .SubMenu("Wardjump")
                .AddItem(new MenuItem("ElLeeSin.Combo.W", "Wardjump in combo").SetValue(false));
            Menu.SubMenu("Combo")
                .SubMenu("Wardjump")
                .AddItem(new MenuItem("ElLeeSin.Combo.Mode.WW", "Out of AA range").SetValue(false));

            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.KS.R", "KS R").SetValue(true));
            Menu.SubMenu("Combo")
                .AddItem(
                    new MenuItem("starCombo", "Star Combo").SetValue(
                        new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));

            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.AAStacks", "Wait for Passive").SetValue(false));

            var harassMenu = Menu.AddSubMenu(new Menu("Harass", "Harass"));
            {
                harassMenu.AddItem(new MenuItem("ElLeeSin.Harass.Q1", "Use Q").SetValue(true));
                harassMenu.AddItem(new MenuItem("ElLeeSin.Harass.Wardjump", "Use W").SetValue(true));
                harassMenu.AddItem(new MenuItem("ElLeeSin.Harass.E1", "Use E").SetValue(false));
                harassMenu.AddItem(new MenuItem("ElLeeSin.Harass.PassiveStacks", "Min Stacks").SetValue(new Slider(1, 1, 2)));

            }

            var waveclearMenu = Menu.AddSubMenu(new Menu("Clear", "Clear"));
            {
                waveclearMenu.AddItem(new MenuItem("Sep111", "Wave Clear"));
                waveclearMenu.AddItem(new MenuItem("ElLeeSin.Lane.Q", "Use Q").SetValue(true));
                waveclearMenu.AddItem(new MenuItem("ElLeeSin.Lane.E", "Use E").SetValue(true));

                waveclearMenu.AddItem(new MenuItem("sep222", "Jungle Clear"));

                waveclearMenu.AddItem(new MenuItem("ElLeeSin.Jungle.Q", "Use Q").SetValue(true));
                waveclearMenu.AddItem(new MenuItem("ElLeeSin.Jungle.W", "Use W").SetValue(true));
                waveclearMenu.AddItem(new MenuItem("ElLeeSin.Jungle.E", "Use E").SetValue(true));
            }

            var insecMenu = Menu.AddSubMenu(new Menu("Insec", "Insec").SetFontStyle(FontStyle.Bold, Color.BlueViolet));
            {
                insecMenu.AddItem(
                    new MenuItem("InsecEnabled", "Insec key:").SetValue(
                        new KeyBind("Y".ToCharArray()[0], KeyBindType.Press)));
                insecMenu.AddItem(new MenuItem("rnshsasdhjk", "Insec Mode:")).SetFontStyle(FontStyle.Bold, Color.BlueViolet);
                insecMenu.AddItem(new MenuItem("insecMode", "Left click target to Insec").SetValue(true));
                insecMenu.AddItem(new MenuItem("insecOrbwalk", "Orbwalking").SetValue(true));
                insecMenu.AddItem(new MenuItem("flashInsec", "Flash Insec when no ward").SetValue(false));
                insecMenu.AddItem(new MenuItem("waitForQBuff", "Wait For Q").SetValue(false));
                insecMenu.AddItem(new MenuItem("clickInsec", "Click Insec").SetValue(true));
                insecMenu.AddItem(new MenuItem("checkOthers", "Check for units to Insec").SetValue(true));

                insecMenu.AddItem(new MenuItem("bonusRangeA", "Ally Bonus Range").SetValue(new Slider(0, 0, 1000)));
                insecMenu.AddItem(new MenuItem("bonusRangeT", "Towers Bonus Range").SetValue(new Slider(0, 0, 1000)));

                insecMenu.SubMenu("Insec Modes")
                    .AddItem(new MenuItem("ElLeeSin.Insec.Ally", "Insec to allies").SetValue(true));
                insecMenu.SubMenu("Insec Modes")
                    .AddItem(new MenuItem("ElLeeSin.Insec.Tower", "Insec to tower").SetValue(false));
                insecMenu.SubMenu("Insec Modes")
                    .AddItem(new MenuItem("ElLeeSin.Insec.Original.Pos", "Insec to original pos").SetValue(true));
                insecMenu.SubMenu("Insec Modes").AddItem(new MenuItem("insecmouse", "Insec to mouse").SetValue(false));

                insecMenu.AddItem(new MenuItem("ElLeeSin.Insec.UseInstaFlash", "Flash insec").SetValue(true));
                insecMenu.AddItem(
                    new MenuItem("ElLeeSin.Insec.Insta.Flashx", "Flash Insec key: ").SetValue(
                        new KeyBind("P".ToCharArray()[0], KeyBindType.Press)));
            }


            var wardjumpMenu = Menu.AddSubMenu(new Menu("Wardjump", "Wardjump"));
            {
                wardjumpMenu.AddItem(
                    new MenuItem("ElLeeSin.Wardjump", "Wardjump key").SetValue(
                        new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));
                wardjumpMenu.AddItem(new MenuItem("ElLeeSin.Wardjump.Mouse", "Jump to mouse").SetValue(true));
                wardjumpMenu.AddItem(new MenuItem("ElLeeSin.Wardjump.Minions", "Jump to minions").SetValue(true));
                wardjumpMenu.AddItem(new MenuItem("ElLeeSin.Wardjump.Champions", "Jump to champions").SetValue(true));
            }

            var drawMenu = Menu.AddSubMenu(new Menu("Drawing", "Drawing"));
            {
                drawMenu.AddItem(new MenuItem("DrawEnabled", "Draw Enabled").SetValue(false));
                drawMenu.AddItem(new MenuItem("ElLeeSin.Draw.Insec.Text", "Draw Insec text").SetValue(true));
                drawMenu.AddItem(new MenuItem("drawOutLineST", "Draw Outline").SetValue(true));
                drawMenu.AddItem(new MenuItem("ElLeeSin.Draw.Insec", "Draw Insec").SetValue(true));
                drawMenu.AddItem(new MenuItem("ElLeeSin.Draw.WJDraw", "Draw WardJump").SetValue(true));
                drawMenu.AddItem(new MenuItem("ElLeeSin.Draw.Q", "Draw Q").SetValue(true));
                drawMenu.AddItem(new MenuItem("ElLeeSin.Draw.W", "Draw W").SetValue(true));
                drawMenu.AddItem(new MenuItem("ElLeeSin.Draw.E", "Draw E").SetValue(true));
                drawMenu.AddItem(new MenuItem("ElLeeSin.Draw.R", "Draw R").SetValue(true));
            }

            var miscMenu = Menu.AddSubMenu(new Menu("Misc", "Misc"));
            {
                miscMenu.AddItem(new MenuItem("IGNks", "Use Ignite?").SetValue(true));
                miscMenu.AddItem(new MenuItem("qSmite", "Smite Q!").SetValue(false));
            }

            Menu.AddToMainMenu();
        }

        #endregion
    }
}