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

        private static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707, 3930 };

        private static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719, 3932 };

        private static readonly int[] SmitePurple = { 3713, 3726, 3725, 3724, 3723, 3933 };

        private static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714, 3931 };

        private static readonly string[] SpellNames =
            {
                "BlindMonkQOne", "BlindMonkWOne", "BlindMonkEOne",
                "blindmonkwtwo", "blindmonkqtwo", "blindmonketwo",
                "BlindMonkRKick"
            };

        private static readonly ItemId[] WardIds =
            {
                ItemId.Warding_Totem_Trinket, ItemId.Greater_Stealth_Totem_Trinket,
                ItemId.Greater_Vision_Totem_Trinket, ItemId.Sightstone,
                ItemId.Ruby_Sightstone, (ItemId)3711, (ItemId)1411, (ItemId)1410,
                (ItemId)1408, (ItemId)1409
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

            var turrets = (from tower in ObjectManager.Get<Obj_Turret>()
                           where
                               tower.IsAlly && !tower.IsDead
                               && target.Distance(tower.Position)
                               < 1500 + InitMenu.Menu.Item("bonusRangeT").GetValue<Slider>().Value && tower.Health > 0
                           select tower).ToList();

            if (GetAllyHeroes(target, 2000 + InitMenu.Menu.Item("bonusRangeA").GetValue<Slider>().Value).Count > 0
                && ParamBool("ElLeeSin.Insec.Ally"))
            {
                var insecPosition =
                    InterceptionPoint(
                        GetAllyInsec(
                            GetAllyHeroes(target, 2000 + InitMenu.Menu.Item("bonusRangeA").GetValue<Slider>().Value)));
                InsecLinePos = Drawing.WorldToScreen(insecPosition);
                return V2E(insecPosition, target.Position, target.Distance(insecPosition) + 230).To3D();
            }

            if (turrets.Any() && ParamBool("ElLeeSin.Insec.Tower"))
            {
                InsecLinePos = Drawing.WorldToScreen(turrets[0].Position);
                return V2E(turrets[0].Position, target.Position, target.Distance(turrets[0].Position) + 230).To3D();
            }

            if (ParamBool("ElLeeSin.Insec.Original.Pos"))
            {
                InsecLinePos = Drawing.WorldToScreen(insecPos);
                return V2E(insecPos, target.Position, target.Distance(insecPos) + 230).To3D();
            }

            if (ParamBool("insecmouse"))
            {
                InsecLinePos = Drawing.WorldToScreen(insecPos);
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
            }

            if (spells[Spells.Q].IsReady() && ParamBool("ElLeeSin.Lane.Q") && LastQ + 200 < Environment.TickCount)
            {
                if (QState && minions.Distance(Player) < spells[Spells.Q].Range)
                {
                    spells[Spells.Q].Cast(minions);
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
            if (!spells[Spells.Q].IsReady() || !target.IsValidTarget(spells[Spells.Q].Range))
            {
                return;
            }

            var prediction = spells[Spells.Q].GetPrediction(target);

            if (prediction.Hitchance != HitChance.Impossible && prediction.Hitchance != HitChance.OutOfRange
                && prediction.Hitchance != HitChance.Collision && prediction.Hitchance >= HitChance.High)
            {
                spells[Spells.Q].Cast(target);
            }
            else if (ParamBool("qSmite") && spells[Spells.Q].IsReady() && target.IsValidTarget(spells[Spells.Q].Range)
                     && prediction.CollisionObjects.Count(a => a.NetworkId != target.NetworkId && a.IsMinion) == 1
                     && Player.GetSpellSlot(SmiteSpellName()).IsReady())
            {
                Player.Spellbook.CastSpell(
                    Player.GetSpellSlot(SmiteSpellName()),
                    prediction.CollisionObjects.Where(a => a.NetworkId != target.NetworkId && a.IsMinion).ToList()[0
                        ]);

                spells[Spells.Q].Cast(prediction.CastPosition);
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

            if (target.HasQBuff() && ParamBool("ElLeeSin.Combo.Q2"))
            {
                if (castQAgain
                    || target.HasBuffOfType(BuffType.Knockback) && !Player.IsValidTarget(300)
                    && !spells[Spells.R].IsReady() || !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player))
                    || spells[Spells.Q].GetDamage(target, 1) > target.Health
                    || ReturnQBuff().Distance(target) < Player.Distance(target)
                    && !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                {
                    spells[Spells.Q].Cast(target);
                }
            }

            if (spells[Spells.R].GetDamage(target) >= target.Health && ParamBool("ElLeeSin.Combo.KS.R")
                && target.IsValidTarget())
            {
                spells[Spells.R].Cast(target);
            }

            if (ParamBool("ElLeeSin.Combo.AAStacks")
                && PassiveStacks > InitMenu.Menu.Item("ElLeeSin.Combo.PassiveStacks").GetValue<Slider>().Value
                && Orbwalking.GetRealAutoAttackRange(Player) > Player.Distance(target))
            {
                return;
            }

            if (ParamBool("ElLeeSin.Combo.W"))
            {
                if (ParamBool("ElLeeSin.Combo.Mode.WW")
                    && target.Distance(Player) > Orbwalking.GetRealAutoAttackRange(Player))
                {
                    WardJump(target.Position, false, true);
                }

                if (!ParamBool("ElLeeSin.Combo.Mode.WW") && target.Distance(Player) > spells[Spells.Q].Range)
                {
                    WardJump(target.Position, false, true);
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

            if (spells[Spells.Q].IsReady() && spells[Spells.Q].Instance.Name == "BlindMonkQOne"
                && ParamBool("ElLeeSin.Combo.Q"))
            {
                CastQ(target, ParamBool("qSmite"));
            }

            if (spells[Spells.R].IsReady() && spells[Spells.Q].IsReady() && target.HasQBuff()
                && ParamBool("ElLeeSin.Combo.R"))
            {
                spells[Spells.R].CastOnUnit(target);
            }
        }

        private static InventorySlot FindBestWardItem()
        {
            return
                WardIds.Select(wardId => Player.InventoryItems.FirstOrDefault(a => a.Id == wardId))
                    .FirstOrDefault(slot => slot != null);
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
            //Console.WriteLine(FindBestWardItem() == );

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

            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
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
                WardCombo();
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

        private static List<Obj_AI_Hero> GetAllyHeroes(Obj_AI_Hero position, int range)
        {
            var temp = new List<Obj_AI_Hero>();
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsAlly && !hero.IsMe && !hero.IsDead && hero.Distance(position) < range)
                {
                    temp.Add(hero);
                }
            }
            return temp;
        }

        private static List<Obj_AI_Hero> GetAllyInsec(List<Obj_AI_Hero> heroes)
        {
            byte alliesAround = 0;
            var tempObject = new Obj_AI_Hero();
            foreach (var hero in heroes)
            {
                var localTemp =
                    GetAllyHeroes(hero, 500 + InitMenu.Menu.Item("bonusRangeA").GetValue<Slider>().Value).Count;
                if (localTemp > alliesAround)
                {
                    tempObject = hero;
                    alliesAround = (byte)localTemp;
                }
            }
            return GetAllyHeroes(tempObject, 500 + InitMenu.Menu.Item("bonusRangeA").GetValue<Slider>().Value);
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

            if (ParamBool("ElLeeSin.Combo.AAStacks")
                && PassiveStacks > InitMenu.Menu.Item("ElLeeSin.Harass.PassiveStacks").GetValue<Slider>().Value
                && Orbwalking.GetRealAutoAttackRange(Player) > Player.Distance(target))
            {
                return;
            }

            if (spells[Spells.Q].IsReady() && ParamBool("ElLeeSin.Harass.Q1") && LastQ + 200 < Environment.TickCount)
            {
                if (QState && target.Distance(Player) < spells[Spells.Q].Range)
                {
                    CastQ(target, ParamBool("qSmite"));
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

        private static void InsecCombo(Obj_AI_Hero target)
        {
            /* if (Player.Mana < 80)
            {
                return;
            }*/

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
                            if (ParamBool("checkOthers"))
                            {
                                foreach (var insecMinion in
                                    ObjectManager.Get<Obj_AI_Minion>()
                                        .Where(
                                            x =>
                                            x.Health > spells[Spells.Q].GetDamage(x) && x.IsValidTarget()
                                            && x.Distance(GetInsecPos(target)) < 0x1c2)
                                        .ToList())
                                {
                                    spells[Spells.Q].Cast(insecMinion);
                                }
                            }

                            CastQ(target, ParamBool("qSmite"));
                        }

                        else if (target.HasQBuff())
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
                        if (Player.Distance(target) < 600)
                        {
                            if (FindBestWardItem() == null && GetInsecPos(target).Distance(Player.Position) < 400)
                            {
                                if (spells[Spells.R].IsReady()
                                    && Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Ready
                                    && ParamBool("flashInsec") && LastWard + 1000 < Environment.TickCount)
                                {
                                    Player.Spellbook.CastSpell(flashSlot, GetInsecPos(target));
                                    return;
                                }
                            }
                            WardJump(GetInsecPos(target), false, false, true);
                        }

                        if (Player.Distance(GetInsecPos(target)) < 200)
                        {
                            spells[Spells.R].Cast(target);
                        }
                        break;

                    case InsecComboStepSelect.Pressr:
                        spells[Spells.R].CastOnUnit(target);
                        break;
                }
            }
        }

        private static Vector3 InterceptionPoint(List<Obj_AI_Hero> heroes)
        {
            var result = new Vector3();
            foreach (var hero in heroes)
            {
                result += hero.Position;
            }
            result.X /= heroes.Count;
            result.Y /= heroes.Count;
            return result;
        }

        private static void JungleClear()
        {
            var minion =
                MinionManager.GetMinions(
                    spells[Spells.Q].Range,
                    MinionTypes.All,
                    MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth).FirstOrDefault();

            if (!minion.IsValidTarget() || minion == null)
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

            if (PassiveStacks > 0 || LastSpell + 400 > Environment.TickCount)
            {
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
                if (EState && minion.Distance(Player) < spells[Spells.E].Range && LastE + 200 < Environment.TickCount)
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

            if (InitMenu.Menu.Item("ElLeeSin.Insec.Insta.Flashx").GetValue<KeyBind>().Active
                && args.SData.Name == "BlindMonkRKick")
            {
                Player.Spellbook.CastSpell(flashSlot, GetInsecPos((Obj_AI_Hero)(args.Target)));
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
                PassiveStacks--;
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

        private static void WardCombo()
        {
            var target = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Physical);

            Orbwalking.Orbwalk(
                target ?? null,
                Game.CursorPos,
                InitMenu.Menu.Item("ExtraWindup").GetValue<Slider>().Value,
                InitMenu.Menu.Item("HoldPosRadius").GetValue<Slider>().Value);

            if (target == null)
            {
                return;
            }

            UseItems(target);

            if (target.HasQBuff())
            {
                if (castQAgain
                    || target.HasBuffOfType(BuffType.Knockback) && !Player.IsValidTarget(300)
                    && !spells[Spells.R].IsReady()
                    || !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && !spells[Spells.R].IsReady())
                {
                    spells[Spells.Q].Cast();
                }
            }
            if (target.Distance(Player) > spells[Spells.R].Range
                && target.Distance(Player) < spells[Spells.R].Range + 580 && target.HasQBuff())
            {
                WardJump(target.Position, false);
            }
            if (spells[Spells.E].IsReady() && EState && Player.Distance(target) < spells[Spells.E].Range)
            {
                spells[Spells.E].Cast();
            }

            if (spells[Spells.Q].IsReady() && QState)
            {
                CastQ(target);
            }

            if (spells[Spells.R].IsReady() && spells[Spells.Q].IsReady() && target.HasQBuff())
            {
                spells[Spells.R].CastOnUnit(target);
            }
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

            if (!isWard && castWardAgain)
            {
                var ward = FindBestWardItem();
                if (ward == null || WStage != WCastStage.First)
                {
                    return;
                }

                Player.Spellbook.CastSpell(ward.SpellSlot, JumpPos.To3D());

                lastWardPos = JumpPos.To3D();
                LastWard = Environment.TickCount;
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