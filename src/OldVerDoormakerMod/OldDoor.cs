using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace OldVerDoormakerMod;

public sealed class OldDoor : MonsterModel
{
	private static readonly PropertyInfo EncounterSlotsProperty = typeof(NCombatRoom).GetProperty("EncounterSlots", BindingFlags.Instance | BindingFlags.NonPublic);

	public const string InitialMoveId = "DRAMATIC_OPEN_MOVE";

	private MoveState _dramaticOpenMove;

	private MoveState _deadState;

	private Creature _doormaker;

	public override LocString Title => MonsterModel.L10NMonsterLookup("DOOR.name");

	protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/door");

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 165, 155);

	public override int MaxInitialHp => MinInitialHp;

	public MoveState DeadState
	{
		get
		{
			return _deadState;
		}
		private set
		{
			AssertMutable();
			_deadState = value;
		}
	}

	private int DramaticOpenDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 28, 25);

	private int EnforceDamage => 20;

	private int EnforceStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

	private int DoorSlamDamage => 15;

	private int DoorSlamRepeat => 2;

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.ArmorBig;

	public override bool ShouldDisappearFromDoom => false;

	public Creature Doormaker
	{
		get
		{
			return _doormaker ?? throw new InvalidOperationException();
		}
		private set
		{
			AssertMutable();
			_doormaker = value;
		}
	}

	public Creature GetOrCreateDoormaker()
	{
		if (_doormaker != null)
		{
			return _doormaker;
		}
		OldDoormaker monster = (OldDoormaker)ModelDb.Monster<OldDoormaker>().ToMutable();
		_doormaker = base.CombatState.CreateCreature(monster, CombatSide.Enemy, "doormaker");
		return _doormaker;
	}

	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		RuntimeLog.Write("OldDoor.AfterAddedToRoom");
		Creature creature = base.Creature;
		if (creature.SlotName == null)
		{
			creature.SlotName = "doormaker";
		}
		TrySnapToEncounterMarker(base.Creature, "doormaker");
		EnsureDoorVisible();
		await PowerCmd.Apply<OldDoorRevivalPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, (CardModel)null, false);
		OldDoormaker model = (OldDoormaker)ModelDb.Monster<OldDoormaker>().ToMutable();
		Doormaker = base.CombatState.CreateCreature(model, CombatSide.Enemy, "doormaker");
		creature = Doormaker;
		if (creature.SlotName == null)
		{
			creature.SlotName = "doormaker";
		}
		RuntimeLog.Write("OldDoor.AfterAddedToRoom complete");
	}

	private void EnsureDoorVisible()
	{
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null)
		{
			NCreature creatureNode = instance.GetCreatureNode(base.Creature);
			if (creatureNode != null)
			{
				creatureNode.Visible = true;
			}
		}
	}

	public static bool HasEncounterSlots()
	{
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return false;
		}
		return EncounterSlotsProperty?.GetValue(instance) is Control;
	}

	public static void TrySnapToEncounterMarker(Creature creature, string markerName)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return;
		}
		NCreature creatureNode = instance.GetCreatureNode(creature);
		if (creatureNode != null && EncounterSlotsProperty?.GetValue(instance) is Control control)
		{
			NodePath path = markerName;
			Marker2D nodeOrNull = control.GetNodeOrNull<Marker2D>(path);
			if (nodeOrNull != null)
			{
				creatureNode.GlobalPosition = nodeOrNull.GlobalPosition;
			}
		}
	}

	public void PrepareForRevival()
	{
		if (_dramaticOpenMove != null)
		{
			DeadState.FollowUpState = _dramaticOpenMove;
		}
	}

	public void PrepareForDeath()
	{
		DeadState.FollowUpState = DeadState;
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		_dramaticOpenMove = new MoveState("DRAMATIC_OPEN_MOVE", DramaticOpenMove, new SingleAttackIntent(DramaticOpenDamage));
		MoveState moveState = new MoveState("ENFORCE_MOVE", EnforceMove, new SingleAttackIntent(EnforceDamage), new BuffIntent());
		MoveState moveState2 = new MoveState("DOOR_SLAM_MOVE", DoorSlamMove, new MultiAttackIntent(DoorSlamDamage, DoorSlamRepeat));
		DeadState = new MoveState("DEAD_MOVE", DeadMove);
		DeadState.FollowUpState = DeadState;
		_dramaticOpenMove.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState;
		moveState.FollowUpState = _dramaticOpenMove;
		return new MonsterMoveStateMachine(new List<MonsterState> { _dramaticOpenMove, moveState, moveState2, DeadState }, _dramaticOpenMove);
	}

	private async Task DeadMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		if (!(base.Creature.GetPower<OldDoorRevivalPower>()?.IsHalfDead ?? false))
		{
			return;
		}
		Creature doormaker = GetOrCreateDoormaker();
		Creature creature = doormaker;
		if (creature.SlotName == null)
		{
			creature.SlotName = "doormaker";
		}
		if (!base.CombatState.ContainsCreature(doormaker))
		{
			RuntimeLog.Write("DeadMove fallback spawning doormaker");
			await CreatureCmd.Add(doormaker);
			TrySnapToEncounterMarker(doormaker, "doormaker");
			doormaker.Monster.RollMove(base.CombatState.Players.Select((Player p) => p.Creature));
		}
	}

	private async Task DramaticOpenMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await DamageCmd.Attack(DramaticOpenDamage).FromMonster(this).WithAttackerAnim("Attack", 0.15f)
			.WithHitFx("vfx/vfx_attack_blunt")
			.Execute(null);
	}

	private async Task EnforceMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await DamageCmd.Attack(EnforceDamage).FromMonster(this).WithAttackerAnim("Attack", 0.15f)
			.WithHitFx("vfx/vfx_attack_blunt")
			.Execute(null);
		await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, (decimal)EnforceStrength, base.Creature, (CardModel)null, false);
	}

	private async Task DoorSlamMove(IReadOnlyList<Creature> targets)
	{
		_ = targets;
		await DamageCmd.Attack(DoorSlamDamage).WithHitCount(DoorSlamRepeat).FromMonster(this)
			.WithAttackerAnim("Attack", 0.15f)
			.WithHitFx("vfx/vfx_attack_blunt")
			.Execute(null);
	}

	public void Close()
	{
		NCreature nCreature = NCombatRoom.Instance?.GetCreatureNode(base.Creature);
		if (nCreature != null)
		{
			TrySnapToEncounterMarker(base.Creature, "doormaker");
			nCreature.Visible = true;
		}
	}

	public void Open()
	{
		NCreature nCreature = NCombatRoom.Instance?.GetCreatureNode(base.Creature);
		if (nCreature != null)
		{
			nCreature.Visible = false;
		}
	}
}
