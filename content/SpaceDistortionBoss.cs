using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

internal static class SpaceDistortionBoss
{
    // NPC Type ID - using a high ID to avoid conflicts
    public const int BossType = 6000;
    public const int ProjectileType_VoidBolt = 6001;
    public const int ProjectileType_DistortionWave = 6002;
    public const int ProjectileType_StarShard = 6003;
    public const int ProjectileType_BlackHole = 6004;
    public const int ItemType_SpawnItem = 5000;

    // Animation frames
    private const int FrameCount = 8;
    private const int FrameWidth = 120;
    private const int FrameHeight = 120;

    // AI States
    private enum AIState : int
    {
        Spawn = 0,
        Phase1_Idle = 1,
        Phase1_Charge = 2,
        Phase1_VoidBoltBarrage = 3,
        Phase1_DistortionWave = 4,
        Phase2_Transition = 5,
        Phase2_BlackHole = 6,
        Phase2_StarRain = 7,
        Phase2_VoidCharge = 8,
        Enraged = 9,
        Death = 10
    }

    // Static texture cache
    private static Texture2D bossTexture;
    private static Texture2D glowTexture;
    private static bool texturesLoaded = false;

    // Boss stats
    private const int MaxLife = 45000;
    private const int Phase2Life = 22500;
    private const int EnragedLife = 4500;

    public static void SetDefaults(On_NPC.orig_SetDefaults orig, NPC npc, int type)
    {
        orig(npc, type);

        if (type != BossType) return;

        npc.width = FrameWidth;
        npc.height = FrameHeight;
        npc.lifeMax = MaxLife;
        npc.damage = 80;
        npc.defense = 30;
        npc.knockBackResist = 0f;
        npc.value = 500000;
        npc.npcSlots = 10f;
        npc.boss = true;
        npc.lavaImmune = true;
        npc.noGravity = true;
        npc.noTileCollide = true;
        npc.HitSound = SoundID.NPCHit4;
        npc.DeathSound = SoundID.NPCDeath14;
        npc.aiStyle = -1;
        npc.buffImmune[BuffID.OnFire] = true;
        npc.buffImmune[BuffID.Poisoned] = true;
        npc.buffImmune[BuffID.Venom] = true;
        npc.buffImmune[BuffID.CursedInferno] = true;
        npc.buffImmune[BuffID.Frostburn] = true;
        npc.buffImmune[BuffID.ShadowFlame] = true;
        npc.buffImmune[BuffID.Ichor] = true;
    }

    public static void AI(On_NPC.orig_AI orig, NPC npc)
    {
        if (npc.type != BossType)
        {
            orig(npc);
            return;
        }

        // Initialize on spawn
        if (npc.ai[0] == 0 && npc.ai[1] == 0)
        {
            InitializeBoss(npc);
        }

        // Handle despawn if player is dead or not in range
        Player player = Main.player[npc.target];
        if (!player.active || player.dead || Vector2.Distance(npc.Center, player.Center) > 3000f)
        {
            npc.TargetClosest(false);
            player = Main.player[npc.target];
            if (!player.active || player.dead)
            {
                npc.velocity.Y -= 0.5f;
                if (npc.timeLeft > 10) npc.timeLeft = 10;
                return;
            }
        }

        // Run state machine
        switch ((AIState)(int)npc.ai[0])
        {
            case AIState.Spawn:
                RunSpawnState(npc, player);
                break;
            case AIState.Phase1_Idle:
                RunPhase1Idle(npc, player);
                break;
            case AIState.Phase1_Charge:
                RunPhase1Charge(npc, player);
                break;
            case AIState.Phase1_VoidBoltBarrage:
                RunPhase1VoidBolts(npc, player);
                break;
            case AIState.Phase1_DistortionWave:
                RunPhase1DistortionWave(npc, player);
                break;
            case AIState.Phase2_Transition:
                RunPhase2Transition(npc, player);
                break;
            case AIState.Phase2_BlackHole:
                RunPhase2BlackHole(npc, player);
                break;
            case AIState.Phase2_StarRain:
                RunPhase2StarRain(npc, player);
                break;
            case AIState.Phase2_VoidCharge:
                RunPhase2VoidCharge(npc, player);
                break;
            case AIState.Enraged:
                RunEnragedState(npc, player);
                break;
            case AIState.Death:
                RunDeathState(npc);
                break;
        }

        // Check phase transitions
        CheckPhaseTransitions(npc);

        // Update animation
        UpdateAnimation(npc);

        // Apply visual effects
        UpdateVisualEffects(npc);
    }

