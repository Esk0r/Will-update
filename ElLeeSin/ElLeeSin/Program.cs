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

        //private static readonly bool castWardAgain = true;

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

        private static SpellSlot flashSlot;

        private static SpellSlot igniteSlot;

        private static Vector3 mouse = Game.CursorPos;

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

        #endregion

        #region Public Properties

        public static Obj_AI_Base BuffedEnemy
        {
            get
            {
                return ObjectManager.Get<Obj_AI_Base>().FirstOrDefault(unit => unit.IsEnemy && unit.HasQBuff());
            }
        }

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
                    spells[Spells.Q].Cast(target);
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
            Orbwalking.AfterAttack += OrbwalkingAfterAttack;
            Game.OnWndProc += InsecHandler.OnClick;
            GameObject.OnDelete += Obj_AI_Hero_OnCreate;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }

            if (InitMenu.Menu.Item("starCombo").GetValue<KeyBind>().Active)
            {
                StarCombo();
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

            if (InitMenu.Menu.Item("insec").GetValue<KeyBind>().Active)
            {
                InsecHandler.DoInsec();
                return;
            }
            InsecHandler.FlashPos = new Vector3();
            InsecHandler.FlashR = false;

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
                WardjumpHandler.Jump(
                    Game.CursorPos,
                    InitMenu.Menu.Item("ElLeeSin.Wardjump.MaxRange").GetValue<bool>(),
                    true);
            }
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
            if (args.SData.Name.ToLower().Contains("ward") || args.SData.Name.ToLower().Contains("totem"))
            {
                LastWard = Environment.TickCount;
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
                    if (InsecHandler.FlashR)
                    {
                        Player.Spellbook.CastSpell(Player.GetSpellSlot("summonerflash"), InsecHandler.FlashPos);
                        InsecHandler.FlashPos = new Vector3();
                        InsecHandler.FlashR = false;
                    }
                    break;
            }
        }

        private static void Obj_AI_Hero_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Position.Distance(Player.Position) > 200)
            {
                return;
            }
            if (sender.Name == "blindMonk_Q_resonatingStrike_tar_blood.troy")
            {
                CheckQ = true;
            }
        }

        private static void OrbwalkingAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (unit.IsMe)
            {
                if (PassiveStacks == 0)
                {
                    return;
                }
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
                    WardjumpHandler.Jump(
                        Player.Position.Extend(target.Position, Player.Position.Distance(target.Position) - 50));
                }
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

        #endregion
    }
}