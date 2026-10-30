using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ExitCombatSetup
{
    public static string Build()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "Laboratory")
            throw new System.InvalidOperationException("Open the Laboratory scene first.");

        GameObject player = GameObject.Find("Player");
        GameObject lab = GameObject.Find("Refined Laboratory");
        GameObject canvas = GameObject.Find("GameCanvas");
        if (player == null || lab == null || canvas == null)
            throw new System.InvalidOperationException("Laboratory scene is incomplete.");

        GameObject zoneObject = GameObject.Find("Exit Combat");
        if (zoneObject == null)
        {
            zoneObject = new GameObject("Exit Combat");
            zoneObject.transform.SetParent(lab.transform, false);
        }
        CombatArea zone = zoneObject.GetComponent<CombatArea>();
        if (zone == null) zone = zoneObject.AddComponent<CombatArea>();
        BoxCollider2D[] regions = zoneObject.GetComponents<BoxCollider2D>();
        while (regions.Length < 2)
        {
            zoneObject.AddComponent<BoxCollider2D>();
            regions = zoneObject.GetComponents<BoxCollider2D>();
        }
        regions[0].isTrigger = true;
        regions[0].offset = new Vector2(56.5f, 10f);
        regions[0].size = new Vector2(17f, 3.5f);
        regions[1].isTrigger = true;
        regions[1].offset = new Vector2(64.5f, 20.5f);
        regions[1].size = new Vector2(3f, 21f);
        var areaData = new SerializedObject(zone);
        var areaRegions = areaData.FindProperty("regions");
        areaRegions.arraySize = 2;
        for (int i = 0; i < 2; i++)
            areaRegions.GetArrayElementAtIndex(i).objectReferenceValue = regions[i];
        areaData.ApplyModifiedPropertiesWithoutUndo();

        GameObject ward = GameObject.Find("Ward Return");
        if (ward == null)
        {
            ward = new GameObject("Ward Return");
            ward.transform.SetParent(lab.transform, false);
        }
        ward.transform.position = new Vector3(10f, 4f, -3f);

        PlayerCondition condition = player.GetComponent<PlayerCondition>();
        if (condition == null) condition = player.AddComponent<PlayerCondition>();
        var conditionData = new SerializedObject(condition);
        conditionData.FindProperty("combatZone").objectReferenceValue = zone;
        conditionData.FindProperty("wardReturn").objectReferenceValue = ward.transform;
        conditionData.ApplyModifiedPropertiesWithoutUndo();

        int guards = 0;
        foreach (NpcPatrol patrol in lab.GetComponentsInChildren<NpcPatrol>(true))
        {
            if (!patrol.name.StartsWith("Guard ")) continue;
            GuardCombat combat = patrol.GetComponent<GuardCombat>();
            if (combat == null) combat = patrol.gameObject.AddComponent<GuardCombat>();
            if (patrol.GetComponent<GuardEscort>() == null)
                patrol.gameObject.AddComponent<GuardEscort>();
            var combatData = new SerializedObject(combat);
            combatData.FindProperty("player").objectReferenceValue = condition;
            combatData.FindProperty("combatZone").objectReferenceValue = zone;
            combatData.ApplyModifiedPropertiesWithoutUndo();
            var animationData = new SerializedObject(patrol.GetComponent<NpcAnimation>());
            animationData.FindProperty("hasAttack").boolValue = true;
            animationData.ApplyModifiedPropertiesWithoutUndo();
            guards++;
        }

        RectTransform hud = canvas.transform.Find("EnergyHUD") as RectTransform;
        if (hud == null) throw new System.InvalidOperationException("EnergyHUD is missing.");
        hud.sizeDelta = new Vector2(324f, 190f);
        hud.anchoredPosition = new Vector2(182f, -115f);
        (hud.Find("HUDBackground") as RectTransform).sizeDelta = hud.sizeDelta;

        TextMeshProUGUI foodText = hud.Find("FoodText").GetComponent<TextMeshProUGUI>();
        Image foodFrame = hud.Find("FoodFillFrame").GetComponent<Image>();
        Image foodFill = hud.Find("FoodFill").GetComponent<Image>();
        TextMeshProUGUI healthText = Clone(foodText, hud, "HealthText");
        Image healthFrame = Clone(foodFrame, hud, "HealthFillFrame");
        Image healthFill = Clone(foodFill, hud, "HealthFill");

        healthText.text = "ЗДОРОВЬЕ  100 / 100";
        healthText.rectTransform.anchoredPosition = new Vector2(0f, 72f);
        healthFrame.rectTransform.anchoredPosition = new Vector2(0f, 47f);
        healthFill.rectTransform.anchoredPosition = new Vector2(0f, 47f);
        healthFill.color = new Color32(194, 94, 100, 255);
        healthFill.fillAmount = 1f;

        foodText.rectTransform.anchoredPosition = new Vector2(0f, 15f);
        foodFrame.rectTransform.anchoredPosition = new Vector2(0f, -10f);
        foodFill.rectTransform.anchoredPosition = new Vector2(0f, -10f);
        (hud.Find("MagicText") as RectTransform).anchoredPosition = new Vector2(0f, -40f);
        (hud.Find("MagicFillFrame") as RectTransform).anchoredPosition = new Vector2(0f, -65f);
        (hud.Find("MagicFill") as RectTransform).anchoredPosition = new Vector2(0f, -65f);

        var uiData = new SerializedObject(canvas.GetComponent<GameInterface>());
        uiData.FindProperty("condition").objectReferenceValue = condition;
        uiData.FindProperty("healthFill").objectReferenceValue = healthFill;
        uiData.FindProperty("healthText").objectReferenceValue = healthText;
        uiData.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "combat regions=2, guards=" + guards + ", health HUD=ready, scene saved";
    }

    private static T Clone<T>(T source, Transform parent, string name) where T : Component
    {
        Transform old = parent.Find(name);
        if (old != null) return old.GetComponent<T>();
        T copy = Object.Instantiate(source, parent);
        copy.name = name;
        return copy;
    }
}
