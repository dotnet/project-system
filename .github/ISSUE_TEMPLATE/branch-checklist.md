---
name: Branch checklist
about: An outline of the steps needed to create a new branch (internal use)
title: Create [BRANCH NAME] branch checklist
labels: Area-Infrastructure
assignees: ''

---

_Descriptions of these steps can be found in the team OneNote._

<!-- Replace all ❓ characters as you work through this. -->

- [ ] Identify base commit for new branch
  - [ ] According to the [schedule](https://dev.azure.com/devdiv/DevDiv/_wiki/wikis/DevDiv.wiki/10097/Dev17-Release), `main` snapped to `rel/d17.❓` at ❓ PST
  - [ ] Last [completed Project System insertion PR](https://dev.azure.com/devdiv/DevDiv/_git/VS/pullrequests?_a=completed&assignedTo=6e89082d-fdd2-4442-a310-051df5bdc73c): https://dev.azure.com/devdiv/DevDiv/_git/VS/pullrequest/❓
  - [ ] Last inserted GitHub PR: https://github.com/dotnet/project-system/pull/❓
  - [ ] Merge commit of that PR: ❓
- [ ] Create branch on GitHub at that commit
  - [ ] https://github.com/dotnet/project-system/tree/dev17.❓.x
- [ ] For branches not matching `dev*` or `feature/*` (usually we skip these)
  - [ ] Update the [pull-request.yml](https://github.com/dotnet/project-system/blob/main/eng/pipelines/pull-request.yml) (via `pr`) to support PR builds
  - [ ] Update the [official.yml](https://github.com/dotnet/project-system/blob/main/eng/pipelines/official.yml) (via `trigger`) to have signed builds the new branch
- [ ] Update [version.json](https://github.com/dotnet/project-system/blob/main/version.json) (via `"version"`) to match the version of VS, if needed
  - [ ] In new branch: https://github.com/dotnet/project-system/blob/dev17.❓.x/version.json
  - [ ] In `main`: https://github.com/dotnet/project-system/blob/main/version.json
- [ ] In the new branch, update the "Build VS Bootstrapper" task in [build-official-release.yml](https://github.com/dotnet/project-system/blob/main/eng/pipelines/templates/build-official-release.yml) and set `channelName` to match the VS insertion branch.
  - E.g., for VS `main` the `channelName` is "int.main"; for `rel/d17.8` it would be "int.d17.8"; etc.
- [ ] To run manual insertions, use the [DotNet-Project-System pipeline](https://devdiv.visualstudio.com/DevDiv/_build?definitionId=9675&_a=summary). Enter the GitHub branch as _Branch/tag_, the VS branch as _VS Insertion Branch Name_, and check _Create VS Insertion PR_.
- [ ] Update [Policy Service milestone tracking](https://github.com/dotnet/project-system/blob/main/.github/policies)
