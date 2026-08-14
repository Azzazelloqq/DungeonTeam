using DungeonTeam.Gameplay.DungeonRun.Application;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonTeam.DeveloperTools
{
    [AddComponentMenu("")]
    public sealed class DeveloperRunConsoleView : MonoBehaviour
    {
        private const float WindowWidth = 620f;
        private const int WindowId = 814701;
        private readonly string[] _roles = { "None", "Leader", "Companion" };
        private DeveloperRunConsoleController _controller;
        private Rect _windowRect = new Rect(20f, 20f, WindowWidth, 600f);
        private Vector2 _scroll;
        private string _seedText;
        private bool _visible;

        public void Initialize(DeveloperRunConsoleController controller)
        {
            _controller = controller;
            _seedText = controller != null ? controller.Seed.ToString() : string.Empty;
        }

        private void Update()
        {
            if (Keyboard.current?.f10Key.wasPressedThisFrame == true)
            {
                _visible = !_visible;
            }
        }

        private void OnGUI()
        {
            if (_controller == null)
            {
                return;
            }

            if (!_visible)
            {
                if (GUI.Button(new Rect(8f, 8f, 64f, 32f), "DEV"))
                {
                    _visible = true;
                }

                return;
            }

            _windowRect = GUILayout.Window(
                WindowId,
                _windowRect,
                DrawWindow,
                "Dungeon Run Console (F10)");
        }

        private void DrawWindow(int windowId)
        {
            _scroll = GUILayout.BeginScrollView(_scroll);
            DrawPresets();
            DrawSeed();
            GUILayout.Space(8f);
            GUILayout.Label("Team");
            for (var index = 0; index < _controller.Members.Count; index++)
            {
                DrawActor(_controller.Members[index]);
            }

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Run"))
            {
                if (_controller.TrySetSeed(_seedText))
                {
                    _controller.Run();
                }
            }

            if (GUILayout.Button("Stop"))
            {
                _controller.Stop();
            }

            if (GUILayout.Button("Reset"))
            {
                _controller.Reset();
                _seedText = _controller.Seed.ToString();
            }

            if (GUILayout.Button("Hide"))
            {
                _visible = false;
            }

            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(_controller.ErrorMessage))
            {
                GUILayout.Label(_controller.ErrorMessage);
            }

            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, WindowWidth, 24f));
        }

        private void DrawPresets()
        {
            GUILayout.Label("Scenario preset");
            var labels = new string[_controller.Presets.Count];
            var selectedIndex = 0;
            for (var index = 0; index < labels.Length; index++)
            {
                var preset = _controller.Presets[index];
                labels[index] = preset.DisplayName;
                if (preset.PresetId == _controller.SelectedPresetId)
                {
                    selectedIndex = index;
                }
            }

            var nextIndex = GUILayout.SelectionGrid(selectedIndex, labels, 2);
            if (nextIndex != selectedIndex)
            {
                _controller.SelectPreset(_controller.Presets[nextIndex].PresetId);
                _seedText = _controller.Seed.ToString();
            }
        }

        private void DrawSeed()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Seed", GUILayout.Width(80f));
            _seedText = GUILayout.TextField(_seedText);
            if (GUILayout.Button("Randomize", GUILayout.Width(100f)))
            {
                _controller.RandomizeSeed();
                _seedText = _controller.Seed.ToString();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawActor(DungeonRunTeamMemberOption member)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"{member.DisplayName}  ({member.ActorId})");
            GUILayout.BeginHorizontal();

            var currentRole = !_controller.IsActorIncluded(member.ActorId)
                ? 0
                : member.ActorId == _controller.LeaderActorId ? 1 : 2;
            var nextRole = GUILayout.SelectionGrid(currentRole, _roles, 3);
            if (nextRole != currentRole)
            {
                if (nextRole == 0)
                {
                    _controller.SetActorIncluded(member.ActorId, false);
                }
                else if (nextRole == 1)
                {
                    _controller.SetLeader(member.ActorId);
                }
                else
                {
                    _controller.SetActorIncluded(member.ActorId, true);
                }
            }

            DrawLevel(member);
            DrawLoadout(member);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawLevel(DungeonRunTeamMemberOption member)
        {
            var labels = new string[member.AvailableLevels.Count];
            var selected = 0;
            for (var index = 0; index < labels.Length; index++)
            {
                labels[index] = $"Lv {member.AvailableLevels[index]}";
                if (member.AvailableLevels[index] == _controller.GetActorLevel(member.ActorId))
                {
                    selected = index;
                }
            }

            var next = GUILayout.SelectionGrid(selected, labels, labels.Length, GUILayout.Width(100f));
            if (next != selected)
            {
                _controller.SetActorLevel(member.ActorId, member.AvailableLevels[next]);
            }
        }

        private void DrawLoadout(DungeonRunTeamMemberOption member)
        {
            var labels = new string[member.AvailableLoadoutIds.Count];
            var selected = 0;
            for (var index = 0; index < labels.Length; index++)
            {
                labels[index] = member.AvailableLoadoutIds[index];
                if (labels[index] == _controller.GetActorLoadout(member.ActorId))
                {
                    selected = index;
                }
            }

            var next = GUILayout.SelectionGrid(
                selected,
                labels,
                labels.Length,
                GUILayout.MinWidth(180f));
            if (next != selected)
            {
                _controller.SetActorLoadout(member.ActorId, member.AvailableLoadoutIds[next]);
            }
        }

    }
}
