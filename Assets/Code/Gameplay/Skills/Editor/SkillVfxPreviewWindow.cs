using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor;
using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.Gameplay.Skills.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonTeam.Gameplay.Skills.Editor
{
    internal sealed class SkillVfxPreviewWindow : EditorWindow
    {
        private const float HitDistance = 0.05f;
        private const float ScrollWheelSpeed = 24f;
        private const float TimelineTrackHeight = 19f;
        private const float TimelineLabelWidth = 210f;

        private static readonly Color AnimationColor = new(0.25f, 0.65f, 1f, 0.9f);
        private static readonly Color VfxColor = new(1f, 0.45f, 0.12f, 0.9f);

        [SerializeField] private string _selectedSkillId = "skill.fireball";
        [SerializeField] private int _selectedLevel = 1;
        [SerializeField] private string _sourceActorId = "actor.wizard";
        [SerializeField] private string _targetActorId = "actor.skeleton.warrior";
        [SerializeField] private float _targetDistance = 5f;
        [SerializeField] private float _targetSideOffset;
        [SerializeField] private float _targetHeightOffset;
        [SerializeField] private float _timeScale = 1f;
        [SerializeField] private bool _loop = true;
        [SerializeField] private float _loopDelay = 0.6f;
        [SerializeField] private SelectedCueKind _selectedCueKind = SelectedCueKind.Vfx;
        [SerializeField] private int _selectedAnimationCue;
        [SerializeField] private int _selectedVfxCue;
        [SerializeField] private bool _showAllCues;
        [SerializeField] private Vector2 _scroll;

        private readonly List<ScheduledCue> _scheduledVfx = new();
        private readonly List<ScheduledAnimation> _scheduledAnimations = new();
        private readonly List<ActiveVfx> _activeVfx = new();

        private SkillVfxLabCatalog _catalog;
        private SkillVfxLabSkill _skill;
        private SkillPresentationAsset _presentationDraft;
        private SerializedObject _draftSerialized;
        private SerializedProperty _animationCues;
        private SerializedProperty _vfxCues;
        private GameObject _sourceActor;
        private GameObject _targetActor;
        private Transform _sourceSlot;
        private Transform _targetSlot;
        private Transform _sourceAnchor;
        private Transform _targetAnchor;
        [SerializeField] private float _commitTime;
        [SerializeField] private float _recoveryDuration;
        [SerializeField] private float _projectileSpeed = 1f;
        [SerializeField] private Vector3 _projectileScale = Vector3.one;
        [SerializeField] private Vector3 _projectileRotationEuler;
        [SerializeField] private bool _hasChanges;

        private string _baselinePresentationJson;
        private float _baselineCommitTime;
        private float _baselineRecoveryDuration;
        private float _baselineProjectileSpeed;
        private Vector3 _baselineProjectileScale;
        private Vector3 _baselineProjectileRotationEuler;

        private SkillPresentationSequence _sequence;
        private GameObject _projectile;
        private Quaternion _projectileAuthoredRotation;
        private double _lastEditorTime;
        private float _elapsed;
        private float _completedAt;
        private float _minimumPreviewEndTime;
        private float _scrubTime;
        private bool _isPreviewing;
        private bool _isFullSequence;
        private bool _isScrubbing;
        private bool _projectileSpawned;
        private bool _impactTriggered;
        private int _timelineResizeControlId;
        private int _timelineUndoGroup = -1;
        private Hash128 _selectedSourceHash;
        private bool _hasSelectedSourceHash;
        private bool _externalReplayPending;

        [MenuItem("DungeonTeam/Skills/VFX Lab/Open")]
        private static void OpenFromMenu()
        {
            SkillVfxPreviewSceneBuilder.OpenSceneAndLab();
        }

        internal static void OpenAndBindScene()
        {
            var window = GetWindow<SkillVfxPreviewWindow>("Skill VFX Lab");
            window.minSize = new Vector2(640f, 520f);
            window.Initialize();
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += DrawSceneGuide;
            Undo.undoRedoPerformed += HandleUndoRedo;
            EditorApplication.projectChanged += HandleProjectChanged;
            EditorApplication.delayCall += Initialize;
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= Initialize;
            SceneView.duringSceneGui -= DrawSceneGuide;
            Undo.undoRedoPerformed -= HandleUndoRedo;
            EditorApplication.projectChanged -= HandleProjectChanged;
            EditorApplication.delayCall -= ReplayAfterExternalAssetChange;
            StopPreview();
            DestroyPreviewObject(ref _sourceActor);
            DestroyPreviewObject(ref _targetActor);
            DestroyDraft();
        }

        private void Initialize()
        {
            if (this == null)
                return;

            Run(() =>
            {
                _catalog = SkillVfxLabCatalog.Load();
                LoadSelectedSkill();
                BindSceneDefaults();
            });
        }

        private void OnGUI()
        {
            if (_catalog == null || _presentationDraft == null)
            {
                EditorGUILayout.HelpBox(
                    "Skill VFX Lab is initializing. If this remains visible, press Reload.",
                    MessageType.Info);
                if (GUILayout.Button("Reload"))
                    Initialize();
                return;
            }

            HandleMouseWheel();
            _scroll = EditorGUILayout.BeginScrollView(
                _scroll,
                false,
                true,
                GUILayout.ExpandHeight(true));
            DrawHeader();
            DrawSelection();
            DrawSceneSetup();
            DrawProductionSettings();
            DrawPreviewControls();
            DrawTimeline();
            DrawCueEditor();
            DrawValidationAndApply();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Skill VFX Lab", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(
                    _hasChanges ? "Modified (not applied)" : "Production assets loaded",
                    _hasChanges ? EditorStyles.boldLabel : EditorStyles.miniLabel,
                    GUILayout.Width(170f));
            }

            EditorGUILayout.HelpBox(
                "Edit Mode preview using production skill, presentation, projectile and actor " +
                "prefabs. Click any timeline row to select its cue. Drag a bar to change Delay; " +
                "drag its right edge to change Lifetime.",
                MessageType.Info);
        }

        private void DrawSelection()
        {
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            var skillLabels = BuildSkillLabels();
            var currentSkillIndex = FindSkillIndex(_selectedSkillId);
            var requestedSkillIndex = EditorGUILayout.Popup(
                "Skill",
                Mathf.Max(0, currentSkillIndex),
                skillLabels);
            if (requestedSkillIndex != currentSkillIndex && requestedSkillIndex >= 0)
                TryChangeSkill(requestedSkillIndex);

            var levels = _skill.Definition.Levels;
            var levelLabels = new string[levels.Count];
            var currentLevelIndex = 0;
            for (var index = 0; index < levels.Count; index++)
            {
                levelLabels[index] = $"Level {levels[index].Level}";
                if (levels[index].Level == _selectedLevel)
                    currentLevelIndex = index;
            }

            var requestedLevelIndex = EditorGUILayout.Popup(
                "Level",
                currentLevelIndex,
                levelLabels);
            if (requestedLevelIndex != currentLevelIndex)
                TryChangeLevel(levels[requestedLevelIndex].Level);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Presentation",
                    _skill.Presentation,
                    typeof(SkillPresentationAsset),
                    false);
                EditorGUILayout.ObjectField(
                    "Projectile",
                    _skill.ProjectilePrefab,
                    typeof(GameObject),
                    false);
            }
        }

        private void DrawSceneSetup()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actors and Test Layout", EditorStyles.boldLabel);
            if (_sourceSlot == null || _targetSlot == null)
            {
                EditorGUILayout.HelpBox(
                    "Open the dedicated preview scene to replace actors and run the sequence.",
                    MessageType.Warning);
                if (GUILayout.Button("Open VFX Lab Scene"))
                    SkillVfxPreviewSceneBuilder.OpenSceneAndLab();
                return;
            }

            var actorLabels = BuildActorLabels();
            var sourceIndex = FindActorIndex(_sourceActorId);
            var requestedSource = EditorGUILayout.Popup(
                "Source Actor",
                Mathf.Max(0, sourceIndex),
                actorLabels);
            if (requestedSource != sourceIndex && requestedSource >= 0)
            {
                _sourceActorId = _catalog.Actors[requestedSource].ActorId;
                SpawnPreviewActors();
            }

            var targetIndex = FindActorIndex(_targetActorId);
            var requestedTarget = EditorGUILayout.Popup(
                "Target Actor",
                Mathf.Max(0, targetIndex),
                actorLabels);
            if (requestedTarget != targetIndex && requestedTarget >= 0)
            {
                _targetActorId = _catalog.Actors[requestedTarget].ActorId;
                SpawnPreviewActors();
            }

            EditorGUI.BeginChangeCheck();
            var targetDistance = EditorGUILayout.Slider(
                "Distance",
                _targetDistance,
                0.5f,
                12f);
            var targetSideOffset = EditorGUILayout.Slider(
                "Side Offset",
                _targetSideOffset,
                -5f,
                5f);
            var targetHeightOffset = EditorGUILayout.Slider(
                "Height Offset",
                _targetHeightOffset,
                -2f,
                5f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Change VFX Lab test layout");
                _targetDistance = targetDistance;
                _targetSideOffset = targetSideOffset;
                _targetHeightOffset = targetHeightOffset;
                UpdateActorLayout();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Face Each Other"))
                    FaceActors();
                if (GUILayout.Button("Frame Actors"))
                    FrameActors();
                if (GUILayout.Button("Respawn Actors"))
                    SpawnPreviewActors();
            }
        }

        private void DrawProductionSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Production Settings (staged)", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var commitTime = Mathf.Max(
                0f,
                EditorGUILayout.FloatField("Commit Delay", _commitTime));
            var recoveryDuration = Mathf.Max(
                0f,
                EditorGUILayout.FloatField("Recovery Duration", _recoveryDuration));
            var projectileSpeed = _projectileSpeed;
            var projectileScale = _projectileScale;
            var projectileRotationEuler = _projectileRotationEuler;

            if (_skill.Definition is ProjectileDamageSkillDefinition)
            {
                projectileSpeed = Mathf.Max(
                    0.01f,
                    EditorGUILayout.FloatField("Projectile Speed", _projectileSpeed));
                projectileScale = EditorGUILayout.Vector3Field(
                    "Projectile Root Scale",
                    _projectileScale);
                projectileRotationEuler = EditorGUILayout.Vector3Field(
                    "Projectile Root Rotation",
                    _projectileRotationEuler);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Change VFX Lab production settings");
                _commitTime = commitTime;
                _recoveryDuration = recoveryDuration;
                _projectileSpeed = projectileSpeed;
                _projectileScale = projectileScale;
                _projectileRotationEuler = projectileRotationEuler;
                RefreshHasChanges();
                StopPreview();
            }

            if (_skill.ProjectilePrefab != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Select Projectile Prefab"))
                        Selection.activeObject = _skill.ProjectilePrefab;
                    if (GUILayout.Button("Open Projectile Prefab"))
                        AssetDatabase.OpenAsset(_skill.ProjectilePrefab);
                }
            }
        }

        private void DrawPreviewControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            _timeScale = EditorGUILayout.Slider("Time Scale", _timeScale, 0.05f, 2f);
            _loop = EditorGUILayout.Toggle("Loop Full Sequence", _loop);
            _loopDelay = Mathf.Max(
                0f,
                EditorGUILayout.FloatField("Loop Delay", _loopDelay));

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!CanPreview))
                {
                    if (GUILayout.Button("Play Full Sequence", GUILayout.Height(30f)))
                        Run(PlayFullSequence);
                }

                using (new EditorGUI.DisabledScope(!_isPreviewing && !_isScrubbing))
                {
                    if (GUILayout.Button("Stop / Clear", GUILayout.Height(30f)))
                        StopPreview();
                }
            }

            using (new EditorGUI.DisabledScope(!CanPreview))
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawPhaseButton(SkillPresentationPhase.Start);
                DrawPhaseButton(SkillPresentationPhase.Commit);
                DrawPhaseButton(SkillPresentationPhase.Impact);
                DrawPhaseButton(SkillPresentationPhase.Complete);
                DrawPhaseButton(SkillPresentationPhase.Cancel);
            }

            var duration = CalculateTimelineDuration();
            EditorGUI.BeginChangeCheck();
            _scrubTime = EditorGUILayout.Slider("Scrub", _scrubTime, 0f, duration);
            if (EditorGUI.EndChangeCheck() && CanPreview)
                Run(() => PreviewAtTime(_scrubTime));

            EditorGUILayout.LabelField(
                _isPreviewing
                    ? $"Playing: {_elapsed:0.00} s"
                    : _isScrubbing
                        ? $"Scrubbed: {_elapsed:0.00} s"
                        : "Stopped",
                EditorStyles.miniBoldLabel);
        }

        private void DrawPhaseButton(SkillPresentationPhase phase)
        {
            if (GUILayout.Button(phase.ToString()))
                Run(() => PlayPhase(phase));
        }

        private void DrawTimeline()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sequence Timeline", EditorStyles.boldLabel);
            _draftSerialized.Update();
            var duration = CalculateTimelineDuration();
            DrawTimelineRuler(duration);

            for (var index = 0; index < _animationCues.arraySize; index++)
                DrawTimelineRow(_animationCues.GetArrayElementAtIndex(index), index, false,
                    duration);
            for (var index = 0; index < _vfxCues.arraySize; index++)
                DrawTimelineRow(_vfxCues.GetArrayElementAtIndex(index), index, true, duration);

            DrawTimelineCursor(duration);
            if (_draftSerialized.ApplyModifiedPropertiesWithoutUndo())
            {
                RefreshHasChanges();
                StopPreview();
            }
        }

        private void DrawTimelineRuler(float duration)
        {
            var row = EditorGUILayout.GetControlRect(false, TimelineTrackHeight);
            var track = TimelineTrackRect(row);
            EditorGUI.DrawRect(track, new Color(0.12f, 0.12f, 0.12f, 0.5f));
            for (var index = 0; index <= 5; index++)
            {
                var normalized = index / 5f;
                var x = track.x + track.width * normalized;
                EditorGUI.DrawRect(new Rect(x, track.y, 1f, track.height),
                    new Color(1f, 1f, 1f, 0.18f));
                EditorGUI.LabelField(
                    new Rect(x - 18f, row.y, 44f, row.height),
                    $"{duration * normalized:0.##}",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawTimelineRow(
            SerializedProperty cue,
            int index,
            bool isVfx,
            float duration)
        {
            var phaseProperty = cue.FindPropertyRelative("_phase");
            var delayProperty = cue.FindPropertyRelative("_delay");
            var phase = (SkillPresentationPhase)phaseProperty.enumValueIndex;
            var phaseStart = GetPhaseStart(phase);
            var absoluteStart = phaseStart + delayProperty.floatValue;
            var lifetimeProperty = isVfx ? cue.FindPropertyRelative("_lifetime") : null;
            var lifetime = isVfx ? Mathf.Max(0.01f, lifetimeProperty.floatValue) : 0.02f;

            var row = EditorGUILayout.GetControlRect(false, TimelineTrackHeight);
            var label = isVfx
                ? GetVfxCueLabel(cue, index)
                : $"{phase} / {cue.FindPropertyRelative("_cue").enumDisplayNames[cue.FindPropertyRelative("_cue").enumValueIndex]}";
            EditorGUI.LabelField(
                new Rect(row.x, row.y, TimelineLabelWidth - 4f, row.height),
                label,
                EditorStyles.miniLabel);
            var track = TimelineTrackRect(row);
            EditorGUI.DrawRect(track, new Color(0.12f, 0.12f, 0.12f, 0.45f));

            var x = track.x + track.width * Mathf.Clamp01(absoluteStart / duration);
            var width = isVfx
                ? Mathf.Max(8f, track.width * lifetime / duration)
                : 5f;
            width = Mathf.Min(width, Mathf.Max(2f, track.xMax - x));
            var bar = new Rect(x, track.y + 2f, width, track.height - 4f);
            EditorGUI.DrawRect(bar, isVfx ? VfxColor : AnimationColor);
            if (IsSelectedCue(isVfx, index))
                Handles.DrawSolidRectangleWithOutline(bar, Color.clear, Color.white);

            HandleTimelineRowSelection(row, bar, isVfx, index);
            HandleTimelineDrag(
                bar,
                track,
                duration,
                delayProperty,
                lifetimeProperty,
                isVfx,
                index);
        }

        private void HandleTimelineDrag(
            Rect bar,
            Rect track,
            float duration,
            SerializedProperty delay,
            SerializedProperty lifetime,
            bool isVfx,
            int index)
        {
            var controlId = GUIUtility.GetControlID(
                (isVfx ? 10000 : 20000) + index,
                FocusType.Passive,
                bar);
            var current = Event.current;
            switch (current.GetTypeForControl(controlId))
            {
                case EventType.MouseDown when current.button == 0 && bar.Contains(current.mousePosition):
                    Undo.IncrementCurrentGroup();
                    _timelineUndoGroup = Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("Adjust VFX Lab timeline");
                    GUIUtility.hotControl = controlId;
                    _timelineResizeControlId = isVfx && current.mousePosition.x >= bar.xMax - 8f
                        ? controlId
                        : 0;
                    SelectCue(isVfx, index);
                    current.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == controlId:
                    var delta = current.delta.x / Mathf.Max(1f, track.width) * duration;
                    var resize = _timelineResizeControlId == controlId;
                    Undo.RecordObject(_presentationDraft, "Adjust VFX Lab timeline");
                    if (resize)
                        lifetime.floatValue = Mathf.Max(0.01f, lifetime.floatValue + delta);
                    else
                        delay.floatValue = Mathf.Max(0f, delay.floatValue + delta);
                    current.Use();
                    Repaint();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == controlId:
                    GUIUtility.hotControl = 0;
                    _timelineResizeControlId = 0;
                    if (_timelineUndoGroup >= 0)
                        Undo.CollapseUndoOperations(_timelineUndoGroup);
                    _timelineUndoGroup = -1;
                    current.Use();
                    break;
            }
        }

        private void HandleTimelineRowSelection(
            Rect row,
            Rect bar,
            bool isVfx,
            int index)
        {
            var current = Event.current;
            if (current.type != EventType.MouseDown || current.button != 0 ||
                !row.Contains(current.mousePosition) || bar.Contains(current.mousePosition))
                return;

            SelectCue(isVfx, index);
            current.Use();
        }

        private bool IsSelectedCue(bool isVfx, int index)
        {
            return isVfx
                ? _selectedCueKind == SelectedCueKind.Vfx && _selectedVfxCue == index
                : _selectedCueKind == SelectedCueKind.Animation &&
                  _selectedAnimationCue == index;
        }

        private void SelectCue(bool isVfx, int index)
        {
            _selectedCueKind = isVfx
                ? SelectedCueKind.Vfx
                : SelectedCueKind.Animation;
            if (isVfx)
                _selectedVfxCue = index;
            else
                _selectedAnimationCue = index;
            UpdateSelectedSourceHash();
            Repaint();
        }

        private void DrawTimelineCursor(float duration)
        {
            var row = EditorGUILayout.GetControlRect(false, 4f);
            var track = TimelineTrackRect(row);
            var x = track.x + track.width * Mathf.Clamp01(_scrubTime / duration);
            EditorGUI.DrawRect(new Rect(x, row.y - 4f, 2f, 8f), Color.white);
        }

        private void DrawCueEditor()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected Cue", EditorStyles.boldLabel);
            _draftSerialized.Update();
            EnsureSelectedCue();

            var selectedCue = GetSelectedCueProperty();
            if (selectedCue == null)
            {
                EditorGUILayout.HelpBox(
                    "This presentation has no animation or VFX cues.",
                    MessageType.Info);
                DrawAllCuesEditor();
                return;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                selectedCue,
                new GUIContent(GetSelectedCueLabel()),
                includeChildren: true);
            if (EditorGUI.EndChangeCheck())
            {
                _draftSerialized.ApplyModifiedProperties();
                RefreshHasChanges();
                UpdateSelectedSourceHash();
                StopPreview();
            }

            DrawSelectedSourceAsset();
            DrawSelectedCueActions();
            DrawAllCuesEditor();
        }

        private void DrawSelectedSourceAsset()
        {
            var source = GetSelectedSourceAsset();
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(
                        "Source Asset",
                        source,
                        typeof(UnityEngine.Object),
                        false);
                }
                using (new EditorGUI.DisabledScope(source == null))
                {
                    if (GUILayout.Button("Select", GUILayout.Width(70f)))
                    {
                        Selection.activeObject = source;
                        EditorGUIUtility.PingObject(source);
                    }
                    if (GUILayout.Button("Edit Source", GUILayout.Width(100f)))
                    {
                        Selection.activeObject = source;
                        EditorGUIUtility.PingObject(source);
                        AssetDatabase.OpenAsset(source);
                    }
                }
            }
        }

        private void DrawSelectedCueActions()
        {
            using (new EditorGUI.DisabledScope(!CanPreview))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Replay Selected", GUILayout.Height(28f)))
                    Run(ReplaySelectedCue);
                if (GUILayout.Button("Apply & Replay", GUILayout.Height(28f)))
                {
                    Run(() =>
                    {
                        if (_hasChanges)
                            ApplyChanges();
                        ReplaySelectedCue();
                    });
                }
                using (new EditorGUI.DisabledScope(GetSelectedSourceAsset() == null))
                {
                    if (GUILayout.Button("Save Source & Replay", GUILayout.Height(28f)))
                        Run(SaveSourceAndReplay);
                }
            }
        }

        private void DrawAllCuesEditor()
        {
            _showAllCues = EditorGUILayout.Foldout(
                _showAllCues,
                "All Cues (add, remove, reorder)",
                toggleOnLabelClick: true);
            if (!_showAllCues)
                return;

            _draftSerialized.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_animationCues, includeChildren: true);
            EditorGUILayout.PropertyField(_vfxCues, includeChildren: true);
            if (!EditorGUI.EndChangeCheck())
                return;

            _draftSerialized.ApplyModifiedProperties();
            EnsureSelectedCue();
            RefreshHasChanges();
            UpdateSelectedSourceHash();
            StopPreview();
        }

        private void DrawValidationAndApply()
        {
            EditorGUILayout.Space();
            var errors = new List<string>();
            CollectDraftErrors(errors);
            EditorGUILayout.HelpBox(
                errors.Count == 0 ? "Draft is valid." : string.Join("\n", errors),
                errors.Count == 0 ? MessageType.Info : MessageType.Error);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!_hasChanges || errors.Count > 0))
                {
                    if (GUILayout.Button("Apply to Production Assets", GUILayout.Height(30f)))
                        Run(ApplyChanges);
                }

                using (new EditorGUI.DisabledScope(!_hasChanges))
                {
                    if (GUILayout.Button("Revert Draft", GUILayout.Height(30f)))
                        Run(LoadSelectedSkill);
                }

                if (GUILayout.Button("Reload Catalog", GUILayout.Height(30f)))
                    Run(Initialize);
            }
        }

        private void TryChangeSkill(int newIndex)
        {
            if (!ResolvePendingChanges("switch skill"))
                return;

            _selectedSkillId = _catalog.Skills[newIndex].Definition.SkillId;
            _selectedLevel = _catalog.Skills[newIndex].Definition.Levels[0].Level;
            LoadSelectedSkill();
        }

        private void TryChangeLevel(int level)
        {
            if (!ResolvePendingChanges("switch level"))
                return;

            _selectedLevel = level;
            LoadSelectedSkill();
        }

        private bool ResolvePendingChanges(string action)
        {
            if (!_hasChanges)
                return true;

            var result = EditorUtility.DisplayDialogComplex(
                "Skill VFX Lab",
                $"Apply staged changes before you {action}?",
                "Apply",
                "Cancel",
                "Discard");
            if (result == 1)
                return false;
            if (result == 0)
                ApplyChanges();
            else
                LoadSelectedSkill();
            return true;
        }

        private void LoadSelectedSkill()
        {
            StopPreview();
            DestroyDraft();
            var index = FindSkillIndex(_selectedSkillId);
            if (index < 0)
            {
                index = 0;
                _selectedSkillId = _catalog.Skills[index].Definition.SkillId;
            }

            _skill = _catalog.Skills[index];
            if (!ContainsLevel(_skill.Definition, _selectedLevel))
                _selectedLevel = _skill.Definition.Levels[0].Level;

            _presentationDraft = Instantiate(_skill.Presentation);
            _presentationDraft.name = $"{_skill.Presentation.name} (VFX Lab Draft)";
            _presentationDraft.hideFlags =
                HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            _draftSerialized = new SerializedObject(_presentationDraft);
            _animationCues = _draftSerialized.FindProperty("_animationCues");
            _vfxCues = _draftSerialized.FindProperty("_vfxCues");

            var level = _skill.Definition.RequireLevel(_selectedLevel);
            _commitTime = level.UseTiming.CommitDelay;
            _recoveryDuration = level.UseTiming.RecoveryDuration;
            _projectileSpeed = level is ProjectileDamageSkillLevelDefinition projectileLevel
                ? projectileLevel.ProjectileSpeed
                : 1f;
            if (_skill.ProjectilePrefab != null)
            {
                _projectileScale = _skill.ProjectilePrefab.transform.localScale;
                _projectileRotationEuler = _skill.ProjectilePrefab.transform.localEulerAngles;
            }
            else
            {
                _projectileScale = Vector3.one;
                _projectileRotationEuler = Vector3.zero;
            }

            _selectedVfxCue = Mathf.Clamp(
                _selectedVfxCue,
                0,
                Mathf.Max(0, _vfxCues.arraySize - 1));
            _selectedAnimationCue = Mathf.Clamp(
                _selectedAnimationCue,
                0,
                Mathf.Max(0, _animationCues.arraySize - 1));
            EnsureSelectedCue();
            CaptureBaseline();
            UpdateSelectedSourceHash();
            Repaint();
        }

        private void HandleMouseWheel()
        {
            var current = Event.current;
            if (current.type != EventType.ScrollWheel ||
                !new Rect(Vector2.zero, position.size).Contains(current.mousePosition))
                return;

            if (current.shift)
                _scroll.x = Mathf.Max(0f, _scroll.x + current.delta.y * ScrollWheelSpeed);
            else
                _scroll.y = Mathf.Max(0f, _scroll.y + current.delta.y * ScrollWheelSpeed);
            current.Use();
            Repaint();
        }

        private void HandleUndoRedo()
        {
            if (_presentationDraft == null || _skill.Presentation == null)
                return;

            _draftSerialized.Update();
            EnsureSelectedCue();
            RefreshHasChanges();
            UpdateSelectedSourceHash();
            UpdateActorLayout();
            StopPreview();
            Repaint();
            SceneView.RepaintAll();
        }

        private void HandleProjectChanged()
        {
            var source = GetSelectedSourceAsset();
            var path = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrWhiteSpace(path))
                return;

            var hash = AssetDatabase.GetAssetDependencyHash(path);
            if (!_hasSelectedSourceHash)
            {
                _selectedSourceHash = hash;
                _hasSelectedSourceHash = true;
                return;
            }
            if (hash == _selectedSourceHash || _externalReplayPending)
                return;

            _selectedSourceHash = hash;
            _externalReplayPending = true;
            EditorApplication.delayCall += ReplayAfterExternalAssetChange;
        }

        private void ReplayAfterExternalAssetChange()
        {
            EditorApplication.delayCall -= ReplayAfterExternalAssetChange;
            _externalReplayPending = false;
            if (this == null || !CanPreview)
                return;

            var sourcePath = AssetDatabase.GetAssetPath(GetSelectedSourceAsset());
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.assetPath == sourcePath)
                StageUtility.GoToMainStage();
            Run(ReplaySelectedCue);
        }

        private void UpdateSelectedSourceHash()
        {
            var path = AssetDatabase.GetAssetPath(GetSelectedSourceAsset());
            _hasSelectedSourceHash = !string.IsNullOrWhiteSpace(path);
            _selectedSourceHash = _hasSelectedSourceHash
                ? AssetDatabase.GetAssetDependencyHash(path)
                : default;
        }

        private void CaptureBaseline()
        {
            _baselinePresentationJson = EditorJsonUtility.ToJson(_presentationDraft);
            _baselineCommitTime = _commitTime;
            _baselineRecoveryDuration = _recoveryDuration;
            _baselineProjectileSpeed = _projectileSpeed;
            _baselineProjectileScale = _projectileScale;
            _baselineProjectileRotationEuler = _projectileRotationEuler;
            _hasChanges = false;
        }

        private void RefreshHasChanges()
        {
            _hasChanges = EditorJsonUtility.ToJson(_presentationDraft) !=
                          _baselinePresentationJson ||
                          !Mathf.Approximately(_commitTime, _baselineCommitTime) ||
                          !Mathf.Approximately(
                              _recoveryDuration,
                              _baselineRecoveryDuration) ||
                          !Mathf.Approximately(
                              _projectileSpeed,
                              _baselineProjectileSpeed) ||
                          _projectileScale != _baselineProjectileScale ||
                          _projectileRotationEuler != _baselineProjectileRotationEuler;
        }

        private void ApplyChanges()
        {
            var errors = new List<string>();
            CollectDraftErrors(errors);
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", errors));

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"Apply {_skill.Definition.DisplayName} VFX Lab changes");
            Undo.RecordObject(_skill.Presentation, "Apply skill presentation draft");
            var sourcePresentation = new SerializedObject(_presentationDraft);
            var targetPresentation = new SerializedObject(_skill.Presentation);
            sourcePresentation.Update();
            targetPresentation.Update();
            targetPresentation.CopyFromSerializedProperty(
                sourcePresentation.FindProperty("_animationCues"));
            targetPresentation.CopyFromSerializedProperty(
                sourcePresentation.FindProperty("_vfxCues"));
            targetPresentation.ApplyModifiedProperties();
            EditorUtility.SetDirty(_skill.Presentation);

            _catalog.ApplyLevelTiming(
                _skill.Definition.SkillId,
                _selectedLevel,
                _commitTime,
                _recoveryDuration,
                _skill.Definition is ProjectileDamageSkillDefinition
                    ? _projectileSpeed
                    : null);

            if (_skill.ProjectilePrefab != null)
            {
                Undo.RecordObject(_skill.ProjectilePrefab.transform,
                    "Apply projectile root transform");
                _skill.ProjectilePrefab.transform.localScale = _projectileScale;
                _skill.ProjectilePrefab.transform.localEulerAngles = _projectileRotationEuler;
                EditorUtility.SetDirty(_skill.ProjectilePrefab);
                PrefabUtility.SavePrefabAsset(_skill.ProjectilePrefab);
            }

            Undo.CollapseUndoOperations(undoGroup);
            AssetDatabase.SaveAssets();
            _catalog = SkillVfxLabCatalog.Load();
            LoadSelectedSkill();
        }

        private void CollectDraftErrors(List<string> errors)
        {
            _presentationDraft.CollectValidationErrors(errors);
            CollectVfxScalingErrors(errors);
            if (!IsFinite(_commitTime) || _commitTime < 0f)
                errors.Add("Commit Delay must be a finite value greater than or equal to zero.");
            if (!IsFinite(_recoveryDuration) || _recoveryDuration < 0f)
                errors.Add("Recovery Duration must be a finite value greater than or equal to zero.");
            if (_skill.Definition is not ProjectileDamageSkillDefinition)
                return;

            if (!IsFinite(_projectileSpeed) || _projectileSpeed <= 0f)
                errors.Add("Projectile Speed must be a finite value greater than zero.");
            if (!IsPositiveFinite(_projectileScale))
                errors.Add("Projectile Root Scale components must be finite and greater than zero.");
            if (!IsFinite(_projectileRotationEuler))
                errors.Add("Projectile Root Rotation components must be finite.");
        }

        private void CollectVfxScalingErrors(List<string> errors)
        {
            var checkedPrefabs = new HashSet<GameObject>();
            _draftSerialized.Update();
            for (var cueIndex = 0; cueIndex < _vfxCues.arraySize; cueIndex++)
            {
                var prefab = _vfxCues.GetArrayElementAtIndex(cueIndex)
                    .FindPropertyRelative("_prefab").objectReferenceValue as GameObject;
                if (prefab == null || !checkedPrefabs.Add(prefab))
                    continue;

                foreach (var particleSystem in
                         prefab.GetComponentsInChildren<ParticleSystem>(true))
                {
                    if (particleSystem.main.scalingMode ==
                        ParticleSystemScalingMode.Hierarchy)
                        continue;
                    errors.Add(
                        $"VFX prefab '{prefab.name}' uses non-hierarchy scaling on " +
                        $"Particle System '{particleSystem.name}'. Scale Multiplier would " +
                        "not scale the complete effect.");
                }
            }
        }

        private static bool IsPositiveFinite(Vector3 value)
        {
            return IsFinite(value) && value.x > 0f && value.y > 0f && value.z > 0f;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void BindSceneDefaults()
        {
            _sourceSlot = FindSceneRoot(SkillVfxPreviewSceneBuilder.SourceSlotName);
            _targetSlot = FindSceneRoot(SkillVfxPreviewSceneBuilder.TargetSlotName);
            if (_sourceSlot != null && _targetSlot != null)
                SpawnPreviewActors();
            Repaint();
            SceneView.RepaintAll();
        }

        private void SpawnPreviewActors()
        {
            StopPreview();
            DestroyPreviewObject(ref _sourceActor);
            DestroyPreviewObject(ref _targetActor);
            if (_sourceSlot == null || _targetSlot == null)
                return;

            var source = _catalog.Actors[Mathf.Max(0, FindActorIndex(_sourceActorId))];
            var target = _catalog.Actors[Mathf.Max(0, FindActorIndex(_targetActorId))];
            _sourceActorId = source.ActorId;
            _targetActorId = target.ActorId;
            _sourceActor = InstantiateActor(source.Prefab, _sourceSlot, "Preview_Source");
            _targetActor = InstantiateActor(target.Prefab, _targetSlot, "Preview_Target");
            _sourceAnchor = FindChild(_sourceActor.transform, "SkillOriginAnchor") ??
                            _sourceActor.transform;
            _targetAnchor = FindChild(_targetActor.transform, "HitVfxAnchor") ??
                            _targetActor.transform;
            UpdateActorLayout();
            ResetActorPose(_sourceActor);
            ResetActorPose(_targetActor);
            UpdateSelectedSourceHash();
            SceneView.RepaintAll();
        }

        private static GameObject InstantiateActor(
            GameObject prefab,
            Transform slot,
            string name)
        {
            var instance = Instantiate(prefab);
            instance.name = name;
            MarkTransient(instance);
            SceneManager.MoveGameObjectToScene(instance, slot.gameObject.scene);
            instance.transform.SetParent(slot, false);
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.SetActive(true);
            return instance;
        }

        private void UpdateActorLayout()
        {
            if (_sourceSlot == null || _targetSlot == null)
                return;
            _sourceSlot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _targetSlot.position = new Vector3(
                _targetSideOffset,
                _targetHeightOffset,
                _targetDistance);
            FaceActors();
            SceneView.RepaintAll();
        }

        private void FaceActors()
        {
            if (_sourceSlot == null || _targetSlot == null)
                return;

            FaceTransform(_sourceSlot, _targetSlot.position);
            FaceTransform(_targetSlot, _sourceSlot.position);
        }

        private static void FaceTransform(Transform transform, Vector3 position)
        {
            var direction = position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void FrameActors()
        {
            if (_sourceActor == null || _targetActor == null)
                return;
            Selection.objects = new UnityEngine.Object[] { _sourceActor, _targetActor };
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        private void PlayFullSequence()
        {
            PreparePreview(true, false);
            SchedulePhase(SkillPresentationPhase.Start, 0f);
            SchedulePhase(SkillPresentationPhase.Commit, _commitTime);
            SchedulePhase(
                SkillPresentationPhase.Complete,
                _commitTime + _recoveryDuration);
            Tick(0f);
        }

        private void PlayPhase(SkillPresentationPhase phase)
        {
            PreparePreview(false, false);
            SchedulePhase(phase, 0f);
            _impactTriggered = true;
            Tick(0f);
        }

        private void ReplaySelectedCue()
        {
            EnsureSelectedCue();
            PreparePreview(false, false);
            _impactTriggered = true;
            if (_selectedCueKind == SelectedCueKind.Vfx)
            {
                var cue = _sequence.VfxCues[_selectedVfxCue];
                _scheduledVfx.Add(new ScheduledCue(cue, cue.Delay));
            }
            else
            {
                var cue = _sequence.AnimationCues[_selectedAnimationCue];
                _scheduledAnimations.Add(new ScheduledAnimation(cue, cue.Delay));
                var clip = ResolveSelectedAnimationClip();
                _minimumPreviewEndTime = cue.Delay + Mathf.Max(
                    0.25f,
                    clip != null ? clip.length : 1f);
            }
            Tick(0f);
        }

        private void SaveSourceAndReplay()
        {
            var sourcePath = AssetDatabase.GetAssetPath(GetSelectedSourceAsset());
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.assetPath == sourcePath)
            {
                PrefabUtility.SaveAsPrefabAsset(
                    prefabStage.prefabContentsRoot,
                    prefabStage.assetPath);
            }
            AssetDatabase.SaveAssets();
            if (prefabStage != null && prefabStage.assetPath == sourcePath)
                StageUtility.GoToMainStage();
            UpdateSelectedSourceHash();
            ReplaySelectedCue();
        }

        private void PreviewAtTime(float time)
        {
            PreparePreview(true, true);
            SchedulePhase(SkillPresentationPhase.Start, 0f);
            SchedulePhase(SkillPresentationPhase.Commit, _commitTime);
            SchedulePhase(
                SkillPresentationPhase.Complete,
                _commitTime + _recoveryDuration);
            Tick(0f);
            while (_elapsed + 0.0001f < time)
                Tick(Mathf.Min(1f / 30f, time - _elapsed));
            _isPreviewing = false;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void PreparePreview(bool fullSequence, bool scrubbing)
        {
            StopPreview();
            ValidateConfiguration();
            _sequence = _presentationDraft.CreateSequence();
            _elapsed = 0f;
            _completedAt = -1f;
            _minimumPreviewEndTime = 0f;
            _isFullSequence = fullSequence;
            _isScrubbing = scrubbing;
            _projectileSpawned = false;
            _impactTriggered = false;
            _isPreviewing = !scrubbing;
            _lastEditorTime = EditorApplication.timeSinceStartup;
            if (!scrubbing)
                EditorApplication.update += OnEditorUpdate;
        }

        private void ValidateConfiguration()
        {
            if (_presentationDraft == null)
                throw new InvalidOperationException("Presentation draft is not loaded.");
            if (_sourceAnchor == null || _targetAnchor == null)
            {
                throw new InvalidOperationException(
                    "Preview actors are not bound. Open or rebuild the VFX Lab scene.");
            }
        }

        private void StopPreview()
        {
            EditorApplication.update -= OnEditorUpdate;
            _isPreviewing = false;
            _isScrubbing = false;
            _scheduledVfx.Clear();
            _scheduledAnimations.Clear();
            DestroyPreviewObject(ref _projectile);
            for (var index = _activeVfx.Count - 1; index >= 0; index--)
                DestroyPreviewObject(_activeVfx[index].GameObject);
            _activeVfx.Clear();
            ResetActorPose(_sourceActor);
            ResetActorPose(_targetActor);
            Repaint();
            SceneView.RepaintAll();
        }

        private void OnEditorUpdate()
        {
            var now = EditorApplication.timeSinceStartup;
            var deltaTime = Mathf.Min(0.1f, (float)(now - _lastEditorTime)) * _timeScale;
            _lastEditorTime = now;
            Tick(deltaTime);
            _scrubTime = _elapsed;
            Repaint();
            SceneView.RepaintAll();
        }

        private void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            SpawnDueAnimations();
            SpawnDueVfx();

            if (_isFullSequence && !_projectileSpawned && _elapsed >= _commitTime)
            {
                _projectileSpawned = true;
                if (_skill.ProjectilePrefab != null)
                    SpawnProjectile();
                else
                    TriggerImpact();
            }

            if (_projectile != null)
                TickProjectile(deltaTime);
            SimulateParticles(deltaTime);
            TickAnimators(deltaTime);
            ExpireVfx();
            TryCompleteOrLoop();
        }

        private void SpawnDueAnimations()
        {
            for (var index = _scheduledAnimations.Count - 1; index >= 0; index--)
            {
                var scheduled = _scheduledAnimations[index];
                if (scheduled.StartTime > _elapsed)
                    continue;
                PlayAnimation(scheduled.Cue.Cue);
                _scheduledAnimations.RemoveAt(index);
            }
        }

        private void SpawnDueVfx()
        {
            for (var index = _scheduledVfx.Count - 1; index >= 0; index--)
            {
                var scheduled = _scheduledVfx[index];
                if (scheduled.StartTime > _elapsed)
                    continue;
                SpawnVfx(scheduled.Cue);
                _scheduledVfx.RemoveAt(index);
            }
        }

        private void PlayAnimation(ActorSkillAnimationCue cue)
        {
            var trigger = cue switch
            {
                ActorSkillAnimationCue.Attack => ActorView.AttackParameter,
                ActorSkillAnimationCue.Cast => ActorView.CastParameter,
                _ => throw new ArgumentOutOfRangeException(nameof(cue), cue, null)
            };
            foreach (var animator in _sourceActor.GetComponentsInChildren<Animator>(true))
            {
                if (animator.runtimeAnimatorController == null)
                    continue;
                animator.SetTrigger(trigger);
                animator.Update(0.001f);
            }
        }

        private void SpawnVfx(SkillVfxCue cue)
        {
            var anchor = ResolveAnchor(cue.Anchor);
            GameObject instance;
            if (cue.FollowAnchor)
            {
                instance = Instantiate(cue.Prefab, anchor, false);
                instance.transform.localPosition = cue.PositionOffset;
                instance.transform.localRotation = cue.Prefab.transform.localRotation *
                                                   Quaternion.Euler(cue.RotationOffsetEuler);
            }
            else
            {
                instance = Instantiate(
                    cue.Prefab,
                    cue.Anchor == SkillVfxAnchor.ImpactPosition
                        ? anchor.position + cue.PositionOffset
                        : anchor.TransformPoint(cue.PositionOffset),
                    cue.Prefab.transform.rotation * Quaternion.Euler(cue.RotationOffsetEuler));
                SceneManager.MoveGameObjectToScene(instance, _sourceAnchor.gameObject.scene);
            }

            instance.name = $"Preview_{cue.Phase}_{cue.Prefab.name}";
            instance.transform.localScale = Vector3.Scale(
                instance.transform.localScale,
                Vector3.one * cue.ScaleMultiplier);
            MarkTransient(instance);
            instance.SetActive(true);
            foreach (var particleSystem in instance.GetComponentsInChildren<ParticleSystem>(true))
                particleSystem.Play(withChildren: false);
            SimulateParticles(instance, 0.001f);
            _activeVfx.Add(new ActiveVfx(instance, _elapsed + cue.Lifetime));
        }

        private void SpawnProjectile()
        {
            _projectileAuthoredRotation = Quaternion.Euler(_projectileRotationEuler);
            var difference = _targetAnchor.position - _sourceAnchor.position;
            var rotation = difference.sqrMagnitude > Mathf.Epsilon
                ? Quaternion.LookRotation(difference.normalized) * _projectileAuthoredRotation
                : _sourceAnchor.rotation * _projectileAuthoredRotation;
            _projectile = Instantiate(_skill.ProjectilePrefab, _sourceAnchor.position, rotation);
            _projectile.transform.localScale = _projectileScale;
            SceneManager.MoveGameObjectToScene(_projectile, _sourceAnchor.gameObject.scene);
            _projectile.name = $"Preview_Projectile_{_skill.ProjectilePrefab.name}";
            MarkTransient(_projectile);
            _projectile.SetActive(true);
            foreach (var particleSystem in _projectile.GetComponentsInChildren<ParticleSystem>(true))
                particleSystem.Play(withChildren: false);
            SimulateParticles(_projectile, 0.001f);
        }

        private void TickProjectile(float deltaTime)
        {
            var difference = _targetAnchor.position - _projectile.transform.position;
            if (difference.sqrMagnitude > Mathf.Epsilon)
            {
                _projectile.transform.rotation = Quaternion.LookRotation(difference.normalized) *
                                                 _projectileAuthoredRotation;
            }

            var travelDistance = _projectileSpeed * deltaTime;
            if (difference.sqrMagnitude <=
                (travelDistance + HitDistance) * (travelDistance + HitDistance))
            {
                _projectile.transform.position = _targetAnchor.position;
                DestroyPreviewObject(ref _projectile);
                TriggerImpact();
                return;
            }

            _projectile.transform.position += difference.normalized * travelDistance;
        }

        private void TriggerImpact()
        {
            if (_impactTriggered)
                return;
            _impactTriggered = true;
            SchedulePhase(SkillPresentationPhase.Impact, _elapsed);
            SpawnDueAnimations();
            SpawnDueVfx();
        }

        private void SchedulePhase(SkillPresentationPhase phase, float phaseStart)
        {
            for (var index = 0; index < _sequence.AnimationCues.Count; index++)
            {
                var cue = _sequence.AnimationCues[index];
                if (cue.Phase == phase)
                    _scheduledAnimations.Add(
                        new ScheduledAnimation(cue, phaseStart + cue.Delay));
            }

            for (var index = 0; index < _sequence.VfxCues.Count; index++)
            {
                var cue = _sequence.VfxCues[index];
                if (cue.Phase == phase)
                    _scheduledVfx.Add(new ScheduledCue(cue, phaseStart + cue.Delay));
            }
        }

        private void ExpireVfx()
        {
            for (var index = _activeVfx.Count - 1; index >= 0; index--)
            {
                if (_activeVfx[index].EndTime > _elapsed)
                    continue;
                DestroyPreviewObject(_activeVfx[index].GameObject);
                _activeVfx.RemoveAt(index);
            }
        }

        private void SimulateParticles(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;
            if (_projectile != null)
                SimulateParticles(_projectile, deltaTime);
            for (var index = 0; index < _activeVfx.Count; index++)
                SimulateParticles(_activeVfx[index].GameObject, deltaTime);
        }

        private static void SimulateParticles(GameObject instance, float deltaTime)
        {
            foreach (var particleSystem in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                particleSystem.Simulate(
                    deltaTime,
                    withChildren: false,
                    restart: false,
                    fixedTimeStep: false);
            }
        }

        private void TickAnimators(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;
            TickAnimators(_sourceActor, deltaTime);
            TickAnimators(_targetActor, deltaTime);
        }

        private static void TickAnimators(GameObject actor, float deltaTime)
        {
            if (actor == null)
                return;
            foreach (var animator in actor.GetComponentsInChildren<Animator>(true))
            {
                if (animator.runtimeAnimatorController != null)
                    animator.Update(deltaTime);
            }
        }

        private void TryCompleteOrLoop()
        {
            if (_isScrubbing || _scheduledVfx.Count > 0 || _scheduledAnimations.Count > 0 ||
                _activeVfx.Count > 0 || _projectile != null ||
                (_isFullSequence && !_impactTriggered) ||
                _elapsed < _minimumPreviewEndTime)
                return;

            if (_completedAt < 0f)
                _completedAt = _elapsed;
            if (!_loop || !_isFullSequence)
            {
                StopPreview();
                return;
            }
            if (_elapsed >= _completedAt + _loopDelay)
                PlayFullSequence();
        }

        private Transform ResolveAnchor(SkillVfxAnchor anchor)
        {
            return anchor switch
            {
                SkillVfxAnchor.SourceOrigin => _sourceAnchor,
                SkillVfxAnchor.TargetHit => _targetAnchor,
                SkillVfxAnchor.ImpactPosition => _targetAnchor,
                _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null)
            };
        }

        private float CalculateTimelineDuration()
        {
            var max = Mathf.Max(1f, _commitTime + _recoveryDuration, GetPhaseStart(
                SkillPresentationPhase.Impact));
            max = Mathf.Max(max, FindMaxEnd(_animationCues, false));
            max = Mathf.Max(max, FindMaxEnd(_vfxCues, true));
            return max + 0.15f;
        }

        private float FindMaxEnd(SerializedProperty cues, bool isVfx)
        {
            var max = 0f;
            for (var index = 0; index < cues.arraySize; index++)
            {
                var cue = cues.GetArrayElementAtIndex(index);
                var phase = (SkillPresentationPhase)cue.FindPropertyRelative("_phase")
                    .enumValueIndex;
                var end = GetPhaseStart(phase) + cue.FindPropertyRelative("_delay").floatValue;
                if (isVfx)
                    end += cue.FindPropertyRelative("_lifetime").floatValue;
                max = Mathf.Max(max, end);
            }
            return max;
        }

        private float GetPhaseStart(SkillPresentationPhase phase)
        {
            return phase switch
            {
                SkillPresentationPhase.Start => 0f,
                SkillPresentationPhase.Commit => _commitTime,
                SkillPresentationPhase.Impact => _commitTime +
                    (_skill.ProjectilePrefab != null
                        ? CurrentTravelDistance / Mathf.Max(0.01f, _projectileSpeed)
                        : 0f),
                SkillPresentationPhase.Complete => _commitTime + _recoveryDuration,
                SkillPresentationPhase.Cancel => 0f,
                _ => 0f
            };
        }

        private float CurrentTravelDistance => _sourceAnchor != null && _targetAnchor != null
            ? Vector3.Distance(_sourceAnchor.position, _targetAnchor.position)
            : _targetDistance;

        private bool CanPreview => _presentationDraft != null &&
                                   _sourceAnchor != null &&
                                   _targetAnchor != null;

        private string[] BuildSkillLabels()
        {
            var labels = new string[_catalog.Skills.Count];
            for (var index = 0; index < labels.Length; index++)
                labels[index] = _catalog.Skills[index].Label;
            return labels;
        }

        private string[] BuildActorLabels()
        {
            var labels = new string[_catalog.Actors.Count];
            for (var index = 0; index < labels.Length; index++)
                labels[index] = _catalog.Actors[index].Label;
            return labels;
        }

        private int FindSkillIndex(string skillId)
        {
            for (var index = 0; index < _catalog.Skills.Count; index++)
            {
                if (_catalog.Skills[index].Definition.SkillId == skillId)
                    return index;
            }
            return -1;
        }

        private int FindActorIndex(string actorId)
        {
            for (var index = 0; index < _catalog.Actors.Count; index++)
            {
                if (_catalog.Actors[index].ActorId == actorId)
                    return index;
            }
            return -1;
        }

        private static bool ContainsLevel(SkillDefinition skill, int level)
        {
            for (var index = 0; index < skill.Levels.Count; index++)
            {
                if (skill.Levels[index].Level == level)
                    return true;
            }
            return false;
        }

        private void EnsureSelectedCue()
        {
            if (_animationCues == null || _vfxCues == null)
                return;

            _selectedAnimationCue = Mathf.Clamp(
                _selectedAnimationCue,
                0,
                Mathf.Max(0, _animationCues.arraySize - 1));
            _selectedVfxCue = Mathf.Clamp(
                _selectedVfxCue,
                0,
                Mathf.Max(0, _vfxCues.arraySize - 1));
            if (_selectedCueKind == SelectedCueKind.Vfx && _vfxCues.arraySize == 0)
                _selectedCueKind = SelectedCueKind.Animation;
            else if (_selectedCueKind == SelectedCueKind.Animation &&
                     _animationCues.arraySize == 0)
                _selectedCueKind = SelectedCueKind.Vfx;
        }

        private SerializedProperty GetSelectedCueProperty()
        {
            EnsureSelectedCue();
            if (_animationCues == null || _vfxCues == null)
                return null;
            if (_selectedCueKind == SelectedCueKind.Vfx)
            {
                return _vfxCues.arraySize > 0
                    ? _vfxCues.GetArrayElementAtIndex(_selectedVfxCue)
                    : null;
            }

            return _animationCues.arraySize > 0
                ? _animationCues.GetArrayElementAtIndex(_selectedAnimationCue)
                : null;
        }

        private string GetSelectedCueLabel()
        {
            return _selectedCueKind == SelectedCueKind.Vfx
                ? $"VFX #{_selectedVfxCue + 1}"
                : $"Animation #{_selectedAnimationCue + 1}";
        }

        private UnityEngine.Object GetSelectedSourceAsset()
        {
            var selectedCue = GetSelectedCueProperty();
            if (selectedCue == null)
                return null;
            if (_selectedCueKind == SelectedCueKind.Vfx)
            {
                return selectedCue.FindPropertyRelative("_prefab")
                    .objectReferenceValue;
            }

            return ResolveSelectedAnimationClip();
        }

        private AnimationClip ResolveSelectedAnimationClip()
        {
            if (_sourceActor == null || _animationCues == null ||
                _animationCues.arraySize == 0)
                return null;

            var cueProperty = _animationCues.GetArrayElementAtIndex(
                Mathf.Clamp(_selectedAnimationCue, 0, _animationCues.arraySize - 1));
            var cue = (ActorSkillAnimationCue)cueProperty
                .FindPropertyRelative("_cue").enumValueIndex;
            foreach (var animator in _sourceActor.GetComponentsInChildren<Animator>(true))
            {
                var controller = animator.runtimeAnimatorController;
                if (controller == null)
                    continue;
                if (controller is AnimatorOverrideController overrideController)
                {
                    var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                    overrideController.GetOverrides(overrides);
                    for (var index = 0; index < overrides.Count; index++)
                    {
                        var original = overrides[index].Key;
                        if (original == null || original.name.IndexOf(
                                "Attack",
                                StringComparison.OrdinalIgnoreCase) < 0)
                            continue;
                        return overrides[index].Value != null
                            ? overrides[index].Value
                            : original;
                    }
                }

                var clips = controller.animationClips;
                for (var index = 0; index < clips.Length; index++)
                {
                    if (clips[index] != null && clips[index].name.IndexOf(
                            cue.ToString(),
                            StringComparison.OrdinalIgnoreCase) >= 0)
                        return clips[index];
                }
                for (var index = 0; index < clips.Length; index++)
                {
                    if (clips[index] != null && clips[index].name.IndexOf(
                            "Attack",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                        return clips[index];
                }
            }
            return null;
        }

        private static string GetVfxCueLabel(SerializedProperty cue, int index)
        {
            var phase = cue.FindPropertyRelative("_phase");
            var prefab = cue.FindPropertyRelative("_prefab").objectReferenceValue;
            return $"{phase.enumDisplayNames[phase.enumValueIndex]} / " +
                   (prefab != null ? prefab.name : $"VFX #{index + 1} (missing)");
        }

        private static Rect TimelineTrackRect(Rect row)
        {
            return new Rect(
                row.x + TimelineLabelWidth,
                row.y,
                Mathf.Max(1f, row.width - TimelineLabelWidth),
                row.height);
        }

        private void DrawSceneGuide(SceneView sceneView)
        {
            if (_sourceAnchor == null || _targetAnchor == null)
                return;
            Handles.color = new Color(1f, 0.75f, 0.1f, 0.9f);
            Handles.SphereHandleCap(0, _sourceAnchor.position, Quaternion.identity, 0.16f,
                EventType.Repaint);
            Handles.SphereHandleCap(0, _targetAnchor.position, Quaternion.identity, 0.16f,
                EventType.Repaint);
            Handles.DrawLine(_sourceAnchor.position, _targetAnchor.position, 2f);
            Handles.Label(
                Vector3.Lerp(_sourceAnchor.position, _targetAnchor.position, 0.5f) +
                Vector3.up * 0.2f,
                $"{CurrentTravelDistance:0.00} m");
            Handles.Label(_sourceAnchor.position + Vector3.up * 0.2f, "SourceOrigin");
            Handles.Label(_targetAnchor.position + Vector3.up * 0.2f, "TargetHit / Impact");
        }

        private static Transform FindSceneRoot(string name)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == name)
                    return root.transform;
            }
            return null;
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child;
            }
            return null;
        }

        private static void ResetActorPose(GameObject actor)
        {
            if (actor == null)
                return;
            foreach (var animator in actor.GetComponentsInChildren<Animator>(true))
            {
                if (animator.runtimeAnimatorController == null)
                    continue;
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static void MarkTransient(GameObject instance)
        {
            foreach (var child in instance.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.hideFlags = HideFlags.DontSaveInEditor |
                                             HideFlags.DontSaveInBuild;
            }
        }

        private void DestroyDraft()
        {
            if (_presentationDraft != null)
                DestroyImmediate(_presentationDraft);
            _presentationDraft = null;
            _draftSerialized = null;
            _animationCues = null;
            _vfxCues = null;
        }

        private static void DestroyPreviewObject(ref GameObject instance)
        {
            DestroyPreviewObject(instance);
            instance = null;
        }

        private static void DestroyPreviewObject(GameObject instance)
        {
            if (instance != null)
                DestroyImmediate(instance);
        }

        private void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Skill VFX Lab", exception.Message, "OK");
            }
        }

        private enum SelectedCueKind
        {
            Animation = 0,
            Vfx = 1
        }

        private readonly struct ScheduledCue
        {
            public ScheduledCue(SkillVfxCue cue, float startTime)
            {
                Cue = cue;
                StartTime = startTime;
            }

            public SkillVfxCue Cue { get; }
            public float StartTime { get; }
        }

        private readonly struct ScheduledAnimation
        {
            public ScheduledAnimation(SkillActorAnimationCue cue, float startTime)
            {
                Cue = cue;
                StartTime = startTime;
            }

            public SkillActorAnimationCue Cue { get; }
            public float StartTime { get; }
        }

        private readonly struct ActiveVfx
        {
            public ActiveVfx(GameObject gameObject, float endTime)
            {
                GameObject = gameObject;
                EndTime = endTime;
            }

            public GameObject GameObject { get; }
            public float EndTime { get; }
        }
    }
}
