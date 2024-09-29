using Code.DungeonTeam.MovementNavigator;
using TickHandler.UnityTickHandler;
using UnityEngine;

namespace Code
{
public class GameRoot : MonoBehaviour
{
	[SerializeField]
	private UnityDispatcherBehaviour _dispatcherBehaviour;

	[SerializeField]
	private MovementNavigatorView _view;
	
	private void Start()
	{
		
	}
}
}