    private static void InitializeBoss(NPC npc)
    {
        npc.ai[0] = (int)AIState.Spawn;
        npc.ai[1] = 0; // Timer
        npc.ai[2] = 0; // Phase (0 = phase 1, 1 = phase 2, 2 = enraged)
        npc.ai[3] = 0; // Attack counter
        npc.localAI[0] = 0; // Rotation
        npc.localAI[1] = 1f; // Scale
        npc.localAI[2] = 0; // Glow intensity
        npc.netUpdate = true;

        // Spawn effects
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            for (int i = 0; i < 30; i++)
            {
                Dust.NewDustPerfect(npc.Center, DustID.Vortex, Main.rand.NextVector2Circular(20, 20), 0, default, 1.5f);
            }
        }
    }

    private static void RunSpawnState(NPC npc, Player player)
    {
        npc.ai[1]++;

        // Dramatic entrance from top of screen
        Vector2 targetPos = player.Center + new Vector2(0, -300);
        Vector2 toTarget = targetPos - npc.Center;
        float distance = toTarget.Length();

        if (distance > 20f)
        {
            npc.velocity = toTarget.SafeNormalize(Vector2.Zero) * Math.Min(distance * 0.05f, 20f);
        }
        else
        {
            npc.velocity = Vector2.Zero;
            npc.ai[0] = (int)AIState.Phase1_Idle;
            npc.ai[1] = 0;
            npc.netUpdate = true;

            // Roar sound
            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Roar, npc.Center);
            }
        }

        // Visual effects during spawn
        npc.localAI[1] = MathHelper.Lerp(0.5f, 1f, npc.ai[1] / 120f);
        npc.localAI[2] = npc.ai[1] / 120f;

        if (npc.ai[1] % 10 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(60, 60), DustID.Vortex, Main.rand.NextVector2Circular(5, 5), 0, default, Main.rand.NextFloat(1f, 2f));
        }
    }

    private static void RunPhase1Idle(NPC npc, Player player)
    {
        npc.ai[1]++;

        // Hover above player
        Vector2 hoverPos = player.Center + new Vector2(0, -250);
        Vector2 toHover = hoverPos - npc.Center;
        npc.velocity = toHover * 0.02f;

        // Slow rotation
        npc.localAI[0] += 0.005f;

        // Pulsing glow
        npc.localAI[2] = 0.5f + 0.3f * (float)Math.Sin(npc.ai[1] * 0.05f);

        // Decide next attack after idle
        if (npc.ai[1] > 180) // 3 seconds idle
        {
            npc.ai[3]++; // Increment attack counter
            float hpRatio = (float)npc.life / npc.lifeMax;

            if (hpRatio > 0.5f)
            {
                // Phase 1 attacks
                int attackChoice = (int)npc.ai[3] % 3;
                switch (attackChoice)
                {
                    case 0:
                        npc.ai[0] = (int)AIState.Phase1_Charge;
                        break;
                    case 1:
                        npc.ai[0] = (int)AIState.Phase1_VoidBoltBarrage;
                        break;
                    case 2:
                        npc.ai[0] = (int)AIState.Phase1_DistortionWave;
                        break;
                }
            }
            else
            {
                // Phase 2 attacks
                npc.ai[0] = (int)AIState.Phase2_Transition;
            }

            npc.ai[1] = 0;
            npc.netUpdate = true;
        }
    }

    private static void RunPhase1Charge(NPC npc, Player player)
    {
        npc.ai[1]++;

        if (npc.ai[1] == 1)
        {
            // Telegraph charge direction
            Vector2 chargeDir = (player.Center - npc.Center).SafeNormalize(Vector2.UnitY);
            npc.ai[2] = chargeDir.X;
            npc.ai[3] = chargeDir.Y;
            npc.localAI[2] = 1.5f; // Bright glow
        }

        if (npc.ai[1] < 60)
        {
            // Charge up - shake and glow
            npc.velocity *= 0.95f;
            npc.localAI[0] += Main.rand.NextFloat(-0.05f, 0.05f);
        }
        else if (npc.ai[1] == 60)
        {
            // Launch charge
            Vector2 chargeVel = new Vector2(npc.ai[2], npc.ai[3]) * 25f;
            npc.velocity = chargeVel;
            npc.localAI[2] = 2f;

            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Item73, npc.Center);
            }
        }
        else if (npc.ai[1] < 120)
        {
            // Charging
            npc.localAI[2] = 2f - (npc.ai[1] - 60) / 60f;

            // Leave trail
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[1] % 3 == 0)
            {
                Dust.NewDustPerfect(npc.Center, DustID.Vortex, npc.velocity * 0.1f, 0, default, 1.2f);
            }
        }
        else
        {
            // End charge, return to idle
            npc.ai[0] = (int)AIState.Phase1_Idle;
            npc.ai[1] = 0;
            npc.velocity *= 0.5f;
            npc.netUpdate = true;
        }
    }

    private static void RunPhase1VoidBolts(NPC npc, Player player)
    {
        npc.ai[1]++;

        if (npc.ai[1] == 1)
        {
            npc.velocity = Vector2.Zero;
        }

        // Circle around player while shooting
        float orbitAngle = npc.ai[1] * 0.02f;
        Vector2 orbitCenter = player.Center;
        Vector2 orbitPos = orbitCenter + new Vector2((float)Math.Cos(orbitAngle), (float)Math.Sin(orbitAngle)) * 300f;
        Vector2 toOrbit = orbitPos - npc.Center;
        npc.velocity = toOrbit * 0.05f;

        // Fire void bolts every 20 frames
        if (npc.ai[1] % 20 == 0 && npc.ai[1] < 300 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            Vector2 shootDir = (player.Center - npc.Center).SafeNormalize(Vector2.UnitY);
            shootDir = shootDir.RotatedBy(Main.rand.NextFloat(-0.3f, 0.3f));

            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, shootDir * 12f,
                ProjectileType_VoidBolt, 40, 2f, Main.myPlayer);

            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Item94, npc.Center);
            }
        }

        if (npc.ai[1] >= 300)
        {
            npc.ai[0] = (int)AIState.Phase1_Idle;
            npc.ai[1] = 0;
            npc.netUpdate = true;
        }
    }

    private static void RunPhase1DistortionWave(NPC npc, Player player)
    {
        npc.ai[1]++;

        if (npc.ai[1] == 1)
        {
            npc.velocity = Vector2.Zero;
            npc.localAI[2] = 1.5f;
        }

        // Charge up distortion
        if (npc.ai[1] < 90)
        {
            npc.localAI[0] += 0.02f;
            npc.localAI[2] = 1f + 0.5f * (float)Math.Sin(npc.ai[1] * 0.1f);
        }
        else if (npc.ai[1] == 90)
        {
            // Fire distortion wave
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 5; i++)
                {
                    float angle = -MathHelper.PiOver2 + i * MathHelper.PiOver4;
                    Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 8f,
                        ProjectileType_DistortionWave, 50, 3f, Main.myPlayer, angle);
                }
                SoundEngine.PlaySound(SoundID.Item122, npc.Center);
            }
            npc.localAI[2] = 2f;
        }
        else if (npc.ai[1] < 180)
        {
            npc.localAI[2] = 2f - (npc.ai[1] - 90) / 90f;
        }
        else
        {
            npc.ai[0] = (int)AIState.Phase1_Idle;
            npc.ai[1] = 0;
            npc.netUpdate = true;
        }
    }

    private static void RunPhase2Transition(NPC npc, Player player)
    {
        npc.ai[1]++;

        if (npc.ai[1] == 1)
        {
            npc.velocity = Vector2.Zero;
            npc.ai[2] = 1; // Mark phase 2
            npc.localAI[2] = 2f;

            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                // Screen shake
                for (int i = 0; i < 50; i++)
                {
                    Dust.NewDustPerfect(npc.Center, DustID.Vortex, Main.rand.NextVector2Circular(30, 30), 0, default, Main.rand.NextFloat(2f, 3f));
                }
            }
        }

        // Dramatic transformation
        npc.localAI[1] = 1f + 0.3f * (float)Math.Sin(npc.ai[1] * 0.2f);
        npc.localAI[0] += 0.05f;

        if (npc.ai[1] > 180)
        {
            npc.ai[0] = (int)AIState.Phase2_BlackHole;
            npc.ai[1] = 0;
            npc.ai[3] = 0; // Reset attack counter
            npc.localAI[1] = 1.3f; // Slightly larger
            npc.netUpdate = true;
        }
    }

    private static void RunPhase2BlackHole(NPC npc, Player player)
    {
        npc.ai[1]++;

        // Orbit player faster
        float orbitAngle = npc.ai[1] * 0.03f;
        Vector2 orbitPos = player.Center + new Vector2((float)Math.Cos(orbitAngle), (float)Math.Sin(orbitAngle)) * 400f;
        Vector2 toOrbit = orbitPos - npc.Center;
        npc.velocity = toOrbit * 0.03f;

        // Create black hole projectile periodically
        if (npc.ai[1] % 120 == 0 && npc.ai[1] < 600 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            Vector2 bhPos = player.Center + Main.rand.NextVector2Circular(200, 200);
            Projectile.NewProjectile(npc.GetSource_FromAI(), bhPos, Vector2.Zero,
                ProjectileType_BlackHole, 60, 5f, Main.myPlayer);
        }

        // Also fire star shards
        if (npc.ai[1] % 30 == 0 && npc.ai[1] < 600 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            Vector2 shootDir = (player.Center - npc.Center).SafeNormalize(Vector2.UnitY);
            shootDir = shootDir.RotatedBy(Main.rand.NextFloat(-0.5f, 0.5f));
            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, shootDir * 15f,
                ProjectileType_StarShard, 45, 2f, Main.myPlayer);
        }

        if (npc.ai[1] >= 600)
        {
            npc.ai[0] = (int)AIState.Phase2_StarRain;
            npc.ai[1] = 0;
            npc.netUpdate = true;
        }
    }

    private static void RunPhase2StarRain(NPC npc, Player player)
    {
        npc.ai[1]++;

        // Hover above
        Vector2 hoverPos = player.Center + new Vector2(0, -350);
        Vector2 toHover = hoverPos - npc.Center;
        npc.velocity = toHover * 0.02f;

        if (npc.ai[1] % 15 == 0 && npc.ai[1] < 400 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            // Rain stars from above
            Vector2 spawnPos = player.Center + new Vector2(Main.rand.NextFloat(-500, 500), -600);
            Vector2 vel = new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(8, 15));
            Projectile.NewProjectile(npc.GetSource_FromAI(), spawnPos, vel,
                ProjectileType_StarShard, 35, 1f, Main.myPlayer);
        }

        if (npc.ai[1] >= 400)
        {
            npc.ai[0] = (int)AIState.Phase2_VoidCharge;
            npc.ai[1] = 0;
            npc.netUpdate = true;
        }
    }

    private static void RunPhase2VoidCharge(NPC npc, Player player)
    {
        npc.ai[1]++;

        if (npc.ai[1] == 1)
        {
            // Position for charge
            Vector2 chargePos = player.Center + new Vector2(0, -500);
            npc.ai[2] = chargePos.X;
            npc.ai[3] = chargePos.Y;
        }

        if (npc.ai[1] < 60)
        {
            // Move to charge position
            Vector2 targetPos = new Vector2(npc.ai[2], npc.ai[3]);
            Vector2 toTarget = targetPos - npc.Center;
            npc.velocity = toTarget * 0.1f;
            npc.localAI[2] = 1f + npc.ai[1] / 60f;
        }
        else if (npc.ai[1] < 120)
        {
            // Charge down
            Vector2 chargeDir = (player.Center - npc.Center).SafeNormalize(Vector2.UnitY);
            npc.velocity = chargeDir * 30f;
            npc.localAI[2] = 2f;

            // Leave void trail
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[1] % 2 == 0)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero,
                    ProjectileType_BlackHole, 50, 3f, Main.myPlayer, 0, 0, 0, 0.5f);
            }
        }
        else
        {
            npc.ai[0] = (int)AIState.Phase1_Idle;
            npc.ai[1] = 0;
            npc.velocity *= 0.3f;
            npc.netUpdate = true;
        }
    }

    private static void RunEnragedState(NPC npc, Player player)
    {
        npc.ai[1]++;

        // Extremely aggressive - combine all attacks
        float hpRatio = (float)npc.life / npc.lifeMax;

        // Constant movement
        Vector2 targetPos = player.Center + new Vector2(
            (float)Math.Sin(npc.ai[1] * 0.05f) * 400,
            -300 + (float)Math.Cos(npc.ai[1] * 0.03f) * 100);
        Vector2 toTarget = targetPos - npc.Center;
        npc.velocity = toTarget * 0.05f;

        // Rapid fire everything
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            if (npc.ai[1] % 10 == 0)
            {
                Vector2 dir = (player.Center - npc.Center).SafeNormalize(Vector2.UnitY);
                dir = dir.RotatedBy(Main.rand.NextFloat(-0.4f, 0.4f));
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 18f,
                    ProjectileType_VoidBolt, 55, 3f, Main.myPlayer);
            }

            if (npc.ai[1] % 40 == 0)
            {
                Vector2 bhPos = player.Center + Main.rand.NextVector2Circular(150, 150);
                Projectile.NewProjectile(npc.GetSource_FromAI(), bhPos, Vector2.Zero,
                    ProjectileType_BlackHole, 70, 5f, Main.myPlayer);
            }

            if (npc.ai[1] % 120 == 0)
            {
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * MathHelper.PiOver4;
                    Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, dir * 12f,
                        ProjectileType_DistortionWave, 60, 4f, Main.myPlayer, angle);
                }
            }
        }

        // Visual intensity
        npc.localAI[2] = 2f + 0.5f * (float)Math.Sin(npc.ai[1] * 0.1f);
        npc.localAI[0] += 0.03f;
    }

    private static void RunDeathState(NPC npc)
    {
        npc.ai[1]++;
        npc.velocity *= 0.95f;
        npc.localAI[1] = MathHelper.Lerp(1.3f, 3f, npc.ai[1] / 180f);
        npc.localAI[2] = MathHelper.Lerp(2f, 5f, npc.ai[1] / 180f);
        npc.alpha = (int)(255 * (npc.ai[1] / 180f));

        if (npc.ai[1] % 5 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
        {
            for (int i = 0; i < 5; i++)
            {
                Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(80, 80), DustID.Vortex, Main.rand.NextVector2Circular(15, 15), 0, default, Main.rand.NextFloat(2f, 4f));
            }
        }

        if (npc.ai[1] >= 180)
        {
            npc.life = 0;
            npc.checkDead();
        }
    }

    private static void CheckPhaseTransitions(NPC npc)
    {
        float hpRatio = (float)npc.life / npc.lifeMax;

        if (npc.ai[2] == 0 && hpRatio <= 0.5f)
        {
            // Transition to phase 2
            npc.ai[2] = 1;
            npc.ai[0] = (int)AIState.Phase2_Transition;
            npc.ai[1] = 0;
            npc.ai[3] = 0;
            npc.netUpdate = true;

            // Heal slightly on phase transition (boss mechanic)
            npc.life = (int)(npc.lifeMax * 0.52f);
            npc.HealEffect((int)(npc.lifeMax * 0.02f));
        }
        else if (npc.ai[2] == 1 && hpRatio <= 0.1f)
        {
            // Enraged phase
            npc.ai[2] = 2;
            npc.ai[0] = (int)AIState.Enraged;
            npc.ai[1] = 0;
            npc.damage = (int)(npc.damage * 1.5f);
            npc.defense = (int)(npc.defense * 0.8f);
            npc.netUpdate = true;

            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Roar, npc.Center);
            }
        }
    }

    private static void UpdateAnimation(NPC npc)
    {
        npc.frameCounter++;
        if (npc.frameCounter >= 6)
        {
            npc.frameCounter = 0;
            npc.frame.Y += FrameHeight;
            if (npc.frame.Y >= FrameHeight * FrameCount)
            {
                npc.frame.Y = 0;
            }
        }
    }

    private static void UpdateVisualEffects(NPC npc)
    {
        // Passive dust
        if (Main.rand.NextBool(3) && Main.netMode != NetmodeID.MultiplayerClient)
        {
            Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(50, 50), DustID.Vortex, Main.rand.NextVector2Circular(2, 2), 0, default, Main.rand.NextFloat(0.8f, 1.5f));
        }
    }

    public static void FindFrame(On_NPC.orig_FindFrame orig, NPC npc, int frameHeight)
    {
        if (npc.type == BossType)
        {
            // Handled in AI
            return;
        }
        orig(npc, frameHeight);
    }

    public static void HitEffect(On_NPC.orig_HitEffect orig, NPC npc, NPC.HitInfo hit)
    {
        orig(npc, hit);

        if (npc.type != BossType) return;

        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(40, 40), DustID.Vortex, Main.rand.NextVector2Circular(8, 8), 0, default, Main.rand.NextFloat(1.5f, 2.5f));
            }
        }
    }

    public static bool CheckDead(On_NPC.orig_CheckDead orig, NPC npc)
    {
        if (npc.type != BossType) return orig(npc);

        if (npc.ai[0] != (int)AIState.Death)
        {
            npc.ai[0] = (int)AIState.Death;
            npc.ai[1] = 0;
            npc.life = 1;
            npc.dontTakeDamage = true;
            npc.netUpdate = true;
            return false;
        }

        // Drop loot
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            // Boss bag
            Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, ItemID.BossBag, 1);

            // Custom drops
            if (Main.rand.NextBool(7)) // 1/7 chance for trophy
            {
                Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, ItemID.VortexMonolith, 1);
            }

            // Resource drops
            for (int i = 0; i < Main.rand.Next(15, 25); i++)
            {
                Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, ItemID.FragmentVortex, Main.rand.Next(3, 8));
            }

            // Unique weapon drop
            if (Main.rand.NextBool(4))
            {
                Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, ItemID.VortexBeater, 1);
            }
        }

        return orig(npc);
    }

    public static bool PreDraw(On_NPC.orig_PreDraw orig, NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (npc.type != BossType) return orig(npc, spriteBatch, screenPos, drawColor);

        LoadTextures();

        if (bossTexture == null) return orig(npc, spriteBatch, screenPos, drawColor);

        Vector2 drawPos = npc.Center - screenPos;
        Rectangle frameRect = new Rectangle(0, npc.frame.Y, FrameWidth, FrameHeight);
        Vector2 origin = new Vector2(FrameWidth / 2f, FrameHeight / 2f);
        float scale = npc.localAI[1];
        float rotation = npc.localAI[0];

        // Draw glow effect behind
        if (glowTexture != null && npc.localAI[2] > 0.5f)
        {
            Color glowColor = new Color(155, 89, 182, 100) * npc.localAI[2] * 0.5f;
            spriteBatch.Draw(glowTexture, drawPos, null, glowColor, rotation, origin, scale * 1.5f, SpriteEffects.None, 0f);
        }

        // Draw main body with phase color tint
        Color bodyColor = drawColor;
        if (npc.ai[2] == 1) // Phase 2
        {
            bodyColor = Color.Lerp(drawColor, new Color(180, 100, 220), 0.5f);
        }
        else if (npc.ai[2] == 2) // Enraged
        {
            bodyColor = Color.Lerp(drawColor, new Color(255, 50, 150), 0.7f);
        }

        // Pulsing effect
        float pulse = 1f + 0.05f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f);
        scale *= pulse;

        spriteBatch.Draw(bossTexture, drawPos, frameRect, bodyColor, rotation, origin, scale, SpriteEffects.None, 0f);

        // Draw distortion particles around boss
        DrawDistortionParticles(spriteBatch, drawPos, screenPos, npc);

        return false; // We handled drawing
    }

    private static void DrawDistortionParticles(SpriteBatch spriteBatch, Vector2 drawPos, Vector2 screenPos, NPC npc)
    {
        // Simple distortion particles using vanilla textures
        Texture2D particleTex = Terraria.GameContent.TextureAssets.MagicPixel.Value;
        if (particleTex == null) return;

        int particleCount = (int)(10 + npc.localAI[2] * 10);
        for (int i = 0; i < particleCount; i++)
        {
            float angle = Main.GlobalTimeWrappedHourly * 2f + i * MathHelper.TwoPi / particleCount + npc.whoAmI * 0.1f;
            float radius = 70f * npc.localAI[1] + 20f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f + i);
            Vector2 particlePos = drawPos + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
            float particleScale = 0.5f + 0.3f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f + i);
            Color particleColor = new Color(155, 89, 182, 150) * npc.localAI[2] * 0.5f;

            spriteBatch.Draw(particleTex, particlePos, null, particleColor, 0f, new Vector2(0.5f), particleScale, SpriteEffects.None, 0f);
        }
    }

    private static void LoadTextures()
    {
        if (texturesLoaded) return;

        try
        {
            // Try to load custom textures from mod
            // For now, use vanilla textures as placeholders
            bossTexture = Terraria.GameContent.TextureAssets.Npc[BossType].Value;
            glowTexture = Terraria.GameContent.TextureAssets.Npc[BossType].Value;

            // If custom textures don't exist, create placeholder using vortex textures
            if (bossTexture == null || bossTexture == Terraria.GameContent.TextureAssets.MagicPixel.Value)
            {
                // Will use vanilla NPC texture rendering with custom draw code
            }

            texturesLoaded = true;
        }
        catch
        {
            texturesLoaded = true;
        }
    }

    public static void Unload()
    {
        bossTexture = null;
        glowTexture = null;
        texturesLoaded = false;
    }
}