﻿// Copyright (c) MudBlazor 2021
// MudBlazor licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor.Utilities;

namespace MudBlazor
{
    public partial class MudPageContentNavigation : IAsyncDisposable
    {
        private List<MudPageContentSection> _sections = new();
        private IScrollSpy _scrollSpy;

        [Inject] IScrollSpyFactory ScrollSpyFactory { get; set; }

        /// <summary>
        /// The displayed section within the MudPageContentNavigation
        /// </summary>
        public IEnumerable<MudPageContentSection> Sections => _sections.AsEnumerable();

        /// <summary>
        /// The currently active session. null if there is no section selected
        /// </summary>
        public MudPageContentSection ActiveSection => _sections.FirstOrDefault(x => x.IsActive == true);

        /// <summary>
        /// The text displayed about the section links. Defaults to "Conents"
        /// </summary>
        [Parameter] public string Headline { get; set; } = "Contents";

        /// <summary>
        /// The css selector used to identifify the HTML elements that should be observed for viewport changes
        /// </summary>
        [Parameter] public string SectionClassSelector { get; set; } = string.Empty;

        /// <summary>
        /// If there are mutliple levels, this can specified to make a mapping between a level class likw "second-level" and the level in the hierarchy
        /// </summary>
        [Parameter] public IDictionary<string, int> HierarchyMapper { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// If there are multiple levels, this property controls they visibility of them.
        /// </summary>
        [Parameter] public ContentNavigationExpandBehaviour ExpandBehaviour { get; set; } = ContentNavigationExpandBehaviour.Always;

        /// <summary>
        /// If this option is true the first added section will become active when there is no other indication of an active session. Default value is false  
        /// </summary>
        [Parameter] public bool ActivateFirstSectionAsDefault { get; set; } = false;

        private Task OnNavLinkClick(string id)
        {
            SelectActiveSection(id);
            return _scrollSpy.ScrollToSection(id);
        }

        private void ScrollSpy_ScrollSectionSectionCentered(object sender, ScrollSectionCenteredEventArgs e) =>
             SelectActiveSection(e.Id);

        private void SelectActiveSection(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            var activelink = _sections.FirstOrDefault(x => x.Id == id);
            if (activelink == null)
            {
                return;
            }

            _sections.ToList().ForEach(item => item.Deactive());
            activelink.Activate();

            StateHasChanged();
        }

        private string GetNavLinkClass(MudPageContentSection section) => new CssBuilder("page-content-navigation-navlink")
            .AddClass("active", section.IsActive)
            .AddClass($"navigation-level-{section.Level}")
            .Build();
        
        private string GetPanelClass() => new CssBuilder("page-content-navigation").AddClass(Class).Build();

        /// <summary>
        /// Scrolls to a section based on the fragment of the uri. If there is no fragment, no scroll will occured
        /// </summary>
        /// <param name="uri">The uri containing the fragment to scroll</param>
        /// <returns>A task that completes when the viewport has scrolled</returns>
        public Task ScrollToSection(Uri uri) => _scrollSpy.ScrollToSection(uri);

        /// <summary>
        /// Add a section to the content navigation
        /// </summary>
        /// <param name="sectionName">name of the section will be displayed in the navigation</param>
        /// <param name="sectionId">id of the section. It will be appending to the current url, if the section becomes active</param>
        /// <param name="forceUpdate">If true, StateHasChanged is called, forcing a rerender of the component</param>
        public void AddSection(string sectionName, string sectionId, bool forceUpdate) => AddSection(new(sectionName, sectionId), forceUpdate);

        private Dictionary<MudPageContentSection, MudPageContentSection> _parentMapper = new();

        /// <summary>
        /// Add a section to the content navigation
        /// </summary>
        /// <param name="section">The section that needs to be added</param>
        /// <param name="forceUpdate">If true, StateHasChanged is called, forcing a rerender of the component</param>
        public void AddSection(MudPageContentSection section, bool forceUpdate)
        {
            _sections.Add(section);

            int diffRootLevel = 1_000_000;
            int counter = 0;
            foreach (var item in _sections.Where(x => x.Parent == null))
            {
                item.SetLevelStructure(counter, diffRootLevel);
                counter += diffRootLevel;
            }

            if (section.Id == _scrollSpy.CenteredSection)
            {
                section.Activate();
            }
            else if (_sections.Count == 1 && ActivateFirstSectionAsDefault == true)
            {
                section.Activate();
                _scrollSpy.SetSectionAsActive(section.Id).AndForget();
            }

            if (forceUpdate == true)
            {
                StateHasChanged();
            }
        }

        /// <summary>
        /// Rerender the component
        /// </summary>
        public void Update() => StateHasChanged();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _scrollSpy = ScrollSpyFactory.Create();
                _scrollSpy.ScrollSectionSectionCentered += ScrollSpy_ScrollSectionSectionCentered;

                if (string.IsNullOrEmpty(SectionClassSelector) == false)
                {
                    await _scrollSpy.StartSpying(SectionClassSelector);
                }

                SelectActiveSection(_scrollSpy.CenteredSection);
            }
        }

        public ValueTask DisposeAsync()
        {
            if (_scrollSpy == null) { return ValueTask.CompletedTask; }

            _scrollSpy.ScrollSectionSectionCentered -= ScrollSpy_ScrollSectionSectionCentered;
            return _scrollSpy.DisposeAsync();
        }
    }
}
