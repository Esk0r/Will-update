namespace ElLeeSin
{
    using System;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using Color = System.Drawing.Color;

    internal class InsecHandler
    {
        #region Static Fields

        public static Vector3 FlashPos;

        public static bool FlashR;

        private static Obj_AI_Base _selectedEnemy;

        private static Obj_AI_Base _selectedUnit;

        private static Vector3 insecPos;

        private static bool isNullInsecPos = true;

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

        public static void DoInsec()
        {
            if (_selectedEnemy == null && InitMenu.Menu.Item("insecOrbwalk").GetValue<bool>())
            {
                Orbwalking.Orbwalk(null, Game.CursorPos);
                return;
            }
            if (_selectedEnemy != null)
            {
                Orbwalking.Orbwalk(
                    Orbwalking.InAutoAttackRange(_selectedEnemy) ? _selectedEnemy : null,
                    _selectedUnit == null ? _selectedEnemy.Position : Game.CursorPos);
            }
            if (!InsecPos().IsValid() || !_selectedEnemy.IsValidTarget() || !Program.spells[Program.Spells.R].IsReady())
            {
                return;
            }

            if (Player.Distance(InsecPos()) <= 120)
            {
                Program.spells[Program.Spells.R].CastOnUnit(_selectedEnemy);
                return;
            }
            if (Player.Distance(InsecPos()) < 600)
            {
                if (Program.WState && Program.spells[Program.Spells.W].IsReady() && Program.CheckQ)
                {
                    Obj_AI_Base unit =
                        ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(a => a.IsAlly && a.Distance(InsecPos()) < 120);
                    if (unit != null)
                    {
                        Program.spells[Program.Spells.W].CastOnUnit(unit);
                    }
                    else if (Program.LastWard + 500 < Environment.TickCount && Items.GetWardSlot() != null
                             && Player.Distance(InsecPos()) < 600)
                    {
                        Player.Spellbook.CastSpell(Items.GetWardSlot().SpellSlot, InsecPos());
                    }
                    return;
                }
                if (!InitMenu.Menu.Item("flashInsec").GetValue<bool>()
                    || Program.WState && Program.spells[Program.Spells.W].IsReady() && Items.GetWardSlot() != null
                    || Program.LastW + 2000 > Environment.TickCount)
                {
                    return;
                }
                if (_selectedEnemy.Distance(Player) < Program.spells[Program.Spells.R].Range)
                {
                    Program.spells[Program.Spells.R].CastOnUnit(_selectedEnemy);
                    FlashPos = InsecPos();
                    FlashR = true;
                }
                else
                {
                    if (InsecPos().Distance(Player.Position) < 400)
                    {
                        Player.Spellbook.CastSpell(Player.GetSpellSlot("summonerflash"), InsecPos());
                    }
                }
            }
            if (Player.Distance(_selectedEnemy) < Program.spells[Program.Spells.Q].Range && Program.QState
                && Program.spells[Program.Spells.Q].IsReady())
            {
                Program.spells[Program.Spells.Q].Cast(_selectedEnemy);
            }
            if (!Program.QState && _selectedEnemy.HasQBuff()
                || (InitMenu.Menu.Item("q2InsecRange").GetValue<bool>() && Program.BuffedEnemy.IsValidTarget()
                    && Program.BuffedEnemy.Distance(InsecPos()) < 500))
            {
                Program.spells[Program.Spells.Q].Cast();
            }

            if (InitMenu.Menu.Item("q1InsecRange").GetValue<bool>() || !Program.QState
                || !Program.spells[Program.Spells.Q].IsReady() || !InsecPos().IsValid())
            {
                return;
            }
            foreach (var unit in
                ObjectManager.Get<Obj_AI_Base>()
                    .Where(
                        a =>
                        a.IsEnemy && (a.IsValid<Obj_AI_Hero>() || a.IsValid<Obj_AI_Minion>())
                        && a.Distance(InsecPos()) < 400))
            {
                if (!unit.IsValidTarget())
                {
                    return;
                }

                Program.spells[Program.Spells.Q].Cast(unit);
            }
        }

        public static void Draw()
        {
            if (_selectedUnit != null)
            {
                Render.Circle.DrawCircle(_selectedUnit.Position, _selectedUnit.BoundingRadius + 50, Color.White);
                Drawing.DrawText(
                    Drawing.WorldToScreen(_selectedUnit.Position).X - 40,
                    Drawing.WorldToScreen(_selectedUnit.Position).Y + 10,
                    Color.White,
                    "Selected Ally");
            }
            if (_selectedEnemy.IsValidTarget() && _selectedEnemy.IsVisible && !_selectedEnemy.IsDead)
            {
                Drawing.DrawText(
                    Drawing.WorldToScreen(_selectedEnemy.Position).X - 40,
                    Drawing.WorldToScreen(_selectedEnemy.Position).Y + 10,
                    Color.White,
                    "Insec Target");

                Render.Circle.DrawCircle(_selectedEnemy.Position, _selectedEnemy.BoundingRadius + 50, Color.Gold);
                if (InsecPos().IsValid())
                {
                    Render.Circle.DrawCircle(InsecPos(), 110, Color.Gold);
                }
            }
        }

        public static Vector3 InsecPos()
        {
            if (isNullInsecPos)
            {
                isNullInsecPos = false;
                insecPos = Player.Position;
            }

            if (_selectedUnit != null && _selectedEnemy.IsValidTarget()
                && InitMenu.Menu.Item("clickInsec").GetValue<bool>())
            {
                return _selectedUnit.Position.Extend(
                    _selectedEnemy.Position,
                    _selectedUnit.Distance(_selectedEnemy) + 250);
            }
            if (_selectedEnemy.IsValidTarget() && InitMenu.Menu.Item("easyInsec").GetValue<bool>())
            {
                foreach (var tower in
                    ObjectManager.Get<Obj_AI_Turret>()
                        .Where(tower => tower.IsAlly && tower.Health > 0 && tower.Distance(_selectedEnemy) < 2000))
                {
                    return tower.Position.Extend(_selectedEnemy.Position, tower.Distance(_selectedEnemy) + 250);
                }
                foreach (var ally in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            ally =>
                            ally.IsAlly && !ally.IsMe && ally.HealthPercent > 10 && ally.Distance(_selectedEnemy) < 2000)
                    )
                {
                    return ally.Position.Extend(_selectedEnemy.Position, ally.Distance(_selectedEnemy) + 250);
                }
            }
            if (_selectedUnit == null && _selectedEnemy.IsValidTarget()
                && InitMenu.Menu.Item("mouseInsec").GetValue<bool>())
            {
                return Game.CursorPos.Extend(
                    _selectedEnemy.Position,
                    Game.CursorPos.Distance(_selectedEnemy.Position) + 250);
            }

            if (InitMenu.Menu.Item("ElLeeSin.Insec.Original.Pos").GetValue<bool>())
                //&& Player.CountEnemiesInRange(2000) == 0
            {
                return V2E(insecPos, _selectedEnemy.Position, _selectedEnemy.Distance(insecPos) + 230).To3D();
            }

            return new Vector3();
        }

        public static void OnClick(WndEventArgs args)
        {
            if (args.Msg != (uint)WindowsMessages.WM_LBUTTONDOWN)
            {
                return;
            }
            var unit2 =
                ObjectManager.Get<Obj_AI_Base>()
                    .FirstOrDefault(
                        a =>
                        (a.IsValid<Obj_AI_Hero>()) && a.IsEnemy && a.Distance(Game.CursorPos) < a.BoundingRadius + 80
                        && a.IsValidTarget());
            if (unit2 != null)
            {
                _selectedEnemy = unit2;
                return;
            }
            var unit =
                ObjectManager.Get<Obj_AI_Base>()
                    .FirstOrDefault(
                        a =>
                        (a.IsValid<Obj_AI_Hero>() || a.IsValid<Obj_AI_Minion>() || a.IsValid<Obj_AI_Turret>())
                        && a.IsAlly && a.Distance(Game.CursorPos) < a.BoundingRadius + 80 && a.IsValid && !a.IsDead
                        && !a.Name.ToLower().Contains("ward") && !a.IsMe);
            _selectedUnit = unit;
            if (_selectedUnit == null)
            {
                _selectedEnemy = null;
            }
        }

        #endregion

        #region Methods

        private static Vector2 V2E(Vector3 from, Vector3 direction, float distance)
        {
            return from.To2D() + distance * Vector3.Normalize(direction - from).To2D();
        }

        #endregion
    }
}