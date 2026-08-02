using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace OldVerDoormakerMod;

[HarmonyPatch(typeof(NCombatRoom))]
internal static class OldDoormakerPositionPatches
{
	private static readonly PropertyInfo EncounterSlotsProperty = AccessTools.Property(typeof(NCombatRoom), "EncounterSlots");

	[HarmonyPatch(typeof(NCombatRoom), "AddCreature")]
	[HarmonyPostfix]
	private static void AddCreaturePostfix(NCombatRoom __instance, Creature creature)
	{
		if (!(creature.Monster is OldDoor) && !(creature.Monster is OldDoormaker))
		{
			return;
		}
		NCreature creatureNode = __instance.GetCreatureNode(creature);
		if (creatureNode == null)
		{
			return;
		}
		string text = ((creature.Monster is OldDoor) ? "door" : "doormaker");
		if (EncounterSlotsProperty?.GetValue(__instance) is Control control)
		{
			NodePath path = text;
			Marker2D nodeOrNull = control.GetNodeOrNull<Marker2D>(path);
			if (nodeOrNull != null)
			{
				creatureNode.GlobalPosition = nodeOrNull.GlobalPosition;
			}
		}
		else
		{
			// v0.110 no longer contains the old encounter scene.  Give both
			// restored phases stable positions without depending on removed slots.
			creatureNode.GlobalPosition = creature.Monster is OldDoor
				? new Vector2(1500f, 740f)
				: new Vector2(1660f, 740f);
		}
	}
}
