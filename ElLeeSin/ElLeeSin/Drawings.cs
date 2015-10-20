namespace ElLeeSin
{
    using System;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using Color = System.Drawing.Color;

    public class Drawings
    {
        #region Public Methods and Operators

        public static void Drawing_OnDraw(EventArgs args)
        {
            var newTarget = Program.ParamBool("insecMode")
                                ? TargetSelector.GetSelectedTarget()
                                : TargetSelector.GetTarget(
                                    Program.spells[Program.Spells.Q].Range + 200,
                                    TargetSelector.DamageType.Physical);

            if (Program.ClicksecEnabled)
            {
                Render.Circle.DrawCircle(Program.InsecClickPos, 100, Color.White);
            }

            var playerPos = Drawing.WorldToScreen(ObjectManager.Player.Position);
            if (Program.ParamBool("ElLeeSin.Draw.Insec.Text"))
            {
                Drawing.DrawText(playerPos.X, playerPos.Y + 40, Color.White, "Flash Insec enabled");
            }

            /*if (_selectedEnemy.IsValidTarget() && _selectedEnemy.IsVisible && !_selectedEnemy.IsDead)
            {
                Drawing.DrawText(
                    Drawing.WorldToScreen(_selectedEnemy.Position).X - 40,
                    Drawing.WorldToScreen(_selectedEnemy.Position).Y + 10,
                    Color.White,
                    "Selected Target");
            }*/


            if (newTarget != null && newTarget.IsVisible && newTarget.IsValidTarget() && !newTarget.IsDead && Program.Player.Distance(newTarget) < 3000)
            {
                Vector2 targetPos = Drawing.WorldToScreen(newTarget.Position);
                Drawing.DrawLine(
                    Program.InsecLinePos.X,
                    Program.InsecLinePos.Y,
                    targetPos.X,
                    targetPos.Y,
                    3,
                    Color.Gold);

                Drawing.DrawText(
                    Drawing.WorldToScreen(newTarget.Position).X - 40,
                    Drawing.WorldToScreen(newTarget.Position).Y + 10,
                    Color.White,
                    "Selected Target");

                Drawing.DrawCircle(Program.GetInsecPos(newTarget), 100, Color.Gold);

            }


            /* if (newTarget != null && newTarget.IsVisible && Program.Player.Distance(newTarget) < 3000
                 && Program.ParamBool("ElLeeSin.Draw.Insec.Text"))
             {
                 var targetPos = Drawing.WorldToScreen(newTarget.Position);
                 Drawing.DrawLine(
                     Program.InsecLinePos.X,
                     Program.InsecLinePos.Y,
                     targetPos.X,
                     targetPos.Y,
                     3,
                     Color.White);

            Render.Circle.DrawCircle(Program.GetInsecPos(newTarget), 100, Color.White);
            }*/
            if (!Program.ParamBool("DrawEnabled"))
            {
                return;
            }
            foreach (var t in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (t.HasBuff("BlindMonkQOne") || t.HasBuff("blindmonkqonechaos"))
                {
                    Drawing.DrawCircle(t.Position, 200, Color.Red);
                }
            }

         

            if (InitMenu.Menu.Item("ElLeeSin.Wardjump").GetValue<KeyBind>().Active
                && Program.ParamBool("ElLeeSin.Draw.WJDraw"))
            {
                Render.Circle.DrawCircle(Program.JumpPos.To3D(), 20, Color.Red);
                Render.Circle.DrawCircle(Program.Player.Position, 600, Color.Red);
            }
            if (Program.ParamBool("ElLeeSin.Draw.Q"))
            {
                Render.Circle.DrawCircle(
                    Program.Player.Position,
                    Program.spells[Program.Spells.Q].Range - 80,
                    Program.spells[Program.Spells.Q].IsReady() ? Color.LightSkyBlue : Color.Tomato);
            }
            if (Program.ParamBool("ElLeeSin.Draw.W"))
            {
                Render.Circle.DrawCircle(
                    Program.Player.Position,
                    Program.spells[Program.Spells.W].Range - 80,
                    Program.spells[Program.Spells.W].IsReady() ? Color.LightSkyBlue : Color.Tomato);
            }
            if (Program.ParamBool("ElLeeSin.Draw.E"))
            {
                Render.Circle.DrawCircle(
                    Program.Player.Position,
                    Program.spells[Program.Spells.E].Range - 80,
                    Program.spells[Program.Spells.E].IsReady() ? Color.LightSkyBlue : Color.Tomato);
            }
            if (Program.ParamBool("ElLeeSin.Draw.R"))
            {
                Render.Circle.DrawCircle(
                    Program.Player.Position,
                    Program.spells[Program.Spells.R].Range - 80,
                    Program.spells[Program.Spells.R].IsReady() ? Color.LightSkyBlue : Color.Tomato);
            }
        }

        #endregion
    }
}