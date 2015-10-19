namespace ElLeeSin
{
    using System;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using Color = System.Drawing.Color;

    internal class WardjumpHandler
    {
        #region Static Fields

        public static bool DrawEnabled;

        private static Vector3 _drawPos;

        #endregion

        #region Properties

        private static Obj_AI_Hero Player
        {
            get
            {
                return ObjectManager.Player;
            }
        }

        #endregion

        #region Public Methods and Operators

        public static void Draw()
        {
            if (!DrawEnabled)
            {
                return;
            }

            if (_drawPos.IsValid())
            {
                Render.Circle.DrawCircle(_drawPos, 70, Color.RoyalBlue);
                Render.Circle.DrawCircle(Player.Position, 600, Color.White);
            }
        }

        public static void Jump(Vector3 pos, bool maxRange = false, bool moveToMouse = false, bool onlyPos = false)
        {
            if (moveToMouse)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(Game.CursorPos, 150));
            }

            _drawPos = new Vector3();

            if (maxRange && pos.Distance(Player.Position) > 600)
            {
                pos = Player.Position.Extend(pos, 600);
            }

            _drawPos = pos;

            var unit = WardJumpUnit(pos, onlyPos);
            if (unit != null && Program.WState)
            {
                Program.spells[Program.Spells.W].Cast(unit);
                return;
            }

            if (pos.Distance(Player.Position) > 600)
            {
                return;
            }

            if (pos.Distance(Player.Position) < 600 && Program.LastWard + 600 < Environment.TickCount
                && Items.GetWardSlot() != null && Program.WState && Program.spells[Program.Spells.W].IsReady())
            {
                Player.Spellbook.CastSpell(Items.GetWardSlot().SpellSlot, pos);
            }
        }

        #endregion

        #region Methods

        private static Obj_AI_Base WardJumpUnit(Vector3 pos, bool onlyPos = false)
        {
            var minions = InitMenu.Menu.Item("ElLeeSin.Wardjump.Minions").GetValue<bool>();
            var champions = InitMenu.Menu.Item("ElLeeSin.Wardjump.Champions").GetValue<bool>();
            var wards = InitMenu.Menu.Item("ElLeeSin.Wardjump.Mouse").GetValue<bool>();

            if (minions)
            {
                var selectedMinion =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(
                            minion =>
                            minion != null && minion.Distance(Player) <= 700 && minion.IsAlly
                            && !minion.Name.ToLower().Contains("ward") && !minion.IsMe
                            && (!onlyPos || minion.Distance(pos) < 70))
                        .OrderByDescending(a => Player.Distance(a))
                        .FirstOrDefault();
                if (selectedMinion != null)
                {
                    return selectedMinion;
                }
            }
            if (champions)
            {
                var selectedHero =
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            minion =>
                            minion != null && minion.Distance(Player) <= 700 && minion.IsAlly && !minion.IsMe
                            && (!onlyPos || minion.Distance(pos) < 70))
                        .OrderByDescending(a => Player.Distance(a))
                        .FirstOrDefault();
                if (selectedHero != null)
                {
                    return selectedHero;
                }
            }

            if (wards)
            {
                var selectedMinion =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(
                            minion =>
                            minion != null && minion.Distance(Player) <= 700 && minion.IsAlly
                            && minion.Name.ToLower().Contains("ward") && !minion.IsMe
                            && (!onlyPos || minion.Distance(pos) < 70))
                        .OrderByDescending(a => Player.Distance(a))
                        .FirstOrDefault();

                if (selectedMinion != null)
                {
                    return selectedMinion;
                }
            }

            return null;
        }

        #endregion
    }
}