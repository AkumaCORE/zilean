namespace Zileanz
{
    using System;
    using System.Linq;

    using EloBuddy;
    using EloBuddy.SDK;
    using EloBuddy.SDK.Enumerations;
    using EloBuddy.SDK.Rendering;
    using EloBuddy.SDK.Events;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK.Menu.Values;

    using SharpDX;

    internal class Zilean
    {
        public static Spell.Skillshot Q { get; set; }

        public static Spell.Active W { get; set; }

        public static Spell.Targeted E { get; set; }

        public static Spell.Targeted R { get; set; }

        public static AIHeroClient Player => ObjectManager.Player;

        public static Menu ComboMenu { get; private set; }

        public static Menu UltMenu { get; private set; }

        public static Menu HarassMenu { get; private set; }

        public static Menu LaneMenu { get; private set; }

        public static Menu MiscMenu { get; private set; }

        public static Menu FleeMenu { get; private set; }

        public static Menu DrawMenu { get; private set; }

        public static Menu SkinMenu { get; private set; }

        public static SpellSlot IgniteSlot = SpellSlot.Unknown;

        private static Spell.Targeted ign;

        private static Menu ZilMenu;

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoad;
        }

        private static void OnLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != "Zilean")
            {
                return;
            }

            ZilMenu = MainMenu.AddMenu("Zilean", "Zilean");
            ZilMenu.AddGroupLabel("Welcome to FUE Zilean addon!");
            UltMenu = ZilMenu.AddSubMenu("Ultimate");
            UltMenu.AddGroupLabel("Ultimate Settings");
            UltMenu.Add("ultially", new CheckBox("Use ult on ally"));
            UltMenu.Add("allyhpult", new Slider("Ally Health %", 15, 1, 100));
            UltMenu.AddGroupLabel("Ult ally");
            foreach (var ally in ObjectManager.Get<AIHeroClient>().Where(hero => hero.IsAlly && !hero.IsMe))
            {
                CheckBox cb = new CheckBox(ally.BaseSkinName) { CurrentValue = false };
                if (ally.Team == ObjectManager.Player.Team)
                {
                    UltMenu.Add("castultally" + ally.BaseSkinName, cb);
                }
            }
            UltMenu.AddSeparator();
            UltMenu.Add("user", new CheckBox("Use ult on Myself"));
            UltMenu.Add("rhp", new Slider("Self Health %", 15, 1, 100));



            ComboMenu = ZilMenu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("Combo Settings");
            ComboMenu.Add("useQ", new CheckBox("Use Q"));
            ComboMenu.Add("useW1", new CheckBox("Use W", false));
            ComboMenu.Add("useW2", new CheckBox("Use W only if Q hits"));
            ComboMenu.Add("useE", new CheckBox("Use E"));
            ComboMenu.Add("Qcc", new CheckBox("Use Q on immobile"));
            ComboMenu.Add("useign", new CheckBox("Use Ignite"));


            HarassMenu = ZilMenu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass Settings");
            HarassMenu.Add("useQH", new CheckBox("Use Q"));
            HarassMenu.Add("useEH", new CheckBox("Use E", false));
            HarassMenu.Add("autoH", new KeyBind("Auto Harass", false, KeyBind.BindTypes.PressToggle, 'T'));
            HarassMenu.AddSeparator();
            HarassMenu.Add("autoHQ", new CheckBox("Use Q AutoHarass"));
            HarassMenu.Add("autoHE", new CheckBox("Use E AutoHarass"));
            HarassMenu.AddSeparator();
            HarassMenu.Add("HMana", new Slider("Min % mana for AutoHarass", 30, 0, 100));


            LaneMenu = ZilMenu.AddSubMenu("Farm");
            LaneMenu.AddGroupLabel("LaneClear Settings");
            LaneMenu.Add("useQL", new CheckBox("Use Q"));
            LaneMenu.Add("useWL", new CheckBox("Use W to reset bomb"));
            LaneMenu.AddSeparator();
            LaneMenu.Add("usemanaL", new Slider("Min % mana for LaneClear", 30, 0, 100));
            LaneMenu.Add("lccount", new Slider("Min Minions for Q", 3, 0, 8));

            
            MiscMenu = ZilMenu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc Settings");
            MiscMenu.Add("disableAA", new CheckBox("Disable AA", false));
            MiscMenu.Add("suppmode", new CheckBox("Support mode", false));
            MiscMenu.Add("gap", new CheckBox("gapcloser"));
            MiscMenu.Add("int", new CheckBox("interrupter"));
            MiscMenu.Add("ksQ", new CheckBox("Q ks"));


            DrawMenu = ZilMenu.AddSubMenu("Draw");
            DrawMenu.AddGroupLabel("Drawings Settings");
            DrawMenu.Add("drawaoff", new CheckBox("Disable Draws", false));
            DrawMenu.Add("drawq", new CheckBox("Draw Q"));
            DrawMenu.Add("draww", new CheckBox("Draw W"));
            DrawMenu.Add("drawe", new CheckBox("Draw E"));
            DrawMenu.Add("drawr", new CheckBox("Draw R"));

            FleeMenu = ZilMenu.AddSubMenu("Flee");
            FleeMenu.AddGroupLabel("Flee Settings");
            FleeMenu.Add("fleee", new CheckBox("Use E"));
            FleeMenu.Add("fleew", new CheckBox("Use W"));

            SkinMenu = ZilMenu.AddSubMenu("Skin", "Skin");
            SkinMenu.AddGroupLabel("Skin Selectior");

            var skin = SkinMenu.Add("SkinID", new Slider("Skin", 5, 0, 5));
            var SkinID = new[] { "Classic Zilean", "Old Saint Zilean", "Groovy Zilean", "Shurima Desert Zilean", "Time Machine Zilean", "Blood Moon Zilean" };
            skin.DisplayName = SkinID[skin.CurrentValue];

            skin.OnValueChange +=
                delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs changeArgs)
                {
                    sender.DisplayName = SkinID[changeArgs.NewValue];
                };


            Q = new Spell.Skillshot(SpellSlot.Q, 880, SkillShotType.Circular, (int)0.3f, 2000, 150);
            Q.AllowedCollisionCount = int.MaxValue;
            W = new Spell.Active(SpellSlot.W, 0);
            E = new Spell.Targeted(SpellSlot.E, 700);
            R = new Spell.Targeted(SpellSlot.R, 900);

            ign = new Spell.Targeted(ObjectManager.Player.GetSpellSlotFromName("summonerdot"), 600);

            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            Game.OnUpdate += OnGameUpdate;
            Gapcloser.OnGapcloser += OnGapCloser;
            Interrupter.OnInterruptableSpell += Interrupt;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnBasicAttack += Obj_AI_Base_OnBasicAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnSpell;
        }
        

        private static void Obj_AI_Base_OnBasicAttack(Obj_AI_Base Sender, GameObjectProcessSpellCastEventArgs args)
        {
            
            if (Sender == null || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
               return;
            }

            if (!Sender.IsDashing() && Sender.Type == GameObjectType.AIHeroClient && Sender.IsValidTarget(Q.Range) && Q.IsReady() && Sender.IsEnemy)
            {
                {
                    Q.Cast(Sender.ServerPosition);
                }
            } 
        }
        private static void Obj_AI_Base_OnSpell(Obj_AI_Base Sender, GameObjectProcessSpellCastEventArgs args)
        {
            
            if (Sender == null || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
               return;
            }

            if (!Sender.IsDashing() && Sender.Type == GameObjectType.AIHeroClient && Sender.IsValidTarget(Q.Range) && Q.IsReady() && Sender.IsEnemy)
            {
                {
                    Q.Cast(Sender);
                }
            } 
        }


        public static void Drawing_OnDraw(EventArgs args)
        {

            if (DrawMenu["drawaoff"].Cast<CheckBox>().CurrentValue)
            {
                return;
            }

            if (DrawMenu["drawq"].Cast<CheckBox>().CurrentValue)
            {
                if (Q.Level > 0)
                {
                    Circle.Draw(Color.Blue, Q.Range, ObjectManager.Player.Position);
                }
            }

            if (DrawMenu["draww"].Cast<CheckBox>().CurrentValue)
            {
                if (W.Level > 0)
                {
                    Circle.Draw(Color.Blue, W.Range, ObjectManager.Player.Position);
                }
            }

            if (DrawMenu["drawe"].Cast<CheckBox>().CurrentValue)
            {
                if (E.Level > 0)
                {
                    Circle.Draw(Color.Blue, E.Range, ObjectManager.Player.Position);
                }
            }

            if (DrawMenu["drawr"].Cast<CheckBox>().CurrentValue)
            {
                if (R.Level > 0)
                {
                    Circle.Draw(Color.Blue, R.Range, ObjectManager.Player.Position);
                }
            }
        }

        private static void OnGapCloser(Obj_AI_Base sender, Gapcloser.GapcloserEventArgs args)
        {
            if (sender.IsEnemy &&sender is AIHeroClient &&sender.Distance(Player) <= E.Range && E.IsReady() && MiscMenu["gap"].Cast<CheckBox>().CurrentValue)
            {
                E.Cast(sender);
            }
        }

        private static void Interrupt(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs args)
        {
            if (args.DangerLevel == DangerLevel.High &&
                sender.IsEnemy &&
                sender is AIHeroClient &&
                sender.Distance(Player) < Q.Range && Q.IsReady() &&
                MiscMenu["int"].Cast<CheckBox>().CurrentValue)
            {
                Q.Cast(sender);

                if (W.IsReady())
                {
                    W.Cast();
                    Q.Cast(sender);
                }
            }
        }

        static void Orbwalker_OnPreAttack(AttackableUnit ff, Orbwalker.PreAttackArgs args)
        {
            if (MiscMenu["disableAA"].Cast<CheckBox>().CurrentValue  && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                args.Process = false;
            }
            else
            {
                if (Q.IsReady() && Player.Distance(args.Target) < Q.Range - 100 && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                {
                    args.Process = false;
                }
            }

            if (MiscMenu["suppmode"].Cast<CheckBox>().CurrentValue)
            {
                if (args.Target is Obj_AI_Minion)
                {
                    args.Process = false;
                }
            }
        }

        private static void Ult()
        {
            if (Player.IsRecalling() || Player.IsInShopRange() || Player.IsInvulnerable || Player.HasBuffOfType(BuffType.SpellImmunity)
               || Player.HasBuffOfType(BuffType.Invulnerability))
            {
                return;
            }

            var useSelftHp = UltMenu["rhp"].Cast<Slider>().CurrentValue;
            if (UltMenu["user"].Cast<CheckBox>().CurrentValue && (Player.Health / Player.MaxHealth) * 100 <= useSelftHp
                && R.IsReady() && Player.CountEnemiesInRange(650) > 0)
            {
                R.Cast(Player);
            }
        }

        private static void Ultbitches()
        {
            if (Player.IsRecalling() || Player.IsInShopRange())
            {
                return;
            }

            foreach (var hero in ObjectManager.Get<AIHeroClient>().Where(x => x.IsAlly && !x.IsMe))
            {
                if (UltMenu["ultially"].Cast<CheckBox>().CurrentValue
                    && ((hero.Health / hero.MaxHealth) * 100
                        <= UltMenu["rhp"].Cast<Slider>().CurrentValue)
                    && R.IsReady() && Player.CountEnemiesInRange(1000) > 0
                    && (hero.Distance(Player.ServerPosition) <= R.Range))
                {

                    if (UltMenu["castultally" + hero.BaseSkinName].Cast<CheckBox>().CurrentValue)
                        {
                        if (hero.IsInvulnerable || hero.HasBuffOfType(BuffType.SpellImmunity) || hero.HasBuffOfType(BuffType.Invulnerability))
                        {
                            return;
                        }

                        R.Cast(hero);
                    }
                }
            }
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) Combo();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear)) LaneClear();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass)) Harass();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee)) Flee();

            Ultbitches();
            ZileanSKins();
            Ult();
            Killsteal();


            if (HarassMenu["autoH"].Cast<KeyBind>().CurrentValue)
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (!target.IsValidTarget())
                {
                    return;
                }

                if (Player.ManaPercent <= HarassMenu["HMana"].Cast<Slider>().CurrentValue)
                {
                    return;
                }

                if (HarassMenu["autoHQ"].Cast<CheckBox>().CurrentValue && Q.IsReady()
                    && target.IsValidTarget(Q.Range))
                {
                    var prediction = Q.GetPrediction(target);
                    if (prediction.HitChance >= EloBuddy.SDK.Enumerations.HitChance.High)
                    {
                        Q.Cast(target);
                    }
                }

                if (HarassMenu["autoHE"].Cast<CheckBox>().CurrentValue && E.IsReady()
                    && target.IsValidTarget(E.Range))
                {
                   E.Cast(target);
                }

                if (MiscMenu["AutoIgnite"].Cast<CheckBox>().CurrentValue)
                {
                    if (!ign.IsReady() || EloBuddy.Player.Instance.IsDead) return;
                    foreach (
                        var enemigo in
                            EntityManager.Heroes.Enemies
                                .Where(
                                    ignite => ignite.IsValidTarget(ign.Range) &&
                                        ignite.Health < 50 + 20 * EloBuddy.Player.Instance.Level - (ignite.HPRegenRate / 5 * 3)))
                    {
                        ign.Cast(enemigo);
                        return;
                    }
                }

                if (ComboMenu["Qcc"].Cast<CheckBox>().CurrentValue)
                {
                    if (!Utils2.CanMove(target))
                        Q.Cast(target);
                    var pred = Q.GetPrediction(target);
                    if (pred.HitChance == EloBuddy.SDK.Enumerations.HitChance.Dashing)
                    {
                        Q.Cast(target);
                    }
                    if (pred.HitChance == EloBuddy.SDK.Enumerations.HitChance.Immobile)
                    {
                        Q.Cast(target);
                    }
                }
            }
        }

        private static void Killsteal()
        {
            foreach (AIHeroClient target in
                ObjectManager.Get<AIHeroClient>()
                    .Where(
                        hero =>
                            hero.IsValidTarget(Q.Range) && !hero.HasBuffOfType(BuffType.Invulnerability) && hero.IsEnemy)
                )
            {
                var qDmg = Player.GetSpellDamage(target, SpellSlot.Q);
                if (MiscMenu["ksQ"].Cast<CheckBox>().CurrentValue && target.IsValidTarget(Q.Range) && target.Health <= qDmg)
                {
                    var qpred = Q.GetPrediction(target);
                    if (qpred.HitChance >= EloBuddy.SDK.Enumerations.HitChance.High && qpred.CollisionObjects.Count(h => h.IsEnemy && !h.IsDead && h is Obj_AI_Minion) < 2)
                        Q.Cast(qpred.CastPosition);
                }
                var qprediction = Q.GetPrediction(target);
                if (MiscMenu["ksQ"].Cast<CheckBox>().CurrentValue && W.IsReady())
                {
                    W.Cast();
                    Q.Cast(qprediction.CastPosition);
                }
            }
        }

        private static void Combo()
        {
            var qTarget =
                EntityManager.Heroes.Enemies.Find(x => x.HasBuff("ZileanQEnemyBomb") && x.IsValidTarget(Q.Range));
            var target = qTarget ?? TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (ComboMenu["useE"].Cast<CheckBox>().CurrentValue && E.IsReady()
                && target.IsValidTarget(E.Range))
            {
                E.Cast(target);
            }

            var zileanQEnemyBomb =
               EntityManager.Heroes.Enemies.Find(x => x.HasBuff("ZileanQEnemyBomb") && x.IsValidTarget(Q.Range));

            if (ComboMenu["useQ"].Cast<CheckBox>().CurrentValue && Q.IsReady()
                && target.IsValidTarget(Q.Range))
            {
                var pred = Q.GetPrediction(target);
                if (pred.HitChance >= HitChance.High)
                {
                    Q.Cast(pred.UnitPosition + 75);
                    Q.Cast(pred.UnitPosition + 50);
                    Q.Cast(pred.UnitPosition + 25);
                }
            }

            if (ComboMenu["useW1"].Cast<CheckBox>().CurrentValue)
            {
                W.Cast();
            }

            else if (ComboMenu["useW2"].Cast<CheckBox>().CurrentValue && zileanQEnemyBomb != null)
            {
                W.Cast();
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (target == null)
            {
                return;
            }

            if (HarassMenu["useQH"].Cast<CheckBox>().CurrentValue && Q.IsReady()
                && target.IsValidTarget(Q.Range))
            {
                var pred = Q.GetPrediction(target);
                if (pred.HitChance >= EloBuddy.SDK.Enumerations.HitChance.High)
                {
                    Q.Cast(pred.UnitPosition);
                }
            }

            if (HarassMenu["useEH"].Cast<CheckBox>().CurrentValue && E.IsReady()
                && target.IsValidTarget(E.Range))
            {
                E.Cast(target);
            }
        }

        private static void Flee()
        {
            Orbwalker.OrbwalkTo(Game.CursorPos);

            if (FleeMenu["fleee"].Cast<CheckBox>().CurrentValue && E.IsReady())
            {
                E.Cast(Player);
            }

            if (FleeMenu["fleew"].Cast<CheckBox>().CurrentValue && W.IsReady())
            {
                W.Cast();
            }
        }

        public static float GetComboDamage(Obj_AI_Base enemy)
        {
            try
            {
                float damage = 0;

                if (Q.IsReady())
                {
                    damage += Player.GetSpellDamage(enemy, SpellSlot.Q);
                }

                return damage;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            return 0;
        }

        private static float ComboDamage(Obj_AI_Base target)
        {
            try
            {
                float damage = 0;


                if (Q.IsReady())
                {
                    damage += Player.GetSpellDamage(target, SpellSlot.Q);
                }


                return damage;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            return 0;
        }




        private static void LaneClear()
        {
            var allMinions = EntityManager.MinionsAndMonsters.Get(
              EntityManager.MinionsAndMonsters.EntityType.Minion,
              EntityManager.UnitTeam.Enemy,
              ObjectManager.Player.Position,
              Q.Range,
              false);
            if (allMinions == null)
            {
                return;
            }

            foreach (var minion in allMinions)
            {
                if (LaneMenu["useQL"].Cast<CheckBox>().CurrentValue && Q.IsReady())
                {
                    allMinions.Any();
                    {
                        var fl = EntityManager.MinionsAndMonsters.GetLineFarmLocation(allMinions, 100, (int)Q.Range);
                        if (fl.HitNumber >= LaneMenu["lccount"].Cast<Slider>().CurrentValue)
                        {
                            Q.Cast(minion);
                        }

                        if (LaneMenu["useWL"].Cast<CheckBox>().CurrentValue && W.IsReady() && Player.ManaPercent >= LaneMenu["usemanaL"].Cast<Slider>().CurrentValue)
                          {
                              W.Cast();
                          }
                    }
                }
            }
        }

        private static void ZileanSKins()
        {
            var style = SkinMenu["SkinID"].DisplayName;

            switch (style)
            {
                case "Classic Zilean":
                    Player.SetSkinId(0);
                    break;
                case "Old Saint Zilean":
                    Player.SetSkinId(1);
                    break;
                case "Groovy Zilean":
                    Player.SetSkinId(2);
                    break;
                case "Shurima Desert Zilean":
                    Player.SetSkinId(3);
                    break;
                case "Time Machine Zilean":
                    Player.SetSkinId(4);
                    break;
                case "Blood Moon Zilean":
                    Player.SetSkinId(5);
                    break;           
            }
        }
    }
}
