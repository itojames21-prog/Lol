using BunnyLoader;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public sealed class SpaceDistortion : Mod
{
    public override void Load()
    {
        // Register the boss NPC
        NPCID.Sets.MustAlwaysDraw[NPCID.Count] = true;

        On_NPC.SetDefaults += SpaceDistortionBoss.SetDefaults;
        On_NPC.AI += SpaceDistortionBoss.AI;
        On_NPC.FindFrame += SpaceDistortionBoss.FindFrame;
        On_NPC.HitEffect += SpaceDistortionBoss.HitEffect;
        On_NPC.CheckDead += SpaceDistortionBoss.CheckDead;
        On_NPC.PreDraw += SpaceDistortionBoss.PreDraw;

        // Register projectiles
        On_Projectile.SetDefaults += SpaceDistortionProjectiles.SetDefaults;
        On_Projectile.AI += SpaceDistortionProjectiles.AI;
        On_Projectile.Kill += SpaceDistortionProjectiles.Kill;
        On_Projectile.PreDraw += SpaceDistortionProjectiles.PreDraw;

        // Boss spawn item
        On_Item.SetDefaults += SpaceDistortionSpawnItem.SetDefaults;
        On_Item.UseItem += SpaceDistortionSpawnItem.UseItem;

        // World generation - add boss to spawn conditions
        On_WorldGen.UpdateWorld += WorldGen_UpdateWorld;

        Log.Info("Space Distortion: Mod loaded. Use 'Cosmic Rift Key' to summon the boss at night.");
    }

    public override void Unload()
    {
        SpaceDistortionBoss.Unload();
        SpaceDistortionProjectiles.Unload();
        SpaceDistortionSpawnItem.Unload();
    }

    private void WorldGen_UpdateWorld(On_WorldGen.orig_UpdateWorld orig)
    {
        orig();
        // Ensure boss can spawn naturally after certain conditions
    }
}