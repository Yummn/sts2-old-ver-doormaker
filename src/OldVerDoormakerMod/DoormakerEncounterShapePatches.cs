using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;

namespace OldVerDoormakerMod;

internal static class DoormakerEncounterShapePatches
{
	[HarmonyPatch("CreateScene")]
	[HarmonyPrefix]
	private static bool CreateScenePrefix(EncounterModel __instance, ref Control __result)
	{
		if (!(__instance is OldDoormakerBoss))
		{
			return true;
		}
		Control control = new Control();
		control.Name = "OldDoormakerEncounterSlots";
		control.Size = new Vector2(1920f, 1080f);
		Marker2D marker2D = new Marker2D();
		marker2D.Name = "doormaker";
		marker2D.Position = new Vector2(1590f, 740f);
		Marker2D marker2D2 = new Marker2D();
		marker2D2.Name = "door";
		marker2D2.Position = new Vector2(1710f, 740f);
		control.AddChild(marker2D, forceReadableName: false, Node.InternalMode.Disabled);
		control.AddChild(marker2D2, forceReadableName: false, Node.InternalMode.Disabled);
		__result = control;
		return false;
	}

	[HarmonyPatch("get_HasScene")]
	[HarmonyPostfix]
	private static void HasScenePostfix(EncounterModel __instance, ref bool __result)
	{
		if (__instance is OldDoormakerBoss)
		{
			__result = true;
		}
	}

	[HarmonyPatch("get_Slots")]
	[HarmonyPostfix]
	private static void SlotsPostfix(EncounterModel __instance, ref IReadOnlyList<string> __result)
	{
		if (__instance is OldDoormakerBoss)
		{
			__result = new List<string> { "doormaker", "door" };
		}
	}
}
