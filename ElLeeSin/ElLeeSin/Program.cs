namespace ElLeeSin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using ItemData = LeagueSharp.Common.Data.ItemData;

    internal static class Program
    {
        #region Static Fields

        public static bool CheckQ = true;

        public static bool ClicksecEnabled;

        public static Vector3 InsecClickPos;

        public static Vector2 InsecLinePos;

        public static Vector2 JumpPos;

        public static int LastQ, LastQ2, LastW, LastW2, LastE, LastE2, LastR, LastWard, LastSpell, PassiveStacks;

        public static Orbwalking.Orbwalker Orbwalker;

        public static Obj_AI_Hero Player = ObjectManager.Player;

        public static Dictionary<Spells, Spell> spells = new Dictionary<Spells, Spell>
                                                             {
                                                                 { Spells.Q, new Spell(SpellSlot.Q, 1100) },
                                                                 { Spells.W, new Spell(SpellSlot.W, 700) },
                                                                 { Spells.E, new Spell(SpellSlot.E, 430) },
                                                                 { Spells.R, new Spell(SpellSlot.R, 375) }
                                                             };

        private static readonly bool castWardAgain = true;

        private static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707 };

        private static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719 };

        private static readonly int[] SmitePurple = { 3713, 3726, 3725, 3726, 3723 };

        private static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714 };

        private static readonly string[] SpellNames =
            {
                "BlindMonkQOne", "BlindMonkWOne", "BlindMonkEOne",
                "blindmonkwtwo", "blindmonkqtwo", "blindmonketwo",
                "BlindMonkRKick"
            };

        private static bool castQAgain;

        private static int clickCount;

        private static bool delayW;

        private static float doubleClickReset;

        private static SpellSlot flashSlot;

        private static SpellSlot igniteSlot;

        private static InsecComboStepSelect insecComboStep;

        private static Vector3 insecPos;

        private static bool isNullInsecPos = true;

        private static bool lastClickBool;

        private static Vector3 lastClickPos;

        private static float lastPlaced;

        private static Vector3 lastWardPos;

        private static Vector3 mouse = Game.CursorPos;

        private static float passiveTimer;

        private static bool q2Done;

        private static float q2Timer;

        private static bool reCheckWard = true;

        private static float resetTime;

        private static SpellSlot smiteSlot;

        private static bool waitforjungle;

        private static bool waitingForQ2;

        private static bool wardJumped;

        private static float wcasttime;

        #endregion

        #region Enums

        internal enum Spells
        {
            Q,

            W,

            E,

            R
        }

        private enum InsecComboStepSelect
        {
            None,

            Qgapclose,

            Wgapclose,

            Pressr
        };

        private enum WCastStage
        {
            First,

            Second,

            Cooldown
        }

        #endregion

        #region Public Properties

        public static bool EState
        {
            get
            {
                return spells[Spells.E].Instance.Name == "BlindMonkEOne";
            }
        }

        public static bool QState
        {
            get
            {
                return spells[Spells.Q].Instance.Name == "BlindMonkQOne";
            }
        }

        public static bool WState
        {
            get
            {
                return spells[Spells.W].Instance.Name == "BlindMonkWOne";
            }
        }

        #endregion

        #region Properties

        private static WCastStage WStage
        {
            get
            {
                if (!spells[Spells.W].IsReady())
                {
                    return WCastStage.Cooldown;
                }

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "blindmonkwtwo"
                            ? WCastStage.Second
                            : WCastStage.First);
            }
        }

        #endregion

        #region Public Methods and Operators

        public static Vector3 GetInsecPos(Obj_AI_Hero target)
        {
            if (ClicksecEnabled && ParamBool("clickInsec"))
            {
                InsecLinePos = Drawing.WorldToScreen(InsecClickPos);
                return V2E(InsecClickPos, target.Position, target.Distance(InsecClickPos) + 230).To3D();
            }
            if (isNullInsecPos)
            {
                isNullInsecPos = false;
                insecPos = Player.Position;
            }

            foreach (var ally in
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(
                        ally =>
                        ally.IsAlly && !ally.IsMe && ally.HealthPercent > 10 && ally.Distance(target) < 2000
                        && ParamBool("ElLeeSin.Insec.Ally")))
            {
                return ally.Position.Extend(target.Position, ally.Distance(target) + 250);
            }

            foreach (var tower in
                ObjectManager.Get<Obj_AI_Turret>()
                    .Where(
                        tower =>
                        tower.IsAlly && tower.Health > 0 && tower.Distance(target) < 2000
                        && ParamBool("ElLeeSin.Insec.Tower")))
            {
                return tower.Position.Extend(target.Position, tower.Distance(target) + 250);
            }

            if (ParamBool("ElLeeSin.Insec.Original.Pos"))
            {
                return V2E(insecPos, target.Position, target.Distance(insecPos) + 230).To3D();
            }

            if (target.IsValidTarget() && ParamBool("insecmouse"))
            {
                return Game.CursorPos.Extend(target.Position, Game.CursorPos.Distance(target.Position) + 250);
            }

            return new Vector3();
        }

        public static bool HasQBuff(this Obj_AI_Base unit)
        {
            return (unit.HasBuff("BlindMonkQOne") || unit.HasBuff("blindmonkqonechaos"));
        }

        public static bool ParamBool(string paramName)
        {
            return InitMenu.Menu.Item(paramName).GetValue<bool>();
        }

        #endregion

        #region Methods

        private static void AllClear()
        {
            var minions = MinionManager.GetMinions(spells[Spells.Q].Range).FirstOrDefault();

            if (!minions.IsValidTarget() || minions == null)
            {
                return;
            }

            UseItems(minions);

            if (ParamBool("ElLeeSin.Lane.Q") && !QState && spells[Spells.Q].IsReady() && minions.HasQBuff()
                && (LastQ + 2700 < Environment.TickCount || spells[Spells.Q].GetDamage(minions, 1) > minions.Health
                    || minions.Distance(Player) > Orbwalking.GetRealAutoAttackRange(Player) + 50))
            {
                spells[Spells.Q].Cast();
                return;
            }

            if (spells[Spells.Q].IsReady() && ParamBool("ElLeeSin.Lane.Q") && LastQ + 200 < Environment.TickCount)
            {
                if (QState && minions.Distance(Player) < spells[Spells.Q].Range)
                {
                    spells[Spells.Q].Cast(minions);
                    return;
                }
            }

            if (spells[Spells.E].IsReady() && ParamBool("ElLeeSin.Lane.E") && LastE + 200 < Environment.TickCount)
            {
                if (EState && minions.Distance(Player) < spells[Spells.E].Range)
                {
                    spells[Spells.E].Cast();
                }
            }
        }

        private static void CastQ(Obj_AI_Base target, bool smiteQ = false)
        {
            var qData = spells[Spells.Q].GetPrediction(target);
            if (spells[Spells.Q].IsReady() && target.IsValidTarget(spells[Spells.Q].Range)
                && qData.Hitchance != HitChance.Collision)
            {
                spells[Spells.Q].CastIfHitchanceEquals(target, HitChance.High);
            }
            else if (spells[Spells.Q].IsReady() && target.IsValidTarget(spells[Spells.Q].Range)
                     && qData.CollisionObjects.Count(a => a.NetworkId != target.NetworkId && a.IsMinion) == 1
                     && Player.GetSpellSlot(SmiteSpellName()).IsReady())
            {
                Player.Spellbook.CastSpell(
                    Player.GetSpellSlot(SmiteSpellName()),
                    qData.CollisionObjects.Where(a => a.NetworkId != target.NetworkId && a.IsMinion).ToList()[0]);

                spells[Spells.Q].Cast(qData.CastPosition);
            }
        }

        private static void CastW(Obj_AI_Base obj)
        {
            if (500 >= Environment.TickCount - wcasttime || WStage != WCastStage.First)
            {
                return;
            }

            spells[Spells.W].CastOnUnit(obj);
            wcasttime = Environment.TickCount;
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Physical);
            if (!target.IsValidTarget() || target == null)
            {
                return;
            }

            UseItems(target);

            if (ParamBool("ElLeeSin.Combo.R") && ParamBool("ElLeeSin.Combo.Q") && spells[Spells.Q].IsReady()
                && spells[Spells.Q].IsReady() && (QState || target.HasQBuff())
                && spells[Spells.R].GetDamage(target) + (QState ? spells[Spells.Q].GetDamage(target) : 0)
                + Q2Damage(
                    target,
                    spells[Spells.R].GetDamage(target) + (QState ? spells[Spells.Q].GetDamage(target) : 0))
                > target.Health)
            {
                if (QState)
                {
                    var prediction = spells[Spells.Q].GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.High)
                    {
                        spells[Spells.Q].Cast(target);
                    }
                    return;
                }

                spells[Spells.R].CastOnUnit(target);
                Utility.DelayAction.Add(300, () => spells[Spells.Q].Cast());
            }

            if (ParamBool("ElLeeSin.Combo.KS.R") && spells[Spells.R].IsReady()
                && spells[Spells.R].GetDamage(target) > target.Health)
            {
                spells[Spells.R].CastOnUnit(target);
                return;
            }

            if (ParamBool("ElLeeSin.Combo.Q") && !QState && spells[Spells.Q].IsReady() && target.HasQBuff()
                && (LastQ + 2700 < Environment.TickCount || spells[Spells.Q].GetDamage(target, 1) > target.Health
                    || target.Distance(Player) > Orbwalking.GetRealAutoAttackRange(Player) + 50))
            {
                spells[Spells.Q].Cast();
                return;
            }

            if (ParamBool("ElLeeSin.Combo.AAStacks") && PassiveStacks > 1
                && Orbwalking.GetRealAutoAttackRange(Player) > Player.Distance(target))
            {
                return;
            }

            if (spells[Spells.Q].IsReady() && ParamBool("ElLeeSin.Combo.Q"))
            {
                if (QState && target.Distance(Player) < spells[Spells.Q].Range)
                {
                    CastQ(target, ParamBool("qSmite"));
                    return;
                }
            }

            if (spells[Spells.E].IsReady() && ParamBool("ElLeeSin.Combo.E"))
            {
                if (EState && target.Distance(Player) < spells[Spells.E].Range)
                {
                    spells[Spells.E].Cast();
                    return;
                }

                if (!EState && target.Distance(Player) > Orbwalking.GetRealAutoAttackRange(Player) + 50)
                {
                    spells[Spells.E].Cast();
                }
            }
        }

        private static InventorySlot FindBestWardItem()
        {
            var slot = Items.GetWardSlot();
            if (slot == default(InventorySlot))
            {
                return null;
            }

            var sdi = GetItemSpell(slot);

            if (sdi != default(SpellDataInst) && sdi.State == SpellState.Ready)
            {
                return slot;
            }
            return slot;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "LeeSin")
            {
                return;
            }

            igniteSlot = Player.GetSpellSlot("SummonerDot");
            flashSlot = Player.GetSpellSlot("summonerflash");

            spells[Spells.Q].SetSkillshot(0.25f, 65f, 1800f, true, SkillshotType.SkillshotLine);

            try
            {
                InitMenu.Initialize();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }

            Drawing.OnDraw += Drawings.Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            GameObject.OnCreate += GameObject_OnCreate;
            Orbwalking.AfterAttack += OrbwalkingAfterAttack;
            GameObject.OnDelete += GameObject_OnDelete;
            Game.OnWndProc += Game_OnWndProc;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (doubleClickReset <= Environment.TickCount && clickCount != 0)
            {
                doubleClickReset = float.MaxValue;
                clickCount = 0;
            }

            if (clickCount >= 2 && ParamBool("clickInsec"))
            {
                resetTime = Environment.TickCount + 3000;
                ClicksecEnabled = true;
                InsecClickPos = Game.CursorPos;
                clickCount = 0;
            }

            if (passiveTimer <= Environment.TickCount)
            {
                PassiveStacks = 0;
            }

            if (resetTime <= Environment.TickCount && !InitMenu.Menu.Item("InsecEnabled").GetValue<KeyBind>().Active
                && ClicksecEnabled)
            {
                ClicksecEnabled = false;
            }

            if (q2Timer <= Environment.TickCount)
            {
                q2Done = false;
            }

            if (Player.IsDead)
            {
                return;
            }

            if ((ParamBool("insecMode")
                     ? TargetSelector.GetSelectedTarget()
                     : TargetSelector.GetTarget(spells[Spells.Q].Range + 200, TargetSelector.DamageType.Physical))
                == null)
            {
                insecComboStep = InsecComboStepSelect.None;
            }

            if (InitMenu.Menu.Item("starCombo").GetValue<KeyBind>().Active)
            {
                StarCombo();
                //WardCombo();
            }

            if (ParamBool("IGNks"))
            {
                var newTarget = TargetSelector.GetTarget(600, TargetSelector.DamageType.True);

                if (newTarget != null && igniteSlot != SpellSlot.Unknown
                    && Player.Spellbook.CanUseSpell(igniteSlot) == SpellState.Ready
                    && ObjectManager.Player.GetSummonerSpellDamage(newTarget, Damage.SummonerSpell.Ignite)
                    > newTarget.Health)
                {
                    Player.Spellbook.CastSpell(igniteSlot, newTarget);
                }
            }

            if (InitMenu.Menu.Item("ElLeeSin.Insec.Insta.Flashx").GetValue<KeyBind>().Active)
            {
                if (ParamBool("insecOrbwalk"))
                {
                    Orbwalk(Game.CursorPos);
                }

                var target = TargetSelector.GetTarget(spells[Spells.R].Range, TargetSelector.DamageType.Physical);
                if (target == null)
                {
                    return;
                }

                if (ParamBool("ElLeeSin.Insec.UseInstaFlash"))
                {
                    Player.Spellbook.CastSpell(flashSlot, GetInsecPos(target));
                    Utility.DelayAction.Add(50, () => spells[Spells.R].CastOnUnit(target));
                }
            }

            if (InitMenu.Menu.Item("InsecEnabled").GetValue<KeyBind>().Active)
            {
                if (ParamBool("insecOrbwalk"))
                {
                    Orbwalk(Game.CursorPos);
                }

                var newTarget = ParamBool("insecMode")
                                    ? TargetSelector.GetSelectedTarget()
                                    : TargetSelector.GetTarget(
                                        spells[Spells.Q].Range + 200,
                                        TargetSelector.DamageType.Physical);

                if (newTarget != null)
                {
                    InsecCombo(newTarget);
                }
            }
            else
            {
                isNullInsecPos = true;
                wardJumped = false;
            }

            if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                insecComboStep = InsecComboStepSelect.None;
            }

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    AllClear();
                    JungleClear();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
            }

            if (InitMenu.Menu.Item("ElLeeSin.Wardjump").GetValue<KeyBind>().Active)
            {
                WardjumpToMouse();
            }
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != (uint)WindowsMessages.WM_LBUTTONDOWN || !ParamBool("clickInsec"))
            {
                return;
            }

            var asec =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(a => a.IsEnemy && a.Distance(Game.CursorPos) < 200 && a.IsValid && !a.IsDead);

            if (asec.Any())
            {
                return;
            }
            if (!lastClickBool || clickCount == 0)
            {
                clickCount++;
                lastClickPos = Game.CursorPos;
                lastClickBool = true;
                doubleClickReset = Environment.TickCount + 600;
                return;
            }
            if (lastClickBool && lastClickPos.Distance(Game.CursorPos) < 200)
            {
                clickCount++;
                lastClickBool = false;
            }
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (Environment.TickCount < lastPlaced + 300)
            {
                var ward = (Obj_AI_Base)sender;
                if (ward.Name.ToLower().Contains("ward") && ward.Distance(lastWardPos) < 500
                    && spells[Spells.E].IsReady())
                {
                    spells[Spells.W].Cast(ward);
                }
            }
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (!(sender is Obj_GeneralParticleEmitter))
            {
                return;
            }
            if (sender.Name.Contains("blindMonk_Q_resonatingStrike") && waitingForQ2)
            {
                waitingForQ2 = false;
                q2Done = true;
                q2Timer = Environment.TickCount + 800;
            }
        }

        private static float GetAutoAttackRange(Obj_AI_Base source = null, Obj_AI_Base target = null)
        {
            if (source == null)
            {
                source = Player;
            }

            var ret = source.AttackRange + Player.BoundingRadius;
            if (target != null)
            {
                ret += target.BoundingRadius;
            }

            return ret;
        }

        private static SpellDataInst GetItemSpell(InventorySlot invSlot)
        {
            return Player.Spellbook.Spells.FirstOrDefault(spell => (int)spell.Slot == invSlot.Slot + 4);
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range + 200, TargetSelector.DamageType.Physical);
            if (target == null)
            {
                return;
            }

            if (!QState && LastQ + 200 < Environment.TickCount && ParamBool("ElLeeSin.Harass.Q1") && !QState
                && spells[Spells.Q].IsReady() && target.HasQBuff()
                && (LastQ + 2700 < Environment.TickCount || spells[Spells.Q].GetDamage(target, 1) > target.Health
                    || target.Distance(Player) > Orbwalking.GetRealAutoAttackRange(Player) + 50))
            {
                spells[Spells.Q].Cast();
                return;
            }

            if (ParamBool("ElLeeSin.Combo.AAStacks") && PassiveStacks > 1
                && Orbwalking.GetRealAutoAttackRange(Player) > Player.Distance(target))
            {
                return;
            }

            if (spells[Spells.Q].IsReady() && ParamBool("ElLeeSin.Harass.Q1") && LastQ + 200 < Environment.TickCount)
            {
                if (QState && target.Distance(Player) < spells[Spells.Q].Range)
                {
                    CastQ(target);
                    return;
                }
            }

            if (spells[Spells.E].IsReady() && ParamBool("ElLeeSin.Harass.E1") && LastE + 200 < Environment.TickCount)
            {
                if (EState && target.Distance(Player) < spells[Spells.E].Range)
                {
                    spells[Spells.E].Cast();
                    return;
                }

                if (!EState && target.Distance(Player) > Orbwalking.GetRealAutoAttackRange(Player) + 50)
                {
                    spells[Spells.E].Cast();
                }
            }

            if (ParamBool("ElLeeSin.Harass.Wardjump") && Player.Distance(target) < 50 && !(target.HasQBuff())
                && (EState || !spells[Spells.E].IsReady() && ParamBool("ElLeeSin.Harass.E1"))
                && (QState || !spells[Spells.Q].IsReady() && ParamBool("ElLeeSin.Harass.Q1")))
            {
                var min =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(a => a.IsAlly && a.Distance(Player) <= spells[Spells.W].Range)
                        .OrderByDescending(a => a.Distance(target))
                        .FirstOrDefault();

                spells[Spells.W].CastOnUnit(min);
            }
        }

        private static bool InAutoAttackRange(Obj_AI_Base target)
        {
            if (target == null)
            {
                return false;
            }

            var myRange = GetAutoAttackRange(Player, target);
            return Vector2.DistanceSquared(target.ServerPosition.To2D(), Player.ServerPosition.To2D())
                   <= myRange * myRange;
        }

        private static void InsecCombo(Obj_AI_Hero target)
        {
            if (target != null && target.IsVisible)
            {
                if (Player.Distance(GetInsecPos(target)) < 200)
                {
                    insecComboStep = InsecComboStepSelect.Pressr;
                }
                else if (insecComboStep == InsecComboStepSelect.None
                         && GetInsecPos(target).Distance(Player.Position) < 600)
                {
                    insecComboStep = InsecComboStepSelect.Wgapclose;
                }
                else if (insecComboStep == InsecComboStepSelect.None
                         && target.Distance(Player) < spells[Spells.Q].Range)
                {
                    insecComboStep = InsecComboStepSelect.Qgapclose;
                }

                switch (insecComboStep)
                {
                    case InsecComboStepSelect.Qgapclose:
                        if (!(target.HasQBuff()) && QState)
                        {
                            CastQ(target, ParamBool("qSmite"));
                        }
                        else if ((target.HasQBuff()))
                        {
                            spells[Spells.Q].Cast();
                            insecComboStep = InsecComboStepSelect.Wgapclose;
                        }
                        else
                        {
                            if (spells[Spells.Q].Instance.Name == "blindmonkqtwo"
                                && ReturnQBuff().Distance(target) <= 600)
                            {
                                spells[Spells.Q].Cast();
                            }
                        }
                        break;

                    case InsecComboStepSelect.Wgapclose:
                        if (FindBestWardItem() != null && spells[Spells.W].IsReady()
                            && spells[Spells.W].Instance.Name == "BlindMonkWOne"
                            && (ParamBool("waitForQBuff")
                                && (QState
                                    || (!spells[Spells.Q].IsReady() || spells[Spells.Q].Instance.Name == "blindmonkqtwo")
                                    && q2Done)) || !ParamBool("waitForQBuff"))
                        {
                            WardJump(GetInsecPos(target), false, false, true);
                            wardJumped = true;
                        }
                        else if (Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Ready && ParamBool("flashInsec")
                                 && !wardJumped && Player.Distance(insecPos) < 400
                                 || Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Ready
                                 && ParamBool("flashInsec") && !wardJumped && Player.Distance(insecPos) < 400
                                 && FindBestWardItem() == null)
                        {
                            Player.Spellbook.CastSpell(flashSlot, GetInsecPos(target));
                            Utility.DelayAction.Add(50, () => spells[Spells.R].CastOnUnit(target));
                        }
                        break;

                    case InsecComboStepSelect.Pressr:
                        spells[Spells.R].CastOnUnit(target);
                        break;
                }
            }
        }

        private static void JungleClear()
        {
            var minion =
                MinionManager.GetMinions(spells[Spells.Q].Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth)
                    .FirstOrDefault();

            if (!minion.IsValidTarget() || minion == null)
            {
                return;
            }

            if (PassiveStacks > 1 || LastSpell + 400 > Environment.TickCount)
            {
                return;
            }

            if (spells[Spells.Q].IsReady() && ParamBool("ElLeeSin.Jungle.Q"))
            {
                if (QState && minion.Distance(Player) < spells[Spells.Q].Range && LastQ + 200 < Environment.TickCount)
                {
                    spells[Spells.Q].Cast(minion);
                    LastSpell = Environment.TickCount;
                    return;
                }

                spells[Spells.Q].Cast();
                LastSpell = Environment.TickCount;
                return;
            }

            if (spells[Spells.W].IsReady() && ParamBool("ElLeeSin.Jungle.W"))
            {
                if (WState && minion.Distance(Player) < Orbwalking.GetRealAutoAttackRange(Player))
                {
                    spells[Spells.W].CastOnUnit(Player);
                    LastSpell = Environment.TickCount;
                    return;
                }

                if (WState)
                {
                    return;
                }

                spells[Spells.W].Cast();
                LastSpell = Environment.TickCount;
                return;
            }

            if (spells[Spells.E].IsReady() && ParamBool("ElLeeSin.Jungle.E"))
            {
                if (EState && minion.Distance(Player) < spells[Spells.E].Range)
                {
                    spells[Spells.E].Cast();
                    LastSpell = Environment.TickCount;
                    return;
                }
                if (EState)
                {
                    return;
                }

                spells[Spells.E].Cast();
                LastSpell = Environment.TickCount;
            }
        }

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (SpellNames.Contains(args.SData.Name))
            {
                PassiveStacks = 2;
                passiveTimer = Environment.TickCount + 3000;
            }

            if (args.SData.Name == "BlindMonkQOne")
            {
                castQAgain = false;
                Utility.DelayAction.Add(2900, () => { castQAgain = true; });
            }

            if (args.SData.Name == "summonerflash" && insecComboStep != InsecComboStepSelect.None)
            {
                var target = ParamBool("insecMode")
                                 ? TargetSelector.GetSelectedTarget()
                                 : TargetSelector.GetTarget(
                                     spells[Spells.Q].Range + 200,
                                     TargetSelector.DamageType.Physical);

                insecComboStep = InsecComboStepSelect.Pressr;

                Utility.DelayAction.Add(80, () => spells[Spells.R].CastOnUnit(target, true));
            }
            if (args.SData.Name == "blindmonkqtwo")
            {
                waitingForQ2 = true;
                Utility.DelayAction.Add(3000, () => { waitingForQ2 = false; });
            }
            if (args.SData.Name == "BlindMonkRKick")
            {
                insecComboStep = InsecComboStepSelect.None;
            }

            switch (args.SData.Name)
            {
                case "BlindMonkQOne":
                    LastQ = Environment.TickCount;
                    LastSpell = Environment.TickCount;
                    PassiveStacks = 2;
                    break;
                case "BlindMonkWOne":
                    LastW = Environment.TickCount;
                    LastSpell = Environment.TickCount;
                    PassiveStacks = 2;
                    break;
                case "BlindMonkEOne":
                    LastE = Environment.TickCount;
                    LastSpell = Environment.TickCount;
                    PassiveStacks = 2;
                    break;
                case "blindmonkqtwo":
                    LastQ2 = Environment.TickCount;
                    LastSpell = Environment.TickCount;
                    PassiveStacks = 2;
                    CheckQ = false;
                    break;
                case "blindmonkwtwo":
                    LastW2 = Environment.TickCount;
                    LastSpell = Environment.TickCount;
                    PassiveStacks = 2;
                    break;
                case "blindmonketwo":
                    LastQ = Environment.TickCount;
                    LastSpell = Environment.TickCount;
                    PassiveStacks = 2;
                    break;
                case "BlindMonkRKick":
                    LastR = Environment.TickCount;
                    LastSpell = Environment.TickCount;
                    PassiveStacks = 2;
                    break;
            }
        }

        private static void Orbwalk(Vector3 pos, Obj_AI_Hero target = null)
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, pos);
        }

        private static void OrbwalkingAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (unit.IsMe && PassiveStacks > 0)
            {
                PassiveStacks = PassiveStacks - 1;
            }
        }

        private static float Q2Damage(Obj_AI_Base target, float subHP = 0, bool monster = false)
        {
            var damage = (50 + (spells[Spells.Q].Level * 30)) + (0.09 * Player.FlatPhysicalDamageMod)
                         + ((target.MaxHealth - (target.Health - subHP)) * 0.08);
            if (monster && damage > 400)
            {
                return (float)Player.CalcDamage(target, Damage.DamageType.Physical, 400);
            }
            return (float)Player.CalcDamage(target, Damage.DamageType.Physical, damage);
        }

        private static Obj_AI_Base ReturnQBuff()
        {
            foreach (var unit in ObjectManager.Get<Obj_AI_Base>().Where(a => a.IsValidTarget(1300)))
            {
                if (unit.HasQBuff())
                {
                    return unit;
                }
            }

            return null;
        }

        private static string SmiteSpellName()
        {
            if (SmiteBlue.Any(a => Items.HasItem(a)))
            {
                return "s5_summonersmiteplayerganker";
            }

            if (SmiteRed.Any(a => Items.HasItem(a)))
            {
                return "s5_summonersmiteduel";
            }

            if (SmiteGrey.Any(a => Items.HasItem(a)))
            {
                return "s5_summonersmitequick";
            }

            if (SmitePurple.Any(a => Items.HasItem(a)))
            {
                return "itemsmiteaoe";
            }

            return "summonersmite";
        }

        private static void StarCombo()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Physical);
            if (target == null)
            {
                Orbwalking.Orbwalk(null, Game.CursorPos);
                return;
            }

            Orbwalking.Orbwalk(Orbwalking.InAutoAttackRange(target) ? target : null, Game.CursorPos);
            if (!target.IsValidTarget())
            {
                return;
            }

            if (target.HasBuffOfType(BuffType.Knockback) && target.Distance(Player) > 300 && target.HasQBuff()
                && !QState)
            {
                spells[Spells.Q].Cast();
                return;
            }

            UseItems(target);

            if (!spells[Spells.R].IsReady())
            {
                return;
            }

            if (spells[Spells.Q].IsReady() && QState)
            {
                CastQ(target, ParamBool("qSmite"));
                return;
            }

            if (target.HasQBuff() && !target.HasBuffOfType(BuffType.Knockback))
            {
                if (target.Distance(Player) < spells[Spells.R].Range && spells[Spells.R].IsReady())
                {
                    spells[Spells.R].CastOnUnit(target);
                    return;
                }

                if (target.Distance(Player) < 600 && WState)
                {
                    WardJump(target.Position, false, true);
                }
            }
        }

        private static void UseClearItems(Obj_AI_Base enemy)
        {
            if (Items.CanUseItem(3077) && Player.Distance(enemy) < 350)
            {
                Items.UseItem(3077);
            }
            if (Items.CanUseItem(3074) && Player.Distance(enemy) < 350)
            {
                Items.UseItem(3074);
            }
        }

        private static void UseItems(Obj_AI_Base target)
        {
            if (ItemData.Ravenous_Hydra_Melee_Only.GetItem().IsReady()
                && ItemData.Ravenous_Hydra_Melee_Only.Range > Player.Distance(target))
            {
                ItemData.Ravenous_Hydra_Melee_Only.GetItem().Cast();
            }
            if (ItemData.Tiamat_Melee_Only.GetItem().IsReady()
                && ItemData.Tiamat_Melee_Only.Range > Player.Distance(target))
            {
                ItemData.Tiamat_Melee_Only.GetItem().Cast();
            }

            if (ItemData.Blade_of_the_Ruined_King.GetItem().IsReady()
                && ItemData.Blade_of_the_Ruined_King.Range > Player.Distance(target))
            {
                ItemData.Blade_of_the_Ruined_King.GetItem().Cast(target);
            }

            if (ItemData.Youmuus_Ghostblade.GetItem().IsReady()
                && Orbwalking.GetRealAutoAttackRange(Player) > Player.Distance(target))
            {
                ItemData.Youmuus_Ghostblade.GetItem().Cast();
            }
        }

        private static Vector2 V2E(Vector3 from, Vector3 direction, float distance)
        {
            return from.To2D() + distance * Vector3.Normalize(direction - from).To2D();
        }

        private static void Waiter()
        {
            waitforjungle = true;
            Utility.DelayAction.Add(300, () => waitforjungle = false);
        }

        private static void WardJump(
            Vector3 pos,
            bool m2M = true,
            bool maxRange = false,
            bool reqinMaxRange = false,
            bool minions = true,
            bool champions = true)
        {
            if (WStage != WCastStage.First)
            {
                return;
            }

            var basePos = Player.Position.To2D();
            var newPos = (pos.To2D() - Player.Position.To2D());

            if (JumpPos == new Vector2())
            {
                if (reqinMaxRange)
                {
                    JumpPos = pos.To2D();
                }
                else if (maxRange || Player.Distance(pos) > 590)
                {
                    JumpPos = basePos + (newPos.Normalized() * (590));
                }
                else
                {
                    JumpPos = basePos + (newPos.Normalized() * (Player.Distance(pos)));
                }
            }
            if (JumpPos != new Vector2() && reCheckWard)
            {
                reCheckWard = false;
                Utility.DelayAction.Add(
                    20,
                    () =>
                        {
                            if (JumpPos != new Vector2())
                            {
                                JumpPos = new Vector2();
                                reCheckWard = true;
                            }
                        });
            }
            if (m2M)
            {
                Orbwalk(pos);
            }
            if (!spells[Spells.W].IsReady() || spells[Spells.W].Instance.Name == "blindmonkwtwo"
                || reqinMaxRange && Player.Distance(pos) > spells[Spells.W].Range)
            {
                return;
            }

            if (minions || champions)
            {
                if (champions)
                {
                    var champs = (from champ in ObjectManager.Get<Obj_AI_Hero>()
                                  where
                                      champ.IsAlly && champ.Distance(Player) < spells[Spells.W].Range
                                      && champ.Distance(pos) < 200 && !champ.IsMe
                                  select champ).ToList();
                    if (champs.Count > 0 && WStage == WCastStage.First)
                    {
                        if (500 >= Environment.TickCount - wcasttime || WStage != WCastStage.First)
                        {
                            return;
                        }

                        CastW(champs[0]);
                        return;
                    }
                }
                if (minions)
                {
                    var minion2 = (from minion in ObjectManager.Get<Obj_AI_Minion>()
                                   where
                                       minion.IsAlly && minion.Distance(Player) < spells[Spells.W].Range
                                       && minion.Distance(pos) < 200 && !minion.Name.ToLower().Contains("ward")
                                   select minion).ToList();
                    if (minion2.Count > 0 && WStage == WCastStage.First)
                    {
                        if (500 >= Environment.TickCount - wcasttime || WStage != WCastStage.First)
                        {
                            return;
                        }

                        CastW(minion2[0]);
                        return;
                    }
                }
            }

            var isWard = false;
            foreach (var ward in ObjectManager.Get<Obj_AI_Base>())
            {
                if (ward.IsAlly && ward.Name.ToLower().Contains("ward") && ward.Distance(JumpPos) < 200)
                {
                    isWard = true;
                    if (500 >= Environment.TickCount - wcasttime || WStage != WCastStage.First) //credits to JackisBack
                    {
                        return;
                    }

                    CastW(ward);
                    wcasttime = Environment.TickCount;
                }
            }

            var delay = InitMenu.Menu.Item("Ward.Delay").GetValue<Slider>().Value;

            if (!isWard && castWardAgain)
            {
                var ward = FindBestWardItem();
                if (ward == null || WStage != WCastStage.First)
                {
                    return;
                }

                Utility.DelayAction.Add(delay, () => Player.Spellbook.CastSpell(ward.SpellSlot, JumpPos.To3D()));
                lastWardPos = JumpPos.To3D();
            }
        }

        private static void WardjumpToMouse()
        {
            WardJump(
                Game.CursorPos,
                ParamBool("ElLeeSin.Wardjump.Mouse"),
                false,
                false,
                ParamBool("ElLeeSin.Wardjump.Minions"),
                ParamBool("ElLeeSin.Wardjump.Champions"));
        }

        #endregion
    }
}