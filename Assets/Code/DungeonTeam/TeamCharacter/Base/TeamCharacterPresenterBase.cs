﻿using Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace;
using MVP;
using UnityEngine;

namespace Code.DungeonTeam.TeamCharacter.Base
{
public abstract class TeamCharacterPresenterBase : Presenter<TeamCharacterViewBase, TeamCharacterModelBase>
{
	public abstract string CharacterId { get; }
	public abstract CharacterClass CharacterClassType { get; }
	public abstract float VisionViewAngel { get; }
	public abstract float VisionViewDistance { get; }
	public abstract Vector3 VisionDirection { get; }

	protected TeamCharacterPresenterBase(TeamCharacterViewBase view, TeamCharacterModelBase model) : base(view, model)
	{
	}

	public abstract void OnTargetChanged(Transform placeTransform);
	public abstract void OnTeamStay();
	public abstract void OnTeamMove();
	public abstract void OnAttackAnimationEnded();
	public abstract void OnAttackAnimationUpdated(float stateInfoNormalizedTime);
	public abstract void OnAttackAnimationStarted();
}
}