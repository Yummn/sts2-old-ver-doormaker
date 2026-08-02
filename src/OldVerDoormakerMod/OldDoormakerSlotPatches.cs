using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;

namespace OldVerDoormakerMod;

[HarmonyPatch(typeof(CombatState))]
internal static class OldDoormakerSlotPatches
{
	[HarmonyPatch("CreateCreature")]
	[HarmonyPrefix]
	private static void CreateCreaturePrefix(MonsterModel monster, ref string? slot)
	{
		if (monster is OldDoor && string.IsNullOrEmpty(slot))
		{
			slot = "door";
		}
		else if (monster is OldDoormaker && string.IsNullOrEmpty(slot))
		{
			slot = "doormaker";
		}
	}
}
