using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class TopNavigationController : MonoBehaviour
    {
        [SerializeField] private PageRouter pageRouter;
        [SerializeField] private List<Button> tabButtons = new List<Button>();
        [SerializeField] private List<Text> tabLabels = new List<Text>();
        [SerializeField] private GameObject simulationRoot;
        [SerializeField] private GameObject blueprintRoot;
        [SerializeField] private GameObject encyclopediaRoot;
        [SerializeField] private GameObject toolsRoot;
        [SerializeField] private GameObject emptyPageRoot;
        [SerializeField] private Text emptyPageTitle;

        private void Awake()
        {
            if (pageRouter == null)
            {
                pageRouter = FindObjectOfType<PageRouter>();
            }

            if (pageRouter == null)
            {
                pageRouter = gameObject.AddComponent<PageRouter>();
            }

            pageRouter.Configure(
                simulationRoot,
                blueprintRoot,
                null,
                encyclopediaRoot,
                toolsRoot,
                null,
                emptyPageRoot,
                emptyPageTitle);

            for (var i = 0; i < tabButtons.Count; i++)
            {
                var index = i;
                tabButtons[i].onClick.AddListener(() => SelectTab(index));
            }

            SelectTab(0);
        }

        public void SelectTab(int index)
        {
            var page = ToPageId(index);
            if (pageRouter != null)
            {
                pageRouter.ShowPage(page);
            }

            RefreshTabStates(index);
        }

        private void RefreshTabStates(int activeIndex)
        {
            for (var i = 0; i < tabButtons.Count; i++)
            {
                var active = i == activeIndex;
                var image = tabButtons[i].GetComponent<Image>();
                if (image != null)
                {
                    image.color = active ? new Color(0.89f, 0.94f, 1f) : Color.white;
                }

                if (i < tabLabels.Count && tabLabels[i] != null)
                {
                    tabLabels[i].color = active ? new Color(0.06f, 0.38f, 0.95f) : new Color(0.05f, 0.08f, 0.14f);
                    tabLabels[i].fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                }
            }
        }

        private static PageId ToPageId(int index)
        {
            switch (index)
            {
                case 0:
                    return PageId.Simulation;
                case 1:
                    return PageId.Blueprint;
                case 2:
                    return PageId.Square;
                case 3:
                    return PageId.Encyclopedia;
                case 4:
                    return PageId.Tools;
                case 5:
                    return PageId.Profile;
                default:
                    return PageId.Simulation;
            }
        }
    }
}
