using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

internal static class SpaceDistortionProjectiles
{
    public const int VoidBolt = SpaceDistortionBoss.ProjectileType_VoidBolt;
    public const int DistortionWave = SpaceDistortionBoss.ProjectileType_DistortionWave;
    public const int StarShard = SpaceDistortionBoss.ProjectileType_StarShard;
    public const int BlackHole = SpaceDistortionBoss.ProjectileType_BlackHole;

    private static Texture2D voidBoltTexture;
    private static Texture2D distortionWaveTexture;
    private static Texture2D starShardTexture;
    private static Texture2D blackHoleTexture;
    private static bool texturesLoaded = false;

    public static void SetDefaults(On_Projectile.orig_SetDefaults orig, Projectile projectile, int type)
    {
        orig(projectile, type);

        switch (type)
        {
            case VoidBolt:
                projectile.width = 24;
                projectile.height = 24;
                projectile.friendly = false;
                projectile.hostile = true;
                projectile.penetrate = 1;
                projectile.timeLeft = 300;
                projectile.tileCollide = false;
                projectile.ignoreWater = true;
                projectile.aiStyle = -1;
                projectile.scale = 1f;
                projectile.alpha = 0;
                projectile.light = 0.5f;
                break;

            case DistortionWave:
                projectile.width = 60;
                projectile.height = 60;
                projectile.friendly = false;
                projectile.hostile = true;
                projectile.penetrate = -1;
                projectile.timeLeft = 180;
                projectile.tileCollide = false;
                projectile.ignoreWater = true;
                projectile.aiStyle = -1;
                projectile.scale = 1f;
                projectile.alpha = 100;
                projectile.light = 0.3f;
                break;

            case StarShard:
                projectile.width = 16;
                projectile.height = 16;
                projectile.friendly = false;
                projectile.hostile = true;
                projectile.penetrate = 1;
                projectile.timeLeft = 240;
                projectile.tileCollide = true;
                projectile.ignoreWater = true;
                projectile.aiStyle = -1;
                projectile.scale = 1f;
                projectile.alpha = 0;
                projectile.light = 0.7f;
                break;

            case BlackHole:
                projectile.width = 80;
                projectile.height = 80;
                projectile.friendly = false;
                projectile.hostile = true;
                projectile.penetrate = -1;
                projectile.timeLeft = 600;
                projectile.tileCollide = false;
                projectile.ignoreWater = true;
                projectile.aiStyle = -1;
                projectile.scale = 1f;
                projectile.alpha = 50;
                projectile.light = 0.2f;
                break;
        }
    }

    public static void AI(On_Projectile.orig_AI orig, Projectile projectile)
    {
        switch (projectile.type)
        {
            case VoidBolt:
                AI_VoidBolt(projectile);
                break;
            case DistortionWave:
                AI_DistortionWave(projectile);
                break;
            case StarShard:
                AI_StarShard(projectile);
                break;
            case BlackHole:
                AI_BlackHole(projectile);
                break;
            default:
                orig(projectile);
                break;
        }
    }

    private static void AI_VoidBolt(Projectile projectile)
    {
        // Homing behavior after initial travel
        if (projectile.ai[0] == 0)
        {
            projectile.ai[0] = 1;
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        if (projectile.timeLeft < 240)
        {
            // Start homing towards nearest player
            Player target = null;
            float closestDist = 400f;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.active && !p.dead)
                {
                    float dist = Vector2.Distance(projectile.Center, p.Center);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        target = p;
                    }
                }
            }

