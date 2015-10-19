namespace ElLeeSin
{
    using System;
    using System.Drawing;

    using LeagueSharp.Common;

    public class Drawings
    {
        #region Public Methods and Operators

        public static void Drawing_OnDraw(EventArgs args)
        {
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

            InsecHandler.Draw();
            WardjumpHandler.Draw();
        }

        #endregion
    }
}