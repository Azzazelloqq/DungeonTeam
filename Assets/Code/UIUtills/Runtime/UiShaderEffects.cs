using UnityEngine;
using UnityEngine.UI;

namespace Code.UIUtills.Runtime
{
    [AddComponentMenu("UI/Shader Effects")]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(Graphic), typeof(Mask))]
    public sealed class UiShaderEffects : BaseMeshEffect
    {
        [SerializeField]
        private bool _maskChildren;

        [SerializeField]
        private bool _showMaskGraphic = true;

        private Mask _mask;

        public bool MaskChildren
        {
            get => _maskChildren;
            set
            {
                if (_maskChildren == value)
                    return;

                _maskChildren = value;
                ApplyMaskSettings();
            }
        }

        public bool ShowMaskGraphic
        {
            get => _showMaskGraphic;
            set
            {
                if (_showMaskGraphic == value)
                    return;

                _showMaskGraphic = value;
                ApplyMaskSettings();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ApplyMaskSettings();
        }

        protected override void OnDisable()
        {
            if (TryGetMask(out var mask))
                mask.enabled = false;

            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            ApplyMaskSettings();
        }
#endif

        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!IsActive() || vertexHelper.currentVertCount == 0)
                return;

            var vertex = new UIVertex();
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            for (var index = 0; index < vertexHelper.currentVertCount; index++)
            {
                vertexHelper.PopulateUIVertex(ref vertex, index);
                var position = (Vector2)vertex.position;
                min = Vector2.Min(min, position);
                max = Vector2.Max(max, position);
            }

            var size = max - min;
            var inverseWidth = size.x > Mathf.Epsilon ? 1f / size.x : 0f;
            var inverseHeight = size.y > Mathf.Epsilon ? 1f / size.y : 0f;

            for (var index = 0; index < vertexHelper.currentVertCount; index++)
            {
                vertexHelper.PopulateUIVertex(ref vertex, index);

                var effectUv = new Vector2(
                    size.x > Mathf.Epsilon ? (vertex.position.x - min.x) * inverseWidth : 0.5f,
                    size.y > Mathf.Epsilon ? (vertex.position.y - min.y) * inverseHeight : 0.5f);

                var textureUv = vertex.uv0;
                textureUv.z = effectUv.x;
                textureUv.w = effectUv.y;
                vertex.uv0 = textureUv;

                vertexHelper.SetUIVertex(vertex, index);
            }
        }

        private void ApplyMaskSettings()
        {
            if (!TryGetMask(out var mask))
                return;

            mask.showMaskGraphic = _showMaskGraphic;
            mask.enabled = isActiveAndEnabled && _maskChildren;
        }

        private bool TryGetMask(out Mask mask)
        {
            if (_mask == null)
                _mask = GetComponent<Mask>();

            mask = _mask;
            return mask != null;
        }
    }
}
