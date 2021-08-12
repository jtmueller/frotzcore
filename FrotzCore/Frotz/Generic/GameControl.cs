namespace Frotz.Generic;

public static class GameControl
{
    public static void Undo()
    {
        if (Main.cwin == 0)
        {
            if (FastMem.RestoreUndo() > 0)
            {
                OS.ResetScreen();
                //OS.DisplayString("One Move Undone...\n>");

                if (Main.h_version >= ZMachine.V5)
                {       /* for V5+ games we must */
                    Process.Store(2);           /* store 2 (for success) */
                    return;
                }

                if (Main.h_version <= ZMachine.V3)
                {       /* for V3- games we must */
                    Screen.ZShowStatus();       /* draw the status line  */
                    return;
                }

                OS.DisplayString("\nOne move undone...\n>");
            }
        }
    }

    public static void SaveGame()
    {
        Process.zargc = 0;
        FastMem.ZSave();
    }

    public static void RestoreGame()
    {
        Process.zargc = 0;
        FastMem.ZRestore();
        OS.ResetScreen();
    }
}
