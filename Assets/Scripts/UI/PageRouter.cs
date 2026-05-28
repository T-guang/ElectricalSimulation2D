using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class PageRouter : MonoBehaviour
    {
        [SerializeField] private GameObject simulationRoot;
        [SerializeField] private GameObject blueprintRoot;
        [SerializeField] private GameObject squareRoot;
        [SerializeField] private GameObject encyclopediaRoot;
        [SerializeField] private GameObject toolsRoot;
        [SerializeField] private GameObject profileRoot;
        [SerializeField] private GameObject emptyPageRoot;
        [SerializeField] private Text emptyPageTitle;
        [SerializeField] private PageId defaultPage = PageId.Simulation;

        public PageId CurrentPage { get; private set; } = PageId.Simulation;

        public void Configure(
            GameObject simulation,
            GameObject blueprint,
            GameObject square,
            GameObject encyclopedia,
            GameObject tools,
            GameObject profile,
            GameObject emptyPage,
            Text emptyTitle)
        {
            simulationRoot = simulation;
            blueprintRoot = blueprint;
            squareRoot = square;
            encyclopediaRoot = encyclopedia;
            toolsRoot = tools;
            profileRoot = profile;
            emptyPageRoot = emptyPage;
            emptyPageTitle = emptyTitle;
        }

        private void Awake()
        {
            ShowPage(defaultPage);
        }

        public void ShowPage(PageId page)
        {
            CurrentPage = page;
            var targetRoot = GetRoot(page);

            SetInactiveIfDifferent(simulationRoot, targetRoot);
            SetInactiveIfDifferent(blueprintRoot, targetRoot);
            SetInactiveIfDifferent(squareRoot, targetRoot);
            SetInactiveIfDifferent(encyclopediaRoot, targetRoot);
            SetInactiveIfDifferent(toolsRoot, targetRoot);
            SetInactiveIfDifferent(profileRoot, targetRoot);
            SetInactiveIfDifferent(emptyPageRoot, targetRoot);

            if (targetRoot != null)
            {
                targetRoot.SetActive(true);
            }

            if (emptyPageTitle != null)
            {
                emptyPageTitle.text = GetPageTitle(page);
            }
        }

        private GameObject GetRoot(PageId page)
        {
            switch (page)
            {
                case PageId.Simulation:
                    return simulationRoot;
                case PageId.Blueprint:
                    return blueprintRoot;
                case PageId.Square:
                    return squareRoot != null ? squareRoot : emptyPageRoot;
                case PageId.Encyclopedia:
                    return encyclopediaRoot;
                case PageId.Tools:
                    return toolsRoot;
                case PageId.Profile:
                    return profileRoot != null ? profileRoot : emptyPageRoot;
                default:
                    return simulationRoot;
            }
        }

        private static void SetInactiveIfDifferent(GameObject root, GameObject activeRoot)
        {
            if (root != null && root != activeRoot)
            {
                root.SetActive(false);
            }
        }

        private static string GetPageTitle(PageId page)
        {
            switch (page)
            {
                case PageId.Simulation:
                    return "\u6a21\u62df\u7535\u8def";
                case PageId.Blueprint:
                    return "\u56fe\u7eb8\u96c6";
                case PageId.Square:
                    return "\u4eff\u771f\u5e7f\u573a";
                case PageId.Encyclopedia:
                    return "\u5143\u5668\u4ef6\u767e\u79d1";
                case PageId.Tools:
                    return "\u5e38\u7528\u5de5\u5177";
                case PageId.Profile:
                    return "\u4e2a\u4eba\u4e2d\u5fc3";
                default:
                    return string.Empty;
            }
        }
    }
}
