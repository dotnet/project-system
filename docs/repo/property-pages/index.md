# Project Property Pages

This documentation details the updated Project Properties UI and associated back end, added to Visual Studio in 2021.

**NOTE** This feature is still under development. These documents may talk about features as if they already exist, though they may not yet be available in public builds.

## Goals

1. Metadata-driven design. The existing property pages are mostly a series of custom WinForms- and/or WPF-based controls, often with custom code for accessing underlying property information. Adding a new page or even a new property is difficult and time-consuming, and properly enforcing visual consistency, theming, accessibility, and extensibility is almost impossible. With the new pages we take the approach of defining pages and properties in a declarative manner, and having the system generate the corresponding UI using metadata on the properties. Similarly, property metadata drives the retrieval and storage of property values.
2. Modern look and feel. With the UI being generated on the fly, we can enforce a consistent, modern UX across all pages--one that matches not just the VS color scheme, but the overall look and feel.
3. Component re-use. Where possible, we have sought to avoid creating a new system or component where there is an existing one that can do the same job. This avoid creating multiple code paths with the same functionality, and allows improvements in an individual component to benefit multiple features.
4. Extensibility. We need to support 3rd parties (both internal and external) adding both entirely new pages _and_ customizing the pages we provide. 
5. Support new functionality. We want to enable scenarios such as searching for properties, editing the MSBuild expressions that underpin many properties, and collecting the most commonly used properties in one area.
