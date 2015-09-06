namespace ElLeeSin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using ItemData = LeagueSharp.Common.Data.ItemData;

    internal class Program
    {
        #region Static Fields

        public static bool ClicksecEnabled;

        public static Vector3 InsecClickPos;

        public static Vector2 InsecLinePos;

        public static Vector2 JumpPos;

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

        private static readonly string ChampName = "LeeSin";

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

        private static int passiveStacks;

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

        private enum WCastStage
        {
            First,

            Second,

            Cooldown
        }

        private enum InsecComboStepSelect
        {
            None,

            Qgapclose,

            Wgapclose,

            Pressr
        };

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
                               < 1500 + InitMenu.Menu.Item("ElLeeSin.Insec.Tower.BonusRange").GetValue<Slider>().Value
                               && tower.Health > 0
                           select tower).ToList();

            if (
                GetAllyHeroes(target, 2000 + InitMenu.Menu.Item("ElLeeSin.Insec.BonusRange").GetValue<Slider>().Value)
                    .Count > 0 && ParamBool("ElLeeSin.Insec.Ally"))
            {
                var insecPosition =
                    InterceptionPoint(
                        GetAllyInsec(
                            GetAllyHeroes(
                                target,
                                2000 + InitMenu.Menu.Item("ElLeeSin.Insec.BonusRange").GetValue<Slider>().Value)));
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
            return new Vector3();
        }

        public static bool ParamBool(string paramName)
        {
            return InitMenu.Menu.Item(paramName).GetValue<bool>();
        }

        #endregion

        #region Methods

        private static void AllClear()
        {
            var minion = MinionManager.GetMinions(Player.ServerPosition, spells[Spells.W].Range).FirstOrDefault();
            if (minion == null)
            {
                return;
            }

            if (ParamBool("ElLeeSin.Lane.Q") && spells[Spells.Q].IsReady())
            {
                if (spells[Spells.Q].Instance.Name == "BlindMonkQOne")
                {
                    spells[Spells.Q].Cast(minion.Position);
                }
                else if ((minion.HasBuff("BlindMonkQOne") || minion.HasBuff("blindmonkqonechaos"))
                         && (spells[Spells.Q].IsKillable(minion, 1)) || Player.Distance(minion) > 500)
                {
                    spells[Spells.Q].Cast();
                }
            }

            UseClearItems(minion);

            if (ParamBool("ElLeeSin.Lane.E") && spells[Spells.E].IsReady())
            {
                if (spells[Spells.E].Instance.Name == "BlindMonkEOne" && minion.IsValidTarget(spells[Spells.E].Range)
                    && !delayW)
                {
                    spells[Spells.E].Cast();
                    delayW = true;
                    Utility.DelayAction.Add(300, () => delayW = false);
                }
                else if (minion.HasBuff("BlindMonkEOne") && (Player.Distance(minion) < 450))
                {
                    spells[Spells.E].Cast();
                }
            }
        }

        private static void CastHydra()
        {
            if (Player.IsWindingUp)
            {
                return;
            }

            if (!ItemData.Tiamat_Melee_Only.GetItem().IsReady()
                && !ItemData.Ravenous_Hydra_Melee_Only.GetItem().IsReady())
            {
                return;
            }

            ItemData.Tiamat_Melee_Only.GetItem().Cast();
            ItemData.Ravenous_Hydra_Melee_Only.GetItem().Cast();
        }

        private static void CastQ1(Obj_AI_Hero target)
        {
            var qpred = spells[Spells.Q].GetPrediction(target);
            if ((qpred.CollisionObjects.Where(a => a.IsValidTarget() && a.IsMinion).ToList().Count) == 1
                && smiteSlot.IsReady() && ParamBool("qSmite") && qpred.CollisionObjects[0].IsValidTarget(780))
            {
                Player.Spellbook.CastSpell(smiteSlot, qpred.CollisionObjects[0]);
                Utility.DelayAction.Add(Game.Ping / 2, () => spells[Spells.Q].Cast(qpred.CastPosition));
            }
            else if (qpred.CollisionObjects.Count == 0)
            {
                if (qpred.Hitchance >= HitChance.VeryHigh)
                {
                    spells[Spells.Q].Cast(target);
                }
            }
        }

        private static void CastW(Obj_AI_Base obj)
        {
            if (500 >= Environment.TickCount - wcasttime || WStage != WCastStage.First)
            {
                return;
            }

            spells[Spells.W].CastOnUnit(obj);
            Utility.DelayAction.Add(3000, () => spells[Spells.W].Cast());
            wcasttime = Environment.TickCount;
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
            if (Player.ChampionName != ChampName)
            {
                return;
            }
            igniteSlot = Player.GetSpellSlot("SummonerDot");
            flashSlot = Player.GetSpellSlot("summonerflash");

            spells[Spells.Q].SetSkillshot(0.25f, 60f, 1800f, true, SkillshotType.SkillshotLine);

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
                passiveStacks = 0;
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
                    StarCombo();
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
                if (hero.IsAlly && !hero.IsMe && hero.Distance(position) < range)
                {
                    temp.Add(hero);
                }
            }
            return temp;
        }

        private static List<Obj_AI_Hero> GetAllyInsec(List<Obj_AI_Hero> heroes)
        {
            var alliesAround = 0;
            var tempObject = new Obj_AI_Hero();
            foreach (var hero in heroes)
            {
                var localTemp =
                    GetAllyHeroes(hero, 500 + InitMenu.Menu.Item("ElLeeSin.Insec.BonusRange").GetValue<Slider>().Value)
                        .Count;
                if (localTemp > alliesAround)
                {
                    tempObject = hero;
                    alliesAround = localTemp;
                }
            }
            return GetAllyHeroes(
                tempObject,
                500 + InitMenu.Menu.Item("ElLeeSin.Insec.BonusRange").GetValue<Slider>().Value);
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

            var q = ParamBool("ElLeeSin.Harass.Q1");
            var q2 = ParamBool("ElLeeSin.Harass.Q2");
            var e = ParamBool("ElLeeSin.Harass.E1");
            var w = ParamBool("ElLeeSin.Harass.Wardjump");

            if (q && spells[Spells.Q].IsReady() && spells[Spells.Q].Instance.Name == "BlindMonkQOne"
                && target.IsValidTarget(spells[Spells.Q].Range))
            {
                CastQ1(target);
            }
            if (q2 && spells[Spells.Q].IsReady()
                && (target.HasBuff("BlindMonkQOne") || target.HasBuff("blindmonkqonechaos")))
            {
                if (castQAgain || !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                {
                    spells[Spells.Q].Cast();
                }
            }
            if (e && spells[Spells.E].IsReady() && target.IsValidTarget(spells[Spells.E].Range)
                && spells[Spells.E].Instance.Name == "BlindMonkEOne")
            {
                spells[Spells.E].Cast();
            }

            if (w && Player.Distance(target) < 50
                && !(target.HasBuff("BlindMonkQOne") && !target.HasBuff("blindmonkqonechaos"))
                && (spells[Spells.E].Instance.Name == "blindmonketwo" || !spells[Spells.E].IsReady() && e)
                && (spells[Spells.Q].Instance.Name == "blindmonkqtwo" || !spells[Spells.Q].IsReady() && q))
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
                        if (!(target.HasBuff("BlindMonkQOne") || target.HasBuff("blindmonkqonechaos"))
                            && spells[Spells.Q].Instance.Name == "BlindMonkQOne")
                        {
                            CastQ1(target);
                        }
                        else if ((target.HasBuff("BlindMonkQOne") || target.HasBuff("blindmonkqonechaos")))
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
                                && (spells[Spells.Q].Instance.Name == "BlindMonkQOne"
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
                    Player.ServerPosition,
                    spells[Spells.Q].Range,
                    MinionTypes.All,
                    MinionTeam.Neutral,
                    MinionOrderTypes.MaxHealth).FirstOrDefault();

            if (minion == null)
            {
                return;
            }

            var passiveIsActive = passiveStacks > 0;
            UseClearItems(minion);

            if (passiveIsActive || waitforjungle)
            {
                return;
            }

            if (ParamBool("ElLeeSin.Jungle.Q") && spells[Spells.Q].IsReady()
                && minion.IsValidTarget(spells[Spells.Q].Range))
            {
                if (spells[Spells.Q].Instance.Name == "BlindMonkQOne")
                {
                    spells[Spells.Q].Cast(minion.Position);
                    Waiter();
                    return;
                }

                if ((minion.HasBuff("BlindMonkQOne") || minion.HasBuff("blindmonkqonechaos")))
                {
                    spells[Spells.Q].Cast();
                    Waiter();
                    return;
                }
            }

            if (InAutoAttackRange(minion))
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, minion);
            }

            if (ParamBool("ElLeeSin.Jungle.Q")
                && Q2Damage(
                    minion,
                    spells[Spells.Q].Instance.Name == "BlindMonkQOne"
                        ? minion.Health - spells[Spells.Q].GetDamage(minion)
                        : minion.Health,
                    true) > minion.Health && spells[Spells.Q].IsReady())
            {
                if (spells[Spells.Q].Instance.Name == "BlindMonkQOne")
                {
                    spells[Spells.Q].Cast(minion.Position);
                    Waiter();
                    return;
                }
                spells[Spells.Q].Cast();
                Waiter();
                return;
            }

            if (spells[Spells.E].IsReady() && minion.IsValidTarget(spells[Spells.E].Range)
                && ParamBool("ElLeeSin.Jungle.E"))
            {
                spells[Spells.E].Cast();

                Waiter();
                return;
            }

            if (spells[Spells.E].IsReady() && spells[Spells.E].Instance.Name != "BlindMonkEOne"
                && !minion.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && ParamBool("ElLeeSin.Jungle.E"))
            {
                Utility.DelayAction.Add(200, () => spells[Spells.E].Cast());
            }

            if (ParamBool("ElLeeSin.Jungle.W") && spells[Spells.W].IsReady()
                && minion.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player) + 200))
            {
                if (spells[Spells.W].Instance.Name == "BlindMonkWOne")
                {
                    spells[Spells.W].Cast();
                    Waiter();
                    return;
                }

                if (spells[Spells.W].Instance.Name != "BlindMonkWOne")
                {
                    Utility.DelayAction.Add(300, () => spells[Spells.W].Cast());
                }
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
                passiveStacks = 2;
                passiveTimer = Environment.TickCount + 3000;
            }

            if (args.SData.Name == "BlindMonkQOne")
            {
                castQAgain = false;
                Utility.DelayAction.Add(2900, () => { castQAgain = true; });
            }
            //
            if (ParamBool("ElLeeSin.Insec.UseInstaFlash")
                && InitMenu.Menu.Item("ElLeeSin.Insec.Insta.Flash").GetValue<KeyBind>().Active
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
        }

        private static void Orbwalk(Vector3 pos, Obj_AI_Hero target = null)
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, pos);
        }

        private static void OrbwalkingAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (unit.IsMe && passiveStacks > 0)
            {
                passiveStacks = passiveStacks - 1;
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
                if (unit.HasBuff("BlindMonkQOne") || unit.HasBuff("blindmonkqonechaos"))
                {
                    return unit;
                }
            }

            return null;
        }

        private static void StarCombo()
        {
            var target = TargetSelector.GetTarget(1300, TargetSelector.DamageType.Physical);
            if (target == null)
            {
                return;
            }
            if ((target.HasBuff("BlindMonkQOne") || target.HasBuff("blindmonkqonechaos"))
                && ParamBool("ElLeeSin.Combo.Q2"))
            {
                if (castQAgain
                    || target.HasBuffOfType(BuffType.Knockup) && !Player.IsValidTarget(300)
                    && !spells[Spells.R].IsReady() || !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player))
                    || spells[Spells.Q].GetDamage(target, 1) > target.Health
                    || ReturnQBuff().Distance(target) < Player.Distance(target)
                    && !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                {
                    spells[Spells.Q].Cast();
                }
            }

            if (spells[Spells.R].GetDamage(target) >= target.Health && ParamBool("ElLeeSin.Combo.KS.R")
                && target.IsValidTarget())
            {
                spells[Spells.R].Cast(target);
            }

            if (ParamBool("ElLeeSin.Combo.AAStacks") && passiveStacks > 0
                && target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player) + 100))
            {
                return;
            }

            if (ParamBool("ElLeeSin.Combo.W"))
            {
                if (ParamBool("ElLeeSin.Combo.Mode.W")
                    && target.Distance(Player) > Orbwalking.GetRealAutoAttackRange(Player))
                {
                    WardJump(target.Position, false, true);
                }
                if (!ParamBool("ElLeeSin.Combo.Mode.W") && target.Distance(Player) > spells[Spells.Q].Range)
                {
                    WardJump(target.Position, false, true);
                }
            }

            if (spells[Spells.E].IsReady() && spells[Spells.E].Instance.Name == "BlindMonkEOne"
                && InAutoAttackRange(target) && ParamBool("ElLeeSin.Combo.E"))
            {
                spells[Spells.E].Cast();

                if (target.IsValidTarget(0x190))
                {
                    CastHydra();
                }
            }

            if (spells[Spells.W].IsReady() && spells[Spells.W].Instance.Name == "BlindMonkWOne"
                && InAutoAttackRange(target) && ParamBool("ElLeeSin.Combo.W2"))
            {
                spells[Spells.W].Cast();
            }

            if (InAutoAttackRange(target))
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }

            if (spells[Spells.W].IsReady() && spells[Spells.W].Instance.Name != "BlindMonkWOne"
                && InAutoAttackRange(target) && ParamBool("ElLeeSin.Combo.W2"))
            {
                Utility.DelayAction.Add(400, () => spells[Spells.W].Cast());
            }

            if (spells[Spells.E].IsReady() && spells[Spells.E].Instance.Name != "BlindMonkEOne"
                && !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && ParamBool("ElLeeSin.Combo.E"))
            {
                Utility.DelayAction.Add(200, () => spells[Spells.E].Cast());
            }

            if (spells[Spells.Q].IsReady() && spells[Spells.Q].Instance.Name == "BlindMonkQOne"
                && ParamBool("ElLeeSin.Combo.Q"))
            {
                CastQ1(target);
            }

            if (spells[Spells.R].IsReady() && spells[Spells.Q].IsReady() && ParamBool("ElLeeSin.Combo.R")
                && spells[Spells.R].GetDamage(target) >= target.Health)
            {
                spells[Spells.R].CastOnUnit(target);
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

        private static void UseItems(Obj_AI_Hero enemy)
        {
            if (Items.CanUseItem(3142) && Player.Distance(enemy) <= 600)
            {
                Items.UseItem(3142);
            }
            if (Items.CanUseItem(3144) && Player.Distance(enemy) <= 450)
            {
                Items.UseItem(3144, enemy);
            }
            if (Items.CanUseItem(3153) && Player.Distance(enemy) <= 450)
            {
                Items.UseItem(3153, enemy);
            }
            if (Items.CanUseItem(3077) && Utility.CountEnemiesInRange(350) >= 1)
            {
                Items.UseItem(3077);
            }
            if (Items.CanUseItem(3074) && Utility.CountEnemiesInRange(350) >= 1)
            {
                Items.UseItem(3074);
            }
            if (Items.CanUseItem(3143) && Utility.CountEnemiesInRange(450) >= 1)
            {
                Items.UseItem(3143);
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

            if ((target.HasBuff("BlindMonkQOne") || target.HasBuff("blindmonkqonechaos")))
            {
                if (castQAgain
                    || target.HasBuffOfType(BuffType.Knockup) && !Player.IsValidTarget(300)
                    && !spells[Spells.R].IsReady()
                    || !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && !spells[Spells.R].IsReady())
                {
                    spells[Spells.Q].Cast();
                }
            }
            if (target.Distance(Player) > spells[Spells.R].Range
                && target.Distance(Player) < spells[Spells.R].Range + 580
                && (target.HasBuff("BlindMonkQOne") || target.HasBuff("blindmonkqonechaos")))
            {
                WardJump(target.Position, false);
            }
            if (spells[Spells.E].IsReady() && spells[Spells.E].Instance.Name == "BlindMonkEOne"
                && target.IsValidTarget(spells[Spells.E].Range))
            {
                spells[Spells.E].Cast();
            }

            if (spells[Spells.E].IsReady() && spells[Spells.E].Instance.Name != "BlindMonkEOne"
                && !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
            {
                spells[Spells.E].Cast();
            }

            if (spells[Spells.Q].IsReady() && spells[Spells.Q].Instance.Name == "BlindMonkQOne")
            {
                CastQ1(target);
            }

            if (spells[Spells.R].IsReady() && spells[Spells.Q].IsReady()
                && ((target.HasBuff("BlindMonkQOne") || target.HasBuff("blindmonkqonechaos"))))
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
                    Console.WriteLine("this");
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
                //castWardAgain = false;
                lastWardPos = JumpPos.To3D();
                //lastPlaced = Environment.TickCount;
                //Utility.DelayAction.Add(500, () => castWardAgain = true);
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