using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow
    {
        #region Tabs
        private Toolbar BuildTabBar()
        {
            Toolbar tabBar = new Toolbar();

            inspectorTabToggle = BuildTabToggle("Inspector", EM_StudioTab.Inspector);
            idRegistryTabToggle = BuildTabToggle("Id Registry", EM_StudioTab.IdRegistry);
            validationTabToggle = BuildTabToggle("Validation", EM_StudioTab.Validation);
            presetsTabToggle = BuildTabToggle("Presets", EM_StudioTab.Presets);

            tabBar.Add(inspectorTabToggle);
            tabBar.Add(idRegistryTabToggle);
            tabBar.Add(validationTabToggle);
            tabBar.Add(presetsTabToggle);

            return tabBar;
        }

        private VisualElement BuildTabContainer()
        {
            VisualElement container = new VisualElement();
            container.style.flexGrow = 1f;

            inspectorTabRoot = BuildInspectorTab();
            idRegistryTabRoot = BuildIdRegistryTab();
            validationTabRoot = BuildValidationTab();
            presetsTabRoot = BuildPresetsTab();

            container.Add(inspectorTabRoot);
            container.Add(idRegistryTabRoot);
            container.Add(validationTabRoot);
            container.Add(presetsTabRoot);

            UpdateTabVisibility();

            return container;
        }

        private ToolbarToggle BuildTabToggle(string label, EM_StudioTab tab)
        {
            ToolbarToggle toggle = new ToolbarToggle();
            toggle.text = label;
            toggle.value = selectedTab == tab;
            toggle.RegisterValueChangedCallback(changeEvent =>
            {
                if (!changeEvent.newValue)
                    return;

                SetActiveTab(tab);
            });

            return toggle;
        }

        private void SetActiveTab(EM_StudioTab tab)
        {
            if (selectedTab == tab)
                return;

            selectedTab = tab;
            UpdateTabToggles();
            UpdateTabVisibility();
            RefreshActiveTab();
        }

        private void UpdateTabToggles()
        {
            if (inspectorTabToggle != null)
                inspectorTabToggle.SetValueWithoutNotify(selectedTab == EM_StudioTab.Inspector);

            if (idRegistryTabToggle != null)
                idRegistryTabToggle.SetValueWithoutNotify(selectedTab == EM_StudioTab.IdRegistry);

            if (validationTabToggle != null)
                validationTabToggle.SetValueWithoutNotify(selectedTab == EM_StudioTab.Validation);

            if (presetsTabToggle != null)
                presetsTabToggle.SetValueWithoutNotify(selectedTab == EM_StudioTab.Presets);
        }
        #endregion
    }
}
