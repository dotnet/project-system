# Project Properties and Launch Profiles

This documentation details the updated Project Properties UI and associated back end, first released in Visual Studio 2022.

## Goals

1. Metadata-driven design. The existing property pages are mostly a series of custom WinForms- and/or WPF-based controls, often with custom code for accessing underlying property information. Adding a new page or even a new property is difficult and time-consuming, and properly enforcing visual consistency, theming, accessibility, and extensibility is almost impossible. With the new pages we take the approach of defining pages and properties in a declarative manner, and having the system generate the corresponding UI using metadata on the properties. Similarly, property metadata drives the retrieval and storage of property values.
2. Modern look and feel. With the UI being generated on the fly, we can enforce a consistent, modern UX across all pages--one that matches not just the VS color scheme, but the overall look and feel.
3. Component re-use. Where possible, we have sought to avoid creating a new system or component where there is an existing one that can do the same job. This avoids creating multiple code paths with the same functionality, and allows improvements in an individual component to benefit multiple features.
4. Extensibility. We need to support 3rd parties (both internal and external) adding both entirely new pages _and_ customizing the pages we provide. 
5. Support new functionality. We want to enable scenarios such as searching for properties, editing the MSBuild expressions that underpin many properties, and collecting the most commonly used properties in one area.

## Customising Project Properties

To customise or extend the properties displayed for a given project start with these HOW TO guides:

- [HOW TO: Add a new project property page](how-to-add-a-new-project-property-page.md)
- [HOW TO: Add a new Launch Profile kind](how-to-add-a-new-launch-profile-kind.md)
- [HOW TO: Extend an existing project property page](how-to-extend-a-project-property-page.md)

And then check these documents for more details:

- [Property Specification](property-specification.md)
- [Property (including visibility) Conditions](property-conditions.md)
- [Localization](localization.md)
- [Validating string property values](string-property-validation.md)
- [Property Value Interception](property-value-interception.md)

## Architecture

The Property Pages can be broken down into two high-level layers, the UI via through which the user interacts, and the back-end through which the UI communicates with the underlying project system to retrieve and update data. This separation allows the feature to work in Codespaces, where the project system is running on the server and the client contains only UI code.

Each layer is documented in more detail:

- [UI Architecture](ui-architecture.md)
- [Back-end Architecture](back-end-architecture.md)
