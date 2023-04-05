namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver.Properties {
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()][global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.5.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()][global::System.Diagnostics.DebuggerNonUserCodeAttribute()][global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool promptForClipboardPaste {
            get {
                return ((bool)(this["promptForClipboardPaste"]));
            }
            set {
                this["promptForClipboardPaste"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()][global::System.Diagnostics.DebuggerNonUserCodeAttribute()][global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool choiceForClipboardPaste {
            get {
                return ((bool)(this["choiceForClipboardPaste"]));
            }
            set {
                this["choiceForClipboardPaste"] = value;
            }
        }
    }
}
