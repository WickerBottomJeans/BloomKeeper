using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class StripeSunburstPlayModeTests
{
    private GameObject managerObject;
    private GameObject petalViewPrefabObject;

    [UnityTest]
    public IEnumerator StripeSunburst_UpdatesModelAndExistingPetalView()
    {
        yield return SceneManager.LoadSceneAsync("MainGame", LoadSceneMode.Single);

        Type spriteLoaderType = FindType("DefaultNamespace.SpriteLoader");
        yield return WaitForLoadedAtlases(spriteLoaderType);

        Type tileType = FindType("DefaultNamespace.Tile");
        Type normalTileType = FindType("DefaultNamespace.NormalTile");
        Type petalType = FindType("Petal");
        Type petalTypeEnum = FindType("DefaultNamespace.PetalType");
        Type skillTypeEnum = FindType("DefaultNamespace.SpecialSkillType");
        Type boardLayoutType = FindType("DefaultNamespace.UI.BoardLayout");
        Type petalViewType = FindType("PetalView");
        Type petalViewManagerType = FindType("PetalViewManager");
        Type comboDataType = FindType("DefaultNamespace.UI.ComboData");
        Type skillActivationType = FindType("DefaultNamespace.UI.SkillActivation");
        Type skillManagerType = FindType("Skills.SkillManager");

        object rose = Enum.Parse(petalTypeEnum, "Rose");
        object noSkill = Enum.Parse(skillTypeEnum, "None");
        object horizontalStripe = Enum.Parse(skillTypeEnum, "StripedHorizontal");
        object stripeSunburst = Enum.Parse(skillTypeEnum, "StripeSunburst");

        object initialPetal = Activator.CreateInstance(petalType, rose, noSkill);
        object tile = Activator.CreateInstance(normalTileType);
        tileType.GetProperty("Petal").SetValue(tile, initialPetal);

        Array grid = Array.CreateInstance(tileType, 1, 1);
        grid.SetValue(tile, 0, 0);

        managerObject = new GameObject("PetalViewManager_Test");
        Component petalViewManager = managerObject.AddComponent(petalViewManagerType);

        petalViewPrefabObject = new GameObject("PetalView_TestPrefab");
        SpriteRenderer prefabRenderer = petalViewPrefabObject.AddComponent<SpriteRenderer>();
        Component petalViewPrefab = petalViewPrefabObject.AddComponent(petalViewType);
        petalViewType.GetField("spriteRenderer").SetValue(petalViewPrefab, prefabRenderer);
        petalViewManagerType
            .GetField("petalViewPrefab", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(petalViewManager, petalViewPrefab);

        object layout = Activator.CreateInstance(
            boardLayoutType,
            1f,
            Vector2.zero,
            1,
            1);

        petalViewManagerType.GetMethod("Init").Invoke(petalViewManager, new[] { grid, layout });

        Component liveView = (Component)petalViewManagerType
            .GetMethod("GetView")
            .Invoke(petalViewManager, new object[] { 0, 0 });
        SpriteRenderer liveRenderer = (SpriteRenderer)petalViewType
            .GetField("spriteRenderer")
            .GetValue(liveView);
        Sprite initialSprite = liveRenderer.sprite;
        Assert.That(initialSprite, Is.Not.Null, "The initial Rose sprite should be loaded in PlayMode.");

        object combo = Activator.CreateInstance(
            comboDataType,
            rose,
            horizontalStripe,
            Vector2Int.zero,
            Vector2Int.right);
        object selfPetal = Activator.CreateInstance(petalType, rose, stripeSunburst);
        object activation = Activator.CreateInstance(
            skillActivationType,
            Vector2Int.zero,
            stripeSunburst,
            selfPetal,
            null,
            combo,
            Vector2.zero);

        object result = skillManagerType
            .GetMethod("UseSkill", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new[] { grid, activation });
        object representation = result.GetType().GetProperty("Representation").GetValue(result);
        object changes = representation.GetType().GetProperty("Changes").GetValue(representation);

        Assert.That(((ICollection)changes).Count, Is.EqualTo(1),
            "The single Rose petal should be reported as changed.");

        object changeTask = petalViewManagerType
            .GetMethod("OnPetalsChanged")
            .Invoke(petalViewManager, new[] { changes, layout, (object)2f });
        PropertyInfo statusProperty = changeTask.GetType().GetProperty("Status");
        yield return new WaitUntil(() => statusProperty.GetValue(changeTask).ToString() != "Pending");

        object awaiter = changeTask.GetType().GetMethod("GetAwaiter").Invoke(changeTask, null);
        awaiter.GetType().GetMethod("GetResult").Invoke(awaiter, null);

        object changedPetal = tileType.GetProperty("Petal").GetValue(tile);
        object changedSkill = petalType.GetProperty("Skill").GetValue(changedPetal);

        Assert.That(changedSkill, Is.EqualTo(horizontalStripe));
        Assert.That(liveRenderer.sprite, Is.Not.Null);
        Assert.That(liveRenderer.sprite, Is.Not.SameAs(initialSprite));
        StringAssert.Contains("StripedHorizontal", liveRenderer.sprite.name);
    }

    [TearDown]
    public void TearDown()
    {
        if (managerObject != null)
            UnityEngine.Object.DestroyImmediate(managerObject);
        if (petalViewPrefabObject != null)
            UnityEngine.Object.DestroyImmediate(petalViewPrefabObject);
    }

    private static IEnumerator WaitForLoadedAtlases(Type spriteLoaderType)
    {
        const int maxFrames = 300;
        FieldInfo atlasesField = spriteLoaderType.GetField(
            "atlases",
            BindingFlags.Instance | BindingFlags.NonPublic);

        for (int frame = 0; frame < maxFrames; frame++)
        {
            MonoBehaviour loader = FindMonoBehaviour(spriteLoaderType);
            if (loader != null && ((IDictionary)atlasesField.GetValue(loader)).Count > 0)
                yield break;

            yield return null;
        }

        Assert.Fail("SpriteLoader did not load an atlas within 300 frames.");
    }

    private static MonoBehaviour FindMonoBehaviour(Type type)
    {
        foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (behaviour.GetType() == type)
                return behaviour;
        }

        return null;
    }

    private static Type FindType(string fullName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(fullName);
            if (type != null)
                return type;
        }

        throw new InvalidOperationException($"Could not find runtime type '{fullName}'.");
    }
}
