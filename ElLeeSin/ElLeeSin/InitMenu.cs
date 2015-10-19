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
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.W2", "Use W").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.E", "Use E").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ElLeeSin.Combo.R", "Use R").SetValue(true));

            Menu.SubMenu("Combo")
                .SubMenu("Wardjump")
                .AddItem(new MenuItem("ElLeeSin.Combo.W", "Wardjump in combo").SetValue(false));
            Menu.SubMenu("Combo")
                .SubMenu("Wardjump")
                .AddItem(new MenuItem("ElLeeSin.Combo.Mode.W", "> AA Range || > Q Range").SetValue(true));

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
            }

            var waveclearMenu = Menu.AddSubMenu(new Menu("Clear", "Clear"));
            {
                waveclearMenu.SubMenu("Laneclear").AddItem(new MenuItem("sjasjsdsjs", "WaveClear"));
                waveclearMenu.SubMenu("Laneclear").AddItem(new MenuItem("ElLeeSin.Lane.Q", "Use Q").SetValue(true));
                waveclearMenu.SubMenu("Laneclear").AddItem(new MenuItem("ElLeeSin.Lane.E", "Use E").SetValue(true));

                waveclearMenu.SubMenu("Jungleclear").AddItem(new MenuItem("ElLeeSin.Jungle.Q", "Use Q").SetValue(true));
                waveclearMenu.SubMenu("Jungleclear").AddItem(new MenuItem("ElLeeSin.Jungle.W", "Use W").SetValue(true));
                waveclearMenu.SubMenu("Jungleclear").AddItem(new MenuItem("ElLeeSin.Jungle.E", "Use E").SetValue(true));
            }

            var insecMenu = Menu.AddSubMenu(new Menu("Insec", "Insec").SetFontStyle(FontStyle.Bold, Color.BlueViolet));
            {
                insecMenu.AddItem(new MenuItem("insecOrbwalk", "Orbwalking").SetValue(true));
                insecMenu.AddItem(new MenuItem("clickInsec", "Click Insec").SetValue(true));
                insecMenu.AddItem(new MenuItem("mouseInsec", "Insec to mouse pos").SetValue(false));
                insecMenu.AddItem(new MenuItem("ElLeeSin.Insec.Original.Pos", "Insec to original pos").SetValue(true));

                insecMenu.AddItem(new MenuItem("easyInsec", "Easy Insec").SetValue(true));
                insecMenu.AddItem(new MenuItem("q2InsecRange", "Use Q2 if buffed unit in range (all)").SetValue(true));
                insecMenu.AddItem(new MenuItem("q1InsecRange", "Use Q1 on units in insec range").SetValue(false));
                insecMenu.AddItem(new MenuItem("flashInsec", "Flash if ward down").SetValue(false));
                insecMenu.AddItem(new MenuItem("insec", "Insec Active").SetValue(new KeyBind('Y', KeyBindType.Press)));
            }

            var wardjumpMenu = Menu.AddSubMenu(new Menu("Wardjump", "Wardjump"));
            {
                wardjumpMenu.AddItem(
                    new MenuItem("ElLeeSin.Wardjump", "Wardjump key").SetValue(
                        new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));
                wardjumpMenu.AddItem(new MenuItem("ElLeeSin.Wardjump.Mouse", "Move to Ward").SetValue(true));
                wardjumpMenu.AddItem(new MenuItem("ElLeeSin.Wardjump.Minions", "Jump to minions").SetValue(false));
                wardjumpMenu.AddItem(new MenuItem("ElLeeSin.Wardjump.Champions", "Jump to champions").SetValue(false));
                wardjumpMenu.AddItem(new MenuItem("ElLeeSin.Wardjump.MaxRange", "Always max range").SetValue(true));
            }

            var drawMenu = Menu.AddSubMenu(new Menu("Drawing", "Drawing"));
            {
                drawMenu.AddItem(new MenuItem("DrawEnabled", "Draw Enabled").SetValue(false));
                drawMenu.AddItem(new MenuItem("ElLeeSin.Draw.Insec.Text", "Draw insec text").SetValue(true));
                drawMenu.AddItem(new MenuItem("drawOutLineST", "Draw Outline").SetValue(true));
                drawMenu.AddItem(new MenuItem("ElLeeSin.Draw.Insec", "Draw INSEC").SetValue(true));
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