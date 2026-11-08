using BunnyLoader;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

public sealed class OPWeapon : Mod
{
    public override void Load()
    {
        On_Item.SetDefaults += (orig, item, type, variant) =>
        {
            orig(item, type, variant);

            // Make Copper Broadsword into an OP weapon
            if (type == ItemID.CopperBroadsword)
            {
                item.damage = 999999;
                item.knockBack = 20f;
                item.useTime = 2;
                item.useAnimation = 2;
                item.autoReuse = true;
                item.useStyle = ItemUseStyleID.Swing;
                item.UseSound = SoundID.Item1;
                item.value = Item.buyPrice(gold: 1);
                item.rare = ItemRarityID.Red;

                // Shoot projectiles too
                item.shoot = ProjectileID.MoonlordBullet;
                item.shootSpeed = 30f;
            }

            // Make Wooden Sword OP too
            if (type == ItemID.WoodenSword)
            {
                item.damage = 500000;
                item.knockBack = 15f;
                item.useTime = 3;
                item.useAnimation = 3;
                item.autoReuse = true;
                item.useStyle = ItemUseStyleID.Swing;
                item.UseSound = SoundID.Item1;
                item.shoot = ProjectileID.LastPrism;
                item.shootSpeed = 25f;
            }
        };

        // Add global projectile buffs
        On_Projectile.SetDefaults += (orig, projectile, type) =>
        {
            orig(projectile, type);

            // Make all friendly projectiles OP
            if (projectile.friendly && !projectile.hostile)
            {
                projectile.damage *= 100;
                projectile.knockBack *= 5f;
                projectile.penetrate = -1; // Infinite pierce
            }
        };

        Log.Info("OP Weapon: Loaded! Copper Broadsword and Wooden Sword are now overpowered.");
    }
}