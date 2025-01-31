﻿/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.EasternKingdoms.Karazhan.OperaEvent
{
    struct TextIds
    {
        public const uint SayDorotheeDeath = 0;
        public const uint SayDorotheeSummon = 1;
        public const uint SayDorotheeTitoDeath = 2;
        public const uint SayDorotheeAggro = 3;

        public const uint SayRoarAggro = 0;
        public const uint SayRoarDeath = 1;
        public const uint SayRoarSlay = 2;

        public const uint SayStrawmanAggro = 0;
        public const uint SayStrawmanDeath = 1;
        public const uint SayStrawmanSlay = 2;

        public const uint SayTinheadAggro = 0;
        public const uint SayTinheadDeath = 1;
        public const uint SayTinheadSlay = 2;
        public const uint EmoteRust = 3;

        public const uint SayCroneAggro = 0;
        public const uint SayCroneDeath = 1;
        public const uint SayCroneSlay = 2;
    }

    struct SpellIds
    {
        // Dorothee
        public const uint Waterbolt = 31012;
        public const uint Scream = 31013;
        public const uint Summontito = 31014;

        // Tito
        public const uint Yipping = 31015;

        // Strawman
        public const uint BrainBash = 31046;
        public const uint BrainWipe = 31069;
        public const uint BurningStraw = 31075;

        // Tinhead
        public const uint Cleave = 31043;
        public const uint Rust = 31086;

        // Roar
        public const uint Mangle = 31041;
        public const uint Shred = 31042;
        public const uint FrightenedScream = 31013;

        // Crone
        public const uint ChainLightning = 32337;

        // Cyclone
        public const uint Knockback = 32334;
        public const uint CycloneVisual = 32332;
    }

    struct CreatureIds
    {
        public const uint Tito = 17548;
        public const uint Cyclone = 18412;
        public const uint Crone = 18168;
    }

    #region Wizard of Oz
    public abstract class WizardofOzBase : ScriptedAI
    {
        public WizardofOzBase(Creature creature) : base(creature)
        {
            Initialize();
            instance = creature.GetInstanceScript();
        }

        public abstract void Initialize();

        public override void Reset()
        {
            Initialize();
        }

        public void SummonCroneIfReady(InstanceScript instance, Creature creature)
        {
            instance.SetBossState(DataTypes.OperaOzDeathcount, EncounterState.Special);  // Increment DeathCount

            if (instance.GetData(DataTypes.OperaOzDeathcount) == 4)
            {
                Creature pCrone = creature.SummonCreature(CreatureIds.Crone, -10891.96f, -1755.95f, creature.GetPositionZ(), 4.64f, TempSummonType.TimedOrDeadDespawn, Time.Hour * 2 * Time.InMilliseconds);
                if (pCrone)
                {
                    if (creature.GetVictim())
                        pCrone.GetAI().AttackStart(creature.GetVictim());
                }
            }
        }

        public bool TitoDied;
        public ObjectGuid DorotheeGUID;
        public uint AggroTimer;

        public InstanceScript instance;
    }

    [Script]
    public class boss_dorothee : WizardofOzBase
    {
        public boss_dorothee(Creature creature) : base(creature) { }

        public override void Initialize()
        {
            AggroTimer = 500;

            WaterBoltTimer = 5000;
            FearTimer = 15000;
            SummonTitoTimer = 47500;

            SummonedTito = false;
            TitoDied = false;
        }

        public override void EnterCombat(Unit who)
        {
            Talk(TextIds.SayDorotheeAggro);
        }

        public override void JustReachedHome()
        {
            me.DespawnOrUnsummon();
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayDorotheeDeath);

            SummonCroneIfReady(instance, me);
        }

        public override void AttackStart(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.AttackStart(who);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.MoveInLineOfSight(who);
        }

        public override void UpdateAI(uint diff)
        {
            if (AggroTimer != 0)
            {
                if (AggroTimer <= diff)
                {
                    me.RemoveUnitFlag(UnitFlags.NonAttackable);
                    AggroTimer = 0;
                }
                else AggroTimer -= diff;
            }

            if (!UpdateVictim())
                return;

            if (WaterBoltTimer <= diff)
            {
                DoCast(SelectTarget(SelectAggroTarget.Random, 0), SpellIds.Waterbolt);
                WaterBoltTimer = (uint)(TitoDied ? 1500 : 5000);
            }
            else WaterBoltTimer -= diff;

            if (FearTimer <= diff)
            {
                DoCastVictim(SpellIds.Scream);
                FearTimer = 30000;
            }
            else FearTimer -= diff;

            if (!SummonedTito)
            {
                if (SummonTitoTimer <= diff)
                    SummonTito();
                else SummonTitoTimer -= diff;
            }

            DoMeleeAttackIfReady();
        }

        void SummonTito()
        {
            Creature pTito = me.SummonCreature(CreatureIds.Tito, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOOC, 30000);
            if (pTito)
            {
                Talk(TextIds.SayDorotheeSummon);
                DorotheeGUID = me.GetGUID();
                pTito.GetAI().AttackStart(me.GetVictim());
                SummonedTito = true;
                TitoDied = false;
            }
        }

        uint WaterBoltTimer;
        uint FearTimer;
        uint SummonTitoTimer;

        bool SummonedTito;
    }

    [Script]
    public class npc_tito : WizardofOzBase
    {
        public npc_tito(Creature creature) : base(creature)
        {
            Initialize();
        }

        public override void Initialize()
        {
            DorotheeGUID.Clear();
            YipTimer = 10000;
        }

        public override void EnterCombat(Unit who) { }

        public override void JustDied(Unit killer)
        {
            if (!DorotheeGUID.IsEmpty())
            {
                Creature Dorothee = ObjectAccessor.GetCreature(me, DorotheeGUID);
                if (Dorothee && Dorothee.IsAlive())
                {
                    TitoDied = true;
                    Talk(TextIds.SayDorotheeTitoDeath, Dorothee);
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (YipTimer <= diff)
            {
                DoCastVictim(SpellIds.Yipping);
                YipTimer = 10000;
            }
            else YipTimer -= diff;

            DoMeleeAttackIfReady();
        }

        uint YipTimer;
    }

    [Script]
    class boss_strawman : WizardofOzBase
    {
        public boss_strawman(Creature creature) : base(creature)
        {
            Initialize();
            instance = creature.GetInstanceScript();
        }

        public override void Initialize()
        {
            AggroTimer = 13000;
            BrainBashTimer = 5000;
            BrainWipeTimer = 7000;
        }

        public override void AttackStart(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.AttackStart(who);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.MoveInLineOfSight(who);
        }

        public override void EnterCombat(Unit who)
        {
            Talk(TextIds.SayStrawmanAggro);
        }

        public override void SpellHit(Unit caster, SpellInfo Spell)
        {
            if ((Spell.SchoolMask == SpellSchoolMask.Fire) && ((RandomHelper.randChance() % 10) == 0))
            {
                DoCast(me, SpellIds.BurningStraw, true);
            }
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayStrawmanDeath);

            SummonCroneIfReady(instance, me);
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(TextIds.SayStrawmanSlay);
        }

        public override void UpdateAI(uint diff)
        {
            if (AggroTimer != 0)
            {
                if (AggroTimer <= diff)
                {
                    me.RemoveUnitFlag(UnitFlags.NonAttackable);
                    AggroTimer = 0;
                }
                else AggroTimer -= diff;
            }

            if (!UpdateVictim())
                return;

            if (BrainBashTimer <= diff)
            {
                DoCastVictim(SpellIds.BrainBash);
                BrainBashTimer = 15000;
            }
            else BrainBashTimer -= diff;

            if (BrainWipeTimer <= diff)
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100, true);
                if (target)
                    DoCast(target, SpellIds.BrainWipe);
                BrainWipeTimer = 20000;
            }
            else BrainWipeTimer -= diff;

            DoMeleeAttackIfReady();
        }

        uint BrainBashTimer;
        uint BrainWipeTimer;
    }

    [Script]
    class boss_tinhead : WizardofOzBase
    {
        public boss_tinhead(Creature creature) : base(creature) { }

        public override void Initialize()
        {
            AggroTimer = 15000;
            CleaveTimer = 5000;
            RustTimer = 30000;

            RustCount = 0;
        }

        public override void EnterCombat(Unit who)
        {
            Talk(TextIds.SayTinheadAggro);
        }

        public override void JustReachedHome()
        {
            me.DespawnOrUnsummon();
        }

        public override void AttackStart(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.AttackStart(who);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.MoveInLineOfSight(who);
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayTinheadDeath);

            SummonCroneIfReady(instance, me);
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(TextIds.SayTinheadSlay);
        }

        public override void UpdateAI(uint diff)
        {
            if (AggroTimer != 0)
            {
                if (AggroTimer <= diff)
                {
                    me.RemoveUnitFlag(UnitFlags.NonAttackable);
                    AggroTimer = 0;
                }
                else AggroTimer -= diff;
            }

            if (!UpdateVictim())
                return;

            if (CleaveTimer <= diff)
            {
                DoCastVictim(SpellIds.Cleave);
                CleaveTimer = 5000;
            }
            else CleaveTimer -= diff;

            if (RustCount < 8)
            {
                if (RustTimer <= diff)
                {
                    ++RustCount;
                    Talk(TextIds.EmoteRust);
                    DoCast(me, SpellIds.Rust);
                    RustTimer = 6000;
                }
                else RustTimer -= diff;
            }

            DoMeleeAttackIfReady();
        }

        uint CleaveTimer;
        uint RustTimer;

        byte RustCount;
    }

    [Script]
    class boss_roar : WizardofOzBase
    {
        public boss_roar(Creature creature) : base(creature) { }

        public override void Initialize()
        {
            AggroTimer = 20000;
            MangleTimer = 5000;
            ShredTimer = 10000;
            ScreamTimer = 15000;
        }

        public override void AttackStart(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.AttackStart(who);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.MoveInLineOfSight(who);
        }

        public override void EnterCombat(Unit who)
        {
            Talk(TextIds.SayRoarAggro);
        }

        public override void JustReachedHome()
        {
            me.DespawnOrUnsummon();
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayRoarDeath);

            SummonCroneIfReady(instance, me);
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(TextIds.SayRoarSlay);
        }

        public override void UpdateAI(uint diff)
        {
            if (AggroTimer != 0)
            {
                if (AggroTimer <= diff)
                {
                    me.RemoveUnitFlag(UnitFlags.NonAttackable);
                    AggroTimer = 0;
                }
                else AggroTimer -= diff;
            }

            if (!UpdateVictim())
                return;

            if (MangleTimer <= diff)
            {
                DoCastVictim(SpellIds.Mangle);
                MangleTimer = RandomHelper.URand(5000, 8000);
            }
            else MangleTimer -= diff;

            if (ShredTimer <= diff)
            {
                DoCastVictim(SpellIds.Shred);
                ShredTimer = RandomHelper.URand(10000, 15000);
            }
            else ShredTimer -= diff;

            if (ScreamTimer <= diff)
            {
                DoCastVictim(SpellIds.FrightenedScream);
                ScreamTimer = RandomHelper.URand(20000, 30000);
            }
            else ScreamTimer -= diff;

            DoMeleeAttackIfReady();
        }

        uint MangleTimer;
        uint ShredTimer;
        uint ScreamTimer;
    }

    [Script]
    class boss_crone : WizardofOzBase
    {
        public boss_crone(Creature creature) : base(creature)
        {
            instance = creature.GetInstanceScript();
        }

        public override void Initialize()
        {
            me.RemoveUnitFlag(UnitFlags.ImmuneToPc | UnitFlags.NonAttackable);
            CycloneTimer = 30000;
            ChainLightningTimer = 10000;
        }

        public override void JustReachedHome()
        {
            me.DespawnOrUnsummon();
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(TextIds.SayCroneSlay);
        }

        public override void EnterCombat(Unit who)
        {
            Talk(TextIds.SayCroneAggro);
            me.RemoveUnitFlag(UnitFlags.NonAttackable);
            me.RemoveUnitFlag(UnitFlags.ImmuneToPc);
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayCroneDeath);

            instance.SetBossState(DataTypes.OperaPerformance, EncounterState.Done);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                me.RemoveUnitFlag(UnitFlags.NonAttackable);

            if (CycloneTimer <= diff)
            {
                Creature Cyclone = DoSpawnCreature(CreatureIds.Cyclone, RandomHelper.FRand(0, 9), RandomHelper.FRand(0, 9), 0, 0, TempSummonType.TimedDespawn, 15000);
                if (Cyclone)
                    Cyclone.CastSpell(Cyclone, SpellIds.CycloneVisual, true);
                CycloneTimer = 30000;
            }
            else CycloneTimer -= diff;

            if (ChainLightningTimer <= diff)
            {
                DoCastVictim(SpellIds.ChainLightning);
                ChainLightningTimer = 15000;
            }
            else ChainLightningTimer -= diff;

            DoMeleeAttackIfReady();
        }

        uint CycloneTimer;
        uint ChainLightningTimer;
    }

    [Script]
    class npc_cyclone : ScriptedAI
    {
        public npc_cyclone(Creature creature) : base(creature) { }

        public override void Reset()
        {
            MoveTimer = 1000;
        }

        public override void EnterCombat(Unit who) { }

        public override void MoveInLineOfSight(Unit who) { }

        public override void UpdateAI(uint diff)
        {
            if (!me.HasAura(SpellIds.Knockback))
                DoCast(me, SpellIds.Knockback, true);

            if (MoveTimer <= diff)
            {
                Position pos = me.GetRandomNearPosition(10);
                me.GetMotionMaster().MovePoint(0, pos);
                MoveTimer = RandomHelper.URand(5000, 8000);
            }
            else MoveTimer -= diff;
        }

        uint MoveTimer;
    }

    #endregion

    #region Red Riding Hood
    struct RedRidingHood
    {
        public const uint SayWolfAggro = 0;
        public const uint SayWolfSlay = 1;
        public const uint SayWolfHood = 2;
        public const uint OptionWhatPhatLewtsYouHave = 7443;
        public const uint SoundWolfDeath = 9275;

        public const uint SpellLittleRedRidingHood = 30768;
        public const uint SpellTerrifyingHowl = 30752;
        public const uint SpellWideSwipe = 30761;

        public const uint NpcBigBadWolf = 17521;
    }

    [Script]
    class npc_grandmother : ScriptedAI
    {
        public npc_grandmother(Creature creature) : base(creature) { }

        public override bool GossipSelect(Player player, uint menuId, uint gossipListId)
        {
            if (menuId == RedRidingHood.OptionWhatPhatLewtsYouHave && gossipListId == 0)
            {
                player.PlayerTalkClass.SendCloseGossip();

                Creature pBigBadWolf = me.SummonCreature(RedRidingHood.NpcBigBadWolf, me.GetPositionX(), me.GetPositionY(), me.GetPositionZ(), me.GetOrientation(), TempSummonType.TimedOrDeadDespawn, Time.Hour * 2 * Time.InMilliseconds);
                if (pBigBadWolf)
                    pBigBadWolf.GetAI().AttackStart(player);

                me.DespawnOrUnsummon();
            }
            return false;
        }
    }

    [Script]
    class boss_bigbadwolf : ScriptedAI
    {
        public boss_bigbadwolf(Creature creature) : base(creature)
        {
            instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            ChaseTimer = 30000;
            FearTimer = RandomHelper.URand(25000, 35000);
            SwipeTimer = 5000;

            HoodGUID.Clear();
            TempThreat = 0;

            IsChasing = false;
        }

        public override void EnterCombat(Unit who)
        {
            Talk(RedRidingHood.SayWolfAggro);
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(RedRidingHood.SayWolfSlay);
        }

        public override void JustReachedHome()
        {
            me.DespawnOrUnsummon();
        }

        public override void JustDied(Unit killer)
        {
            DoPlaySoundToSet(me, RedRidingHood.SoundWolfDeath);
            instance.SetBossState(DataTypes.OperaPerformance, EncounterState.Done);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            DoMeleeAttackIfReady();

            if (ChaseTimer <= diff)
            {
                if (!IsChasing)
                {
                    Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100, true);
                    if (target)
                    {
                        Talk(RedRidingHood.SayWolfHood);
                        DoCast(target, RedRidingHood.SpellLittleRedRidingHood, true);
                        TempThreat = GetThreat(target);
                        if (TempThreat != 0.0f)
                            ModifyThreatByPercent(target, -100);
                        HoodGUID = target.GetGUID();
                        AddThreat(target, 1000000.0f);
                        ChaseTimer = 20000;
                        IsChasing = true;
                    }
                }
                else
                {
                    IsChasing = false;
                    Unit target = Global.ObjAccessor.GetUnit(me, HoodGUID);
                    if (target)
                    {
                        HoodGUID.Clear();
                        if (GetThreat(target) != 0f)
                            ModifyThreatByPercent(target, -100);
                        AddThreat(target, TempThreat);
                        TempThreat = 0;
                    }

                    ChaseTimer = 40000;
                }
            }
            else ChaseTimer -= diff;

            if (IsChasing)
                return;

            if (FearTimer <= diff)
            {
                DoCastVictim(RedRidingHood.SpellTerrifyingHowl);
                FearTimer = RandomHelper.URand(25000, 35000);
            }
            else FearTimer -= diff;

            if (SwipeTimer <= diff)
            {
                DoCastVictim(RedRidingHood.SpellWideSwipe);
                SwipeTimer = RandomHelper.URand(25000, 30000);
            }
            else SwipeTimer -= diff;
        }

        InstanceScript instance;

        uint ChaseTimer;
        uint FearTimer;
        uint SwipeTimer;

        ObjectGuid HoodGUID;
        float TempThreat;

        bool IsChasing;
    }

    #endregion

    #region Romeo & Juliet
    struct JulianneRomulo
    {
        public const uint SayJulianneAggro = 0;
        public const uint SayJulianneEnter = 1;
        public const uint SayJulianneDeath01 = 2;
        public const uint SayJulianneDeath02 = 3;
        public const uint SayJulianneResurrect = 4;
        public const uint SayJulianneSlay = 5;

        public const uint SayRomuloAggro = 0;
        public const uint SayRomuloDeath = 1;
        public const uint SayRomuloEnter = 2;
        public const uint SayRomuloResurrect = 3;
        public const uint SayRomuloSlay = 4;

        public const uint SpellBlindingPassion = 30890;
        public const uint SpellDevotion = 30887;
        public const uint SpellEternalAffection = 30878;
        public const uint SpellPowerfulAttraction = 30889;
        public const uint SpellDrinkPoison = 30907;

        public const uint SpellBackwardLunge = 30815;
        public const uint SpellDaring = 30841;
        public const uint SpellDeadlySwathe = 30817;
        public const uint SpellPoisonThrust = 30822;

        public const uint SpellUndyingLove = 30951;
        public const uint SpellResVisual = 24171;

        public const uint NpcRomulo = 17533;
        public const int RomuloX = -10900;
        public const int RomuloY = -1758;
    }

    public enum RAJPhase
    {
        Julianne = 0,
        Romulo = 1,
        Both = 2,
    }

    public class julianne_romuloAI : ScriptedAI
    {
        public julianne_romuloAI(Creature creature) : base(creature) { }

        public override void JustReachedHome()
        {
            me.DespawnOrUnsummon();
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.MoveInLineOfSight(who);
        }

        public void PretendToDie()
        {
            me.InterruptNonMeleeSpells(true);
            me.RemoveAllAuras();
            me.SetHealth(0);
            me.AddUnitFlag(UnitFlags.NotSelectable);
            me.GetMotionMaster().MovementExpired(false);
            me.GetMotionMaster().MoveIdle();
            me.SetStandState(UnitStandStateType.Dead);
        }

        public void Resurrect(Creature target)
        {
            target.RemoveUnitFlag(UnitFlags.NotSelectable);
            target.SetFullHealth();
            target.SetStandState(UnitStandStateType.Stand);
            target.CastSpell(target, JulianneRomulo.SpellResVisual, true);
            if (target.GetVictim())
            {
                target.GetMotionMaster().MoveChase(target.GetVictim());
                target.GetAI().AttackStart(target.GetVictim());
            }
            else
                target.GetMotionMaster().Initialize();
        }

        public InstanceScript instance;

        public ObjectGuid JulianneGUID;
        public ObjectGuid RomuloGUID;

        public uint EntryYellTimer;
        public uint AggroYellTimer;

        public RAJPhase Phase;

        public uint ResurrectSelfTimer;
        public uint ResurrectTimer;
        public bool JulianneDead;
        public bool RomuloDead;

        public bool IsFakingDeath;
    }

    [Script]
    public class boss_julianne : julianne_romuloAI
    {
        public boss_julianne(Creature creature) : base(creature)
        {
            instance = creature.GetInstanceScript();
            EntryYellTimer = 1000;
            AggroYellTimer = 10000;
            IsFakingDeath = false;
        }

        public override void Reset()
        {
            RomuloGUID.Clear();
            Phase = RAJPhase.Julianne;

            BlindingPassionTimer = 30000;
            DevotionTimer = 15000;
            EternalAffectionTimer = 25000;
            PowerfulAttractionTimer = 5000;
            SummonRomuloTimer = 10000;
            DrinkPoisonTimer = 0;
            ResurrectSelfTimer = 0;

            if (IsFakingDeath)
            {
                Resurrect(me);
                IsFakingDeath = false;
            }

            SummonedRomulo = false;
            RomuloDead = false;
        }

        public override void EnterCombat(Unit who) { }

        public override void AttackStart(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.AttackStart(who);
        }

        public override void SpellHit(Unit caster, SpellInfo Spell)
        {
            if (Spell.Id == JulianneRomulo.SpellDrinkPoison)
            {
                Talk(JulianneRomulo.SayJulianneDeath01);
                DrinkPoisonTimer = 2500;
            }
        }

        public override void DamageTaken(Unit done_by, ref uint damage)
        {
            if (damage < me.GetHealth())
                return;

            //anything below only used if incoming damage will kill

            if (Phase == RAJPhase.Julianne)
            {
                damage = 0;

                //this means already drinking, so return
                if (IsFakingDeath)
                    return;

                me.InterruptNonMeleeSpells(true);
                DoCast(me, JulianneRomulo.SpellDrinkPoison);

                IsFakingDeath = true;
                //IS THIS USEFULL? Creature Julianne = (Global.ObjAccessor.GetCreature(me, JulianneGUID));
                return;
            }

            if (Phase == RAJPhase.Romulo)
            {
                Log.outError(LogFilter.Scripts, "boss_julianneAI: cannot take damage in PHASE_ROMULO, why was i here?");
                damage = 0;
                return;
            }

            if (Phase == RAJPhase.Both)
            {
                Creature Romulo;
                //if this is true then we have to kill romulo too
                if (RomuloDead)
                {
                    Romulo = ObjectAccessor.GetCreature(me, RomuloGUID);
                    if (Romulo)
                    {
                        Romulo.RemoveUnitFlag(UnitFlags.NotSelectable);
                        Romulo.GetMotionMaster().Clear();
                        Romulo.SetDeathState(DeathState.JustDied);
                        Romulo.CombatStop(true);
                        Romulo.GetThreatManager().ClearAllThreat();
                        Romulo.SetDynamicFlags(UnitDynFlags.Lootable);
                    }

                    return;
                }

                //if not already returned, then romulo is alive and we can pretend die
                Romulo = ObjectAccessor.GetCreature(me, RomuloGUID);
                if (Romulo)
                {
                    PretendToDie();
                    IsFakingDeath = true;
                    ((julianne_romuloAI)Romulo.GetAI()).ResurrectTimer = 10000;
                    ((julianne_romuloAI)Romulo.GetAI()).JulianneDead = true;
                    damage = 0;
                    return;
                }
            }
            Log.outError(LogFilter.Scripts, "boss_julianneAI: DamageTaken reach end of code, that should not happen.");
        }

        public override void JustDied(Unit killer)
        {
            Talk(JulianneRomulo.SayJulianneDeath02);
            instance.SetBossState(DataTypes.OperaPerformance, EncounterState.Done);
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(JulianneRomulo.SayJulianneSlay);
        }

        public override void UpdateAI(uint diff)
        {
            if (EntryYellTimer != 0)
            {
                if (EntryYellTimer <= diff)
                {
                    Talk(JulianneRomulo.SayJulianneEnter);
                    EntryYellTimer = 0;
                }
                else EntryYellTimer -= diff;
            }

            if (AggroYellTimer != 0)
            {
                if (AggroYellTimer <= diff)
                {
                    Talk(JulianneRomulo.SayJulianneAggro);
                    me.RemoveUnitFlag(UnitFlags.NonAttackable);
                    me.SetFaction(16);
                    AggroYellTimer = 0;
                }
                else AggroYellTimer -= diff;
            }

            if (DrinkPoisonTimer != 0)
            {
                //will do this 2secs after spell hit. this is time to display visual as expected
                if (DrinkPoisonTimer <= diff)
                {
                    PretendToDie();
                    Phase = RAJPhase.Romulo;
                    SummonRomuloTimer = 10000;
                    DrinkPoisonTimer = 0;
                }
                else DrinkPoisonTimer -= diff;
            }

            if (Phase == RAJPhase.Romulo && !SummonedRomulo)
            {
                if (SummonRomuloTimer <= diff)
                {
                    Creature pRomulo = me.SummonCreature(JulianneRomulo.NpcRomulo, JulianneRomulo.RomuloX, JulianneRomulo.RomuloY, me.GetPositionZ(), 0, TempSummonType.TimedOrDeadDespawn, Time.Hour * 2 * Time.InMilliseconds);
                    if (pRomulo)
                    {
                        RomuloGUID = pRomulo.GetGUID();
                        ((julianne_romuloAI)pRomulo.GetAI()).JulianneGUID = me.GetGUID();
                        ((julianne_romuloAI)pRomulo.GetAI()).Phase = RAJPhase.Romulo;
                        DoZoneInCombat(pRomulo);

                        pRomulo.SetFaction(16);
                    }
                    SummonedRomulo = true;
                }
                else SummonRomuloTimer -= diff;
            }

            if (ResurrectSelfTimer != 0)
            {
                if (ResurrectSelfTimer <= diff)
                {
                    Resurrect(me);
                    Phase = RAJPhase.Both;
                    IsFakingDeath = false;

                    if (me.GetVictim())
                        AttackStart(me.GetVictim());

                    ResurrectSelfTimer = 0;
                    ResurrectTimer = 1000;
                }
                else ResurrectSelfTimer -= diff;
            }

            if (!UpdateVictim() || IsFakingDeath)
                return;

            if (RomuloDead)
            {
                if (ResurrectTimer <= diff)
                {
                    Creature Romulo = ObjectAccessor.GetCreature(me, RomuloGUID);
                    if (Romulo && ((julianne_romuloAI)Romulo.GetAI()).IsFakingDeath)
                    {
                        Talk(JulianneRomulo.SayJulianneResurrect);
                        Resurrect(Romulo);
                        ((julianne_romuloAI)Romulo.GetAI()).IsFakingDeath = false;
                        RomuloDead = false;
                        ResurrectTimer = 10000;
                    }
                }
                else ResurrectTimer -= diff;
            }

            if (BlindingPassionTimer <= diff)
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100, true);
                if (target)
                    DoCast(target, JulianneRomulo.SpellBlindingPassion);
                BlindingPassionTimer = RandomHelper.URand(30000, 45000);
            }
            else BlindingPassionTimer -= diff;

            if (DevotionTimer <= diff)
            {
                DoCast(me, JulianneRomulo.SpellDevotion);
                DevotionTimer = RandomHelper.URand(15000, 45000);
            }
            else DevotionTimer -= diff;

            if (PowerfulAttractionTimer <= diff)
            {
                DoCast(SelectTarget(SelectAggroTarget.Random, 0), JulianneRomulo.SpellPowerfulAttraction);
                PowerfulAttractionTimer = RandomHelper.URand(5000, 30000);
            }
            else PowerfulAttractionTimer -= diff;

            if (EternalAffectionTimer <= diff)
            {
                if (RandomHelper.URand(0, 1) != 0 && SummonedRomulo)
                {
                    Creature Romulo = ObjectAccessor.GetCreature(me, RomuloGUID);
                    if (Romulo && Romulo.IsAlive() && !RomuloDead)
                        DoCast(Romulo, JulianneRomulo.SpellEternalAffection);
                }
                else DoCast(me, JulianneRomulo.SpellEternalAffection);

                EternalAffectionTimer = RandomHelper.URand(45000, 60000);
            }
            else EternalAffectionTimer -= diff;

            DoMeleeAttackIfReady();
        }

        uint BlindingPassionTimer;
        uint DevotionTimer;
        uint EternalAffectionTimer;
        uint PowerfulAttractionTimer;
        uint SummonRomuloTimer;
        uint DrinkPoisonTimer;

        bool SummonedRomulo;
    }

    [Script]
    public class boss_romulo : julianne_romuloAI
    {
        public boss_romulo(Creature creature) : base(creature)
        {
            instance = creature.GetInstanceScript();
            EntryYellTimer = 8000;
            AggroYellTimer = 15000;
        }

        public override void Reset()
        {
            JulianneGUID.Clear();
            Phase = RAJPhase.Romulo;

            BackwardLungeTimer = 15000;
            DaringTimer = 20000;
            DeadlySwatheTimer = 25000;
            PoisonThrustTimer = 10000;
            ResurrectTimer = 10000;

            IsFakingDeath = false;
            JulianneDead = false;
        }

        public override void DamageTaken(Unit attacker, ref uint damage)
        {
            if (damage < me.GetHealth())
                return;

            //anything below only used if incoming damage will kill

            if (Phase == RAJPhase.Romulo)
            {
                Talk(JulianneRomulo.SayRomuloDeath);
                PretendToDie();
                IsFakingDeath = true;
                Phase = RAJPhase.Both;

                Creature Julianne = ObjectAccessor.GetCreature(me, JulianneGUID);
                if (Julianne)
                {
                    ((julianne_romuloAI)Julianne.GetAI()).RomuloDead = true;
                    ((julianne_romuloAI)Julianne.GetAI()).ResurrectSelfTimer = 10000;
                }

                damage = 0;
                return;
            }

            if (Phase == RAJPhase.Both)
            {
                Creature Julianne;
                if (JulianneDead)
                {
                    Julianne = ObjectAccessor.GetCreature(me, JulianneGUID);
                    if (Julianne)
                    {
                        Julianne.RemoveUnitFlag(UnitFlags.NotSelectable);
                        Julianne.GetMotionMaster().Clear();
                        Julianne.SetDeathState(DeathState.JustDied);
                        Julianne.CombatStop(true);
                        Julianne.GetThreatManager().ClearAllThreat();
                        Julianne.SetDynamicFlags(UnitDynFlags.Lootable);
                    }
                    return;
                }

                Julianne = ObjectAccessor.GetCreature(me, JulianneGUID);
                if (Julianne)
                {
                    PretendToDie();
                    IsFakingDeath = true;
                    ((julianne_romuloAI)Julianne.GetAI()).ResurrectTimer = 10000;
                    ((julianne_romuloAI)Julianne.GetAI()).RomuloDead = true;
                    damage = 0;
                    return;
                }
            }

            Log.outError(LogFilter.Scripts, "boss_romuloAI: DamageTaken reach end of code, that should not happen.");
        }

        public override void EnterCombat(Unit who)
        {
            Talk(JulianneRomulo.SayRomuloAggro);
            if (!JulianneGUID.IsEmpty())
            {
                Creature Julianne = (ObjectAccessor.GetCreature(me, JulianneGUID));
                if (Julianne && Julianne.GetVictim())
                {
                    AddThreat(Julianne.GetVictim(), 1.0f);
                    AttackStart(Julianne.GetVictim());
                }
            }
        }

        public override void JustDied(Unit killer)
        {
            Talk(JulianneRomulo.SayRomuloDeath);
            instance.SetBossState(DataTypes.OperaPerformance, EncounterState.Done);
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(JulianneRomulo.SayRomuloSlay);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() || IsFakingDeath)
                return;

            if (JulianneDead)
            {
                if (ResurrectTimer <= diff)
                {
                    Creature Julianne = (ObjectAccessor.GetCreature(me, JulianneGUID));
                    if (Julianne && ((julianne_romuloAI)Julianne.GetAI()).IsFakingDeath)
                    {
                        Talk(JulianneRomulo.SayRomuloResurrect);
                        Resurrect(Julianne);
                        ((julianne_romuloAI)Julianne.GetAI()).IsFakingDeath = false;
                        JulianneDead = false;
                        ResurrectTimer = 10000;
                    }
                }
                else ResurrectTimer -= diff;
            }

            if (BackwardLungeTimer <= diff)
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 1, 100, true);
                if (target && !me.HasInArc(MathFunctions.PI, target))
                {
                    DoCast(target, JulianneRomulo.SpellBackwardLunge);
                    BackwardLungeTimer = RandomHelper.URand(15000, 30000);
                }
            }
            else BackwardLungeTimer -= diff;

            if (DaringTimer <= diff)
            {
                DoCast(me, JulianneRomulo.SpellDaring);
                DaringTimer = RandomHelper.URand(20000, 40000);
            }
            else DaringTimer -= diff;

            if (DeadlySwatheTimer <= diff)
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100, true);
                if (target)
                    DoCast(target, JulianneRomulo.SpellDeadlySwathe);
                DeadlySwatheTimer = RandomHelper.URand(15000, 25000);
            }
            else DeadlySwatheTimer -= diff;

            if (PoisonThrustTimer <= diff)
            {
                DoCastVictim(JulianneRomulo.SpellPoisonThrust);
                PoisonThrustTimer = RandomHelper.URand(10000, 20000);
            }
            else PoisonThrustTimer -= diff;

            DoMeleeAttackIfReady();
        }

        uint BackwardLungeTimer;
        uint DaringTimer;
        uint DeadlySwatheTimer;
        uint PoisonThrustTimer;
    }

    #endregion

    [Script]
    class npc_barnes : CreatureScript
    {
        public npc_barnes() : base("npc_barnes") { }

        class npc_barnesAI : EscortAI
        {
            public npc_barnesAI(Creature creature) : base(creature)
            {
                Initialize();
                instance = creature.GetInstanceScript();
            }

            void Initialize()
            {
                m_uiSpotlightGUID.Clear();

                TalkCount = 0;
                TalkTimer = 2000;
                WipeTimer = 5000;

                PerformanceReady = false;
            }

            public override void Reset()
            {
                Initialize();

                m_uiEventId = instance.GetData(DataTypes.OperaPerformance);
            }

            public void StartEvent()
            {
                instance.SetBossState(DataTypes.OperaPerformance, EncounterState.InProgress);

                //resets count for this event, in case earlier failed
                if (m_uiEventId == OperaEvents.Oz)
                    instance.SetData(DataTypes.OperaOzDeathcount, (uint)EncounterState.InProgress);

                Start(false, false);
            }

            public override void EnterCombat(Unit who) { }

            public override void WaypointReached(uint waypointId, uint pathId)
            {
                switch (waypointId)
                {
                    case 0:
                        DoCast(me, karazhanConst.SpellTuxedo, false);
                        instance.DoUseDoorOrButton(instance.GetGuidData(DataTypes.GoStagedoorleft));
                        break;
                    case 4:
                        TalkCount = 0;
                        SetEscortPaused(true);

                        Creature spotlight = me.SummonCreature(karazhanConst.NpcSpotlight, me.GetPositionX(), me.GetPositionY(), me.GetPositionZ(), 0.0f, TempSummonType.TimedOrDeadDespawn, 60000);
                        if (spotlight)
                        {
                            spotlight.AddUnitFlag(UnitFlags.NotSelectable);
                            spotlight.CastSpell(spotlight, karazhanConst.SpellSpotlight, false);
                            m_uiSpotlightGUID = spotlight.GetGUID();
                        }
                        break;
                    case 8:
                        instance.DoUseDoorOrButton(instance.GetGuidData(DataTypes.GoStagedoorleft));
                        PerformanceReady = true;
                        break;
                    case 9:
                        PrepareEncounter();
                        instance.DoUseDoorOrButton(instance.GetGuidData(DataTypes.GoCurtains));
                        break;
                }
            }

            void Talk(uint count)
            {
                int text = 0;

                switch (m_uiEventId)
                {
                    case OperaEvents.Oz:
                        if (karazhanConst.OzDialogue[count].TextId != 0)
                            text = karazhanConst.OzDialogue[count].TextId;
                        if (karazhanConst.OzDialogue[count].Timer != 0)
                            TalkTimer = karazhanConst.OzDialogue[count].Timer;
                        break;

                    case OperaEvents.Hood:
                        if (karazhanConst.HoodDialogue[count].TextId != 0)
                            text = karazhanConst.HoodDialogue[count].TextId;
                        if (karazhanConst.HoodDialogue[count].Timer != 0)
                            TalkTimer = karazhanConst.HoodDialogue[count].Timer;
                        break;

                    case OperaEvents.RAJ:
                        if (karazhanConst.RAJDialogue[count].TextId != 0)
                            text = karazhanConst.RAJDialogue[count].TextId;
                        if (karazhanConst.RAJDialogue[count].Timer != 0)
                            TalkTimer = karazhanConst.RAJDialogue[count].Timer;
                        break;
                }

                if (text != 0)
                    base.Talk((uint)text);
            }

            void PrepareEncounter()
            {
                int index = 0;
                int count = 0;

                switch (m_uiEventId)
                {
                    case OperaEvents.Oz:
                        index = 0;
                        count = 4;
                        break;
                    case OperaEvents.Hood:
                        index = 4;
                        count = index + 1;
                        break;
                    case OperaEvents.RAJ:
                        index = 5;
                        count = index + 1;
                        break;
                }

                for (; index < count; ++index)
                {
                    uint entry = (uint)karazhanConst.Spawns[index][0];
                    float PosX = karazhanConst.Spawns[index][1];

                    Creature creature = me.SummonCreature(entry, PosX, karazhanConst.SPAWN_Y, karazhanConst.SPAWN_Z, karazhanConst.SPAWN_O, TempSummonType.TimedOrDeadDespawn, Time.Hour * 2 * Time.InMilliseconds);
                    if (creature)
                        creature.AddUnitFlag(UnitFlags.NonAttackable);
                }

                RaidWiped = false;
            }

            public override void UpdateAI(uint diff)
            {
                base.UpdateAI(diff);

                if (HasEscortState(EscortState.Paused))
                {
                    if (TalkTimer <= diff)
                    {
                        if (TalkCount > 3)
                        {
                            Creature pSpotlight = ObjectAccessor.GetCreature(me, m_uiSpotlightGUID);
                            if (pSpotlight)
                                pSpotlight.DespawnOrUnsummon();

                            SetEscortPaused(false);
                            return;
                        }

                        Talk(TalkCount);
                        ++TalkCount;
                    }
                    else
                        TalkTimer -= diff;
                }

                if (PerformanceReady)
                {
                    if (!RaidWiped)
                    {
                        if (WipeTimer <= diff)
                        {
                            var PlayerList = me.GetMap().GetPlayers();
                            if (PlayerList.Empty())
                                return;

                            RaidWiped = true;
                            foreach (var player in PlayerList)
                            {
                                if (player.IsAlive() && !player.IsGameMaster())
                                {
                                    RaidWiped = false;
                                    break;
                                }
                            }

                            if (RaidWiped)
                            {
                                RaidWiped = true;
                                EnterEvadeMode();
                                return;
                            }

                            WipeTimer = 15000;
                        }
                        else
                            WipeTimer -= diff;
                    }
                }
            }

            public override bool GossipHello(Player player)
            {
                // Check for death of Moroes and if opera event is not done already
                if (instance.GetBossState(DataTypes.Moroes) == EncounterState.Done && instance.GetBossState(DataTypes.OperaPerformance) != EncounterState.Done)
                {
                    AddGossipItemFor(player, GossipOptionIcon.Chat, karazhanConst.OZ_GOSSIP1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);

                    if (player.IsGameMaster())
                    {
                        AddGossipItemFor(player, GossipOptionIcon.Dot, karazhanConst.OZ_GM_GOSSIP1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 3);
                        AddGossipItemFor(player, GossipOptionIcon.Dot, karazhanConst.OZ_GM_GOSSIP2, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 4);
                        AddGossipItemFor(player, GossipOptionIcon.Dot, karazhanConst.OZ_GM_GOSSIP3, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 5);
                    }

                    if (!RaidWiped)
                        SendGossipMenuFor(player, 8970, me.GetGUID());
                    else
                        SendGossipMenuFor(player, 8975, me.GetGUID());

                    return true;
                }

                SendGossipMenuFor(player, 8978, me.GetGUID());
                return true;
            }

            public override bool GossipSelect(Player player, uint menuId, uint gossipListId)
            {
                uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
                ClearGossipMenuFor(player);

                switch (action)
                {
                    case eTradeskill.GossipActionInfoDef + 1:
                        AddGossipItemFor(player, GossipOptionIcon.Chat, karazhanConst.OZ_GOSSIP2, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                        SendGossipMenuFor(player, 8971, me.GetGUID());
                        break;
                    case eTradeskill.GossipActionInfoDef + 2:
                        player.CLOSE_GOSSIP_MENU();
                        StartEvent();
                        break;
                    case eTradeskill.GossipActionInfoDef + 3:
                        CloseGossipMenuFor(player);
                        m_uiEventId = OperaEvents.Oz;
                        Log.outInfo(LogFilter.Scripts, "player (GUID {0}) manually set Opera event to EVENT_OZ", player.GetGUID());
                        break;
                    case eTradeskill.GossipActionInfoDef + 4:
                        CloseGossipMenuFor(player);
                        m_uiEventId = OperaEvents.Hood;
                        Log.outInfo(LogFilter.Scripts, "player (GUID {0}) manually set Opera event to EVENT_HOOD", player.GetGUID());
                        break;
                    case eTradeskill.GossipActionInfoDef + 5:
                        CloseGossipMenuFor(player);
                        m_uiEventId = OperaEvents.RAJ;
                        Log.outInfo(LogFilter.Scripts, "player (GUID {0}) manually set Opera event to EVENT_RAJ", player.GetGUID());
                        break;
                }

                return true;
            }

            InstanceScript instance;

            ObjectGuid m_uiSpotlightGUID;

            uint TalkCount;
            uint TalkTimer;
            uint WipeTimer;
            public uint m_uiEventId;

            bool PerformanceReady;
            public bool RaidWiped;
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return GetInstanceAI<npc_barnesAI>(creature);
        }
    }
}
