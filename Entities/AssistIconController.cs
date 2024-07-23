using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.AurorasAdditions.Entities
{
    /*
     * 1. figure out what variants
     * 2. draw icons
     *
     * Variants we care about:
     * Invincibility
     * Infinite Stamina
     * Air Dashes?
     * Game Speed?
     * "Variant/Assist enabled in general?"
     */

    class AssistIconController : Entity
    {

        private enum AssistIconNames
        {
            AssistMode, // 3d85b0
            VariantMode, // e69f00
            Invincible, // f0e442
            InfiniteStamina, // 009e73
            DashMode2, // cc79a7
            DashModeInf, // d55e00
            GameSpeed, // cccccc
        }

        private Dictionary<AssistIconNames, MTexture> textures;

        public AssistIconController()
        {
            Tag = Tags.HUD | Tags.Global;

            textures = new();
            string path_prefix = "assisticons/";

            MTexture texture = GFX.Gui[path_prefix + "assist"];
            textures[AssistIconNames.AssistMode] = texture;
            texture = GFX.Gui[path_prefix + "variant"];
            textures[AssistIconNames.VariantMode] = texture;
            texture = GFX.Gui[path_prefix + "invincible"];
            textures[AssistIconNames.Invincible] = texture;
            texture = GFX.Gui[path_prefix + "infinitestamina"];
            textures[AssistIconNames.InfiniteStamina] = texture;
            texture = GFX.Gui[path_prefix + "dashmode2"];
            textures[AssistIconNames.DashMode2] = texture;
            texture = GFX.Gui[path_prefix + "dashmodeinf"];
            textures[AssistIconNames.DashModeInf] = texture;
            texture = GFX.Gui[path_prefix + "gamespeed"];
            textures[AssistIconNames.GameSpeed] = texture;
        }


        public override void Render()
        {
            float offsetPerImage = 60f;
            Color alphaWhite = Color.White * ((AurorasAdditionsModule.ModSettings.IconAlpha)/10f);
            Vector2 position = new(-10f, 1080f - 50f);//new(100f, 2f);

            SaveData savedata = SaveData.Instance;

            if(AurorasAdditionsModule.ModSettings.ShowModeIcons)
            {
                if (savedata.AssistMode)
                {
                    textures[AssistIconNames.AssistMode].Draw(position, Vector2.Zero, alphaWhite);
                    position.X += offsetPerImage;
                }
                else if (savedata.VariantMode)
                {
                    textures[AssistIconNames.VariantMode].Draw(position, Vector2.Zero, alphaWhite);
                    position.X += offsetPerImage;
                }
            }
            if (!AurorasAdditionsModule.ModSettings.ShowAssistIcons) return;
            Assists assists = savedata.Assists;
            if (assists.Invincible)
            {
                textures[AssistIconNames.Invincible].Draw(position, Vector2.Zero, alphaWhite);
                position.X += offsetPerImage;
            }
            if (assists.InfiniteStamina)
            {
                textures[AssistIconNames.InfiniteStamina].Draw(position, Vector2.Zero, alphaWhite);
                position.X += offsetPerImage;
            }
            if (assists.DashMode == Assists.DashModes.Two)
            {
                textures[AssistIconNames.DashMode2].Draw(position, Vector2.Zero, alphaWhite);
                position.X += offsetPerImage;
            }
            if (assists.DashMode == Assists.DashModes.Infinite)
            {
                textures[AssistIconNames.DashModeInf].Draw(position, Vector2.Zero, alphaWhite);
                position.X += offsetPerImage;
            }
            if (assists.GameSpeed != 10) textures[AssistIconNames.GameSpeed].Draw(position, Vector2.Zero, alphaWhite);
        }
    }
}
