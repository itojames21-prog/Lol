using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

public class SpaceDistortionRecipeSystem : ModSystem
{
    public override void AddRecipes()
    {
        // Cosmic Rift Key - Summons Space Distortion
        Recipe recipe = Recipe.Create(SpaceDistortionBoss.ItemType_SpawnItem);
        recipe.AddIngredient(ItemID.FragmentVortex, 18);
        recipe.AddIngredient(ItemID.LunarBar, 10);
        recipe.AddIngredient(ItemID.SoulofMight, 5);
        recipe.AddIngredient(ItemID.SoulofSight, 5);
        recipe.AddIngredient(ItemID.SoulofFright, 5);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();

        // Space Distortion Trophy (from boss bag)
        Recipe trophyRecipe = Recipe.Create(ItemID.VortexMonolith);
        trophyRecipe.AddIngredient(ItemID.BossBag, 1);
        trophyRecipe.AddTile(TileID.WorkBenches);
        trophyRecipe.Register();
    }
}