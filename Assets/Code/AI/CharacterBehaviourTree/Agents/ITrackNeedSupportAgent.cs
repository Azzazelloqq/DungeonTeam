﻿using Code.AI.CharacterBehaviourTree.Agents.Base;

namespace Code.AI.CharacterBehaviourTree.Agents
{
public interface ITrackNeedSupportAgent : IBehaviourTreeAgent
{
    public bool TryFindTargetToHeal();
}
}