            if (target != null)
            {
                Vector2 toTarget = (target.Center - projectile.Center).SafeNormalize(Vector2.Zero);
                projectile.velocity = Vector2.Lerp(projectile.velocity, toTarget * 12f, 0.05f);
                projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }
        }

        // Trail dust
        if (Main.rand.NextBool(2))
        {
            Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.Vortex, projectile.velocity * 0.1f, 0, default, 1.2f);
            dust.noGravity = true;
        }

        // Fade in
        if (projectile.alpha > 0)
        {
            projectile.alpha -= 10;
        }
    }

    private static void AI_DistortionWave(Projectile projectile)
    {
        // Expanding ring of distortion
        float progress = 1f - projectile.timeLeft / 180f;
        projectile.scale = 0.5f + progress * 2.5f;
        projectile.alpha = (int)(100 + progress * 155);

        // Rotate
        projectile.rotation += 0.02f;

        // Pull players towards it
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (p.active && !p.dead)
            {
                float dist = Vector2.Distance(projectile.Center, p.Center);
                float pullRadius = 60f * projectile.scale;

                if (dist < pullRadius && dist > 20f)
                {
                    Vector2 pullDir = (projectile.Center - p.Center).SafeNormalize(Vector2.Zero);
                    float pullStrength = (pullRadius - dist) / pullRadius * 0.3f;
                    p.velocity += pullDir * pullStrength;
                }

                // Damage if touching core
                if (dist < 30f * projectile.scale)
                {
                    p.Hurt(PlayerDeathReason.ByProjectile(p.whoAmI, projectile.whoAmI), projectile.damage, 0);
                }
            }
        }

        // Visual dust
        if (Main.rand.NextBool(3))
        {
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            float radius = 50f * projectile.scale;
            Vector2 pos = projectile.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
            Dust dust = Dust.NewDustPerfect(pos, DustID.Vortex, Vector2.Zero, 0, default, 1.5f);
            dust.noGravity = true;
            dust.velocity = (projectile.Center - pos) * 0.05f;
        }
    }

    private static void AI_StarShard(Projectile projectile)
    {
        if (projectile.ai[0] == 0)
        {
            projectile.ai[0] = 1;
            projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        // Spin
        projectile.rotation += 0.15f * (projectile.velocity.X > 0 ? 1 : -1);

        // Gravity
        projectile.velocity.Y += 0.15f;
        projectile.velocity.Y = Math.Min(projectile.velocity.Y, 20f);

        // Trail
        if (Main.rand.NextBool(2))
        {
            Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.Vortex, projectile.velocity * 0.2f, 0, default, 1f);
            dust.noGravity = true;
        }

        // Fade out near end
        if (projectile.timeLeft < 60)
        {
            projectile.alpha += 4;
        }
    }

    private static void AI_BlackHole(Projectile projectile)
    {
        // AI[0] = 0: normal black hole, AI[0] = 1: boss charge trail (smaller, shorter)
        bool isTrail = projectile.ai[0] == 1;

        float progress = 1f - projectile.timeLeft / (isTrail ? 120f : 600f);
        projectile.scale = isTrail ? (0.3f + progress * 0.5f) : (0.5f + progress * 1.5f);
        projectile.alpha = isTrail ? (int)(50 + progress * 200) : (int)(50 + progress * 100);

        // Rotation
        projectile.rotation += 0.03f;

        // Pull radius
        float pullRadius = isTrail ? 100f * projectile.scale : 200f * projectile.scale;
        float damageRadius = isTrail ? 40f * projectile.scale : 60f * projectile.scale;

        // Pull players and projectiles
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (p.active && !p.dead)
            {
                float dist = Vector2.Distance(projectile.Center, p.Center);
                if (dist < pullRadius && dist > 10f)
                {
                    Vector2 pullDir = (projectile.Center - p.Center).SafeNormalize(Vector2.Zero);
                    float pullStrength = (pullRadius - dist) / pullRadius * (isTrail ? 0.2f : 0.5f);
                    p.velocity += pullDir * pullStrength;
                }

                if (dist < damageRadius)
                {
                    p.Hurt(PlayerDeathReason.ByProjectile(p.whoAmI, projectile.whoAmI), projectile.damage, 0);
                }
            }
        }

        // Pull other hostile projectiles (optional visual effect)
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile other = Main.projectile[i];
            if (other.active && other.whoAmI != projectile.whoAmI && other.hostile && !other.friendly)
            {
                float dist = Vector2.Distance(projectile.Center, other.Center);
                if (dist < pullRadius * 0.5f && dist > 10f)
                {
                    Vector2 pullDir = (projectile.Center - other.Center).SafeNormalize(Vector2.Zero);
                    other.velocity += pullDir * 0.5f;
                }
            }
        }

        // Visual effects
        if (Main.rand.NextBool(isTrail ? 2 : 3))
        {
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            float radius = pullRadius * Main.rand.NextFloat(0.3f, 1f);
            Vector2 pos = projectile.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
            Dust dust = Dust.NewDustPerfect(pos, DustID.Vortex, Vector2.Zero, 0, default, isTrail ? 0.8f : 1.5f);
            dust.noGravity = true;
            dust.velocity = (projectile.Center - pos) * 0.1f;
            dust.color = new Color(100, 50, 150, 200);
        }

        // Screen shake for big black holes
        if (!isTrail && Main.rand.NextBool(30) && Vector2.Distance(projectile.Center, Main.LocalPlayer.Center) < 800f)
        {
            // Screen shake handled by game automatically for large explosions
        }
    }

    public static void Kill(On_Projectile.orig_Kill orig, Projectile projectile, int timeLeft)
    {
        orig(projectile, timeLeft);

        switch (projectile.type)
        {
            case VoidBolt:
                OnKill_VoidBolt(projectile);
                break;
            case DistortionWave:
                OnKill_DistortionWave(projectile);
                break;
            case StarShard:
                OnKill_StarShard(projectile);
                break;
            case BlackHole:
                OnKill_BlackHole(projectile);
                break;
        }
    }

    private static void OnKill_VoidBolt(Projectile projectile)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            for (int i = 0; i < 15; i++)
            {
                Dust.NewDustPerfect(projectile.Center, DustID.Vortex, Main.rand.NextVector2Circular(8, 8), 0, default, Main.rand.NextFloat(1f, 2f));
            }
            SoundEngine.PlaySound(SoundID.Item94, projectile.Center);
        }
    }

    private static void OnKill_DistortionWave(Projectile projectile)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            for (int i = 0; i < 30; i++)
            {
                float angle = i * MathHelper.TwoPi / 30;
                Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * Main.rand.NextFloat(5, 10);
                Dust.NewDustPerfect(projectile.Center, DustID.Vortex, vel, 0, default, Main.rand.NextFloat(1.5f, 2.5f));
            }
            SoundEngine.PlaySound(SoundID.Item122, projectile.Center);
        }
    }

    private static void OnKill_StarShard(Projectile projectile)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDustPerfect(projectile.Center, DustID.Vortex, Main.rand.NextVector2Circular(5, 5), 0, default, Main.rand.NextFloat(0.8f, 1.5f));
            }
            SoundEngine.PlaySound(SoundID.Item73, projectile.Center);
        }
    }

    private static void OnKill_BlackHole(Projectile projectile)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            for (int i = 0; i < 50; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * Main.rand.NextFloat(5, 15);
                Dust.NewDustPerfect(projectile.Center, DustID.Vortex, vel, 0, default, Main.rand.NextFloat(2f, 4f));
            }
            SoundEngine.PlaySound(SoundID.NPCDeath14, projectile.Center);
        }
    }

    public static bool PreDraw(On_Projectile.orig_PreDraw orig, Projectile projectile, ref Color lightColor)
    {
        switch (projectile.type)
        {
            case VoidBolt:
                return PreDraw_VoidBolt(projectile, ref lightColor);
            case DistortionWave:
                return PreDraw_DistortionWave(projectile, ref lightColor);
            case StarShard:
                return PreDraw_StarShard(projectile, ref lightColor);
            case BlackHole:
                return PreDraw_BlackHole(projectile, ref lightColor);
            default:
                return orig(projectile, ref lightColor);
        }
    }

    private static bool PreDraw_VoidBolt(Projectile projectile, ref Color lightColor)
    {
        LoadTextures();

        Texture2D tex = voidBoltTexture ?? Terraria.GameContent.TextureAssets.Projectile[ProjectileID.VortexBeaterRocket].Value;
        Vector2 drawPos = projectile.Center - Main.screenPosition;
        Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
        Color color = new Color(155, 89, 182, 200) * (1f - projectile.alpha / 255f);

        // Main bolt
        Main.spriteBatch.Draw(tex, drawPos, null, color, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);

        // Glow trail
        for (int i = 1; i < 5; i++)
        {
            Vector2 trailPos = projectile.Center - projectile.velocity * i * 4f - Main.screenPosition;
            float trailScale = projectile.scale * (1f - i * 0.2f);
            Color trailColor = color * (1f - i * 0.2f);
            Main.spriteBatch.Draw(tex, trailPos, null, trailColor, projectile.rotation, origin, trailScale, SpriteEffects.None, 0f);
        }

        return false;
    }

    private static bool PreDraw_DistortionWave(Projectile projectile, ref Color lightColor)
    {
        LoadTextures();

        Texture2D tex = distortionWaveTexture ?? Terraria.GameContent.TextureAssets.Projectile[ProjectileID.VortexBeaterRocket].Value;
        Vector2 drawPos = projectile.Center - Main.screenPosition;
        Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
        Color color = new Color(155, 89, 182, 150) * (1f - projectile.alpha / 255f);

        // Draw multiple rotating rings
        for (int ring = 0; ring < 3; ring++)
        {
            float ringProgress = projectile.timeLeft / 180f;
            float ringScale = projectile.scale * (0.5f + ring * 0.3f);
            float ringRot = projectile.rotation + ring * MathHelper.PiOver2;
            Color ringColor = color * (0.5f - ring * 0.1f);

            Main.spriteBatch.Draw(tex, drawPos, null, ringColor, ringRot, origin, ringScale, SpriteEffects.None, 0f);
        }

        return false;
    }

    private static bool PreDraw_StarShard(Projectile projectile, ref Color lightColor)
    {
        LoadTextures();

        Texture2D tex = starShardTexture ?? Terraria.GameContent.TextureAssets.Projectile[ProjectileID.Starfury].Value;
        Vector2 drawPos = projectile.Center - Main.screenPosition;
        Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
        Color color = new Color(255, 220, 100, 255) * (1f - projectile.alpha / 255f);

        // Star shape (draw multiple rotated)
        for (int i = 0; i < 3; i++)
        {
            float rot = projectile.rotation + i * MathHelper.PiOver2 * 2f / 3f;
            Main.spriteBatch.Draw(tex, drawPos, null, color * 0.7f, rot, origin, projectile.scale * 0.8f, SpriteEffects.None, 0f);
        }

        // Center glow
        Texture2D glowTex = Terraria.GameContent.TextureAssets.MagicPixel.Value;
        Main.spriteBatch.Draw(glowTex, drawPos, null, new Color(255, 220, 100, 100), 0f, new Vector2(0.5f), projectile.scale * 2f, SpriteEffects.None, 0f);

        return false;
    }

    private static bool PreDraw_BlackHole(Projectile projectile, ref Color lightColor)
    {
        LoadTextures();

        Texture2D tex = blackHoleTexture ?? Terraria.GameContent.TextureAssets.Projectile[ProjectileID.VortexBeaterRocket].Value;
        Vector2 drawPos = projectile.Center - Main.screenPosition;
        Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);

        // Outer distortion rings
        for (int ring = 0; ring < 5; ring++)
        {
            float ringProgress = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f + ring * 0.5f) * 0.5f + 0.5f;
            float ringScale = projectile.scale * (0.6f + ring * 0.2f) * ringProgress;
            float ringAlpha = 0.3f - ring * 0.05f;
            Color ringColor = new Color(80, 30, 120, (int)(255 * ringAlpha)) * (1f - projectile.alpha / 255f);

            Main.spriteBatch.Draw(tex, drawPos, null, ringColor, projectile.rotation + ring * 0.2f, origin, ringScale, SpriteEffects.None, 0f);
        }

        // Core
        Color coreColor = new Color(155, 89, 182, 200) * (1f - projectile.alpha / 255f);
        Main.spriteBatch.Draw(tex, drawPos, null, coreColor, projectile.rotation, origin, projectile.scale * 0.5f, SpriteEffects.None, 0f);

        // Center void
        Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
        Main.spriteBatch.Draw(pixel, drawPos, null, new Color(0, 0, 0, 200), 0f, new Vector2(0.5f), projectile.scale * 0.3f, SpriteEffects.None, 0f);

        return false;
    }

    private static void LoadTextures()
    {
        if (texturesLoaded) return;

        try
        {
            voidBoltTexture = Terraria.GameContent.TextureAssets.Projectile[VoidBolt].Value;
            distortionWaveTexture = Terraria.GameContent.TextureAssets.Projectile[DistortionWave].Value;
            starShardTexture = Terraria.GameContent.TextureAssets.Projectile[StarShard].Value;
            blackHoleTexture = Terraria.GameContent.TextureAssets.Projectile[BlackHole].Value;
            texturesLoaded = true;
        }
        catch
        {
            texturesLoaded = true;
        }
    }

    public static void Unload()
    {
        voidBoltTexture = null;
        distortionWaveTexture = null;
        starShardTexture = null;
        blackHoleTexture = null;
        texturesLoaded = false;
    }
}