using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Code.Skills.CharacterSkill.Factory.SkillsPresenter
{
public interface ISkillsPresenterFactory
{
	public Task<T> GetAsync<T>(
		string characterId,
		string skillId,
		Transform parent,
		CancellationToken token);
}
}