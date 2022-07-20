// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    [SupportedOSPlatform("windows")]
    static class Program {
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            using var mainForm = new MainForm();
            var screen = Screen.FromPoint(Cursor.Position);
            mainForm.StartPosition = FormStartPosition.Manual;
            mainForm.Location = Screen.FromPoint(Cursor.Position).WorkingArea.Location;
            Application.Run(mainForm);
        }
    }
}
