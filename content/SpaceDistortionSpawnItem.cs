using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

internal static class SpaceDistortionSpawnItem
{
    public const int ItemType = SpaceDistortionBoss.ItemType_SpawnItem;

    public static void SetDefaults(On_Item.orig_SetDefaults orig, Item item, int type, bool noMatCheck)
    {
        orig(item, type, noMatCheck);

        if (type != ItemType) return;

        item.width = 32;
        item.height = 32;
        item.maxStack = 1;
        item.value = 100000;
        item.rare = ItemRarityID.Red;
        item.useStyle = ItemUseStyleID.HoldUp;
        item.useTime = 45;
        item.useAnimation = 45;
        item.useTurn = true;
        item.autoReuse = false;
        item.consumable = true;
        item.UseSound = SoundID.Item44;
        item.toolTip = "Summons the Space Distortion boss at night\n'The fabric of reality tears apart...'";
    }

    public static bool UseItem(On_Item.orig_UseItem orig, Item item, Player player)
    {
        if (item.type != ItemType) return orig(item, player);

        // Can only use at night
        if (!Main.nightTime)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText("The cosmic energies only align at night...", 255, 100, 150);
            }
            return false;
        }

        // Check if boss already exists
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc.active && npc.type == SpaceDistortionBoss.BossType)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    Main.NewText("Space Distortion has already been summoned!", 255, 100, 150);
                }
                return false;
            }
        }

        // Spawn the boss
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            int bossIndex = NPC.NewNPC(player.GetSource_ItemUse(item), (int)player.Center.X, (int)player.Center.Y - 500, SpaceDistortionBoss.BossType);
            if (bossIndex < Main.maxNPCs)
            {
                Main.npc[bossIndex].TargetClosest(true);
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, bossIndex);
            }
        }

        // Effects
        if (Main.netMode != NetmodeID.Server)
        {
            SoundEngine.PlaySound(SoundID.Roar, player.Center);
            for (int i = 0; i < 50; i++)
            {
                Dust.NewDustPerfect(player.Center, DustID.Vortex, Main.rand.NextVector2Circular(15, 15), 0, default, Main.rand.NextFloat(1.5f, 2.5f));
            }
            Main.NewText("Space Distortion has awoken!", 155, 89, 182);
        }

        return true;
    }

    public static void AddRecipes()
    {
        // Recipe is added via ModSystem in main mod file
    }

    public static void Unload()
    {
        // Nothing to unload
    }
}