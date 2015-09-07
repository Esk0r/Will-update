using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElLeeSin
{
    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using Color = System.Drawing.Color;

    public class Drawings
    {
        public static void Drawing_OnDraw(EventArgs args)
        {
            Obj_AI_Hero newTarget = Program.ParamBool("insecMode")
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

            if (newTarget != null && newTarget.IsVisible && Program.Player.Distance(newTarget) < 3000
                && Program.ParamBool("ElLeeSin.Draw.Insec.Text"))
            {
                Vector2 targetPos = Drawing.WorldToScreen(newTarget.Position);
                Drawing.DrawLine(
                    Program.InsecLinePos.X,
                    Program.InsecLinePos.Y,
                    targetPos.X,
                    targetPos.Y,
                    3,
                    Color.White);
                Render.Circle.DrawCircle(Program.GetInsecPos(newTarget), 100, Color.White);
            }
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
    }
}