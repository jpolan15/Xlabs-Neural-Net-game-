using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Convergence.Gameplay.Hub;
using Convergence.Presentation.Hub;
using Convergence.Presentation.Ride;
using Convergence.Presentation.Ride.Level2;

namespace Convergence.Tests.EditMode.Ride
{
    /// <summary>The level hub (ADR-024): the catalog, the build order, the built scene and the scoreboard.</summary>
    [TestFixture]
    public class LevelHubTests
    {
        private const string HubPath = "Assets/Scenes/LevelHub.unity";
        private const string CatalogPath = "Assets/ScriptableObjects/LevelCatalog.asset";

        private static LevelCatalog Catalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            Assert.IsNotNull(catalog, CatalogPath + " missing: run Convergence/Build Level Hub");
            return catalog;
        }

        [Test]
        public void TheCatalog_OffersBothLevels_AndEveryPlayableSceneIsInTheBuild()
        {
            LevelCatalog catalog = Catalog();
            string[] scenes = catalog.Levels.Select(e => e.scene).ToArray();
            CollectionAssert.Contains(scenes, "NeuralRide", "Level 1 has a card");
            CollectionAssert.Contains(scenes, "WordMachine", "Level 2 has a card");
            string[] built = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path)).ToArray();
            foreach (LevelCatalog.Entry e in catalog.Levels)
            {
                Assert.IsFalse(string.IsNullOrEmpty(e.badge) || string.IsNullOrEmpty(e.title), "every card has a badge and a title");
                if (!string.IsNullOrEmpty(e.scene)) CollectionAssert.Contains(built, e.scene, e.badge + "'s scene must be an enabled build scene");
            }
        }

        [Test]
        public void TheGame_StartsInTheHub_ThenLevel1_ThenLevel2()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.GreaterOrEqual(scenes.Length, 3);
            Assert.AreEqual(HubPath, scenes[0].path, "the hub is build scene 0, so the headset starts there");
            Assert.AreEqual("Assets/Scenes/NeuralRide.unity", scenes[1].path);
            Assert.AreEqual("Assets/Scenes/WordMachine.unity", scenes[2].path);
            Assert.IsTrue(scenes[0].enabled && scenes[1].enabled && scenes[2].enabled);
        }

        [Test]
        public void TheHubScene_HasTheSeat_TheLasers_AndTheCardsView()
        {
            var scene = EditorSceneManager.OpenScene(HubPath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var view = roots.SelectMany(r => r.GetComponentsInChildren<LevelHubView>(true)).SingleOrDefault();
                Assert.IsNotNull(view, "one LevelHubView");
                var so = new SerializedObject(view);
                Assert.AreEqual(Catalog(), so.FindProperty("catalog").objectReferenceValue, "the view reads the catalog asset");
                Assert.IsNotNull(so.FindProperty("coverMaterial").objectReferenceValue, "the fade has its material");
                Assert.IsNotNull(view.GetComponent<Level2Kit>(), "the hub draws with Level 2's kit");

                string[] behaviours = roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).Where(b => b != null).Select(b => b.GetType().Name).ToArray();
                CollectionAssert.Contains(behaviours, "RideHandLasers", "the hand lasers pick a card");
                CollectionAssert.Contains(behaviours, "DesktopSeat", "the mouse picks a card at the desk");
                CollectionAssert.Contains(behaviours, "PodSeat", "the seat re-centres the eye");
                Camera head = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Single();
                Assert.AreEqual("MainCamera", head.tag);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void Level2_EndsInTheHub()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/WordMachine.unity", OpenSceneMode.Additive);
            try
            {
                var finale = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<FinaleView>(true)).Single();
                Assert.AreEqual(LevelHubNav.HubScene, new SerializedObject(finale).FindProperty("nextScene").stringValue);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void TheScoreboard_KeepsTheBest_AndCountsRides()
        {
            const string level = "__HubTestLevel";
            LevelProgress.Clear(level);
            try
            {
                Assert.IsFalse(LevelProgress.Get(level).Ridden);
                Assert.IsTrue(LevelProgress.RecordFinish(level, 9, 15, 1200), "a first ride is a record");
                Assert.IsFalse(LevelProgress.RecordFinish(level, 6, 15, 900), "a worse ride is not");
                LevelProgress.Record r = LevelProgress.Get(level);
                Assert.AreEqual(2, r.Finished);
                Assert.AreEqual(9, r.BestStars);
                Assert.AreEqual(15, r.MaxStars);
                Assert.AreEqual(1200, r.HighScore);
                Assert.AreEqual(6, r.LastStars);
                Assert.AreEqual(900, r.LastScore);
                Assert.IsTrue(LevelProgress.RecordFinish(level, 9, 15, 1300), "a higher score is a record");
            }
            finally
            {
                LevelProgress.Clear(level);
            }

            Assert.IsFalse(LevelProgress.Get(level).Ridden, "Clear forgets the level");
        }
    }
}
