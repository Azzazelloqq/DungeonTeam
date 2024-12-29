using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Skills.CharacterSkill.SkillPresenters.Base;
using MVP;
using UnityEngine;

namespace Code.Skills.CharacterSkill.Factory.SkillsPresenter
{
/// <summary>
/// Factory for retrieving skill presenters.
/// Provides both asynchronous and synchronous approaches to obtain a presenter.
/// </summary>
public interface ISkillsPresenterFactory
{
	/// <summary>
	/// Asynchronously retrieves a skill presenter by character and skill identifiers.
	/// </summary>
	/// <param name="characterId">The identifier of the character associated with the skill.</param>
	/// <param name="skillId">The identifier of the skill for which the presenter is requested.</param>
	/// <param name="parent">The parent transform to which the presenter's view will be attached.</param>
	/// <param name="token">A cancellation token that can be used to cancel the asynchronous operation.</param>
	/// <returns>
	/// Returns a task whose result is an instance of <see cref="SkillPresenterBase"/>.
	/// If no matching implementation is found, this returns <c>null</c>.
	/// </returns>
	public Task<SkillPresenterBase> GetAsync(
		string characterId,
		string skillId,
		Transform parent,
		CancellationToken token);

	/// <summary>
	/// Synchronously retrieves a skill presenter by character and skill identifiers,
	/// using a callback to return the result.
	/// </summary>
	/// <remarks>
	/// The result is provided via the <paramref name="onPresenterCreated"/> delegate.
	/// If no matching implementation is found, the delegate will receive <c>null</c>.
	/// </remarks>
	/// <param name="characterId">The identifier of the character associated with the skill.</param>
	/// <param name="skillId">The identifier of the skill for which the presenter is requested.</param>
	/// <param name="parent">The parent transform to which the presenter's view will be attached.</param>
	/// <param name="onPresenterCreated">A callback invoked once the presenter has been created.</param>
	/// <param name="token">A cancellation token that can be used to cancel the operation.</param>
	public void Get(
		string characterId,
		string skillId,
		Transform parent,
		Action<SkillPresenterBase> onPresenterCreated,
		CancellationToken token);
}
}