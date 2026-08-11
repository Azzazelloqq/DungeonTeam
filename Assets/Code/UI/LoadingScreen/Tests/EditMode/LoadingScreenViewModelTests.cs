using Code.UIService;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Code.UI.LoadingScreen.Tests
{
    public sealed class LoadingScreenViewModelTests
    {
        private const string PrefabPath =
            "Assets/Content/UI/Windows/Main/LoadingScreen.prefab";

        [Test]
        public void ProductionLoadingScreen_IsReusableOverlay()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var view = prefab != null ? prefab.GetComponent<LoadingScreenView>() : null;

            Assert.That(prefab, Is.Not.Null, $"Loading screen is missing at '{PrefabPath}'.");
            Assert.That(view, Is.Not.Null);
            Assert.That(view.Settings.Group, Is.EqualTo(UIElementGroup.OverlayElement));
            Assert.That(view.Settings.HideBehavior, Is.EqualTo(UIElementHideBehavior.KeepInQueue));
        }

        [Test]
        public void SetStatusText_WithText_PublishesText()
        {
            using var viewModel = new LoadingScreenViewModel(new LoadingScreenModel());

            viewModel.SetStatusText("Preparing dungeon");

            Assert.That(viewModel.StatusText.Value, Is.EqualTo("Preparing dungeon"));
        }

        [Test]
        public void SetStatusText_WithWhitespace_RestoresDefaultText()
        {
            using var viewModel = new LoadingScreenViewModel(new LoadingScreenModel());

            viewModel.SetStatusText(" ");

            Assert.That(viewModel.StatusText.Value, Is.EqualTo("Loading..."));
        }
    }
}
