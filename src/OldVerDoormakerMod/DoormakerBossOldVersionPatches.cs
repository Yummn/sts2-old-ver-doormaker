using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Rooms;

namespace OldVerDoormakerMod;

/// <summary>
/// v0.110 removed the game's DoormakerBoss encounter model entirely.  Keep the
/// old encounter self-contained in the mod instead of referencing the removed
/// core type.
/// </summary>
public sealed class OldDoormakerBoss : EncounterModel
{
    public override RoomType RoomType => RoomType.Boss;
    public override MegaSkeletonDataResource? BossNodeSpineResource =>
        ModelDb.Encounter<QueenBoss>().BossNodeSpineResource;
    public override string BossNodePath => ModelDb.Encounter<QueenBoss>().BossNodePath;
    public override string CustomBgm => "event:/music/act3_boss_queen";
    protected override bool HasCustomBackground => false;
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        new MonsterModel[] { ModelDb.Monster<OldDoor>(), ModelDb.Monster<OldDoormaker>() };

    public override float GetCameraScaling() => 0.9f;
    public override Vector2 GetCameraOffset() => Vector2.Down * 60f;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        new (MonsterModel, string?)[] { (ModelDb.Monster<OldDoor>().ToMutable(), null) };
}

/// <summary>Add the restored boss back to Glory's boss pool and discovery list.</summary>
[HarmonyPatch]
internal static class OldDoormakerGloryPatches
{
    [HarmonyPatch(typeof(Glory), nameof(Glory.GenerateAllEncounters))]
    [HarmonyPostfix]
    private static void GenerateAllEncountersPostfix(ref IEnumerable<EncounterModel> __result)
    {
        var oldBoss = ModelDb.Encounter<OldDoormakerBoss>();
        if (!__result.Any(encounter => encounter.Id == oldBoss.Id))
            __result = __result.Append(oldBoss).ToArray();
    }

    [HarmonyPatch(typeof(Glory), "get_BossDiscoveryOrder")]
    [HarmonyPostfix]
    private static void BossDiscoveryOrderPostfix(ref IEnumerable<EncounterModel> __result)
    {
        var oldBoss = ModelDb.Encounter<OldDoormakerBoss>();
        if (!__result.Any(encounter => encounter.Id == oldBoss.Id))
            __result = __result.Append(oldBoss).ToArray();
    }
}

/// <summary>
/// Generic run-history icons are derived from the custom encounter id.  The
/// removed boss has no packaged icon in v0.110, so reuse Glory's Queen icon
/// instead of asking the preload system for nonexistent resources.
/// </summary>
[HarmonyPatch]
internal static class OldDoormakerIconPatches
{
    [HarmonyPatch(typeof(ImageHelper), nameof(ImageHelper.GetRoomIconPath))]
    [HarmonyPostfix]
    private static void RoomIconPostfix(ModelId? modelId, ref string? __result)
    {
        if (modelId is not { } id || id != ModelDb.Encounter<OldDoormakerBoss>().Id)
            return;
        var queenId = ModelDb.Encounter<QueenBoss>().Id.Entry.ToLowerInvariant();
        __result = ImageHelper.GetImagePath($"ui/run_history/{queenId}.png");
    }

    [HarmonyPatch(typeof(ImageHelper), nameof(ImageHelper.GetRoomIconOutlinePath))]
    [HarmonyPostfix]
    private static void RoomIconOutlinePostfix(ModelId? modelId, ref string? __result)
    {
        if (modelId is not { } id || id != ModelDb.Encounter<OldDoormakerBoss>().Id)
            return;
        var queenId = ModelDb.Encounter<QueenBoss>().Id.Entry.ToLowerInvariant();
        __result = ImageHelper.GetImagePath($"ui/run_history/{queenId}_outline.png");
    }
}
