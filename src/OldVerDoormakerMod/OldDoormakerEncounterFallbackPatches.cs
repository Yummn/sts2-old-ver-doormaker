using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;

namespace OldVerDoormakerMod;

internal static class OldDoormakerEncounterFallbackPatches
{
	private static readonly FieldInfo MonstersWithSlotsField = AccessTools.Field(typeof(EncounterModel), "_monstersWithSlots");

	[HarmonyPatch("GenerateMonstersWithSlots")]
	[HarmonyPostfix]
	[HarmonyPriority(0)]
	private static void GenerateMonstersWithSlotsPostfix(EncounterModel __instance)
	{
		if (__instance is OldDoormakerBoss)
		{
			IReadOnlyList<(MonsterModel, string)> value = new List<(MonsterModel, string)> { (ModelDb.Monster<OldDoor>().ToMutable(), "door") };
			MonstersWithSlotsField?.SetValue(__instance, value);
		}
	}
}
