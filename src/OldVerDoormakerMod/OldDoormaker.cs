using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace OldVerDoormakerMod;

public sealed class OldDoormaker : MonsterModel
{
	private static readonly List<string> WhatIsItLines = new List<string> { "DOORMAKER.moves.WHAT_IS_IT.speakLine1", "DOORMAKER.moves.WHAT_IS_IT.speakLine2" };

	private readonly string[] _visualsPaths = new string[3] { "monsters/beta/door_maker_placeholder_2.png", "monsters/beta/door_maker_placeholder_3.png", "monsters/beta/door_maker_placeholder_4.png" };

	private int _timesGotBackIn;

	public override LocString Title => MonsterModel.L10NMonsterLookup("DOORMAKER.name");

	protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/doormaker");

	private string CurrentVisualPath
	{
		get
		{
			int num = (TimesGotBackIn % _visualsPaths.Length + _visualsPaths.Length) % _visualsPaths.Length;
			return _visualsPaths[num];
		}
	}

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 512, 489);

	public override int MaxInitialHp => MinInitialHp;

	private int LaserBeamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 34, 31);

	private int GetBackInMoveDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 45, 40);

	private int StrengthScale => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

	private int DoorHpScale => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 25, 20);

	public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

	public int TimesGotBackIn
	{
		get
		{
			return _timesGotBackIn;
		}
		private set
		{
			AssertMutable();
			_timesGotBackIn = value;
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState moveState = new MoveState("WHAT_IS_IT_MOVE", WhatIsItMove, new StunIntent());
		MoveState moveState2 = new MoveState("BEAM_MOVE", BeamMove, new SingleAttackIntent(LaserBeamDamage));
		MoveState moveState3 = new MoveState("GET_BACK_IN_MOVE", GetBackInMove, new SingleAttackIntent(GetBackInMoveDamage), new BuffIntent(), new EscapeIntent());
		moveState.FollowUpState = moveState2;
		moveState2.FollowUpState = moveState3;
		moveState3.FollowUpState = moveState3;
		return new MonsterMoveStateMachine(new List<MonsterState> { moveState, moveState2, moveState3 }, moveState);
	}

	private Task WhatIsItMove(IReadOnlyList<Creature> targets)
	{
		RuntimeLog.Write("OldDoormaker.WhatIsItMove start");
		RuntimeLog.Write("OldDoormaker.WhatIsItMove end");
		return Task.CompletedTask;
	}

	private async Task BeamMove(IReadOnlyList<Creature> targets)
	{
		RuntimeLog.Write("OldDoormaker.BeamMove start");
		_ = targets;
		await DamageCmd.Attack(LaserBeamDamage).FromMonster(this).WithAttackerAnim("Attack", 0.15f)
			.WithHitFx("vfx/vfx_attack_blunt")
			.Execute(null);
		RuntimeLog.Write("OldDoormaker.BeamMove end");
	}

	private async Task GetBackInMove(IReadOnlyList<Creature> targets)
	{
		RuntimeLog.Write("OldDoormaker.GetBackInMove start");
		_ = targets;
		await DamageCmd.Attack(GetBackInMoveDamage).FromMonster(this).WithAttackerAnim("Attack", 0.15f)
			.WithHitFx("vfx/vfx_attack_blunt")
			.Execute(null);
		RuntimeLog.Write("OldDoormaker.GetBackInMove after attack");
		if (base.Creature.IsDead)
		{
			RuntimeLog.Write("OldDoormaker.GetBackInMove aborted: self dead");
			return;
		}
		var context = new ThrowingPlayerChoiceContext();
		await PowerCmd.Apply<StrengthPower>(context, base.Creature, 5m, base.Creature, (CardModel)null, false);
		RuntimeLog.Write("OldDoormaker.GetBackInMove after self strength");
		TimesGotBackIn++;
		Creature door = base.Creature.CombatState.GetTeammatesOf(base.Creature).FirstOrDefault((Creature c) => c.Monster is OldDoor);
		if (door != null)
		{
			RuntimeLog.Write("OldDoormaker.GetBackInMove found door teammate");
			OldDoorRevivalPower power = door.GetPower<OldDoorRevivalPower>();
			if (power != null)
			{
				await power.DoRevive();
				RuntimeLog.Write("OldDoormaker.GetBackInMove after DoRevive");
			}
			var desiredStrength = StrengthScale * TimesGotBackIn;
			var doorStrength = door.GetPower<StrengthPower>();
			if (doorStrength == null)
				await PowerCmd.Apply<StrengthPower>(context, door, desiredStrength, base.Creature, (CardModel)null, false);
			else
				await PowerCmd.ModifyAmount(context, doorStrength, desiredStrength - doorStrength.Amount, base.Creature, (CardModel)null);
			RuntimeLog.Write("OldDoormaker.GetBackInMove after door strength set");
			await CreatureCmd.SetMaxAndCurrentHp(door, door.MaxHp + DoorHpScale * TimesGotBackIn);
			RuntimeLog.Write("OldDoormaker.GetBackInMove after door hp scale");
			await PlayReturnToDoorAnimation(door);
			RemoveFromRoomAndCombatPreservingState();
			RuntimeLog.Write("OldDoormaker.GetBackInMove after hard remove");
			await Cmd.Wait(0.25f);
			RuntimeLog.Write("OldDoormaker.GetBackInMove end (re-entered door)");
		}
		else
		{
			RuntimeLog.Write("OldDoormaker.GetBackInMove no door teammate");
			await AnimOut();
			await CreatureCmd.Escape(base.Creature, removeCreatureNode: false);
			RuntimeLog.Write("OldDoormaker.GetBackInMove end (escaped without door)");
		}
	}

	private void RemoveFromRoomAndCombatPreservingState()
	{
		NCreature nCreature = NCombatRoom.Instance?.GetCreatureNode(base.Creature);
		if (nCreature != null)
		{
			NCombatRoom.Instance?.RemoveCreatureNode(nCreature);
			nCreature.ToggleIsInteractable(on: false);
			nCreature.Visible = false;
			nCreature.QueueFreeSafely();
		}
		CombatManager.Instance.RemoveCreature(base.Creature);
		if (base.CombatState.ContainsCreature(base.Creature))
		{
			base.CombatState.RemoveCreature(base.Creature, unattach: false);
		}
	}

	private async Task PlayReturnToDoorAnimation(Creature door)
	{
		NCombatRoom room = NCombatRoom.Instance;
		if (room == null)
		{
			await Cmd.Wait(0.12f);
			return;
		}
		NCreature doormakerNode = room.GetCreatureNode(base.Creature);
		NCreature doorNode = room.GetCreatureNode(door);
		if (doormakerNode == null || doorNode == null)
		{
			await Cmd.Wait(0.12f);
			return;
		}
		doorNode.Visible = true;
		Vector2 doorPos = doorNode.GlobalPosition;
		Node2D doormakerBody = ResolveVisualBody(doormakerNode);
		Node2D doorBody = ResolveVisualBody(doorNode);
		Tween outTween = doormakerNode.CreateTween().SetParallel();
		outTween.TweenProperty(doormakerNode, "global_position", doorPos, 0.22).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic);
		if (doormakerBody != null)
		{
			outTween.TweenProperty(doormakerBody, "modulate", Colors.Transparent, 0.22).From(Colors.White);
			outTween.TweenProperty(doormakerBody, "scale", doormakerBody.Scale * 0.6f, 0.22).From(doormakerBody.Scale).SetEase(Tween.EaseType.In);
		}
		if (doorBody != null)
		{
			Tween doorTween = doorNode.CreateTween().SetParallel();
			doorTween.TweenProperty(doorBody, "modulate", Colors.White, 0.16).From(Colors.Black);
			doorTween.TweenProperty(doorBody, "scale", doorBody.Scale * 1.06f, 0.1).From(doorBody.Scale).SetEase(Tween.EaseType.Out);
			doorTween.TweenProperty(doorBody, "scale", doorBody.Scale, 0.1).SetDelay(0.1).SetEase(Tween.EaseType.In);
		}
		RuntimeLog.Write("OldDoormaker.GetBackInMove return animation played");
		await Cmd.CustomScaledWait(0.1f, 0.3f);
	}

	private async Task RemoveCreatureWhenDone(NCreature creatureNode, Tween tween)
	{
		await tween.ToSignal(tween, Tween.SignalName.Finished);
		NCombatRoom.Instance?.RemoveCreatureNode(creatureNode);
		creatureNode.QueueFreeSafely();
	}

	private async Task AnimOut()
	{
		NCreature creatureNode = NCombatRoom.Instance?.GetCreatureNode(base.Creature);
		if (creatureNode != null)
		{
			Node2D body = ResolveVisualBody(creatureNode);
			if (body == null)
			{
				RuntimeLog.Write("OldDoormaker.AnimOut skipped: no visuals body");
				return;
			}
			Vector2 scale = body.Scale;
			creatureNode.ToggleIsInteractable(on: false);
			Tween tween = creatureNode.CreateTween();
			tween.TweenProperty(body, "scale", scale * 0.5f, 1.2).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic);
			tween.Parallel().TweenProperty(body, "modulate", Colors.Black, 0.25).From(Colors.White);
			tween.Parallel().TweenProperty(body, "modulate", Colors.Transparent, 0.25).From(Colors.Black)
				.SetDelay(0.25);
			TaskHelper.RunSafely(RemoveCreatureWhenDone(creatureNode, tween));
			await Cmd.CustomScaledWait(0.2f, 0.6f);
		}
	}

	public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (creature == base.Creature)
		{
			NRunMusicController.Instance?.UpdateMusicParameter("queen_progress", 5f);
		}
		return Task.CompletedTask;
	}

	public async Task AnimIn()
	{
		NCreature creatureNode = NCombatRoom.Instance?.GetCreatureNode(base.Creature);
		NRunMusicController.Instance?.UpdateMusicParameter("queen_progress", 1f);
		if (creatureNode == null)
		{
			return;
		}
		Node2D body = ResolveVisualBody(creatureNode);
		if (body == null)
		{
			RuntimeLog.Write("OldDoormaker.AnimIn skipped: no visuals body");
			return;
		}
		string visualPath = CurrentVisualPath;
		RuntimeLog.Write($"OldDoormaker.AnimIn visual={visualPath} timesGotBackIn={TimesGotBackIn} hasSpine={creatureNode.HasSpineAnimation}");
		Vector2 scale = body.Scale;
		if (body is Sprite2D sprite)
		{
			sprite.Texture = PreloadManager.Cache.GetTexture2D(ImageHelper.GetImagePath(visualPath));
		}
		Tween tween = creatureNode.CreateTween();
		tween.TweenProperty(body, "scale", scale, 1.2).From(scale * 0.5f).SetEase(Tween.EaseType.In)
			.SetTrans(Tween.TransitionType.Cubic);
		tween.Parallel().TweenProperty(body, "modulate", Colors.Black, 0.25).From(Colors.Transparent);
		tween.Parallel().TweenProperty(body, "modulate", Colors.White, 0.25).From(Colors.Black)
			.SetDelay(0.25);
		TaskHelper.RunSafely(PlayLineAfterAnimIn(tween));
		creatureNode.StartReviveAnim();
		if (!creatureNode.HasSpineAnimation)
		{
			StartFallbackIdleBob(body);
			RuntimeLog.Write("OldDoormaker.AnimIn started fallback idle bob");
		}
		await Cmd.CustomScaledWait(0.2f, 0.6f);
	}

	private static Node2D ResolveVisualBody(NCreature creatureNode)
	{
		NCreatureVisuals visuals = creatureNode.Visuals;
		if (visuals == null)
		{
			return null;
		}
		Node node = visuals;
		Node2D node2D = node.GetNodeOrNull<Node2D>("%Visuals") ?? node.GetNodeOrNull<Node2D>("Visuals");
		if (node2D != null)
		{
			return node2D;
		}
		foreach (Node child in node.GetChildren())
		{
			if (child is Node2D result)
			{
				return result;
			}
		}
		return null;
	}

	private static void StartFallbackIdleBob(Node2D body)
	{
		if (!body.HasMeta("old_doormaker_idle_tween") || !(body.GetMeta("old_doormaker_idle_tween").AsGodotObject() is Tween tween) || !tween.IsValid() || !tween.IsRunning())
		{
			Vector2 position = body.Position;
			Tween tween2 = body.CreateTween().SetLoops();
			tween2.TweenProperty(body, "position", position + new Vector2(0f, -6f), 0.9).SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
			tween2.TweenProperty(body, "position", position + new Vector2(0f, 0f), 0.9).SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
			body.SetMeta("old_doormaker_idle_tween", tween2);
		}
	}

	private async Task PlayLineAfterAnimIn(Tween tween)
	{
		await tween.ToSignal(tween, Tween.SignalName.Finished);
		if (base.Creature.CombatState != null)
		{
			// Android v0.103.2 does not ship the old Doormaker dialogue keys.
			// Calling TalkCmd.Play creates a speech bubble whose deferred text
			// resolution faults outside this task, so skip the unavailable line.
			RuntimeLog.Write("OldDoormaker.PlayLineAfterAnimIn skipped dialogue unavailable in v103");
		}
	}
}
