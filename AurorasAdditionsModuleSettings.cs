using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.AurorasAdditions {
    public class AurorasAdditionsModuleSettings : EverestModuleSettings {

        public bool DisableDrawModeBackground { get; set; } = true;
        [SettingSubText("modoptions_AurorasAdditions_DrawCursor")]
        public bool DrawCursor { get; set; } = true;
        public bool ShowSaveAndQuitToMap { get; set; } = true;
        [SettingSubText("modoptions_AurorasAdditions_ShowAssistIcons_Subtext")]
        public bool ShowAssistIcons { get; set; } = true;
        public bool ShowModeIcons { get; set; } = true;
        [SettingRange(1, 10)]
        public int IconAlpha { get; set; } = 8;

        [SettingName("modoptions_AurorasAdditions_DrawModeColor")]
        [SettingSubText("modoptions_AurorasAdditions_DrawModeColor_Subtext")]
        [SettingMaxLength(6)]
        public string DrawModeColor { get; set; } = "ff6def";

        [SettingSubText("modoptions_AurorasAdditions_DrawModeColor_Subtext")]
        [SettingMaxLength(6)]
        public string DrawModeColor2 { get; set; } = "ac3232";

        [SettingSubText("modoptions_AurorasAdditions_DrawModeColor_Subtext")]
        [SettingMaxLength(6)]
        public string DrawModeColor3 { get; set; } = "44b7ff";

        [SettingSubText("modoptions_AurorasAdditions_DrawModeColor4_Subtext")]
        [SettingMaxLength(6)]
        public string DrawModeColor4 { get; set; } = "F2EB6D";

        [SettingSubText("modoptions_AurorasAdditions_ToggleDrawMode_Subtext")]
        [DefaultButtonBinding(0, Keys.None)]
        public ButtonBinding ToggleDrawMode { get; set; } = new ButtonBinding();

        [DefaultButtonBinding(0, Keys.None)]
        public ButtonBinding HoldForColor2 { get; set; } = new ButtonBinding();

        [DefaultButtonBinding(0, Keys.None)]
        public ButtonBinding HoldForColor3 { get; set; } = new ButtonBinding();


        [DefaultButtonBinding(0, Keys.None)]
        public ButtonBinding JumpToPreviousModOption { get; set; } = new ButtonBinding();
        [DefaultButtonBinding(0, Keys.None)]
        public ButtonBinding JumpToNextModOption { get; set; } = new ButtonBinding();
        [DefaultButtonBinding(0, Keys.None)]
        public ButtonBinding ToggleMusic { get; set; } = new ButtonBinding();

        [SettingName("modoptions_AurorasAdditions_ToggleInvincibility")]
        [DefaultButtonBinding(0, Keys.None)]
        public ButtonBinding ToggleInvicibility { get; set; } = new ButtonBinding();
        [DefaultButtonBinding(0, Keys.None)]
        public ButtonBinding ToggleInfiniteStamina { get; set; } = new ButtonBinding();
        [DefaultButtonBinding(0, Keys.None)]
        public ButtonBinding ToggleAirDashMode { get; set; } = new ButtonBinding();

    }
}
