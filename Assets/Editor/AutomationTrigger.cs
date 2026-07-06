using System.IO;
using UnityEditor;

[InitializeOnLoad]
public static class AutomationTrigger
{
    const string RebuildTriggerPath = "Logs/trigger_rebuild_gameplay.txt";
    const string CaptureTriggerPath = "Logs/trigger_capture_gameplay.txt";
    const string ExitPlayTriggerPath = "Logs/trigger_exit_playmode.txt";
    const string CombatCaptureTriggerPath = "Logs/trigger_capture_combat.txt";
    const string DecalPreviewTriggerPath = "Logs/trigger_decal_preview.txt";
    const string SetupBloodDecalsTriggerPath = "Logs/trigger_setup_blood_decals.txt";

    static AutomationTrigger()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (File.Exists(ExitPlayTriggerPath))
        {
            File.Delete(ExitPlayTriggerPath);
            EditorApplication.isPlaying = false;
        }
        if (File.Exists(RebuildTriggerPath))
        {
            File.Delete(RebuildTriggerPath);
            GameplaySceneBuilder.Build();
        }
        if (File.Exists(CaptureTriggerPath))
        {
            File.Delete(CaptureTriggerPath);
            GameplayPreviewCapture.Run();
        }
        if (File.Exists(CombatCaptureTriggerPath))
        {
            File.Delete(CombatCaptureTriggerPath);
            CombatPreviewCapture.Run();
        }
        if (File.Exists(DecalPreviewTriggerPath))
        {
            File.Delete(DecalPreviewTriggerPath);
            BloodDecalPreview.Run();
        }
        if (File.Exists(SetupBloodDecalsTriggerPath))
        {
            File.Delete(SetupBloodDecalsTriggerPath);
            BloodDecalSetup.Setup();
        }
    }
}
