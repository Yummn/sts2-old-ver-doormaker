using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace OldVerDoormakerMod;

public sealed class OldDoorRevivalPower : PowerModel
{
	private sealed class Data
	{
		public bool IsHalfDead;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	protected override bool IsVisibleInternal => false;

	public bool IsHalfDead => GetInternalData<Data>().IsHalfDead;

	protected override object InitInternalData()
	{
		return new Data();
	}

	public override Task BeforeDeath(Creature creature)
	{
		if (creature == base.Owner)
		{
			GetInternalData<Data>().IsHalfDead = true;
			RuntimeLog.Write("OldDoorRevivalPower.BeforeDeath set IsHalfDead=true");
		}
		return Task.CompletedTask;
	}

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		_ = choiceContext;
		_ = deathAnimLength;
		if (creature != base.Owner)
		{
			return;
		}
		RuntimeLog.Write($"OldDoorRevivalPower.AfterDeath wasRemovalPrevented={wasRemovalPrevented} isHalfDead={IsHalfDead}");
		if (wasRemovalPrevented)
		{
			GetInternalData<Data>().IsHalfDead = false;
			return;
		}
		OldDoor door = (OldDoor)base.Owner.Monster;
		try
		{
			door.PrepareForDeath();
			door.SetMoveImmediate(door.DeadState);
			door.Open();
			Creature doormakerCreature = door.GetOrCreateDoormaker();
			Creature creature2 = doormakerCreature;
			if (creature2.SlotName == null)
			{
				creature2.SlotName = "doormaker";
			}
			if (!base.Owner.CombatState.ContainsCreature(doormakerCreature))
			{
				await CreatureCmd.Add(doormakerCreature);
				RuntimeLog.Write("AfterDeath added doormaker creature");
			}
			OldDoor.TrySnapToEncounterMarker(doormakerCreature, "doormaker");
			doormakerCreature.Monster.RollMove(base.Owner.CombatState.Players.Select((Player p) => p.Creature));
			MonsterModel monster = doormakerCreature.Monster;
			if (monster is OldDoormaker oldDoormaker)
			{
				try
				{
					await oldDoormaker.AnimIn();
					RuntimeLog.Write("AfterDeath AnimIn ok");
				}
				catch (Exception ex)
				{
					Exception animEx = ex;
					RuntimeLog.Write("AfterDeath AnimIn exception: " + animEx.Message);
				}
			}
		}
		catch (Exception ex2)
		{
			GD.PushError($"[OldVerDoormaker] Door phase transition failed: {ex2}");
			RuntimeLog.Write("AfterDeath exception: " + ex2.Message);
			Creature fallback = door.GetOrCreateDoormaker();
			Creature creature2 = fallback;
			if (creature2.SlotName == null)
			{
				creature2.SlotName = "doormaker";
			}
			if (!base.Owner.CombatState.ContainsCreature(fallback))
			{
				await CreatureCmd.Add(fallback);
			}
			OldDoor.TrySnapToEncounterMarker(fallback, "doormaker");
			fallback.Monster.RollMove(base.Owner.CombatState.Players.Select((Player p) => p.Creature));
		}
	}

	public async Task DoRevive()
	{
		GetInternalData<Data>().IsHalfDead = false;
		OldDoor door = (OldDoor)base.Owner.Monster;
		IReadOnlyList<Player> players = base.CombatState.Players;
		await CreatureCmd.SetMaxHp(amount: Creature.ScaleHpForMultiplayer(playerCount: players.Count, actIndex: base.CombatState.RunState.CurrentActIndex, hp: base.Owner.Monster.MinInitialHp, encounter: base.CombatState.Encounter), creature: base.Owner);
		await CreatureCmd.Heal(base.Owner, base.Owner.MaxHp);
		door.PrepareForRevival();
		door.Close();
	}

	public override bool ShouldAllowHitting(Creature creature)
	{
		if (creature != base.Owner)
		{
			return true;
		}
		return !IsHalfDead;
	}

	public override bool ShouldStopCombatFromEnding()
	{
		if (!IsHalfDead)
		{
			return false;
		}
		OldDoor oldDoor = (OldDoor)base.Owner.Monster;
		if (!base.Owner.CombatState.ContainsCreature(oldDoor.GetOrCreateDoormaker()))
		{
			return false;
		}
		return oldDoor.Doormaker.IsAlive;
	}

	public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
	{
		if (creature == base.Owner)
		{
			return false;
		}
		OldDoor oldDoor = (OldDoor)base.Owner.Monster;
		if (creature == oldDoor.Doormaker && !base.Owner.CombatState.ContainsCreature(creature))
		{
			return false;
		}
		return true;
	}

	public override bool ShouldPowerBeRemovedAfterOwnerDeath()
	{
		return false;
	}
}
